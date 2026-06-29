using OpenCvSharp;
using OmrSheet.Models;

namespace OmrSheet.Services
{
    public class OMRService
    {

        public Dictionary<int, string> ProcessOMR(string filePath, TemplateConfig config)
        {
            Mat image = Cv2.ImRead(filePath);

            if (image.Empty())
            {
                Console.WriteLine($"ERROR: Cannot read image {filePath}");
                return new Dictionary<int, string>();
            }

            int imgH = image.Rows;
            int imgW = image.Cols;

            Console.WriteLine($"Image Loaded: {imgW} x {imgH}");

            // ============================================================
            // PREPROCESSING
            // ============================================================

            Mat gray = new Mat();
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

            Cv2.GaussianBlur(gray, gray, new Size(3, 3), 0);

            Mat binary = new Mat();
            Cv2.Threshold(
                gray,
                binary,
                0,
                255,
                ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

            Dictionary<int, string> answers = new();

            Mat debug = image.Clone();

            // ============================================================
            // PROCESS QUESTIONS
            // ============================================================

            for (int q = 1; q <= config.TotalQuestions; q++)
            {
                int colIdx = (q - 1) / config.QuestionsPerColumn;
                int rowIdx = (q - 1) % config.QuestionsPerColumn;

                // Safely get column X positions (fallback to last if misconfigured)
                double[] xCols = colIdx < config.ColumnsX.Count 
                    ? config.ColumnsX[colIdx] 
                    : config.ColumnsX.Last();

                int cy = (int)(config.RowY[rowIdx] * imgH);

                double[] fillRatios = new double[4];

                for (int option = 0; option < 4; option++)
                {
                    int cx = (int)(xCols[option] * imgW);

                    int rx = (int)(config.RadiusX * imgW);
                    int ry = (int)(config.RadiusY * imgH);

                    int x1 = Math.Max(0, cx - rx);
                    int y1 = Math.Max(0, cy - ry);
                    int x2 = Math.Min(imgW, cx + rx);
                    int y2 = Math.Min(imgH, cy + ry);

                    Rect roi = new Rect(
                        x1,
                        y1,
                        x2 - x1,
                        y2 - y1);

                    using Mat bubble = new Mat(binary, roi);

                    // ====================================================
                    // Elliptical mask for better bubble detection
                    // ====================================================

                    using Mat mask = Mat.Zeros(
                        roi.Height,
                        roi.Width,
                        MatType.CV_8UC1);

                    Cv2.Ellipse(
                        mask,
                        new Point(mask.Width / 2, mask.Height / 2),
                        new Size(
                            Math.Max(1, mask.Width / 2 - 2),
                            Math.Max(1, mask.Height / 2 - 2)),
                        0,
                        0,
                        360,
                        Scalar.White,
                        -1);

                    double fill =
                        Cv2.Mean(bubble, mask).Val0 / 255.0;

                    fillRatios[option] = fill;

                    Console.WriteLine(
                        $"Q{q} {config.Options[option]} = {fill * 100:F1}%");

                    Cv2.Rectangle(
                        debug,
                        roi,
                        Scalar.Blue,
                        1);
                }

                // ========================================================
                // FIND BEST OPTION
                // ========================================================

                var ranked = fillRatios
                    .Select((value, index) => new
                    {
                        Value = value,
                        Index = index
                    })
                    .OrderByDescending(x => x.Value)
                    .ToArray();

                double best = ranked[0].Value;
                int bestIdx = ranked[0].Index;

                double second = ranked[1].Value;
                double margin = best - second;

                string selected = "";

                if (best >= config.MinFillRatio &&
                    margin >= config.MinMarginRatio)
                {
                    selected = config.Options[bestIdx];

                    Console.WriteLine(
                        $"Q{q} -> {selected} " +
                        $"(fill={best:P1}, margin={margin:P1})");

                    int cx = (int)(xCols[bestIdx] * imgW);

                    int rx = (int)(config.RadiusX * imgW);
                    int ry = (int)(config.RadiusY * imgH);

                    Rect selectedRect = new Rect(
                        Math.Max(0, cx - rx),
                        Math.Max(0, cy - ry),
                        rx * 2,
                        ry * 2);

                    Cv2.Rectangle(
                        debug,
                        selectedRect,
                        Scalar.Green,
                        2);
                }
                else if (best >= config.MinFillRatio)
                {
                    selected = config.Options[bestIdx];

                    Console.WriteLine(
                        $"Q{q} AMBIGUOUS: " +
                        $"{config.Options[bestIdx]}={best:P1} vs " +
                        $"{config.Options[ranked[1].Index]}={second:P1}");

                    int cx = (int)(xCols[bestIdx] * imgW);

                    int rx = (int)(config.RadiusX * imgW);
                    int ry = (int)(config.RadiusY * imgH);

                    Rect selectedRect = new Rect(
                        Math.Max(0, cx - rx),
                        Math.Max(0, cy - ry),
                        rx * 2,
                        ry * 2);

                    Cv2.Rectangle(
                        debug,
                        selectedRect,
                        Scalar.Yellow,
                        2);
                }
                else
                {
                    Console.WriteLine(
                        $"Q{q} -> UNATTEMPTED");
                }

                answers[q] = selected;
            }

            // ============================================================
            // SAVE DEBUG IMAGES
            // ============================================================

            string debugFolder = "DebugImages";

            if (!Directory.Exists(debugFolder))
                Directory.CreateDirectory(debugFolder);

            Cv2.ImWrite(
                Path.Combine(debugFolder, "gray.jpg"),
                gray);

            Cv2.ImWrite(
                Path.Combine(debugFolder, "binary.jpg"),
                binary);

            Cv2.ImWrite(
                Path.Combine(debugFolder, "debug.jpg"),
                debug);

            Console.WriteLine(
                $"Debug images saved to '{debugFolder}'");

            return answers;
        }

        // ============================================================
        // MARK CALCULATION
        // ============================================================

        public int CalculateMarks(
            Dictionary<int, string> answerKey,
            Dictionary<int, string> studentAnswers,
            int correctMark,
            int incorrectMark,
            int unattemptedMark)
        {
            int totalMarks = 0;

            foreach (var kvp in answerKey)
            {
                int qNum = kvp.Key;
                string correctAnswer = kvp.Value;

                string studentAnswer =
                    studentAnswers.ContainsKey(qNum)
                        ? studentAnswers[qNum]
                        : "";

                if (studentAnswer == correctAnswer)
                {
                    totalMarks += correctMark;
                }
                else if (string.IsNullOrEmpty(studentAnswer))
                {
                    totalMarks += unattemptedMark;
                }
                else
                {
                    totalMarks += incorrectMark; // Note: if incorrect mark is a penalty, it should be negative
                }
            }

            Console.WriteLine(
                $"TOTAL MARKS = {totalMarks}");

            return totalMarks;
        }
    }
}
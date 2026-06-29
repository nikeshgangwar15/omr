using System;
using System.Collections.Generic;
using System.Linq;

namespace OmrSheet.Models
{
    public class TemplateConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int TotalQuestions { get; set; } = 20;
        public int QuestionsPerColumn { get; set; } = 10;
        public double[] RowY { get; set; }
        public List<double[]> ColumnsX { get; set; } = new List<double[]>();
        public string[] Options { get; set; }
        public double RadiusX { get; set; }
        public double RadiusY { get; set; }
        public double MinFillRatio { get; set; }
        public double MinMarginRatio { get; set; }
    }

    public static class TemplateProvider
    {
        public static List<TemplateConfig> GetTemplates()
        {
            return new List<TemplateConfig>
            {
                new TemplateConfig
                {
                    Id = "Template1",
                    Name = "Standard 20 Questions (1369x1149)",
                    TotalQuestions = 20,
                    QuestionsPerColumn = 10,
                    RowY = new double[] { 0.390, 0.446, 0.502, 0.559, 0.614, 0.670, 0.726, 0.782, 0.837, 0.893 },
                    ColumnsX = new List<double[]>
                    {
                        new double[] { 0.161, 0.244, 0.328, 0.411 },
                        new double[] { 0.668, 0.753, 0.839, 0.923 }
                    },
                    Options = new[] { "A", "B", "C", "D" },
                    RadiusX = 0.018,
                    RadiusY = 0.020,
                    MinFillRatio = 0.30,
                    MinMarginRatio = 0.10
                },
                new TemplateConfig
                {
                    Id = "Template2",
                    Name = "50 Questions (2 Columns)",
                    TotalQuestions = 50,
                    QuestionsPerColumn = 25,
                    RowY = Enumerable.Range(0, 25).Select(i => 0.140 + (i * 0.034)).ToArray(),
                    ColumnsX = new List<double[]>
                    {
                        new double[] { 0.430, 0.490, 0.550, 0.610 },
                        new double[] { 0.740, 0.800, 0.860, 0.920 }
                    },
                    Options = new[] { "A", "B", "C", "D" },
                    RadiusX = 0.015,
                    RadiusY = 0.015,
                    MinFillRatio = 0.30,
                    MinMarginRatio = 0.10
                },
                new TemplateConfig
                {
                    Id = "Template3",
                    Name = "100 Questions (4 Columns)",
                    TotalQuestions = 100,
                    QuestionsPerColumn = 25,
                    RowY = Enumerable.Range(0, 25).Select(i => 0.180 + (i * 0.031)).ToArray(),
                    ColumnsX = new List<double[]>
                    {
                        new double[] { 0.110, 0.150, 0.190, 0.230 },
                        new double[] { 0.340, 0.380, 0.420, 0.460 },
                        new double[] { 0.570, 0.610, 0.650, 0.690 },
                        new double[] { 0.800, 0.840, 0.880, 0.920 }
                    },
                    Options = new[] { "A", "B", "C", "D" },
                    RadiusX = 0.012,
                    RadiusY = 0.012,
                    MinFillRatio = 0.30,
                    MinMarginRatio = 0.10
                }
            };
        }

        public static TemplateConfig GetTemplate(string id)
        {
            return GetTemplates().FirstOrDefault(t => t.Id == id) ?? GetTemplates().First();
        }
    }
}

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
                    MinFillRatio = 0.50,
                    MinMarginRatio = 0.10
                },
                new TemplateConfig
                {
                    Id = "Template2",
                    Name = "50 Questions (2 Columns)",
                    TotalQuestions = 50,
                    QuestionsPerColumn = 25,

                    // Bubble row centers
                    RowY = Enumerable.Range(0, 25)
                        .Select(i => 0.149 + (i * 0.0335))
                        .ToArray(),

                    // A B C D
                    ColumnsX = new List<double[]>
                    {
                        new[] { 0.416, 0.488, 0.560, 0.633 },
                        new[] { 0.732, 0.804, 0.876, 0.948 }
                    },

                    Options = new[] { "A", "B", "C", "D" },

                    RadiusX = 0.017,
                    RadiusY = 0.014,

                    MinFillRatio = 0.50,
                    MinMarginRatio = 0.10
                },
               new TemplateConfig
                {
                    Id = "Template3",
                    Name = "100 Questions (4 Columns)",
                    TotalQuestions = 100,
                    QuestionsPerColumn = 25,

                    // Bubble row centers
                    RowY = Enumerable.Range(0, 25)
                        .Select(i => 0.221 + (i * 0.0312))
                        .ToArray(),

                    ColumnsX = new List<double[]>
                    {
                        new[] { 0.153, 0.192, 0.231, 0.270 }, // Q1-25
                        new[] { 0.387, 0.426, 0.465, 0.504 }, // Q26-50
                        new[] { 0.621, 0.660, 0.699, 0.738 }, // Q51-75
                        new[] { 0.856, 0.895, 0.934, 0.973 }  // Q76-100
                    },

                    Options = new[] { "A", "B", "C", "D" },

                    RadiusX = 0.013,
                    RadiusY = 0.012,

                    MinFillRatio = 0.60,
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

using System.Collections.Generic;

namespace AICodeAnalyzer.Models
{
    public class CodeAnalysisResult
    {
        public int ClassCount { get; set; }
        public int MethodCount { get; set; }
        public int PropertyCount { get; set; }
        public int UsingCount { get; set; }
        public List<string> Classes { get; set; } = new();
        public List<string> Methods { get; set; } = new();
        public List<string> UsingStatements { get; set; } = new();
    }

    public class ProjectFileAnalysis
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public CodeAnalysisResult Analysis { get; set; }
        public string QuickInsight { get; set; }
    }
}
namespace PoC1_LegacyAnalyzer_Web.Models
{
    public class ComplexityMetrics
    {
        public int CyclomaticComplexity { get; set; }
        public int CognitiveComplexity { get; set; }
        public int MaxNestingDepth { get; set; }
        public int NumberOfParameters { get; set; }
        public double MaintainabilityIndex { get; set; }
        public string ComplexityLevel { get; set; } = "Low"; // Low, Medium, High, VeryHigh
        
        // Legacy properties for backward compatibility
        public int LinesOfCode { get; set; }
        public int ClassCount { get; set; }
        public int MethodCount { get; set; }
        public int PropertyCount { get; set; }
    }
}


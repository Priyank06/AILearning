namespace PoC1_LegacyAnalyzer_Web.Models.MultiAgent
{
    /// <summary>
    /// Complexity metrics calculated using static analysis
    /// </summary>
    public class ComplexityMetrics
    {
        public int CyclomaticComplexity { get; set; }
        public int CognitiveComplexity { get; set; }
        public int MaxMethodLines { get; set; }
        public int MaxParameterCount { get; set; }
        public int NestingDepth { get; set; }
        public string ComplexityLevel { get; set; } = "Medium"; // Low, Medium, High, Very High
    }
}

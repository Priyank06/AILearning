namespace PoC1_LegacyAnalyzer_Web.Models
{
    public class BusinessAnalysisContext
    {
        public int ActualComplexityScore { get; set; }
        public string ActualRiskLevel { get; set; } = "";
        public int ActualClassCount { get; set; }
        public int ActualMethodCount { get; set; }
        public int ActualPropertyCount { get; set; }
        public int ActualUsingCount { get; set; }
        public string AnalysisType { get; set; } = "";
    }
}

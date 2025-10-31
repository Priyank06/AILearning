namespace PoC1_LegacyAnalyzer_Web.Models.MultiAgent
{
    public class RiskAssessment
    {
        
        public string OverallRisk { get; set; } = string.Empty;
        public string Likelihood { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
        public string Level { get; set; } = "";
        public List<string> RiskFactors { get; set; } = new();
        public string MitigationStrategy { get; set; } = "";
    }
}

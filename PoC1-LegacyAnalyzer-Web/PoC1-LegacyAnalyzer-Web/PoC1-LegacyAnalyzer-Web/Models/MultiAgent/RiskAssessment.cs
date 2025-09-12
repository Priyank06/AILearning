namespace PoC1_LegacyAnalyzer_Web.Models.MultiAgent
{
    public class RiskAssessment
    {
        public string Level { get; set; } = "";
        public List<string> RiskFactors { get; set; } = new();
        public string MitigationStrategy { get; set; } = "";
    }
}

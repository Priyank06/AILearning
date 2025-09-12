namespace PoC1_LegacyAnalyzer_Web.Models.ProjectAnalysis
{
    public class BusinessImpactAssessment
    {
        public decimal EstimatedValue { get; set; }
        public string RiskLevel { get; set; } = "";
        public string MaintenanceOverhead { get; set; } = "";
        public List<string> BusinessCriticalAreas { get; set; } = new();
        public string RecommendedApproach { get; set; } = "";
        public string InvestmentPriority { get; set; } = "";
    }
}

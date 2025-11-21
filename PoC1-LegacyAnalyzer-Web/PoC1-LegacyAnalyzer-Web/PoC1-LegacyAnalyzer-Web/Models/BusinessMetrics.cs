namespace PoC1_LegacyAnalyzer_Web.Models
{
    public class BusinessMetrics
    {
        public decimal EstimatedDeveloperHoursSaved { get; set; }
        public decimal AverageHourlyRate { get; set; }
        
        public decimal ProjectCostSavings { get; set; }

        public string MigrationTimeline { get; set; } = "";
        public string RiskMitigation { get; set; } = "";
        public decimal ComplianceCostAvoidance { get; set; }
        
        public decimal TotalROI { get; set; }

        public string ProjectSize { get; set; } = "";
        public string RecommendedApproach { get; set; } = "";

        // Helper method to calculate values
        public void CalculateValues()
        {
            ProjectCostSavings = EstimatedDeveloperHoursSaved * AverageHourlyRate;
            TotalROI = ProjectCostSavings + ComplianceCostAvoidance;
        }
    }
}

using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Helpers
{
    /// <summary>
    /// Business metrics and calculations for project analysis
    /// Extracted from MultiFile.razor to improve maintainability and testability
    /// </summary>
    public static class BusinessCalculations
    {
        /// <summary>
        /// Calculate estimated manual analysis hours based on code metrics
        /// </summary>
        public static int CalculateManualAnalysisHours(MultiFileAnalysisResult result)
        {
            var baseHours = result.TotalClasses * 2; // 2 hours per class
            var methodHours = result.TotalMethods * 0.25; // 15 minutes per method
            var dependencyHours = result.TotalUsingStatements * 0.5; // 30 minutes per dependency

            return (int)(baseHours + methodHours + dependencyHours);
        }

        /// <summary>
        /// Calculate business metrics for the analyzed project
        /// </summary>
        public static BusinessMetrics CalculateBusinessMetrics(
            MultiFileAnalysisResult result,
            decimal averageHourlyRate = 125m)
        {
            return new BusinessMetrics
            {
                EstimatedDeveloperHoursSaved = result.TotalMethods * 0.5m * ((result.OverallComplexityScore / 100m) + 0.5m),
                AverageHourlyRate = averageHourlyRate,
                MigrationTimeline = result.OverallComplexityScore switch
                {
                    < 30 => "2-4 weeks",
                    < 50 => "4-8 weeks",
                    < 70 => "8-12 weeks",
                    _ => "12+ weeks"
                },
                RiskMitigation = $"{result.OverallRiskLevel} risk level with appropriate mitigation strategy",
                ComplianceCostAvoidance = result.OverallRiskLevel switch
                {
                    "HIGH" => 15000m,
                    "MEDIUM" => 8000m,
                    "LOW" => 3000m,
                    _ => 1000m
                },
                ProjectSize = result.TotalFiles switch
                {
                    < 5 => "Small Project",
                    < 15 => "Medium Project",
                    < 30 => "Large Project",
                    _ => "Enterprise Project"
                },
                RecommendedApproach = result.OverallComplexityScore switch
                {
                    < 30 => "Standard development practices",
                    < 50 => "Structured approach with experienced team",
                    < 70 => "Phased migration with risk mitigation",
                    _ => "Enterprise methodology with dedicated team"
                }
            };
        }

        /// <summary>
        /// Calculate project cost savings
        /// </summary>
        public static decimal CalculateCostSavings(MultiFileAnalysisResult result, decimal hourlyRate = 125m)
        {
            var manualHours = CalculateManualAnalysisHours(result);
            return manualHours * hourlyRate;
        }

        /// <summary>
        /// Calculate total ROI
        /// </summary>
        public static decimal CalculateTotalROI(BusinessMetrics metrics)
        {
            return metrics.ProjectCostSavings + metrics.ComplianceCostAvoidance;
        }
    }
}

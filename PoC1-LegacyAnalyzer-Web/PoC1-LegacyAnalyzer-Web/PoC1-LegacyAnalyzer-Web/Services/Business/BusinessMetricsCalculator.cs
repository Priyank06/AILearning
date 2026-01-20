using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Options;

namespace PoC1_LegacyAnalyzer_Web.Services.Business
{
    public class BusinessMetricsCalculator : IBusinessMetricsCalculator
    {
        private readonly ILogger<BusinessMetricsCalculator> _logger;
        private readonly BusinessCalculationRules _businessRules;

        public BusinessMetricsCalculator(
            ILogger<BusinessMetricsCalculator> logger,
            IOptions<BusinessCalculationRules> businessRulesOptions)
        {
            _logger = logger;
            _businessRules = businessRulesOptions.Value ?? new BusinessCalculationRules();
        }

        public BusinessMetrics CalculateBusinessMetrics(MultiFileAnalysisResult result)
        {
            // Use configuration for business metrics calculation
            var metricsConfig = _businessRules.AnalysisLimits.BusinessMetrics;
            var baseHours = result.TotalMethods * metricsConfig.BaseHoursPerMethod;
            var complexityMultiplier = (result.OverallComplexityScore / 100m) + metricsConfig.ComplexityMultiplierBase;
            var savedHours = baseHours * complexityMultiplier;

            // Compliance cost avoidance based on risk level from configuration
            var complianceConfig = _businessRules.ComplianceCost;
            var riskLevel = result.OverallRiskLevel ?? "DEFAULT";
            var complianceAvoidance = complianceConfig.CostAvoidanceByRiskLevel.TryGetValue(riskLevel, out var cost)
                ? cost
                : complianceConfig.CostAvoidanceByRiskLevel.GetValueOrDefault("DEFAULT", 1000m);

            var hourlyRate = _businessRules.CostCalculation.DefaultDeveloperHourlyRate;
            var metrics = new BusinessMetrics
            {
                EstimatedDeveloperHoursSaved = savedHours,
                AverageHourlyRate = hourlyRate,
                MigrationTimeline = GetMigrationTimeline(result.OverallComplexityScore),
                RiskMitigation = $"{result.OverallRiskLevel} risk level - {GetRiskMitigationStrategy(result.OverallRiskLevel)}",
                ComplianceCostAvoidance = complianceAvoidance,
                ProjectCostSavings = savedHours * hourlyRate,
                TotalROI = (savedHours * hourlyRate) + complianceAvoidance,
                ProjectSize = GetProjectSizeAssessment(result.TotalFiles, result.TotalClasses),
                RecommendedApproach = GetRecommendedApproach(result.OverallComplexityScore)
            };

            // Calculate computed values
            metrics.CalculateValues();

            return metrics;
        }

        private string GetMigrationTimeline(int complexityScore)
        {
            var thresholds = _businessRules.ComplexityThresholds;
            var timeline = _businessRules.TimelineEstimation;

            if (complexityScore < thresholds.Low)
                return timeline.ContainsKey("VeryLow") ? timeline["VeryLow"] : "2-4 weeks";
            if (complexityScore < thresholds.Medium)
                return timeline.ContainsKey("Low") ? timeline["Low"] : "4-8 weeks";
            if (complexityScore < thresholds.High)
                return timeline.ContainsKey("Medium") ? timeline["Medium"] : "8-12 weeks";
            return timeline.ContainsKey("High") ? timeline["High"] : "12+ weeks";
        }

        private string GetProjectSizeAssessment(int fileCount, int classCount)
        {
            foreach (var kvp in _businessRules.ProjectSizeClassification)
            {
                var config = kvp.Value;
                if (config.MaxFiles.HasValue && config.MaxClasses.HasValue)
                {
                    if (fileCount < config.MaxFiles.Value && classCount < config.MaxClasses.Value)
                        return config.Label;
                }
                else if (!config.MaxFiles.HasValue && !config.MaxClasses.HasValue)
                {
                    // Enterprise fallback
                    return config.Label;
                }
            }
            // If no match, fallback to "Enterprise Project"
            return _businessRules.ProjectSizeClassification.ContainsKey("Enterprise")
                ? _businessRules.ProjectSizeClassification["Enterprise"].Label
                : "Enterprise Project";
        }

        private string GetRiskMitigationStrategy(string riskLevel)
        {
            return riskLevel switch
            {
                "HIGH" => "Dedicated migration team with senior architect oversight required",
                "MEDIUM" => "Experienced development team with structured approach recommended",
                "LOW" => "Standard development practices with code review sufficient",
                _ => "Assessment in progress"
            };
        }

        private string GetRecommendedApproach(int complexityScore)
        {
            var thresholds = _businessRules.ComplexityThresholds;
            return complexityScore switch
            {
                var score when score < thresholds.Low => "Agile development with standard practices",
                var score when score < thresholds.Medium => "Structured approach with experienced team",
                var score when score < thresholds.VeryHigh => "Phased migration with risk mitigation",
                _ => "Enterprise methodology with dedicated team"
            };
        }
    }
}


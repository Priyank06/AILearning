using PoC1_LegacyAnalyzer_Web.Models.ProjectAnalysis;
using Microsoft.Extensions.Options;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.Business
{
    public class BusinessImpactCalculator : IBusinessImpactCalculator
    {
        private readonly ILogger<BusinessImpactCalculator> _logger;
        private readonly AgentConfiguration _agentConfig;
        private readonly BusinessCalculationRules _businessRules;

        public BusinessImpactCalculator(
            ILogger<BusinessImpactCalculator> logger,
            IOptions<AgentConfiguration> agentOptions,
            IOptions<BusinessCalculationRules> businessRulesOptions)
        {
            _logger = logger;
            _agentConfig = agentOptions.Value ?? new AgentConfiguration();
            _businessRules = businessRulesOptions.Value ?? new BusinessCalculationRules();
        }

        public async Task<BusinessImpactAssessment> AssessBusinessImpactAsync(
            ProjectAnalysisResult result,
            CancellationToken cancellationToken = default)
        {
            var assessment = new BusinessImpactAssessment();

            var totalFiles = result.DetailedFileAnalysis.Count;
            var totalComplexity = result.DetailedFileAnalysis.Sum(f => f.ComplexityScore);
            var avgComplexity = totalFiles > 0 ? totalComplexity / totalFiles : 0;

            // Calculate estimated value based on project size and complexity
            assessment.EstimatedValue = CalculateProjectValue(result.ProjectInfo, avgComplexity);
            assessment.RiskLevel = DetermineBusinessRiskLevel(avgComplexity, result.Architecture.ArchitecturalDebtScore);
            assessment.MaintenanceOverhead = AssessMaintenanceOverhead(result);
            assessment.BusinessCriticalAreas = IdentifyBusinessCriticalAreas(result.FolderAnalysis);
            assessment.RecommendedApproach = DetermineRecommendedApproach(assessment.RiskLevel, totalFiles);
            assessment.InvestmentPriority = DetermineInvestmentPriority(assessment);

            return await Task.FromResult(assessment);
        }

        private decimal CalculateProjectValue(ProjectMetadata projectInfo, int avgComplexity)
        {
            // Base value on lines of code and complexity
            var baseValue = (decimal)projectInfo.TotalLines * _businessRules.CostCalculation.BaseValuePerLine;
            var complexityMultiplier = (avgComplexity / 100m) + _businessRules.CostCalculation.ComplexityMultiplierBase;
            return Math.Min(_businessRules.CostCalculation.MaxEstimatedValue, baseValue * complexityMultiplier);
        }

        private string DetermineBusinessRiskLevel(int avgComplexity, int architecturalDebt)
        {
            // Use configuration-based risk mapping if available
            if (_agentConfig?.BusinessImpactRules?.RiskLevelMapping != null)
            {
                var description = $"Complexity: {avgComplexity}, Debt: {architecturalDebt}";
                foreach (var mapping in _agentConfig.BusinessImpactRules.RiskLevelMapping)
                {
                    if (description.Contains(mapping.Pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        return mapping.RiskLevel;
                    }
                }
            }
            // Fallback logic
            var riskScore = (avgComplexity + architecturalDebt) / 2;
            return riskScore switch
            {
                var low when low < _businessRules.RiskThresholds.LowRiskMax => "LOW",
                var med when med < _businessRules.RiskThresholds.MediumRiskMax => "MEDIUM",
                _ => "HIGH"
            };
        }

        private string AssessMaintenanceOverhead(ProjectAnalysisResult result)
        {
            var overhead = result.Architecture.ArchitecturalDebtScore switch
            {
                var low when low < _businessRules.ComplexityThresholds.Low => "Low maintenance overhead with good architectural practices",
                var high when high < _businessRules.ComplexityThresholds.High => "Moderate maintenance overhead requiring structured approach",
                _ => "High maintenance overhead with significant refactoring needs"
            };
            return overhead;
        }

        private List<string> IdentifyBusinessCriticalAreas(Dictionary<string, FolderAnalysisResult> folderAnalysis)
        {
            return folderAnalysis.Values
                .Where(f => f.ComplexityScore > _businessRules.ComplexityThresholds.High || f.ArchitecturalRole.Contains("Business"))
                .Select(f => f.FolderName)
                .Take(5)
                .ToList();
        }

        private string DetermineRecommendedApproach(string riskLevel, int totalFiles)
        {
            return (riskLevel, totalFiles) switch
            {
                ("LOW", var veryLow) when veryLow < _businessRules.ComplexityThresholds.VeryLow => "Standard agile development with code review practices",
                ("LOW", _) => "Structured development with architectural guidance",
                ("MEDIUM", _) => "Phased modernization with risk mitigation strategies",
                ("HIGH", _) => "Comprehensive modernization program with dedicated architecture team",
                _ => throw new NotImplementedException()
            };
        }

        private string DetermineInvestmentPriority(BusinessImpactAssessment assessment)
        {
            // Use configuration-based investment priority rules if available
            if (_agentConfig?.BusinessImpactRules?.InvestmentPriorityRules != null)
            {
                foreach (var rule in _agentConfig.BusinessImpactRules.InvestmentPriorityRules)
                {
                    // Only support equality for now
                    if (rule.Condition.Contains("riskLevel =="))
                    {
                        var expected = rule.Condition.Split("==")[1].Trim(' ', '\'', '"');
                        if (assessment.RiskLevel.Equals(expected, StringComparison.OrdinalIgnoreCase))
                        {
                            return rule.Action;
                        }
                    }
                }
            }
            // Fallback logic
            var fallbackConfig = _businessRules.AnalysisLimits.InvestmentPriorityFallback;
            return (assessment.RiskLevel, assessment.EstimatedValue) switch
            {
                ("HIGH", var value) when value > fallbackConfig.HighRiskHighValueThreshold => "CRITICAL - Immediate executive attention required",
                ("HIGH", _) => "HIGH - Significant business risk requiring prompt action",
                ("MEDIUM", var value) when value > fallbackConfig.MediumRiskHighValueThreshold => "MEDIUM - Strategic investment opportunity",
                _ => "LOW - Include in regular development planning cycle"
            };
        }
    }
}


using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Options;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class RecommendationGeneratorService : IRecommendationGeneratorService
    {
        private readonly ILogger<RecommendationGeneratorService> _logger;
        private readonly ComplexityThresholdsConfig _complexityThresholds;
        private readonly ScaleThresholdsConfig _scaleThresholds;
        private readonly FileAnalysisLimitsConfig _fileLimits;
        private readonly BusinessCalculationRules _businessRules;

        public RecommendationGeneratorService(
            ILogger<RecommendationGeneratorService> logger,
            IOptions<ComplexityThresholdsConfig> complexityOptions,
            IOptions<ScaleThresholdsConfig> scaleThresholdOptions,
            IOptions<FileAnalysisLimitsConfig> fileLimitOptions,
            IOptions<BusinessCalculationRules> businessRulesOptions)
        {
            _logger = logger;
            _complexityThresholds = complexityOptions.Value ?? new ComplexityThresholdsConfig();
            _scaleThresholds = scaleThresholdOptions.Value ?? new ScaleThresholdsConfig();
            _fileLimits = fileLimitOptions.Value ?? new FileAnalysisLimitsConfig();
            _businessRules = businessRulesOptions.Value ?? new BusinessCalculationRules();
        }

        public List<string> GenerateStrategicRecommendations(MultiFileAnalysisResult result, string analysisType)
        {
            var recommendations = new List<string>();

            // Risk-based recommendations
            if (result.OverallComplexityScore > _complexityThresholds.VeryHigh)
            {
                recommendations.Add("High complexity project requires dedicated migration team with senior architect oversight");
                recommendations.Add("Implement phased migration approach to minimize business disruption and technical risk");
                recommendations.Add("Establish comprehensive testing strategy before initiating modernization activities");
            }
            else if (result.OverallComplexityScore > _complexityThresholds.Critical)
            {
                recommendations.Add("Moderate complexity project suitable for experienced development team");
                recommendations.Add("Plan structured migration timeline with 6-10 week implementation window");
                recommendations.Add("Implement code quality gates and automated testing during modernization");
            }
            else
            {
                recommendations.Add("Low complexity project appropriate for standard development practices");
                recommendations.Add("Excellent candidate for junior developer skill development and mentoring");
                recommendations.Add("Consider as pilot project for establishing modernization best practices");
            }

            // Scale-based recommendations
            if (result.TotalFiles > _scaleThresholds.LargeCodebaseFileCount)
            {
                recommendations.Add("Large codebase requires automated testing and continuous integration before migration");
                recommendations.Add("Implement code analysis tools and quality metrics tracking throughout modernization");
            }

            // Architecture-based recommendations
            var methodsPerClass = result.TotalClasses > 0 ? (double)result.TotalMethods / result.TotalClasses : 0;
            if (methodsPerClass > _scaleThresholds.HighMethodsPerClass)
            {
                recommendations.Add("High method-to-class ratio indicates potential architectural refactoring opportunities");
            }

            // Analysis-specific recommendations
            if (analysisType == "security")
            {
                recommendations.Add("Implement security code review process with focus on input validation and authentication");
            }
            else if (analysisType == "performance")
            {
                recommendations.Add("Establish performance baselines and monitoring before optimization activities");
            }

            return recommendations.Take(_fileLimits.MaxRecommendations).ToList();
        }

        public string GenerateExecutiveAssessment(MultiFileAnalysisResult result, string analysisType)
        {
            var assessment = $"Comprehensive {analysisType} analysis of {result.TotalFiles}-file enterprise project indicates {result.OverallRiskLevel.ToLower()} modernization complexity. ";

            assessment += analysisType switch
            {
                "security" => $"Security assessment identifies {result.FileResults.Count(f => f.ComplexityScore > _scaleThresholds.HighRiskComplexityScore)} files requiring immediate security review and remediation.",
                "performance" => $"Performance analysis reveals optimization opportunities across {result.TotalMethods} methods with potential for significant efficiency improvements.",
                "migration" => $"Migration assessment indicates {GetMigrationEffortEstimate(result.OverallComplexityScore)} effort requirement with structured implementation approach.",
                _ => $"Code quality assessment reveals {result.TotalClasses} classes requiring modernization attention with varying priority levels."
            };

            return assessment + $" Recommended approach: {GetRecommendedApproach(result.OverallComplexityScore)}.";
        }

        public string GenerateProjectSummary(MultiFileAnalysisResult result)
        {
            return $"Enterprise project analysis: {result.TotalFiles} source files containing {result.TotalClasses} classes, " +
                   $"{result.TotalMethods} methods, and {result.TotalProperties} properties. " +
                   $"Overall assessment: {result.OverallRiskLevel} risk level with complexity rating of {result.OverallComplexityScore}/100. " +
                   $"Project requires {GetResourceRequirement(result.OverallComplexityScore)} with {GetTimelineEstimate(result.OverallComplexityScore)} implementation timeline.";
        }

        private string GetMigrationEffortEstimate(int complexity) => complexity switch
        {
            var score when score < _complexityThresholds.Low => "minimal to moderate",
            var score when score < _complexityThresholds.High => "moderate to substantial",
            _ => "substantial to extensive"
        };

        private string GetRecommendedApproach(int complexityScore) => complexityScore switch
        {
            var score when score < _complexityThresholds.Low => "Agile development with standard practices",
            var score when score < _complexityThresholds.Medium => "Structured approach with experienced team",
            var score when score < _complexityThresholds.VeryHigh => "Phased migration with risk mitigation",
            _ => "Enterprise methodology with dedicated team"
        };

        private string GetResourceRequirement(int complexity) => complexity switch
        {
            var score when score < _complexityThresholds.Low => "standard development resources",
            var score when score < _complexityThresholds.High => "experienced development team with architectural guidance",
            _ => "senior development team with specialist migration expertise"
        };

        private string GetTimelineEstimate(int complexity) => complexity switch
        {
            var score when score < _complexityThresholds.Low => "2-4 week",
            var score when score < _complexityThresholds.High => "4-8 week",
            _ => "8-16 week"
        };
    }
}


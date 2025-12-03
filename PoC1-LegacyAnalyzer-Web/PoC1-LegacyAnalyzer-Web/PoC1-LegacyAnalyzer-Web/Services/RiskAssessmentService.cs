using Microsoft.Extensions.Options;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class RiskAssessmentService : IRiskAssessmentService
    {
        private readonly ILogger<RiskAssessmentService> _logger;
        private readonly ComplexityThresholdsConfig _complexityThresholds;

        public RiskAssessmentService(
            ILogger<RiskAssessmentService> logger,
            IOptions<ComplexityThresholdsConfig> complexityOptions)
        {
            _logger = logger;
            _complexityThresholds = complexityOptions.Value ?? new ComplexityThresholdsConfig();
        }

        public string DetermineRiskLevel(int complexityScore) => complexityScore switch
        {
            var score when score < _complexityThresholds.Low => "LOW",
            var score when score < _complexityThresholds.High => "MEDIUM",
            _ => "HIGH"
        };
    }
}


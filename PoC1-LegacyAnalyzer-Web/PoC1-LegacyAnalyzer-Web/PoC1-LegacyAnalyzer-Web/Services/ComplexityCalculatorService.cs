using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Options;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class ComplexityCalculatorService : IComplexityCalculatorService
    {
        private readonly ILogger<ComplexityCalculatorService> _logger;
        private readonly BusinessCalculationRules _businessRules;

        public ComplexityCalculatorService(
            ILogger<ComplexityCalculatorService> logger,
            IOptions<BusinessCalculationRules> businessRulesOptions)
        {
            _logger = logger;
            _businessRules = businessRulesOptions.Value ?? new BusinessCalculationRules();
        }

        public int CalculateFileComplexity(CodeAnalysisResult analysis)
        {
            // Professional complexity calculation algorithm
            var structuralComplexity = analysis.ClassCount * 10;
            var behavioralComplexity = analysis.MethodCount * 2;
            var dependencyComplexity = analysis.UsingCount * 1;

            var totalComplexity = structuralComplexity + behavioralComplexity + dependencyComplexity;
            return Math.Min(100, Math.Max(0, totalComplexity));
        }

        public int CalculateProjectComplexity(MultiFileAnalysisResult result)
        {
            if (!result.FileResults.Any()) return 0;

            var averageFileComplexity = result.FileResults.Average(f => f.ComplexityScore);
            var scaleComplexityFactor = Math.Min(result.TotalFiles * 1.5, 20); // Project scale impact
            var architecturalComplexity = result.TotalClasses > 0 ? (double)result.TotalMethods / result.TotalClasses : 0;

            var overallComplexity = averageFileComplexity + scaleComplexityFactor + (architecturalComplexity * 2);
            return Math.Min(100, (int)Math.Max(0, overallComplexity));
        }
    }
}


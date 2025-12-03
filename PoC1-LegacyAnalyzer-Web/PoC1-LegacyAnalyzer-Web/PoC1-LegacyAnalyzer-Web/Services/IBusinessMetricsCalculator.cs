using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface IBusinessMetricsCalculator
    {
        BusinessMetrics CalculateBusinessMetrics(MultiFileAnalysisResult result);
    }
}


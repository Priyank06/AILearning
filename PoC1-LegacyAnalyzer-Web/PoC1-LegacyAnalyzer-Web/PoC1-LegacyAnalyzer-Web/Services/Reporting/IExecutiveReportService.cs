using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.Reporting
{
    /// <summary>
    /// Generates executive summary reports from analysis results (SRP: report content only).
    /// </summary>
    public interface IExecutiveReportService
    {
        string GenerateExecutiveReport(MultiFileAnalysisResult result, string analysisType, int complexityLowThreshold, int complexityMediumThreshold, int complexityHighThreshold);
        int CalculateManualAnalysisHours(MultiFileAnalysisResult result);
    }
}

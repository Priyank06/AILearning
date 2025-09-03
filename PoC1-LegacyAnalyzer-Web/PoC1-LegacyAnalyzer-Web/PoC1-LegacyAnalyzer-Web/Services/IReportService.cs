using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface IReportService
    {
        Task<string> GenerateReportAsync(CodeAnalysisResult analysis, string aiAnalysis, string fileName, string analysisType);
        Task<byte[]> GenerateReportAsBytesAsync(CodeAnalysisResult analysis, string aiAnalysis, string fileName, string analysisType);
        string GenerateReportContent(CodeAnalysisResult analysis, string aiAnalysis, string fileName, string analysisType);
    }
}

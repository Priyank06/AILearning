using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Provides methods for generating code analysis reports in various formats.
    /// </summary>
    public interface IReportService
    {
        /// <summary>
        /// Asynchronously generates a report as a string based on the provided code analysis result and AI analysis.
        /// </summary>
        /// <param name="analysis">The result of the code analysis.</param>
        /// <param name="aiAnalysis">The AI-generated analysis content.</param>
        /// <param name="fileName">The name of the file being analyzed.</param>
        /// <param name="analysisType">The type of analysis performed.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the generated report as a string.</returns>
        Task<string> GenerateReportAsync(CodeAnalysisResult analysis, string aiAnalysis, string fileName, string analysisType);

        /// <summary>
        /// Asynchronously generates a report as a byte array based on the provided code analysis result and AI analysis.
        /// </summary>
        /// <param name="analysis">The result of the code analysis.</param>
        /// <param name="aiAnalysis">The AI-generated analysis content.</param>
        /// <param name="fileName">The name of the file being analyzed.</param>
        /// <param name="analysisType">The type of analysis performed.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the generated report as a byte array.</returns>
        Task<byte[]> GenerateReportAsBytesAsync(CodeAnalysisResult analysis, string aiAnalysis, string fileName, string analysisType);

        /// <summary>
        /// Generates the content of a report as a string based on the provided code analysis result and AI analysis.
        /// </summary>
        /// <param name="analysis">The result of the code analysis.</param>
        /// <param name="aiAnalysis">The AI-generated analysis content.</param>
        /// <param name="fileName">The name of the file being analyzed.</param>
        /// <param name="analysisType">The type of analysis performed.</param>
        /// <returns>The generated report content as a string.</returns>
        string GenerateReportContent(CodeAnalysisResult analysis, string aiAnalysis, string fileName, string analysisType);
    }
}

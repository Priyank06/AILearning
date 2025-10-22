using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Defines methods for performing AI-based code analysis and retrieving system prompts.
    /// </summary>
    public interface IAIAnalysisService
    {
        /// <summary>
        /// Performs an AI-based analysis on the provided code using the specified analysis type and static analysis results.
        /// </summary>
        /// <param name="code">The source code to analyze.</param>
        /// <param name="analysisType">The type of analysis to perform.</param>
        /// <param name="staticAnalysis">The static analysis results to supplement the AI analysis.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the analysis output as a string.</returns>
        Task<string> GetAnalysisAsync(string code, string analysisType, CodeAnalysisResult staticAnalysis);

        /// <summary>
        /// Retrieves the system prompt associated with the specified analysis type.
        /// </summary>
        /// <param name="analysisType">The type of analysis for which to get the system prompt.</param>
        /// <returns>The system prompt as a string.</returns>
        string GetSystemPrompt(string analysisType);
    }
}

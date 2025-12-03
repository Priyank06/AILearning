using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Service responsible for transforming raw LLM analysis results into structured SpecialistAnalysisResult objects.
    /// This centralizes common parsing logic across all specialist agents.
    /// </summary>
    public interface IResultTransformerService
    {
        /// <summary>
        /// Transforms raw analysis from an LLM into a structured SpecialistAnalysisResult.
        /// </summary>
        /// <param name="rawAnalysis">The raw text analysis from the LLM</param>
        /// <param name="agentName">The name of the agent that performed the analysis</param>
        /// <param name="specialty">The specialty domain of the agent</param>
        /// <returns>A structured SpecialistAnalysisResult with parsed findings, recommendations, and metrics</returns>
        SpecialistAnalysisResult TransformToResult(
            string rawAnalysis, 
            string agentName, 
            string specialty);

        /// <summary>
        /// Creates an error result for failed analyses.
        /// </summary>
        /// <param name="errorMessage">The error message to include</param>
        /// <param name="agentName">The name of the agent</param>
        /// <param name="specialty">The specialty domain of the agent</param>
        /// <returns>A SpecialistAnalysisResult representing the error state</returns>
        SpecialistAnalysisResult CreateErrorResult(
            string errorMessage, 
            string agentName, 
            string specialty);
    }
}
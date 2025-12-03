using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Defines a service for calculating consensus metrics and team confidence scores.
    /// </summary>
    public interface IConsensusCalculator
    {
        /// <summary>
        /// Calculates consensus metrics for the team discussion and analyses.
        /// </summary>
        /// <param name="discussion">The agent conversation containing discussion messages.</param>
        /// <param name="analyses">The array of specialist analysis results.</param>
        /// <returns>A <see cref="TeamConsensusMetrics"/> object containing consensus metrics.</returns>
        TeamConsensusMetrics CalculateConsensusMetrics(
            AgentConversation discussion,
            SpecialistAnalysisResult[] analyses);

        /// <summary>
        /// Calculates overall team confidence score based on weighted agent scores.
        /// </summary>
        /// <param name="analyses">The array of specialist analysis results.</param>
        /// <returns>An integer confidence score (0-100).</returns>
        int CalculateTeamConfidenceScore(SpecialistAnalysisResult[] analyses);
    }
}


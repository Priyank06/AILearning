using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Defines a service for resolving conflicts between agent analyses.
    /// </summary>
    public interface IConflictResolver
    {
        /// <summary>
        /// Identifies and resolves conflicts in agent analyses.
        /// </summary>
        /// <param name="conversation">The agent conversation containing messages.</param>
        /// <param name="analyses">The list of specialist analysis results.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the conflict resolution operation.</returns>
        Task IdentifyAndResolveConflictsAsync(
            AgentConversation conversation,
            List<SpecialistAnalysisResult> analyses,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates conflict resolution recommendations.
        /// </summary>
        /// <param name="conflictingAgents">List of agent names in conflict.</param>
        /// <param name="priorityLevel">The priority level causing conflict.</param>
        /// <param name="allAnalyses">All analysis results for context.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A string containing conflict resolution recommendations.</returns>
        Task<string> GenerateConflictResolutionAsync(
            List<string> conflictingAgents,
            string priorityLevel,
            List<SpecialistAnalysisResult> allAnalyses,
            CancellationToken cancellationToken = default);
    }
}


using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Defines a service for facilitating agent communication and discussions.
    /// </summary>
    public interface IAgentCommunicationCoordinator
    {
        /// <summary>
        /// Facilitates a discussion among agents on a given topic, using initial analyses as input.
        /// </summary>
        /// <param name="topic">The topic for the agent discussion.</param>
        /// <param name="initialAnalyses">The initial analyses provided by specialists.</param>
        /// <param name="codeContext">Optional code context for the discussion.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An <see cref="AgentConversation"/> representing the discussion.</returns>
        Task<AgentConversation> FacilitateAgentDiscussionAsync(
            string topic,
            List<SpecialistAnalysisResult> initialAnalyses,
            string? codeContext = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a single peer review asynchronously between two agents.
        /// </summary>
        /// <param name="reviewer">The agent performing the review.</param>
        /// <param name="reviewee">The agent whose analysis is being reviewed.</param>
        /// <param name="codeContext">Optional code context for the review.</param>
        /// <param name="conversationId">The conversation ID for message association.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An AgentMessage containing the peer review result or error details.</returns>
        Task<AgentMessage> PerformSinglePeerReviewAsync(
            SpecialistAnalysisResult reviewer,
            SpecialistAnalysisResult reviewee,
            string? codeContext,
            string conversationId,
            CancellationToken cancellationToken);
    }
}


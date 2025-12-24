using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using Microsoft.AspNetCore.Components.Forms;
using System.Runtime.CompilerServices;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Defines orchestration operations for coordinating multi-agent code analysis, discussion, and recommendation synthesis.
    /// </summary>
    public interface IAgentOrchestrationService
    {
        /// <summary>
        /// Coordinates a team of agents to analyze the provided code files according to the specified business objective and required specialties.
        /// Preprocessing is performed first to extract and filter metadata, ensuring only relevant, token-optimized data is routed to each agent.
        /// </summary>
        /// <param name="files">The list of code files to be analyzed.</param>
        /// <param name="businessObjective">The business objective guiding the analysis.</param>
        /// <param name="requiredSpecialties">A list of specialties required for the analysis.</param>
        /// <param name="progress">Optional progress reporter for preprocessing phase.</param>
        /// <param name="detailedProgress">Optional detailed progress reporter for per-agent progress tracking.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="TeamAnalysisResult"/> containing the results of the team analysis.</returns>
        Task<TeamAnalysisResult> CoordinateTeamAnalysisAsync(List<IBrowserFile> files, string businessObjective, List<string> requiredSpecialties, IProgress<string>? progress = null, IProgress<Models.AnalysisProgress>? detailedProgress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Facilitates a discussion among agents on a given topic, using initial analyses as input.
        /// </summary>
        /// <param name="topic">The topic for the agent discussion.</param>
        /// <param name="initialAnalyses">The initial analyses provided by specialists.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An <see cref="AgentConversation"/> representing the discussion.</returns>
        Task<AgentConversation> FacilitateAgentDiscussionAsync(string topic, List<SpecialistAnalysisResult> initialAnalyses, string? codeContext = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Synthesizes consolidated recommendations from multiple specialist analyses within a business context.
        /// </summary>
        /// <param name="analyses">The list of specialist analyses to synthesize.</param>
        /// <param name="businessContext">The business context for the recommendations.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="ConsolidatedRecommendations"/> object containing the synthesized recommendations.</returns>
        Task<ConsolidatedRecommendations> SynthesizeRecommendationsAsync(List<SpecialistAnalysisResult> analyses, string businessContext, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates an executive summary based on the team analysis result and business objective.
        /// </summary>
        /// <param name="teamResult">The result of the team analysis.</param>
        /// <param name="businessObjective">The business objective for the summary.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A string containing the executive summary.</returns>
        Task<string> GenerateExecutiveSummaryAsync(TeamAnalysisResult teamResult, string businessObjective, CancellationToken cancellationToken = default);
        IAsyncEnumerable<string> GenerateExecutiveSummaryStreamingAsync(TeamAnalysisResult teamResult, string businessObjective, [EnumeratorCancellation] CancellationToken cancellationToken = default);
    }
}

using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Defines the contract for a specialist agent service that can analyze code and review peer analyses.
    /// </summary>
    public interface ISpecialistAgentService
    {
        /// <summary>
        /// Gets the name of the specialist agent.
        /// </summary>
        string AgentName { get; }

        /// <summary>
        /// Gets the specialty area of the agent.
        /// </summary>
        string Specialty { get; }

        /// <summary>
        /// Gets the persona or description of the agent.
        /// </summary>
        string AgentPersona { get; }

        /// <summary>
        /// Gets the confidence threshold for the agent's analysis.
        /// </summary>
        int ConfidenceThreshold { get; }

        /// <summary>
        /// Analyzes the provided code within the given business context.
        /// </summary>
        /// <param name="code">The source code to analyze.</param>
        /// <param name="businessContext">The business context relevant to the analysis.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the analysis result as a string.</returns>
        Task<string> AnalyzeAsync(string code, string businessContext, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reviews a peer's analysis of the original code.
        /// </summary>
        /// <param name="peerAnalysis">The analysis provided by a peer agent.</param>
        /// <param name="originalCode">The original source code that was analyzed.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the review result as a string.</returns>
        Task<string> ReviewPeerAnalysisAsync(string peerAnalysis, string originalCode, CancellationToken cancellationToken = default);
    }
}

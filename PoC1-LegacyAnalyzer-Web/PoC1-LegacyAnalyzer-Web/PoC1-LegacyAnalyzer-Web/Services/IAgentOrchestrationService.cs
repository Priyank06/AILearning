using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Service for orchestrating multi-agent AI analysis with optimized token usage
    /// </summary>
    public interface IAgentOrchestrationService
    {
        /// <summary>
        /// LEGACY METHOD - Direct code analysis (high token usage)
        /// Use AnalyzeProjectSummaryAsync instead for cost optimization
        /// </summary>
        [Obsolete("Use AnalyzeProjectSummaryAsync for 75% cost reduction")]
        Task<TeamAnalysisResult> AnalyzeWithTeam(string code, bool includeSecurityAgent, bool includePerformanceAgent, bool includeArchitectureAgent, string businessObjective = "comprehensive-audit");

        /// <summary>
        /// Analyzes pre-processed project summary
        /// Reduces token usage by 75-80% compared to direct code analysis
        /// </summary>
        /// <param name="request">Pre-processed project summary with metadata</param>
        /// <returns>Comprehensive team analysis results</returns>
        Task<TeamAnalysisResult> AnalyzeProjectSummaryAsync(AgentAnalysisRequest request);

        /// <summary>
        /// Get orchestration plan without executing (for preview/planning)
        /// </summary>
        Task<OrchestrationPlan> CreateAnalysisPlanAsync(AgentAnalysisRequest request);

        /// <summary>
        /// Get estimated cost and time for analysis
        /// </summary>
        Task<AnalysisEstimate> EstimateAnalysisCostAsync(AgentAnalysisRequest request);
    }
}
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Defines a service for performing code analysis and generating strategic plans based on business goals and
    /// project context.
    /// </summary>    
    public interface ICodeAnalysisAgentService
    {

        /// <summary>
        /// Analyzes the provided code using an autonomous agent, considering the specified business goal and project context.
        /// </summary>
        /// <param name="code">The source code to be analyzed.</param>
        /// <param name="businessGoal">The business goal guiding the analysis.</param>
        /// <param name="projectContext">Optional. Additional context about the project to inform the analysis.</param>
        /// <param name="cancellationToken">Optional. A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an <see cref="AutonomousAnalysisResult"/>
        /// with detailed analysis, reasoning, and recommendations.
        /// </returns>
        Task<AutonomousAnalysisResult> AnalyzeWithAgentAsync(
            string code,
            string businessGoal,
            string projectContext = "",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a strategic plan based on the analysis of multiple files and a specified business objective.
        /// </summary>
        /// <param name="projectAnalysis">The aggregated analysis results for the project.</param>
        /// <param name="businessObjective">The business objective to guide the strategic plan.</param>
        /// <param name="cancellationToken">Optional. A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a string with the generated strategic plan.
        /// </returns>
        Task<string> GenerateStrategicPlanAsync(
            MultiFileAnalysisResult projectAnalysis,
            string businessObjective,
            CancellationToken cancellationToken = default);
    }
}

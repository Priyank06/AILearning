using System.Collections.Generic;
using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Generates reports for multi-agent team analysis results.
    /// </summary>
    public interface ITeamReportService
    {
        /// <summary>
        /// Generates a markdown report for a completed team analysis.
        /// Presentation components can call this instead of embedding
        /// formatting and aggregation logic in pages.
        /// </summary>
        /// <param name="teamResult">Completed team analysis result.</param>
        /// <param name="projectSummary">Optional high-level project metrics.</param>
        /// <param name="businessObjective">Configured business objective.</param>
        /// <param name="customObjective">Optional custom objective that overrides the business objective.</param>
        /// <param name="selectedAgents">Names of the specialist agents that participated.</param>
        /// <returns>Markdown content for the team analysis report.</returns>
        string GenerateTeamReport(
            TeamAnalysisResult teamResult,
            ProjectSummary? projectSummary,
            string businessObjective,
            string? customObjective,
            IReadOnlyList<string> selectedAgents);
    }
}



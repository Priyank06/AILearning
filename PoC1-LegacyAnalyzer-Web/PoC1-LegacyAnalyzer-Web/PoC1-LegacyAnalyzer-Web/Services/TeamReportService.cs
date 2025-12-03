using System;
using System.Collections.Generic;
using System.Text;
using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Default implementation for generating team analysis reports in markdown format.
    /// </summary>
    public class TeamReportService : ITeamReportService
    {
        public string GenerateTeamReport(
            TeamAnalysisResult teamResult,
            ProjectSummary? projectSummary,
            string businessObjective,
            string? customObjective,
            IReadOnlyList<string> selectedAgents)
        {
            if (teamResult == null)
            {
                return string.Empty;
            }

            var objectiveText = !string.IsNullOrWhiteSpace(customObjective)
                ? customObjective
                : businessObjective;

            var sb = new StringBuilder();

            sb.AppendLine("# Multi-Agent Team Analysis Report");
            sb.AppendLine($"**Objective:** {objectiveText}");
            sb.AppendLine($"**Agents:** {string.Join(", ", selectedAgents)}");
            sb.AppendLine($"**Completed:** {teamResult.CompletedAt:yyyy-MM-dd HH:mm}");
            sb.AppendLine();

            if (projectSummary != null)
            {
                sb.AppendLine("## Project Summary");
                sb.AppendLine($"- Files: {projectSummary.TotalFiles}");
                sb.AppendLine($"- Classes: {projectSummary.TotalClasses}");
                sb.AppendLine($"- Methods: {projectSummary.TotalMethods}");
                sb.AppendLine($"- Properties: {projectSummary.TotalProperties}");
                sb.AppendLine($"- Risk Level: {projectSummary.RiskLevel}");
                sb.AppendLine($"- Complexity Score: {projectSummary.ComplexityScore}");
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(teamResult.ExecutiveSummary))
            {
                sb.AppendLine("## Executive Summary");
                sb.AppendLine(teamResult.ExecutiveSummary);
                sb.AppendLine();
            }

            sb.AppendLine("## Team Metrics");
            sb.AppendLine($"- Specialist Agents: {teamResult.IndividualAnalyses?.Count ?? 0}");
            sb.AppendLine($"- Collaboration Messages: {teamResult.TeamDiscussion?.Count ?? 0}");
            sb.AppendLine($"- Team Consensus: {teamResult.Consensus?.AgreementPercentage ?? 0:F0}%");
            sb.AppendLine($"- Overall Confidence: {teamResult.OverallConfidenceScore}%");
            sb.AppendLine();

            if (teamResult.FinalRecommendations != null)
            {
                sb.AppendLine("## Recommendations Summary");
                sb.AppendLine($"- High Priority: {teamResult.FinalRecommendations.HighPriorityActions?.Count ?? 0}");
                sb.AppendLine($"- Medium Priority: {teamResult.FinalRecommendations.MediumPriorityActions?.Count ?? 0}");
                sb.AppendLine($"- Long-term Strategic: {teamResult.FinalRecommendations.LongTermStrategic?.Count ?? 0}");
                sb.AppendLine($"- Total Estimated Effort: {teamResult.FinalRecommendations.TotalEstimatedEffort} hours");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}



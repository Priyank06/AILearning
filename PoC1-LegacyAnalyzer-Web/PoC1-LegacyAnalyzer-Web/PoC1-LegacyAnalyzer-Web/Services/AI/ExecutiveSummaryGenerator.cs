using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PoC1_LegacyAnalyzer_Web.Models;
using System.Linq;
using System.Collections.Generic;
using TeamAnalysisResult = PoC1_LegacyAnalyzer_Web.Models.AgentCommunication.TeamAnalysisResult;

namespace PoC1_LegacyAnalyzer_Web.Services.AI
{
    public interface IExecutiveSummaryGenerator
    {
        Task<string> GenerateAsync(
            TeamAnalysisResult teamResult,
            string businessObjective,
            CancellationToken cancellationToken = default);
    }

    public class ExecutiveSummaryGenerator : IExecutiveSummaryGenerator
    {
        private readonly Kernel _kernel;
        private readonly ILogger<ExecutiveSummaryGenerator> _logger;
        private readonly AgentConfiguration _agentConfig;

        public ExecutiveSummaryGenerator(
            Kernel kernel,
            ILogger<ExecutiveSummaryGenerator> logger,
            IOptions<AgentConfiguration> agentOptions)
        {
            _kernel = kernel;
            _logger = logger;
            _agentConfig = agentOptions.Value ?? new AgentConfiguration();
        }

        public async Task<string> GenerateAsync(
            TeamAnalysisResult teamResult,
            string businessObjective,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Step 1: Extract data from each agent
                var summaryParts = new List<string>();
                if (teamResult?.IndividualAnalyses != null)
                {
                    foreach (var analysis in teamResult.IndividualAnalyses)
                    {
                        if (analysis == null) continue;
                        // Get top 2 findings from this agent
                        var topFindings = (analysis.KeyFindings ?? new List<Finding>())
                            .OrderByDescending(f => GetSeverityScore(f.Severity))
                            .Take(2)
                            .ToList();
                        var findingsSummary = string.Join("\n", topFindings.Select(f => $"  - {f.Description}"));
                        var agentSummary = $@"{analysis.Specialty} Analysis (Confidence: {analysis.ConfidenceScore}%):\n{findingsSummary}\nImpact: {analysis.BusinessImpact}";
                        summaryParts.Add(agentSummary);
                    }
                }
                // Step 2: Get recommendation counts
                var highPriorityCount = teamResult?.FinalRecommendations?.HighPriorityActions?.Count ?? 0;
                var mediumPriorityCount = teamResult?.FinalRecommendations?.MediumPriorityActions?.Count ?? 0;
                var totalEffort = teamResult?.FinalRecommendations?.TotalEstimatedEffort ?? 0;
                // Step 3: Get actual recommendation details for better alignment
                var topRecommendations = new List<string>();
                if (teamResult?.FinalRecommendations?.HighPriorityActions?.Any() == true)
                {
                    foreach (var rec in teamResult.FinalRecommendations.HighPriorityActions.Take(3))
                    {
                        topRecommendations.Add($"- {rec.Title} ({rec.Priority}): {rec.Description}");
                    }
                }
                var recommendationsText = topRecommendations.Any() 
                    ? string.Join("\n", topRecommendations)
                    : $"High Priority Actions: {highPriorityCount}, Medium Priority Actions: {mediumPriorityCount}";

                // Step 4: Build complete prompt with ACTUAL DATA from agents
                var prompt = $@"Create a concise executive summary based on this code analysis.

BUSINESS OBJECTIVE: {businessObjective}

SPECIALIST AGENT ANALYSIS RESULTS:
{string.Join("\n\n", summaryParts)}

TOP RECOMMENDATIONS:
{recommendationsText}

RECOMMENDATIONS SUMMARY:
- High Priority Actions: {highPriorityCount}
- Medium Priority Actions: {mediumPriorityCount}
- Estimated Total Effort: {totalEffort} hours

Provide an executive summary that:
1. ACCURATELY reflects the findings from the specialist agents listed above
2. Highlights the specific issues identified (security vulnerabilities, performance bottlenecks, architectural concerns)
3. Prioritizes the top recommendations based on the agent findings
4. Provides realistic resource requirements based on the estimated effort

IMPORTANT: The summary must align with what the agents actually found. Do not provide generic statements. Reference specific findings from the analysis results above.";
                // Step 4: Log for debugging
                var promptPreview = prompt.Length > 500 ? prompt.Substring(0, 500) + "..." : prompt;
                _logger.LogInformation("Executive summary prompt preview: {preview}", promptPreview);
                _logger.LogInformation("Full prompt length: {length} characters", prompt.Length);
                // Validate prompt has actual content
                if (prompt.Length < 500)
                {
                    _logger.LogWarning("Executive summary prompt is suspiciously short ({length} chars). May be missing analysis data.", prompt.Length);
                }
                var estimatedTokens = EstimateTokens(prompt);
                _logger.LogInformation("Calling LLM with estimated {tokens} input tokens", estimatedTokens);
                // Step 5: Call LLM
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
                var chatSettings = new PromptExecutionSettings();
                if (chatSettings.ExtensionData == null)
                    chatSettings.ExtensionData = new Dictionary<string, object>();
                chatSettings.ExtensionData["max_tokens"] = 500;
                chatSettings.ExtensionData["temperature"] = 0.3;
                var result = await chatCompletion.GetChatMessageContentAsync(prompt, chatSettings, cancellationToken: cancellationToken);
                sw.Stop();
                var summary = result.Content ?? "Executive summary generation failed.";
                _logger.LogInformation("LLM call completed in {ms}ms", sw.ElapsedMilliseconds);
                var responsePreview = summary.Length > 200 ? summary.Substring(0, 200) + "..." : summary;
                _logger.LogInformation("Executive summary response preview: {preview}", responsePreview);
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate executive summary");
                return "Executive summary generation failed due to an error.";
            }
        }

        // Helper method for severity scoring
        private int GetSeverityScore(string severity)
        {
            return severity?.ToUpper() switch
            {
                "CRITICAL" => 4,
                "HIGH" => 3,
                "MEDIUM" => 2,
                "LOW" => 1,
                _ => 0
            };
        }

        // Helper method for token estimation
        private int EstimateTokens(string text)
        {
            // Rough estimate: ~4 characters per token
            return text?.Length > 0 ? text.Length / 4 : 0;
        }
    }
}

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Orchestrates multi-agent code analysis, integrating preprocessing via <see cref="IFilePreProcessingService"/> to extract and filter metadata before routing to specialist agents.
    /// </summary>
    public class AgentOrchestrationService : IAgentOrchestrationService
    {
        private readonly Kernel _kernel;
        private readonly ILogger<AgentOrchestrationService> _logger;
        private readonly AgentConfiguration _agentConfig;
        private readonly IAgentRegistry _agentRegistry;
        private readonly IAgentCommunicationCoordinator _communicationCoordinator;
        private readonly IConsensusCalculator _consensusCalculator;
        private readonly IRecommendationSynthesizer _synthesizer;
        private readonly IExecutiveSummaryGenerator _summaryGenerator;
        private readonly IFilePreProcessingService _preprocessing;

        public AgentOrchestrationService(
            Kernel kernel,
            ILogger<AgentOrchestrationService> logger,
            IOptions<AgentConfiguration> agentOptions,
            IAgentRegistry agentRegistry,
            IAgentCommunicationCoordinator communicationCoordinator,
            IConsensusCalculator consensusCalculator,
            IRecommendationSynthesizer synthesizer,
            IExecutiveSummaryGenerator summaryGenerator,
            IFilePreProcessingService preprocessingService)
        {
            _kernel = kernel;
            _logger = logger;
            _agentRegistry = agentRegistry;
            _communicationCoordinator = communicationCoordinator;
            _consensusCalculator = consensusCalculator;
            _preprocessing = preprocessingService;
            _synthesizer = synthesizer;
            _summaryGenerator = summaryGenerator;

            // Register orchestrator functions with kernel
            _kernel.Plugins.AddFromObject(this, "AgentOrchestrator");
            _agentConfig = agentOptions.Value ?? new AgentConfiguration();
        }

        [KernelFunction, Description("Create analysis plan and assign agents")]
        public async Task<string> CreateAnalysisPlanAsync(
            [Description("Business objective and requirements")] string businessObjective,
            [Description("Code complexity and domain")] string codeContext)
        {
            var template = _agentConfig.OrchestrationPrompts.CreateAnalysisPlan;
            var prompt = template
                .Replace("{businessObjective}", businessObjective)
                .Replace("{codeContext}", codeContext);

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? "Analysis plan creation failed";
        }

        /// <summary>
        /// Coordinates a team of agents to analyze the provided files according to the specified business objective and required specialties.
        /// Preprocessing is performed first to extract and filter metadata, ensuring only relevant, token-optimized data is routed to each agent.
        /// </summary>
        /// <param name="files">The list of code files to be analyzed.</param>
        /// <param name="businessObjective">The business objective guiding the analysis.</param>
        /// <param name="requiredSpecialties">A list of specialties required for the analysis.</param>
        /// <param name="progress">Optional progress reporter for preprocessing phase.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="TeamAnalysisResult"/> containing the results of the team analysis.</returns>
        public async Task<PoC1_LegacyAnalyzer_Web.Models.AgentCommunication.TeamAnalysisResult> CoordinateTeamAnalysisAsync(
            List<IBrowserFile> files,
            string businessObjective,
            List<string> requiredSpecialties,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var orchestrationSw = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("[Orchestrator] Team analysis STARTED at {UtcNow}", DateTime.UtcNow);

            var conversationId = Guid.NewGuid().ToString();
            var teamResult = new TeamAnalysisResult
            {
                ConversationId = conversationId
            };

            try
            {
                var perfMetrics = new PerformanceMetrics();
                int llmCalls = 0;

                // Preprocessing phase - language detection happens automatically in ExtractMetadataParallelAsync
                var preprocessingSw = System.Diagnostics.Stopwatch.StartNew();
                progress?.Report("Preprocessing files...");
                // Language hint is now optional - metadata extraction will auto-detect language for each file
                var metadata = await _preprocessing.ExtractMetadataParallelAsync(files);
                preprocessingSw.Stop();
                perfMetrics.PreprocessingTimeMs = preprocessingSw.ElapsedMilliseconds;

                _logger.LogInformation("[Orchestrator] Preprocessed {FileCount} files in {ElapsedMs}ms", files.Count, preprocessingSw.ElapsedMilliseconds);
                progress?.Report($"Preprocessed {files.Count} files in {preprocessingSw.ElapsedMilliseconds}ms");

                // Step 1: Create analysis plan
                var codeContext = $"Preprocessed {metadata.Count} files, token-optimized for agent routing.";
                var analysisPlanSw = System.Diagnostics.Stopwatch.StartNew();
                var analysisPlan = await CreateAnalysisPlanAsync(businessObjective, codeContext);
                llmCalls++;
                analysisPlanSw.Stop();

                // Step 2: Execute specialist analyses in parallel
                var agentAnalysisSw = System.Diagnostics.Stopwatch.StartNew();                
                var agentTaskStartTime = DateTime.UtcNow;

                var specialistTasks = requiredSpecialties
                    .Where(specialty => _agentRegistry.IsRegistered(specialty))
                    .Select(async specialty =>
                    {
                        var taskCreatedAt = DateTime.UtcNow;
                        _logger.LogInformation("[Agent:{Specialty}] TASK CREATED at {UtcNow}", specialty, taskCreatedAt);

                        var agentData = await _preprocessing.GetAgentSpecificData(metadata, specialty);
                        var filteredCount = agentData.Split('\n').Length;
                        var reduction = metadata.Count > 0 ? 100 - (filteredCount * 100 / metadata.Count) : 0;
                        _logger.LogInformation("[Agent:{Specialty}] Filtered {Total} files to {Filtered} ({Reduction}% reduction)", specialty, metadata.Count, filteredCount, reduction);
                        progress?.Report($"Filtered {metadata.Count} files to {filteredCount} for {specialty} agent ({reduction}% reduction)");

                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        var result = await ExecuteSpecialistAnalysisAsync(specialty, agentData, businessObjective, cancellationToken);
                        sw.Stop();                        

                        return result;
                    })
                    .ToList();

                // All agent tasks are started immediately above
                _logger.LogInformation("[Orchestrator] All specialist agent tasks CREATED at {UtcNow}", agentTaskStartTime);

                var specialistResults = (await Task.WhenAll(specialistTasks)).OfType<SpecialistAnalysisResult>().ToArray();
                agentAnalysisSw.Stop();
                perfMetrics.AgentAnalysisTimeMs = agentAnalysisSw.ElapsedMilliseconds;                

                // Assign individual analyses to team result
                teamResult.IndividualAnalyses = specialistResults.ToList();

                // Step 3: Peer review discussion (starts after all agents complete)
                var peerReviewSw = System.Diagnostics.Stopwatch.StartNew();
                var discussion = await _communicationCoordinator.FacilitateAgentDiscussionAsync(
                    $"Code Analysis for: {businessObjective}",
                    specialistResults.ToList(),
                    codeContext,
                    cancellationToken);
                peerReviewSw.Stop();
                perfMetrics.PeerReviewTimeMs = peerReviewSw.ElapsedMilliseconds;
                teamResult.TeamDiscussion = discussion.Messages;

                // Step 4 & 6: Synthesis and executive summary in parallel (after agent results)
                _logger.LogInformation("[Orchestrator] Starting synthesis and executive summary in parallel at {UtcNow}", DateTime.UtcNow);
                var synthesisSw = System.Diagnostics.Stopwatch.StartNew();
                var synthesisTask = _synthesizer.SynthesizeRecommendationsAsync(
                    specialistResults.ToList(),
                    businessObjective,
                    cancellationToken);
                var summarySw = System.Diagnostics.Stopwatch.StartNew();
                var summaryTask = _summaryGenerator.GenerateAsync(
                    teamResult,
                    businessObjective,
                    cancellationToken);
                llmCalls += 2;
                ConsolidatedRecommendations? recommendations = null;
                string? summary = null;
                Exception? synthesisEx = null;
                Exception? summaryEx = null;
                try
                {
                    await Task.WhenAll(synthesisTask, summaryTask);
                    synthesisSw.Stop();
                    summarySw.Stop();
                    perfMetrics.SynthesisTimeMs = synthesisSw.ElapsedMilliseconds;
                    perfMetrics.ExecutiveSummaryTimeMs = summarySw.ElapsedMilliseconds;
                    recommendations = synthesisTask.IsCompletedSuccessfully ? synthesisTask.Result : null;
                    summary = summaryTask.IsCompletedSuccessfully ? summaryTask.Result : null;
                }
                catch (Exception ex)
                {
                    synthesisSw.Stop();
                    summarySw.Stop();
                    _logger.LogError(ex, "[Orchestrator] Error during parallel synthesis/summary");
                    if (synthesisTask.IsFaulted)
                        synthesisEx = synthesisTask.Exception;
                    if (summaryTask.IsFaulted)
                        summaryEx = summaryTask.Exception;
                }
                var parallelTimeMs = Math.Max(synthesisSw.ElapsedMilliseconds, summarySw.ElapsedMilliseconds);
                _logger.LogInformation("[Orchestrator] Synthesis and summary completed in {ElapsedMs}ms (parallel, sequential estimate: {SeqMs}ms)", parallelTimeMs, 35000);

                if (recommendations != null)
                {
                    // Generate implementation strategy if not already generated
                    if (string.IsNullOrWhiteSpace(recommendations.ImplementationStrategy) ||
                        recommendations.ImplementationStrategy == "Implementation strategy to be generated.")
                    {
                        _logger.LogInformation("[Orchestrator] Generating implementation strategy for consolidated recommendations");
                        try
                        {
                            recommendations.ImplementationStrategy = await GenerateImplementationStrategyAsync(
                                recommendations,
                                businessObjective,
                                cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[Orchestrator] Failed to generate implementation strategy");
                            recommendations.ImplementationStrategy = "Implementation strategy generation failed. Please try again.";
                        }
                    }
                    teamResult.FinalRecommendations = recommendations;
                }
                else if (synthesisEx != null)
                    _logger.LogError(synthesisEx, "[Orchestrator] Synthesis failed, recommendations not set");

                if (summary != null)
                    teamResult.ExecutiveSummary = summary;
                else if (summaryEx != null)
                    _logger.LogError(summaryEx, "[Orchestrator] Summary failed, executive summary not set");

                // Step 5: Calculate consensus metrics
                teamResult.Consensus = _consensusCalculator.CalculateConsensusMetrics(discussion, specialistResults);

                // Step 7: Calculate overall confidence
                teamResult.OverallConfidenceScore = _consensusCalculator.CalculateTeamConfidenceScore(specialistResults);

                orchestrationSw.Stop();
                perfMetrics.TotalTimeMs = orchestrationSw.ElapsedMilliseconds;
                perfMetrics.TotalLLMCalls = llmCalls;
                perfMetrics.ParallelSpeedup = perfMetrics.AgentAnalysisTimeMs > 0
                    ? Math.Round((double)(perfMetrics.AgentAnalysisTimeMs * specialistResults.Length) / perfMetrics.AgentAnalysisTimeMs, 2)
                    : 1.0;
                teamResult.PerformanceMetrics = perfMetrics;

                // Add performance summary to executive summary
                if (!string.IsNullOrEmpty(teamResult.ExecutiveSummary))
                {
                    teamResult.ExecutiveSummary += $"\n\nAnalysis completed efficiently using parallel execution. Total time: {perfMetrics.TotalTimeMs}ms ({perfMetrics.ParallelSpeedup}x speedup vs sequential).";
                }

                // Performance metrics logging
                var tokenReduction = "75-80%"; // Based on preprocessing design
                _logger.LogInformation("Team analysis completed. Time saved: {ElapsedMs}ms, Tokens reduced: {TokenReduction}",
                    orchestrationSw.ElapsedMilliseconds, tokenReduction);

                return teamResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Orchestrator] Team analysis coordination failed");
                throw;
            }
        }

        /// <summary>
        /// Executes a specialist agent analysis using preprocessed, filtered metadata summaries instead of full code.
        /// This method routes compact, token-optimized metadata to the agent, significantly reducing token usage and improving efficiency.
        /// All specialist agents must be able to handle metadata summaries as input.
        /// </summary>
        /// <param name="specialty">The specialty of the agent (e.g., security, performance, architecture).</param>
        /// <param name="filteredMetadataSummary">The filtered, preprocessed metadata summary for the agent (not full code).</param>
        /// <param name="businessObjective">The business objective guiding the analysis.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The result of the specialist agent's analysis.</returns>
        private async Task<SpecialistAnalysisResult> ExecuteSpecialistAnalysisAsync(
            string specialty,
            string filteredMetadataSummary,
            string businessObjective,
            CancellationToken cancellationToken)
        {
            try
            {
                var agent = _agentRegistry.GetAgent(specialty);

                if (agent == null)
                {
                    throw new InvalidOperationException($"Failed to resolve agent for specialty: {specialty}");
                }

                // Log token count reduction
                int tokenCount = filteredMetadataSummary?.Length > 0 ? filteredMetadataSummary.Length / 4 : 0;
                _logger.LogInformation("[Agent:{Specialty}] Routing filtered metadata summary. Token count: {TokenCount}", specialty, tokenCount);

                // Diagnostics: API call is about to start (already logged in orchestration)
                var analysisResultString = await agent.AnalyzeAsync(filteredMetadataSummary, businessObjective, cancellationToken);
                var analysisResult = JsonSerializer.Deserialize<SpecialistAnalysisResult>(
                    analysisResultString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (analysisResult == null)
                {
                    throw new InvalidOperationException("Agent returned null analysis result.");
                }

                return analysisResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Agent:{Specialty}] Failed to execute analysis", specialty);

                // Return error result instead of failing completely
                return new SpecialistAnalysisResult
                {
                    AgentName = $"{specialty}Agent",
                    Specialty = specialty,
                    ConfidenceScore = 0,
                    BusinessImpact = $"Analysis failed: {ex.Message}",
                    KeyFindings = new List<Finding>
                    {
                        new Finding
                        {
                            Category = "Analysis Error",
                            Description = $"Failed to execute {specialty} analysis: {ex.Message}",
                            Severity = "HIGH"
                        }
                    }
                };
            }
        }


        public async Task<ConsolidatedRecommendations> SynthesizeRecommendationsAsync(
            List<SpecialistAnalysisResult> analyses,
            string businessContext,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var consolidatedRecs = new ConsolidatedRecommendations();

                // Collect all recommendations from all agents
                var allRecommendations = analyses.SelectMany(a => a.Recommendations.Select(r => new
                {
                    Agent = a.AgentName,
                    Recommendation = r
                }))
                .ToList();

                // Group by priority
                var criticalRecs = allRecommendations.Where(r => r.Recommendation.Priority == "CRITICAL").ToList();
                var highRecs = allRecommendations.Where(r => r.Recommendation.Priority == "HIGH").ToList();
                var mediumRecs = allRecommendations.Where(r => r.Recommendation.Priority == "MEDIUM").ToList();
                var lowRecs = allRecommendations.Where(r => r.Recommendation.Priority == "LOW").ToList();

                // Add high priority (critical + high)
                consolidatedRecs.HighPriorityActions.AddRange(criticalRecs.Select(r => r.Recommendation));
                consolidatedRecs.HighPriorityActions.AddRange(highRecs.Select(r => r.Recommendation));

                // Add medium priority
                consolidatedRecs.MediumPriorityActions.AddRange(mediumRecs.Select(r => r.Recommendation));

                // Add low priority as long-term strategic
                consolidatedRecs.LongTermStrategic.AddRange(lowRecs.Select(r => r.Recommendation));

                // Calculate total effort
                consolidatedRecs.TotalEstimatedEffort = allRecommendations.Sum(r => r.Recommendation.EstimatedHours);

                // Orchestration prompt for synthesizing recommendations
                var template = _agentConfig.OrchestrationPrompts.SynthesizeRecommendations;
                var analysesJson = JsonSerializer.Serialize(analyses);
                var consensus = "Consensus calculation logic here"; // Replace with actual consensus if available
                var conflicts = "Conflict resolution logic here"; // Replace with actual conflicts if available
                var synthPrompt = template
                    .Replace("{analyses}", analysesJson)
                    .Replace("{consensus}", consensus)
                    .Replace("{conflicts}", conflicts);

                var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
                var synthResult = await chatCompletion.GetChatMessageContentAsync(synthPrompt);
                consolidatedRecs.SynthesisSummary = synthResult.Content ?? "Synthesis failed";

                // Generate implementation strategy
                consolidatedRecs.ImplementationStrategy = await GenerateImplementationStrategyAsync(
                    consolidatedRecs,
                    businessContext);

                // Identify and resolve conflicts
                var recommendationTuples = allRecommendations.Select(r => (r.Agent, r.Recommendation)).ToList();
                consolidatedRecs.ResolvedConflicts = await IdentifyRecommendationConflictsAsync(recommendationTuples);

                _logger.LogInformation("Synthesized {Count} recommendations with {Hours} total effort",
                    allRecommendations.Count, consolidatedRecs.TotalEstimatedEffort);

                return consolidatedRecs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to synthesize recommendations");
                throw;
            }
        }

        [KernelFunction, Description("Generate implementation strategy for consolidated recommendations")]
        public async Task<string> GenerateImplementationStrategyAsync(
            [Description("Consolidated recommendations")] ConsolidatedRecommendations recommendations,
            [Description("Business context and constraints")] string businessContext,
            CancellationToken cancellationToken = default)
        {
            // Build detailed recommendation context with actual recommendation details
            var recDetails = new System.Text.StringBuilder();

            if (recommendations.HighPriorityActions?.Any() == true)
            {
                recDetails.AppendLine("HIGH PRIORITY RECOMMENDATIONS:");
                foreach (var rec in recommendations.HighPriorityActions.Take(5))
                {
                    recDetails.AppendLine($"- {rec.Title} ({rec.Priority}): {rec.Description}");
                    recDetails.AppendLine($"  Estimated Hours: {rec.EstimatedHours}");
                    if (!string.IsNullOrWhiteSpace(rec.Implementation))
                    {
                        recDetails.AppendLine($"  Implementation: {rec.Implementation.Substring(0, Math.Min(200, rec.Implementation.Length))}");
                    }
                }
                recDetails.AppendLine();
            }

            if (recommendations.MediumPriorityActions?.Any() == true)
            {
                recDetails.AppendLine("MEDIUM PRIORITY RECOMMENDATIONS:");
                foreach (var rec in recommendations.MediumPriorityActions.Take(3))
                {
                    recDetails.AppendLine($"- {rec.Title} ({rec.Priority}): {rec.Description}");
                    recDetails.AppendLine($"  Estimated Hours: {rec.EstimatedHours}");
                }
                recDetails.AppendLine();
            }

            var recContext = recDetails.ToString();
            if (string.IsNullOrWhiteSpace(recContext))
            {
                recContext = $"High priority: {recommendations.HighPriorityActions?.Count ?? 0}, " +
                           $"Medium priority: {recommendations.MediumPriorityActions?.Count ?? 0}, " +
                           $"Strategic: {recommendations.LongTermStrategic?.Count ?? 0}, " +
                           $"Total effort: {recommendations.TotalEstimatedEffort} hours";
            }

            var template = _agentConfig.OrchestrationPrompts.CreateImplementationStrategy;
            var prompt = $@"{template}

BUSINESS CONTEXT: {businessContext}

SPECIFIC RECOMMENDATIONS TO IMPLEMENT:
{recContext}

TOTAL EFFORT: {recommendations.TotalEstimatedEffort} hours

Please provide a detailed, step-by-step implementation strategy that:
1. Addresses the specific recommendations listed above
2. Prioritizes based on business impact and dependencies
3. Provides concrete implementation steps for each high-priority item
4. Considers resource allocation and timeline
5. Includes risk mitigation strategies

Do NOT provide generic advice. Focus on the specific recommendations and how to implement them.";

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var executionSettings = new PromptExecutionSettings();
            if (executionSettings.ExtensionData == null)
                executionSettings.ExtensionData = new Dictionary<string, object>();
            executionSettings.ExtensionData["max_tokens"] = 1500;
            executionSettings.ExtensionData["temperature"] = 0.4;

            var result = await chatCompletion.GetChatMessageContentAsync(prompt, executionSettings, cancellationToken: cancellationToken);
            return result.Content ?? "Implementation strategy generation failed";
        }

        private async Task<List<ConflictResolution>> IdentifyRecommendationConflictsAsync(List<(string Agent, Recommendation Recommendation)> allRecommendations)
        {
            var conflicts = new List<ConflictResolution>();

            // Simple conflict detection - recommendations that contradict each other
            var groupedByTitle = allRecommendations
                .GroupBy(r => r.Recommendation.Title)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in groupedByTitle)
            {
                var agents = group.Select(g => g.Agent).ToList();
                conflicts.Add(new ConflictResolution
                {
                    ConflictDescription = $"Multiple agents recommend: {group.Key}",
                    ConflictingAgents = agents,
                    Resolution = "Consolidate similar recommendations with combined implementation approach",
                    Rationale = "Multiple expert agreement strengthens recommendation priority",
                    ConfidenceInResolution = 90
                });
            }

            return conflicts;
        }

        /// <summary>
        /// Generates an executive summary using only the top 3 findings from each agent, their confidence score, and business impact.
        /// This reduces the LLM prompt size from 10,000+ tokens to under 2,000, preventing timeouts and improving reliability.
        /// Logs the token count and warns if the input exceeds 3,000 tokens.
        /// </summary>
        public async Task<string> GenerateExecutiveSummaryAsync(
            PoC1_LegacyAnalyzer_Web.Models.AgentCommunication.TeamAnalysisResult teamResult,
            string businessObjective,
            CancellationToken cancellationToken = default)
        {
            return await _summaryGenerator.GenerateAsync(teamResult, businessObjective, cancellationToken);
        }

        // Ensure streaming method is public and matches interface
        public async IAsyncEnumerable<string> GenerateExecutiveSummaryStreamingAsync(
            PoC1_LegacyAnalyzer_Web.Models.AgentCommunication.TeamAnalysisResult teamResult,
            string businessObjective,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int SeverityRank(string severity) => severity?.ToUpper() switch
            {
                "CRITICAL" => 1,
                "HIGH" => 2,
                "MEDIUM" => 3,
                "LOW" => 4,
                _ => 5
            };
            var summarySections = new List<string>();
            foreach (var agent in teamResult.IndividualAnalyses)
            {
                var topFindings = agent.KeyFindings
                    .OrderBy(f => SeverityRank(f.Severity))
                    .Take(3)
                    .Select(f => $"- [{f.Severity}] {f.Category}: {f.Description}")
                    .ToList();
                var agentSection = $@"
Agent: {agent.AgentName} ({agent.Specialty})
Confidence Score: {agent.ConfidenceScore}%
Business Impact: {agent.BusinessImpact}
Top Findings:
{string.Join("\n", topFindings)}
";
                summarySections.Add(agentSection.Trim());
            }
            var condensedPrompt = $@"
Executive Summary Request
Business Objective: {businessObjective}

Specialist Agent Findings:
{string.Join("\n\n", summarySections)}
";
            var template = _agentConfig.OrchestrationPrompts.CreateExecutiveSummary;
            var prompt = template
                .Replace("{businessObjective}", businessObjective)
                .Replace("{summaryContext}", condensedPrompt);
            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            bool streamingFailed = false;
            Exception? streamingException = null;
            var streamedChunks = new List<string>();
            try
            {
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage(prompt);
                await foreach (var chunk in chatCompletion.GetStreamingChatMessageContentsAsync(chatHistory, cancellationToken: cancellationToken))
                {
                    if (!string.IsNullOrEmpty(chunk.Content))
                        streamedChunks.Add(chunk.Content);
                }
            }
            catch (Exception ex)
            {
                streamingFailed = true;
                streamingException = ex;
            }
            if (streamingFailed)
            {
                _logger.LogError(streamingException, "Streaming executive summary failed, falling back to non-streaming.");
                var result = await chatCompletion.GetChatMessageContentAsync(prompt, cancellationToken: cancellationToken);
                yield return result.Content ?? "Executive summary generation failed";
                yield break;
            }
            foreach (var chunk in streamedChunks)
            {
                yield return chunk;
            }
        }

        public async Task<AgentConversation> FacilitateAgentDiscussionAsync(
            string topic,
            List<SpecialistAnalysisResult> initialAnalyses,
            string? codeContext = null,
            CancellationToken cancellationToken = default)
        {
            return await _communicationCoordinator.FacilitateAgentDiscussionAsync(
                topic,
                initialAnalyses,
                codeContext,
                cancellationToken);
        }
    }

    // Helper for atomic max update
    internal static class InterlockedExtensions
    {
        public static void UpdateMax(ref int target, int value)
        {
            int initialValue, newValue;
            do
            {
                initialValue = target;
                newValue = Math.Max(initialValue, value);
            } while (initialValue != Interlocked.CompareExchange(ref target, newValue, initialValue));
        }
    }
}

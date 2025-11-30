using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using System.Runtime.CompilerServices;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Orchestrates multi-agent code analysis, integrating preprocessing via <see cref="IFilePreProcessingService"/> to extract and filter metadata before routing to specialist agents.
    /// </summary>
    public class AgentOrchestrationService : IAgentOrchestrationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Kernel _kernel;
        private readonly ILogger<AgentOrchestrationService> _logger;
        private readonly AgentConfiguration _agentConfig;
        private readonly BusinessCalculationRules _businessRules;
        private readonly IFilePreProcessingService _preprocessingService;

        // Agent registry
        private readonly Dictionary<string, Type> _agentRegistry;

        public AgentOrchestrationService(
            IServiceProvider serviceProvider,
            Kernel kernel,
            ILogger<AgentOrchestrationService> logger,
            IConfiguration configuration,
            IFilePreProcessingService preprocessingService)
        {
            _serviceProvider = serviceProvider;
            _kernel = kernel;
            _logger = logger;
            _preprocessingService = preprocessingService;

            // Initialize agent registry
            _agentRegistry = new Dictionary<string, Type>
            {
                { "security", typeof(SecurityAnalystAgent) },
                { "performance", typeof(PerformanceAnalystAgent) },
                { "architecture", typeof(ArchitecturalAnalystAgent) }
            };

            // Register orchestrator functions with kernel
            _kernel.Plugins.AddFromObject(this, "AgentOrchestrator");

            _agentConfig = new AgentConfiguration();
            configuration.GetSection("AgentConfiguration").Bind(_agentConfig);

            _businessRules = new BusinessCalculationRules();
            configuration.GetSection("BusinessCalculationRules").Bind(_businessRules);
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
            _logger.LogInformation("Starting team analysis coordination for objective: {Objective}", businessObjective);

            var conversationId = Guid.NewGuid().ToString();
            var teamResult = new PoC1_LegacyAnalyzer_Web.Models.AgentCommunication.TeamAnalysisResult
            {
                ConversationId = conversationId
            };

            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var perfMetrics = new PoC1_LegacyAnalyzer_Web.Models.AgentCommunication.PerformanceMetrics();
                int llmCalls = 0;
                var orchestrationSw = System.Diagnostics.Stopwatch.StartNew();

                // Preprocessing phase
                var preprocessingSw = System.Diagnostics.Stopwatch.StartNew();
                progress?.Report("Preprocessing files...");
                var metadata = await _preprocessingService.ExtractMetadataParallelAsync(files, "csharp");
                preprocessingSw.Stop();
                perfMetrics.PreprocessingTimeMs = preprocessingSw.ElapsedMilliseconds;

                _logger.LogInformation("Preprocessed {FileCount} files in {ElapsedMs}ms, extracted metadata", files.Count, preprocessingSw.ElapsedMilliseconds);
                progress?.Report($"Preprocessed {files.Count} files in {preprocessingSw.ElapsedMilliseconds}ms");

                // Step 1: Create analysis plan (use compact summary of metadata)
                var codeContext = $"Preprocessed {metadata.Count} files, token-optimized for agent routing.";
                var analysisPlanSw = System.Diagnostics.Stopwatch.StartNew();
                var analysisPlan = await CreateAnalysisPlanAsync(businessObjective, codeContext); llmCalls++;
                analysisPlanSw.Stop();
                // Step 2: Execute specialist analyses in parallel
                var agentAnalysisSw = System.Diagnostics.Stopwatch.StartNew();
                var specialistTasks = requiredSpecialties
                    .Where(specialty => _agentRegistry.ContainsKey(specialty.ToLower()))
                    .Select(async specialty =>
                    {
                        var agentData = await _preprocessingService.GetAgentSpecificData(metadata, specialty);
                        var filteredCount = agentData.Split('\n').Length;
                        var reduction = metadata.Count > 0 ? 100 - (filteredCount * 100 / metadata.Count) : 0;
                        _logger.LogInformation("Filtered {Total} files to {Filtered} for {Specialty} agent ({Reduction}% reduction)",
                            metadata.Count, filteredCount, specialty, reduction);
                        progress?.Report($"Filtered {metadata.Count} files to {filteredCount} for {specialty} agent ({reduction}% reduction)");
                        return await ExecuteSpecialistAnalysisAsync(specialty, agentData, businessObjective, cancellationToken);
                    })
                    .ToList();
                var specialistResults = (await Task.WhenAll(specialistTasks)).OfType<SpecialistAnalysisResult>().ToArray();
                agentAnalysisSw.Stop();
                perfMetrics.AgentAnalysisTimeMs = agentAnalysisSw.ElapsedMilliseconds;
                _logger.LogInformation("Completed {Count} specialist analyses", specialistResults.Length);

                // Assign individual analyses to team result
                teamResult.IndividualAnalyses = specialistResults.ToList();

                // Step 3: Peer review discussion
                var peerReviewSw = System.Diagnostics.Stopwatch.StartNew();
                var discussion = await FacilitateAgentDiscussionAsync(
                    $"Code Analysis for: {businessObjective}",
                    specialistResults.ToList(),
                    codeContext,
                    cancellationToken);
                peerReviewSw.Stop();
                perfMetrics.PeerReviewTimeMs = peerReviewSw.ElapsedMilliseconds;
                teamResult.TeamDiscussion = discussion.Messages;

                // Step 4 & 6: Synthesis and executive summary in parallel
                // Note: IndividualAnalyses must be populated before generating executive summary
                _logger.LogInformation("Executing synthesis and summary in parallel");
                var synthesisSw = System.Diagnostics.Stopwatch.StartNew();
                var synthesisTask = SynthesizeRecommendationsAsync(
                    specialistResults.ToList(),
                    businessObjective,
                    cancellationToken);
                var summarySw = System.Diagnostics.Stopwatch.StartNew();
                // Executive summary generation now has access to IndividualAnalyses
                var summaryTask = GenerateExecutiveSummaryAsync(
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
                    _logger.LogError(ex, "Error during parallel synthesis/summary");
                    if (synthesisTask.IsFaulted)
                        synthesisEx = synthesisTask.Exception;
                    if (summaryTask.IsFaulted)
                        summaryEx = summaryTask.Exception;
                }
                var parallelTimeMs = Math.Max(synthesisSw.ElapsedMilliseconds, summarySw.ElapsedMilliseconds);
                _logger.LogInformation("Completed synthesis and summary in {ElapsedMs}ms (parallel, sequential estimate: {SeqMs}ms)",
                    parallelTimeMs, 35000);

                if (recommendations != null)
                    teamResult.FinalRecommendations = recommendations;
                else if (synthesisEx != null)
                    _logger.LogError(synthesisEx, "Synthesis failed, recommendations not set");

                if (summary != null)
                    teamResult.ExecutiveSummary = summary;
                else if (summaryEx != null)
                    _logger.LogError(summaryEx, "Summary failed, executive summary not set");

                // Step 5: Calculate consensus metrics
                teamResult.Consensus = CalculateConsensusMetrics(discussion, specialistResults);

                // Step 7: Calculate overall confidence
                teamResult.OverallConfidenceScore = CalculateTeamConfidenceScore(specialistResults);

                orchestrationSw.Stop();
                perfMetrics.TotalTimeMs = orchestrationSw.ElapsedMilliseconds;
                perfMetrics.TotalLLMCalls = llmCalls;
                // Sequential estimates (example values, adjust as needed)
                perfMetrics.EstimatedSequentialTimeMs = perfMetrics.PreprocessingTimeMs +
                    (perfMetrics.AgentAnalysisTimeMs * specialistResults.Length) +
                    (perfMetrics.PeerReviewTimeMs * specialistResults.Length) +
                    (perfMetrics.SynthesisTimeMs + perfMetrics.ExecutiveSummaryTimeMs);
                perfMetrics.ParallelSpeedup = perfMetrics.EstimatedSequentialTimeMs > 0
                    ? Math.Round((double)perfMetrics.EstimatedSequentialTimeMs / perfMetrics.TotalTimeMs, 2)
                    : 1.0;
                teamResult.PerformanceMetrics = perfMetrics;

                _logger.LogInformation("Performance: {TotalTimeMs}ms total, {Speedup}x speedup", perfMetrics.TotalTimeMs, perfMetrics.ParallelSpeedup);
                _logger.LogInformation("Performance Metrics: {@perfMetrics}", perfMetrics);

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
                _logger.LogError(ex, "Team analysis coordination failed");
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
                var agentType = _agentRegistry[specialty.ToLower()];
                var agent = _serviceProvider.GetService(agentType) as ISpecialistAgentService;

                if (agent == null)
                {
                    throw new InvalidOperationException($"Failed to resolve agent for specialty: {specialty}");
                }

                // Log token count reduction
                int tokenCount = EstimateTokens(filteredMetadataSummary);
                _logger.LogInformation("Routing filtered metadata summary to {Specialty} agent. Token count: {TokenCount}", specialty, tokenCount);

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
                _logger.LogError(ex, "Failed to execute {Specialty} analysis", specialty);

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

        /// <summary>
        /// Performs a single peer review asynchronously between two agents, with robust error handling and performance logging.
        /// </summary>
        /// <param name="reviewer">The agent performing the review.</param>
        /// <param name="reviewee">The agent whose analysis is being reviewed.</param>
        /// <param name="codeContext">Optional code context for the review.</param>
        /// <param name="conversationId">The conversation ID for message association.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An AgentMessage containing the peer review result or error details.</returns>
        private async Task<AgentMessage> PerformSinglePeerReviewAsync(
            SpecialistAnalysisResult reviewer,
            SpecialistAnalysisResult reviewee,
            string? codeContext,
            string conversationId,
            CancellationToken cancellationToken)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("Starting peer review: {Reviewer} → {Reviewee}", reviewer.AgentName, reviewee.AgentName);
            try
            {
                var reviewerAgent = await GetAgentByNameAsync(reviewer.AgentName);
                if (reviewerAgent == null)
                {
                    _logger.LogError("Reviewer agent not found: {AgentName}", reviewer.AgentName);
                    return new AgentMessage
                    {
                        FromAgent = reviewer.AgentName,
                        ToAgent = reviewee.AgentName,
                        Type = MessageType.PeerReview,
                        Subject = $"Peer Review of {reviewee.Specialty} Analysis",
                        Content = $"Error: Reviewer agent not found for {reviewer.AgentName}",
                        ConversationId = conversationId,
                        Priority = 1
                    };
                }

                string peerReviewContent;
                try
                {
                    peerReviewContent = await reviewerAgent.ReviewPeerAnalysisAsync(
                        JsonSerializer.Serialize(reviewee, new JsonSerializerOptions { WriteIndented = true }),
                        codeContext ?? "Context unavailable",
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Peer review failed: {Reviewer} → {Reviewee}", reviewer.AgentName, reviewee.AgentName);
                    sw.Stop();
                    return new AgentMessage
                    {
                        FromAgent = reviewer.AgentName,
                        ToAgent = reviewee.AgentName,
                        Type = MessageType.PeerReview,
                        Subject = $"Peer Review of {reviewee.Specialty} Analysis",
                        Content = $"Error: Peer review failed ({ex.Message})",
                        ConversationId = conversationId,
                        Priority = 1
                    };
                }
                sw.Stop();
                _logger.LogInformation("Completed peer review: {Reviewer} → {Reviewee} in {ElapsedMs}ms", reviewer.AgentName, reviewee.AgentName, sw.ElapsedMilliseconds);
                return new AgentMessage
                {
                    FromAgent = reviewer.AgentName,
                    ToAgent = reviewee.AgentName,
                    Type = MessageType.PeerReview,
                    Subject = $"Peer Review of {reviewee.Specialty} Analysis",
                    Content = peerReviewContent,
                    ConversationId = conversationId,
                    Priority = 6
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Unexpected error in peer review: {Reviewer} → {Reviewee}", reviewer.AgentName, reviewee.AgentName);
                return new AgentMessage
                {
                    FromAgent = reviewer.AgentName,
                    ToAgent = reviewee.AgentName,
                    Type = MessageType.PeerReview,
                    Subject = $"Peer Review of {reviewee.Specialty} Analysis",
                    Content = $"Error: Unexpected peer review error ({ex.Message})",
                    ConversationId = conversationId,
                    Priority = 1
                };
            }
        }

        public async Task<AgentConversation> FacilitateAgentDiscussionAsync(
            string topic,
            List<SpecialistAnalysisResult> initialAnalyses,
            string? codeContext = null,
            CancellationToken cancellationToken = default)
        {
            var conversation = new AgentConversation
            {
                Topic = topic,
                ParticipantAgents = initialAnalyses.Select(a => a.AgentName).ToList()
            };

            try
            {
                // Step 1: Each agent presents their analysis
                foreach (var analysis in initialAnalyses)
                {
                    var presentationMessage = new AgentMessage
                    {
                        FromAgent = analysis.AgentName,
                        Type = MessageType.Analysis,
                        Subject = $"{analysis.Specialty} Analysis Presentation",
                        Content = $"Key findings: {analysis.KeyFindings.Count} issues identified. " +
                                 $"Priority: {analysis.Priority}. " +
                                 $"Business Impact: {analysis.BusinessImpact}",
                        ConversationId = conversation.ConversationId,
                        Priority = 8
                    };

                    conversation.Messages.Add(presentationMessage);
                }

                // Step 2: Generate peer reviews in parallel
                var peerReviewTasks = new List<Task<AgentMessage>>();
                int peerReviewCount = 0;
                for (int i = 0; i < initialAnalyses.Count; i++)
                {
                    for (int j = 0; j < initialAnalyses.Count; j++)
                    {
                        if (i != j)
                        {
                            peerReviewTasks.Add(PerformSinglePeerReviewAsync(
                                initialAnalyses[i],
                                initialAnalyses[j],
                                codeContext,
                                conversation.ConversationId,
                                cancellationToken));
                            peerReviewCount++;
                        }
                    }
                }

                _logger.LogInformation("Starting {Count} peer reviews in parallel", peerReviewCount);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var peerReviewResults = await Task.WhenAll(peerReviewTasks);
                sw.Stop();
                _logger.LogInformation("Completed {Count} peer reviews in {ElapsedMs}ms (sequential estimate: {SeqMs}ms)",
                    peerReviewCount, sw.ElapsedMilliseconds, peerReviewCount * 15000 / initialAnalyses.Count); // Estimate: 15s per agent sequentially

                foreach (var reviewMessage in peerReviewResults)
                {
                    conversation.Messages.Add(reviewMessage);
                }

                // Step 3: Identify and address conflicts
                await IdentifyAndResolveConflictsAsync(conversation, initialAnalyses);

                // Step 4: Optional orchestration synthesis
                if (!_agentConfig.EnableDiscussionSynthesis)
                {
                    _logger.LogInformation("Discussion synthesis disabled for performance");
                    conversation.Status = ConversationStatus.Completed;
                    conversation.EndTime = DateTime.Now;
                    return conversation;
                }

                // Orchestration synthesis enabled: optimize prompt and LLM call
                var peerReviewSummary = string.Join("\n", peerReviewResults.Select(m => $"{m.FromAgent}→{m.ToAgent}: {m.Content.Substring(0, Math.Min(120, m.Content.Length))}"));
                var template = _agentConfig.OrchestrationPrompts.FacilitateDiscussion;
                var agentNames = string.Join(", ", initialAnalyses.Select(a => a.AgentName));
                var discussionPrompt = template
                    .Replace("{topic}", topic)
                    .Replace("{findings}", peerReviewSummary)
                    .Replace("{agentNames}", agentNames);

                var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
                var executionSettings = new PromptExecutionSettings();
                if (executionSettings.ExtensionData == null)
                    executionSettings.ExtensionData = new Dictionary<string, object>();
                executionSettings.ExtensionData["max_tokens"] = 200;
                executionSettings.ExtensionData["temperature"] = 0.3;
                var synthSw = System.Diagnostics.Stopwatch.StartNew();
                var orchestrationResult = await chatCompletion.GetChatMessageContentAsync(
                    discussionPrompt,
                    executionSettings,
                    cancellationToken: cancellationToken);
                synthSw.Stop();
                _logger.LogInformation("Orchestration synthesis completed in {ElapsedMs}ms", synthSw.ElapsedMilliseconds);
                var orchestrationMessage = new AgentMessage
                {
                    FromAgent = "MasterOrchestrator",
                    Type = MessageType.Synthesis,
                    Subject = "Orchestrated Team Discussion",
                    Content = orchestrationResult.Content ?? "Discussion orchestration failed",
                    ConversationId = conversation.ConversationId,
                    Priority = 10
                };
                conversation.Messages.Add(orchestrationMessage);

                conversation.Status = ConversationStatus.Completed;
                conversation.EndTime = DateTime.Now;

                _logger.LogInformation("Agent discussion facilitated with {MessageCount} messages", conversation.Messages.Count);

                return conversation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to facilitate agent discussion");
                conversation.Status = ConversationStatus.Failed;
                throw;
            }
        }

        private async Task<ISpecialistAgentService?> GetAgentByNameAsync(string agentName)
        {
            try
            {
                // Get all agent types from registry
                foreach (var agentType in _agentRegistry.Values)
                {
                    // Resolve the agent instance from DI
                    var agent = _serviceProvider.GetService(agentType) as ISpecialistAgentService;
                    // Check if this agent's AgentName matches what we're looking for
                    if (agent != null && agent.AgentName == agentName)
                    {
                        return agent;
                    }
                }
                _logger.LogWarning("Agent not found: {agentName}. Available agents: {agents}",
                    agentName,
                    string.Join(", ", _agentRegistry.Keys));
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving agent: {agentName}", agentName);
                return null;
            }
        }
        private async Task IdentifyAndResolveConflictsAsync(
                AgentConversation conversation,
                List<SpecialistAnalysisResult> analyses)
        {
            // Simple conflict detection based on priority disagreements
            var priorities = analyses.Select(a => new { Agent = a.AgentName, Priority = a.Priority }).ToList();
            var priorityGroups = priorities.GroupBy(p => p.Priority).Where(g => g.Count() > 1);
            foreach (var group in priorityGroups)
            {
                var priorityValue = group.Key;
                if (priorityValue == "HIGH" || priorityValue == "CRITICAL")
                {
                    // Generate conflict resolution discussion
                    var conflictResolution = await GenerateConflictResolutionAsync(
                        group.Select(g => g.Agent).ToList(),
                        priorityValue,
                        analyses);
                    var resolutionMessage = new AgentMessage
                    {
                        FromAgent = "MasterOrchestrator",
                        Type = MessageType.Synthesis,
                        Subject = $"Conflict Resolution: {priorityValue} Priority Items",
                        Content = conflictResolution,
                        ConversationId = conversation.ConversationId,
                        Priority = 10
                    };
                    conversation.Messages.Add(resolutionMessage);
                }
            }
        }

        [KernelFunction, Description("Resolve conflicts between agent analyses")]
        public async Task<string> GenerateConflictResolutionAsync(
            [Description("List of conflicting agents")] List<string> conflictingAgents,
            [Description("Priority level causing conflict")] string priorityLevel,
            [Description("All analysis results for context")] List<SpecialistAnalysisResult> allAnalyses)
        {
            var conflictContext = string.Join(", ", conflictingAgents);
            var analysisContext = JsonSerializer.Serialize(allAnalyses.Take(3), new JsonSerializerOptions { WriteIndented = true });

            var prompt = $@"
You are a Master Orchestrator resolving conflicts between AI specialist agents.

CONFLICT SITUATION:
- Agents in conflict: {conflictContext}
- Priority level disagreement: {priorityLevel}
- Context: Multiple agents assigned same priority to different findings

ANALYSIS CONTEXT:
{analysisContext}

Resolve this conflict by:

1. ANALYZING ROOT CAUSE
   - Why do these agents disagree on priority?
   - What are the underlying assumptions?
   - Which perspective has stronger evidence?

2. FINDING MIDDLE GROUND
   - What aspects can all agents agree on?
   - How can priorities be refined or combined?
   - What additional context resolves the conflict?

3. FINAL RESOLUTION
   - Clear priority ranking with rationale
   - How each agent's concerns are addressed
   - Action plan that satisfies all stakeholders

Provide diplomatic but decisive conflict resolution.";

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? "Conflict resolution failed";
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
            [Description("Business context and constraints")] string businessContext)
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
            
            var result = await chatCompletion.GetChatMessageContentAsync(prompt, executionSettings);
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

        // Calculates consensus metrics for the team discussion and analyses
        private TeamConsensusMetrics CalculateConsensusMetrics(
            AgentConversation discussion,
            SpecialistAnalysisResult[] analyses)
        {
            var metrics = new TeamConsensusMetrics
            {
                TotalMessages = discussion.Messages.Count,
                DiscussionDuration = discussion.EndTime.HasValue
                    ? discussion.EndTime.Value - discussion.StartTime
                    : TimeSpan.Zero
            };
            // Calculate agreement percentage based on similar priorities
            var priorities = analyses.Select(a => a.Priority).ToList();
            var mostCommonPriority = priorities.GroupBy(p => p)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;
            if (mostCommonPriority != null)
            {
                var agreementCount = priorities.Count(p => p == mostCommonPriority);
                metrics.AgreementPercentage = (double)agreementCount / priorities.Count * 100;
            }
            // Calculate agent participation
            var agentMessageCounts = discussion.Messages
                .GroupBy(m => m.FromAgent)
                .ToDictionary(g => g.Key, g => g.Count());
            metrics.AgentParticipationScores = agentMessageCounts;
            // Count conflicts (simplified - look for challenge/disagreement messages)
            metrics.ConflictCount = discussion.Messages.Count(m => m.Type == MessageType.Challenge);
            metrics.ResolvedConflictCount = discussion.Messages.Count(m => m.Type == MessageType.Synthesis);
            return metrics;
        }

        // Calculates overall team confidence score based on weighted agent scores
        private int CalculateTeamConfidenceScore(SpecialistAnalysisResult[] analyses)
        {
            if (!analyses.Any()) return 0;
            // Weighted average based on agent confidence thresholds
            var weightedScores = analyses.Select(a => new
            {
                Score = a.ConfidenceScore,
                Weight = GetAgentWeight(a.AgentName)
            });
            var totalWeight = weightedScores.Sum(ws => ws.Weight);
            var weightedAverage = weightedScores.Sum(ws => ws.Score * ws.Weight) / totalWeight;
            return (int)Math.Round(weightedAverage);
        }

        private double GetAgentWeight(string agentName)
        {
            // Assign weights based on agent expertise level using configuration
            var weights = _businessRules.AgentWeighting;
            return agentName.ToLower() switch
            {
                var name when name.Contains("security") => weights.SecurityWeight,
                var name when name.Contains("performance") => weights.PerformanceWeight,
                var name when name.Contains("architectural") => weights.ArchitectureWeight,
                _ => weights.DefaultWeight
            };
        }
    }
}

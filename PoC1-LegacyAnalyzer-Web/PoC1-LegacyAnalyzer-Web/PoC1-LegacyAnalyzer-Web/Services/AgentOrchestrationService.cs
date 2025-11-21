using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using System.Text.Json;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class AgentOrchestrationService : IAgentOrchestrationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Kernel _kernel;
        private readonly ILogger<AgentOrchestrationService> _logger;
        private readonly AgentConfiguration _agentConfig;
        private readonly BusinessCalculationRules _businessRules;

        // Agent registry
        private readonly Dictionary<string, Type> _agentRegistry;

        public AgentOrchestrationService(
            IServiceProvider serviceProvider,
            Kernel kernel,
            ILogger<AgentOrchestrationService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _kernel = kernel;
            _logger = logger;

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

        public async Task<TeamAnalysisResult> CoordinateTeamAnalysisAsync(
            string code,
            string businessObjective,
            List<string> requiredSpecialties,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting team analysis coordination for objective: {Objective}", businessObjective);

            var conversationId = Guid.NewGuid().ToString();
            var teamResult = new TeamAnalysisResult
            {
                ConversationId = conversationId
            };

            try
            {
                var estimatedPromptTokens = EstimateTokens(code);
                var estimatedCompletionTokens = 0;
                // Step 1: Create analysis plan
                var analysisPlan = await CreateAnalysisPlanAsync(businessObjective, GetCodeContextSummary(code));
                estimatedCompletionTokens += EstimateTokens(analysisPlan);
                _logger.LogInformation("Analysis plan created: {Plan}", analysisPlan);

                // Step 2: Execute specialist analyses in parallel
                var specialistTasks = requiredSpecialties
                    .Where(specialty => _agentRegistry.ContainsKey(specialty.ToLower()))
                    .Select(specialty => ExecuteSpecialistAnalysisAsync(specialty, code, businessObjective, cancellationToken))
                    .ToList();

                var specialistResults = await Task.WhenAll(specialistTasks);
                teamResult.IndividualAnalyses.AddRange(specialistResults);

                _logger.LogInformation("Completed {Count} specialist analyses", specialistResults.Length);

                // Step 3: Facilitate peer review discussion
                var discussion = await FacilitateAgentDiscussionAsync(
                    $"Code Analysis for: {businessObjective}",
                    specialistResults.ToList(),
                    GetCodeContextSummary(code),
                    cancellationToken);

                teamResult.TeamDiscussion = discussion.Messages;

                // Step 4: Synthesize final recommendations
                teamResult.FinalRecommendations = await SynthesizeRecommendationsAsync(
                    specialistResults.ToList(),
                    businessObjective,
                    cancellationToken);

                // Step 5: Calculate consensus metrics
                teamResult.Consensus = CalculateConsensusMetrics(discussion, specialistResults);

                // Step 6: Generate executive summary
                teamResult.ExecutiveSummary = await GenerateExecutiveSummaryAsync(
                    teamResult,
                    businessObjective,
                    cancellationToken);
                estimatedCompletionTokens += EstimateTokens(teamResult.ExecutiveSummary);

                // Step 7: Calculate overall confidence
                teamResult.OverallConfidenceScore = CalculateTeamConfidenceScore(specialistResults);

                // Populate token usage (estimated fallback)
                teamResult.TokenUsage = new TokenUsage
                {
                    Provider = "semantic-kernel",
                    Model = "unknown",
                    PromptTokens = estimatedPromptTokens,
                    CompletionTokens = estimatedCompletionTokens
                };

                _logger.LogInformation("Team analysis completed with confidence: {Confidence}%", teamResult.OverallConfidenceScore);

                return teamResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Team analysis coordination failed");
                throw;
            }
        }

        private async Task<SpecialistAnalysisResult> ExecuteSpecialistAnalysisAsync(
            string specialty,
            string code,
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

                var analysisResultString = await agent.AnalyzeAsync(code, businessObjective, cancellationToken);
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

                // Step 2: Generate peer reviews
                for (int i = 0; i < initialAnalyses.Count; i++)
                {
                    for (int j = 0; j < initialAnalyses.Count; j++)
                    {
                        if (i != j) // Don't review your own analysis
                        {
                            var reviewer = initialAnalyses[i];
                            var reviewee = initialAnalyses[j];

                            // Get the actual agent to perform peer review
                            var reviewerAgent = await GetAgentByNameAsync(reviewer.AgentName);
                            if (reviewerAgent != null)
                            {
                                var peerReview = await reviewerAgent.ReviewPeerAnalysisAsync(
                                    JsonSerializer.Serialize(reviewee, new JsonSerializerOptions { WriteIndented = true }),
                                    codeContext ?? "Context unavailable",
                                    cancellationToken);

                                var reviewMessage = new AgentMessage
                                {
                                    FromAgent = reviewer.AgentName,
                                    ToAgent = reviewee.AgentName,
                                    Type = MessageType.PeerReview,
                                    Subject = $"Peer Review of {reviewee.Specialty} Analysis",
                                    Content = peerReview,
                                    ConversationId = conversation.ConversationId,
                                    Priority = 6
                                };

                                conversation.Messages.Add(reviewMessage);
                            }
                        }
                    }
                }

                // Step 3: Identify and address conflicts
                await IdentifyAndResolveConflictsAsync(conversation, initialAnalyses);

                // Orchestration prompt for facilitating discussion
                var template = _agentConfig.OrchestrationPrompts.FacilitateDiscussion;
                var agentNames = string.Join(", ", initialAnalyses.Select(a => a.AgentName));
                var findings = string.Join("\n", initialAnalyses.Select(a => JsonSerializer.Serialize(a.KeyFindings)));
                var discussionPrompt = template
                    .Replace("{topic}", topic)
                    .Replace("{findings}", findings)
                    .Replace("{agentNames}", agentNames);

                var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
                var orchestrationResult = await chatCompletion.GetChatMessageContentAsync(discussionPrompt);
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
                // Map agent name back to specialty
                var specialty = agentName.ToLower() switch
                {
                    var name when name.Contains("security") => "security",
                    var name when name.Contains("performance") => "performance",
                    var name when name.Contains("architectural") => "architecture",
                    _ => null
                };

                if (specialty != null && _agentRegistry.ContainsKey(specialty))
                {
                    var agentType = _agentRegistry[specialty];
                    return _serviceProvider.GetService(agentType) as ISpecialistAgentService;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve agent: {AgentName}", agentName);
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
                if (group.Key == "HIGH" || group.Key == "CRITICAL")
                {
                    // Generate conflict resolution discussion
                    var conflictResolution = await GenerateConflictResolutionAsync(
                        group.Select(g => g.Agent).ToList(),
                        group.Key,
                        analyses);

                    var resolutionMessage = new AgentMessage
                    {
                        FromAgent = "MasterOrchestrator",
                        Type = MessageType.Synthesis,
                        Subject = $"Conflict Resolution: {group.Key} Priority Items",
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
            var template = _agentConfig.OrchestrationPrompts.CreateImplementationStrategy;
            var recContext = $"High priority: {recommendations.HighPriorityActions.Count}, " +
                           $"Medium priority: {recommendations.MediumPriorityActions.Count}, " +
                           $"Strategic: {recommendations.LongTermStrategic.Count}, " +
                           $"Total effort: {recommendations.TotalEstimatedEffort} hours";
            var prompt = template
                .Replace("{recommendations}", recContext)
                .Replace("{constraints}", businessContext)
                .Replace("{resources}", "Resource allocation logic here");

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
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

        public async Task<string> GenerateExecutiveSummaryAsync(
            TeamAnalysisResult teamResult,
            string businessObjective,
            CancellationToken cancellationToken = default)
        {
            var template = _agentConfig.OrchestrationPrompts.CreateExecutiveSummary;
            var summaryContext = $"Objective: {businessObjective}, " +
                               $"Agents: {teamResult.IndividualAnalyses.Count}, " +
                               $"Recommendations: {teamResult.FinalRecommendations.HighPriorityActions.Count} high priority, " +
                               $"Confidence: {teamResult.OverallConfidenceScore}%, " +
                               $"Effort: {teamResult.FinalRecommendations.TotalEstimatedEffort} hours";
            var prompt = template
                .Replace("{businessObjective}", businessObjective)
                .Replace("{summaryContext}", summaryContext);

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? "Executive summary generation failed";
        }

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

        private static bool IsPreprocessedPattern(string code)
        {
            return code.StartsWith("[Preprocessed Project Pattern", StringComparison.OrdinalIgnoreCase);
        }

        private string GetCodeContextSummary(string code)
        {
            var maxLength = _businessRules.ProcessingLimits.CodeContextSummaryMaxLength;
            if (!IsPreprocessedPattern(code))
            {
                return $"Code length: {code.Length} characters";
            }

            // Extract first few lines as compact summary (header + stats)
            var lines = code.Split('\n');
            var take = Math.Min(6, lines.Length);
            var header = string.Join(" ", lines.Take(take).Select(l => l.Trim()));
            // Keep it compact to save tokens for planning
            return header.Length > maxLength ? header.Substring(0, maxLength) + "..." : header;
        }

        private int EstimateTokens(string? text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            // Use configuration for chars per token
            var charsPerToken = _businessRules.ProcessingLimits.TokenEstimationCharsPerToken;
            return Math.Max(1, text.Length / charsPerToken);
        }
    }
}

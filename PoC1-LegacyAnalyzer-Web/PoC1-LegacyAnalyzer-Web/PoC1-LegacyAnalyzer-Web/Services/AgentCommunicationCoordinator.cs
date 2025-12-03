using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Service for facilitating agent communication, discussions, and peer reviews.
    /// </summary>
    public class AgentCommunicationCoordinator : IAgentCommunicationCoordinator
    {
        private readonly Kernel _kernel;
        private readonly ILogger<AgentCommunicationCoordinator> _logger;
        private readonly IAgentRegistry _agentRegistry;
        private readonly IConflictResolver _conflictResolver;
        private readonly AgentConfiguration _agentConfig;

        public AgentCommunicationCoordinator(
            Kernel kernel,
            ILogger<AgentCommunicationCoordinator> logger,
            IAgentRegistry agentRegistry,
            IConflictResolver conflictResolver,
            IOptions<AgentConfiguration> agentOptions)
        {
            _kernel = kernel;
            _logger = logger;
            _agentRegistry = agentRegistry;
            _conflictResolver = conflictResolver;
            _agentConfig = agentOptions.Value ?? new AgentConfiguration();
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
                    peerReviewCount, sw.ElapsedMilliseconds, peerReviewCount * 15000 / initialAnalyses.Count);

                foreach (var reviewMessage in peerReviewResults)
                {
                    conversation.Messages.Add(reviewMessage);
                }

                // Step 3: Identify and address conflicts
                await _conflictResolver.IdentifyAndResolveConflictsAsync(conversation, initialAnalyses, cancellationToken);

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

        public async Task<AgentMessage> PerformSinglePeerReviewAsync(
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
                var reviewerAgent = _agentRegistry.GetAgentByName(reviewer.AgentName);
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
    }
}


using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Text.Json;

namespace PoC1_LegacyAnalyzer_Web.Services.Orchestration
{
    /// <summary>
    /// Service for identifying and resolving conflicts between agent analyses.
    /// </summary>
    public class ConflictResolverService : IConflictResolver
    {
        private readonly Kernel _kernel;
        private readonly ILogger<ConflictResolverService> _logger;
        private readonly AgentConfiguration _agentConfig;

        public ConflictResolverService(
            Kernel kernel,
            ILogger<ConflictResolverService> logger,
            IOptions<AgentConfiguration> agentOptions)
        {
            _kernel = kernel;
            _logger = logger;
            _agentConfig = agentOptions.Value ?? new AgentConfiguration();

            // Register conflict resolution functions with kernel
            _kernel.Plugins.AddFromObject(this, "ConflictResolver");
        }

        public async Task IdentifyAndResolveConflictsAsync(
            AgentConversation conversation,
            List<SpecialistAnalysisResult> analyses,
            CancellationToken cancellationToken = default)
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
                        analyses,
                        cancellationToken);
                    
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
            [Description("All analysis results for context")] List<SpecialistAnalysisResult> allAnalyses,
            CancellationToken cancellationToken = default)
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
            var result = await chatCompletion.GetChatMessageContentAsync(prompt, cancellationToken: cancellationToken);
            return result.Content ?? "Conflict resolution failed";
        }
    }
}


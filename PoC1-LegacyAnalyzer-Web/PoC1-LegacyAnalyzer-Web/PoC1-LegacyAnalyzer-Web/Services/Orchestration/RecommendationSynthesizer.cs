using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.SemanticKernel;
using System.Linq;
using System;
using Microsoft.Extensions.Logging;

namespace PoC1_LegacyAnalyzer_Web.Services.Orchestration
{
    public interface IRecommendationSynthesizer
    {
        Task<ConsolidatedRecommendations> SynthesizeRecommendationsAsync(
            List<SpecialistAnalysisResult> analyses,
            string businessObjective,
            CancellationToken cancellationToken = default);
    }

    public class RecommendationSynthesizer : IRecommendationSynthesizer
    {
        private readonly Kernel _kernel;
        private readonly ILogger<RecommendationSynthesizer> _logger;
        
        public RecommendationSynthesizer(Kernel kernel, ILogger<RecommendationSynthesizer> logger)
        {
            _kernel = kernel;
            _logger = logger;
        }
        public async Task<ConsolidatedRecommendations> SynthesizeRecommendationsAsync(
            List<SpecialistAnalysisResult> analyses,
            string businessObjective,
            CancellationToken cancellationToken = default)
        {
            // Extracted logic from AgentOrchestrationService (lines 300-350)
            // Logic-based synthesis without LLM
            var consolidated = new ConsolidatedRecommendations();
            
            // Filter out error results (those with "Analysis Error" findings)
            var successfulAnalyses = analyses.Where(a => 
                !a.KeyFindings.Any(f => f.Category == "Analysis Error")).ToList();
            
            _logger.LogInformation("Filtering recommendations: Total analyses={Total}, Successful={Successful}, Errors={Errors}",
                analyses.Count, successfulAnalyses.Count, analyses.Count - successfulAnalyses.Count);
            
            // Collect all recommendations from successful agents only
            var allRecommendations = successfulAnalyses.SelectMany(a => a.Recommendations.Select(r => new
            {
                Agent = a.AgentName,
                Recommendation = r
            }))
            .ToList();

            // Log recommendation counts and priorities for debugging
            _logger.LogDebug("Synthesizing recommendations: Total={TotalCount}, From {AgentCount} agents", 
                allRecommendations.Count, analyses.Count);
            
            var priorityBreakdown = allRecommendations
                .GroupBy(r => string.IsNullOrWhiteSpace(r.Recommendation.Priority) ? "EMPTY" : r.Recommendation.Priority.ToUpper())
                .ToDictionary(g => g.Key, g => g.Count());
            
            _logger.LogInformation("Recommendation priority breakdown: {Breakdown}", 
                string.Join(", ", priorityBreakdown.Select(kvp => $"{kvp.Key}={kvp.Value}")));

            // Group by priority (case-insensitive comparison)
            var criticalRecs = allRecommendations.Where(r => 
                !string.IsNullOrWhiteSpace(r.Recommendation.Priority) && 
                r.Recommendation.Priority.Equals("CRITICAL", StringComparison.OrdinalIgnoreCase)).ToList();
            
            var highRecs = allRecommendations.Where(r => 
                !string.IsNullOrWhiteSpace(r.Recommendation.Priority) && 
                r.Recommendation.Priority.Equals("HIGH", StringComparison.OrdinalIgnoreCase)).ToList();
            
            var mediumRecs = allRecommendations.Where(r => 
                !string.IsNullOrWhiteSpace(r.Recommendation.Priority) && 
                r.Recommendation.Priority.Equals("MEDIUM", StringComparison.OrdinalIgnoreCase)).ToList();
            
            var lowRecs = allRecommendations.Where(r => 
                !string.IsNullOrWhiteSpace(r.Recommendation.Priority) && 
                r.Recommendation.Priority.Equals("LOW", StringComparison.OrdinalIgnoreCase)).ToList();

            // Handle recommendations with empty/null priority - default to MEDIUM
            var unprioritizedRecs = allRecommendations.Where(r => 
                string.IsNullOrWhiteSpace(r.Recommendation.Priority)).ToList();

            // Add high priority (critical + high)
            consolidated.HighPriorityActions.AddRange(criticalRecs.Select(r => r.Recommendation));
            consolidated.HighPriorityActions.AddRange(highRecs.Select(r => r.Recommendation));

            // Add medium priority (including unprioritized)
            consolidated.MediumPriorityActions.AddRange(mediumRecs.Select(r => r.Recommendation));
            consolidated.MediumPriorityActions.AddRange(unprioritizedRecs.Select(r => r.Recommendation));

            // Add low priority as long-term strategic
            consolidated.LongTermStrategic.AddRange(lowRecs.Select(r => r.Recommendation));

            consolidated.TotalEstimatedEffort = consolidated.HighPriorityActions.Sum(r => r.EstimatedHours)
                + consolidated.MediumPriorityActions.Sum(r => r.EstimatedHours)
                + consolidated.LongTermStrategic.Sum(r => r.EstimatedHours);
            
            var errorCount = analyses.Count - successfulAnalyses.Count;
            var synthesisParts = new List<string>();
            synthesisParts.Add($"Synthesized {successfulAnalyses.Count} successful agent analyses");
            if (errorCount > 0)
            {
                synthesisParts.Add($"{errorCount} agent(s) encountered errors");
            }
            synthesisParts.Add($"{consolidated.HighPriorityActions.Count} high priority");
            synthesisParts.Add($"{consolidated.MediumPriorityActions.Count} medium priority");
            synthesisParts.Add($"{consolidated.LongTermStrategic.Count} strategic recommendations");
            
            consolidated.SynthesisSummary = string.Join(", ", synthesisParts) + ".";
            consolidated.ImplementationStrategy = successfulAnalyses.Count > 0 
                ? "Implementation strategy based on successful agent recommendations." 
                : "Unable to generate implementation strategy due to agent errors. Please review error details above.";
            consolidated.ResolvedConflicts = new List<PoC1_LegacyAnalyzer_Web.Models.AgentCommunication.ConflictResolution>();
            return await Task.FromResult(consolidated);
        }
    }
}

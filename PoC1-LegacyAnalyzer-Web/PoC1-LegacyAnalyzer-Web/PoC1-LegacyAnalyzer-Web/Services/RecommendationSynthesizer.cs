using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.SemanticKernel;
using System.Linq;
using System;
using Microsoft.Extensions.Logging;

namespace PoC1_LegacyAnalyzer_Web.Services
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
            
            // Collect all recommendations from all agents
            var allRecommendations = analyses.SelectMany(a => a.Recommendations.Select(r => new
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
            
            consolidated.SynthesisSummary = $"Synthesized {analyses.Count} agent analyses: " +
                $"{consolidated.HighPriorityActions.Count} high priority, " +
                $"{consolidated.MediumPriorityActions.Count} medium priority, " +
                $"{consolidated.LongTermStrategic.Count} strategic recommendations.";
            consolidated.ImplementationStrategy = "Implementation strategy to be generated.";
            consolidated.ResolvedConflicts = new List<PoC1_LegacyAnalyzer_Web.Models.AgentCommunication.ConflictResolution>();
            return await Task.FromResult(consolidated);
        }
    }
}

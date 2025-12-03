using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Service for calculating consensus metrics and team confidence scores.
    /// </summary>
    public class ConsensusCalculatorService : IConsensusCalculator
    {
        private readonly ILogger<ConsensusCalculatorService> _logger;
        private readonly BusinessCalculationRules _businessRules;

        public ConsensusCalculatorService(
            ILogger<ConsensusCalculatorService> logger,
            IOptions<BusinessCalculationRules> businessRulesOptions)
        {
            _logger = logger;
            _businessRules = businessRulesOptions.Value ?? new BusinessCalculationRules();
        }

        public TeamConsensusMetrics CalculateConsensusMetrics(
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

        public int CalculateTeamConfidenceScore(SpecialistAnalysisResult[] analyses)
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


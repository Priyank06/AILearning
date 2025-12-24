using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Logging;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Validates and normalizes confidence scores and explainability data.
    /// </summary>
    public class ConfidenceValidationService : IConfidenceValidationService
    {
        private readonly ILogger<ConfidenceValidationService> _logger;

        public ConfidenceValidationService(ILogger<ConfidenceValidationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ExplainableFinding ValidateAndNormalize(ExplainableFinding? explainability)
        {
            if (explainability == null)
            {
                // Create default explainability with low confidence
                return new ExplainableFinding
                {
                    ConfidenceScore = 50,
                    ConfidenceBreakdown = new ConfidenceBreakdown
                    {
                        EvidenceClarity = 50,
                        PatternMatch = 50,
                        ContextUnderstanding = 50,
                        Consistency = 50
                    },
                    ReasoningChain = new List<ReasoningStep>
                    {
                        new ReasoningStep
                        {
                            StepNumber = 1,
                            Description = "Explainability data not provided by AI",
                            Evidence = "No reasoning chain available",
                            Conclusion = "Confidence is low due to missing explainability data"
                        }
                    }
                };
            }

            // Validate confidence score
            if (explainability.ConfidenceScore < 0 || explainability.ConfidenceScore > 100)
            {
                _logger.LogWarning("Invalid confidence score {Score}, normalizing to 0-100 range", explainability.ConfidenceScore);
                explainability.ConfidenceScore = Math.Max(0, Math.Min(100, explainability.ConfidenceScore));
            }

            // Validate and normalize confidence breakdown
            if (explainability.ConfidenceBreakdown == null)
            {
                explainability.ConfidenceBreakdown = new ConfidenceBreakdown();
            }

            // Normalize breakdown components to 0-100 range
            explainability.ConfidenceBreakdown.EvidenceClarity = Math.Max(0, Math.Min(100, explainability.ConfidenceBreakdown.EvidenceClarity));
            explainability.ConfidenceBreakdown.PatternMatch = Math.Max(0, Math.Min(100, explainability.ConfidenceBreakdown.PatternMatch));
            explainability.ConfidenceBreakdown.ContextUnderstanding = Math.Max(0, Math.Min(100, explainability.ConfidenceBreakdown.ContextUnderstanding));
            explainability.ConfidenceBreakdown.Consistency = Math.Max(0, Math.Min(100, explainability.ConfidenceBreakdown.Consistency));

            // If confidence score is 0 or not set, calculate from breakdown
            if (explainability.ConfidenceScore == 0)
            {
                explainability.ConfidenceScore = CalculateConfidenceFromBreakdown(explainability.ConfidenceBreakdown);
            }

            // Validate reasoning chain
            if (explainability.ReasoningChain == null || !explainability.ReasoningChain.Any())
            {
                explainability.ReasoningChain = new List<ReasoningStep>
                {
                    new ReasoningStep
                    {
                        StepNumber = 1,
                        Description = "Reasoning chain not provided",
                        Evidence = "No step-by-step reasoning available",
                        Conclusion = "Analysis performed but reasoning steps were not documented"
                    }
                };
            }
            else
            {
                // Ensure step numbers are sequential
                for (int i = 0; i < explainability.ReasoningChain.Count; i++)
                {
                    explainability.ReasoningChain[i].StepNumber = i + 1;
                }
            }

            // Validate evidence items
            if (explainability.SupportingEvidence == null)
            {
                explainability.SupportingEvidence = new List<EvidenceItem>();
            }
            else
            {
                foreach (var evidence in explainability.SupportingEvidence)
                {
                    evidence.Strength = Math.Max(0, Math.Min(100, evidence.Strength));
                }
            }

            if (explainability.ContradictoryEvidence == null)
            {
                explainability.ContradictoryEvidence = new List<EvidenceItem>();
            }
            else
            {
                foreach (var evidence in explainability.ContradictoryEvidence)
                {
                    evidence.Strength = Math.Max(0, Math.Min(100, evidence.Strength));
                }
            }

            return explainability;
        }

        public int CalculateConfidenceFromBreakdown(ConfidenceBreakdown breakdown)
        {
            if (breakdown == null)
            {
                return 50; // Default to medium confidence
            }

            return breakdown.CalculateOverallConfidence();
        }

        public bool ValidateConfidenceBreakdown(ConfidenceBreakdown breakdown)
        {
            if (breakdown == null)
            {
                return false;
            }

            // Check if all components are in valid range
            var components = new[]
            {
                breakdown.EvidenceClarity,
                breakdown.PatternMatch,
                breakdown.ContextUnderstanding,
                breakdown.Consistency
            };

            return components.All(c => c >= 0 && c <= 100);
        }
    }
}


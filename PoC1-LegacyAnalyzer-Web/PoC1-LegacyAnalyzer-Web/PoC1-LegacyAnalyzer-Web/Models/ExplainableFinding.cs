namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Enhanced finding with explainability: confidence scores, reasoning chains, and evidence breakdown.
    /// </summary>
    public class ExplainableFinding
    {
        /// <summary>
        /// Overall confidence score (0-100) indicating how certain the AI is about this finding.
        /// </summary>
        public int ConfidenceScore { get; set; }

        /// <summary>
        /// Step-by-step reasoning chain explaining how the AI arrived at this finding.
        /// </summary>
        public List<ReasoningStep> ReasoningChain { get; set; } = new();

        /// <summary>
        /// Breakdown of confidence by factor.
        /// </summary>
        public ConfidenceBreakdown ConfidenceBreakdown { get; set; } = new();

        /// <summary>
        /// Evidence that supports this finding.
        /// </summary>
        public List<EvidenceItem> SupportingEvidence { get; set; } = new();

        /// <summary>
        /// Evidence that contradicts or weakens this finding.
        /// </summary>
        public List<EvidenceItem> ContradictoryEvidence { get; set; } = new();

        /// <summary>
        /// Confidence level category (High, Medium, Low) based on score.
        /// </summary>
        public ConfidenceLevel ConfidenceLevel => ConfidenceScore >= 90 ? ConfidenceLevel.High :
                                                   ConfidenceScore >= 60 ? ConfidenceLevel.Medium :
                                                   ConfidenceLevel.Low;
    }

    /// <summary>
    /// A single step in the reasoning chain.
    /// </summary>
    public class ReasoningStep
    {
        public int StepNumber { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Evidence { get; set; } = string.Empty;
        public string Conclusion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Breakdown of confidence by different factors.
    /// </summary>
    public class ConfidenceBreakdown
    {
        /// <summary>
        /// How clear and unambiguous the evidence is (0-100).
        /// </summary>
        public int EvidenceClarity { get; set; }

        /// <summary>
        /// How well the pattern matches known anti-patterns (0-100).
        /// </summary>
        public int PatternMatch { get; set; }

        /// <summary>
        /// How well the AI understands the context (0-100).
        /// </summary>
        public int ContextUnderstanding { get; set; }

        /// <summary>
        /// How consistent the finding is with other findings (0-100).
        /// </summary>
        public int Consistency { get; set; }

        /// <summary>
        /// Calculates overall confidence from breakdown components.
        /// </summary>
        public int CalculateOverallConfidence()
        {
            // Weighted average: Evidence clarity and pattern match are most important
            return (int)Math.Round(
                (EvidenceClarity * 0.35) +
                (PatternMatch * 0.35) +
                (ContextUnderstanding * 0.20) +
                (Consistency * 0.10)
            );
        }
    }

    /// <summary>
    /// Evidence item supporting or contradicting a finding.
    /// </summary>
    public class EvidenceItem
    {
        public string Type { get; set; } = string.Empty; // "CodeSnippet", "Pattern", "Metric", "Context"
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string CodeSnippet { get; set; } = string.Empty;
        public int Strength { get; set; } // 0-100, how strong this evidence is
    }

    /// <summary>
    /// Confidence level categories.
    /// </summary>
    public enum ConfidenceLevel
    {
        Low,      // < 60%
        Medium,   // 60-90%
        High      // >= 90%
    }
}


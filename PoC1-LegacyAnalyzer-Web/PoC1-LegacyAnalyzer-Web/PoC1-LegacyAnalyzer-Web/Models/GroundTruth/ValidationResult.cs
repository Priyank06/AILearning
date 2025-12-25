using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;

namespace PoC1_LegacyAnalyzer_Web.Models.GroundTruth
{
    /// <summary>
    /// Result of validating AI findings against ground truth
    /// </summary>
    public class GroundTruthValidationResult
    {
        /// <summary>
        /// Validation run identifier
        /// </summary>
        public string ValidationId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Dataset that was used for validation
        /// </summary>
        public string DatasetId { get; set; } = string.Empty;

        /// <summary>
        /// Dataset name
        /// </summary>
        public string DatasetName { get; set; } = string.Empty;

        /// <summary>
        /// When validation was performed
        /// </summary>
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Overall quality metrics
        /// </summary>
        public QualityMetrics OverallMetrics { get; set; } = new();

        /// <summary>
        /// Metrics per agent
        /// </summary>
        public Dictionary<string, QualityMetrics> MetricsByAgent { get; set; } = new();

        /// <summary>
        /// Metrics per category
        /// </summary>
        public Dictionary<string, QualityMetrics> MetricsByCategory { get; set; } = new();

        /// <summary>
        /// Metrics per severity level
        /// </summary>
        public Dictionary<string, QualityMetrics> MetricsBySeverity { get; set; } = new();

        /// <summary>
        /// Individual finding matches
        /// </summary>
        public List<FindingMatch> FindingMatches { get; set; } = new();

        /// <summary>
        /// True positives (correctly detected issues)
        /// </summary>
        public List<FindingMatch> TruePositives { get; set; } = new();

        /// <summary>
        /// False positives (AI found issues that don't exist)
        /// </summary>
        public List<Finding> FalsePositives { get; set; } = new();

        /// <summary>
        /// False negatives (AI missed known issues)
        /// </summary>
        public List<GroundTruthIssue> FalseNegatives { get; set; } = new();

        /// <summary>
        /// Summary report
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Configuration used for this validation
        /// </summary>
        public ValidationConfiguration Configuration { get; set; } = new();
    }

    /// <summary>
    /// Quality metrics (precision, recall, F1)
    /// </summary>
    public class QualityMetrics
    {
        /// <summary>
        /// Number of true positives
        /// </summary>
        public int TruePositiveCount { get; set; }

        /// <summary>
        /// Number of false positives
        /// </summary>
        public int FalsePositiveCount { get; set; }

        /// <summary>
        /// Number of false negatives
        /// </summary>
        public int FalseNegativeCount { get; set; }

        /// <summary>
        /// Precision = TP / (TP + FP)
        /// Range: 0-100%
        /// </summary>
        public double Precision { get; set; }

        /// <summary>
        /// Recall = TP / (TP + FN)
        /// Range: 0-100%
        /// </summary>
        public double Recall { get; set; }

        /// <summary>
        /// F1 Score = 2 * (Precision * Recall) / (Precision + Recall)
        /// Range: 0-100%
        /// </summary>
        public double F1Score { get; set; }

        /// <summary>
        /// Accuracy = (TP + TN) / (TP + TN + FP + FN)
        /// Note: TN is hard to define for this use case, so we focus on Precision/Recall
        /// </summary>
        public double Accuracy { get; set; }

        /// <summary>
        /// Calculate all metrics from counts
        /// </summary>
        public void Calculate()
        {
            // Precision: What % of AI findings are correct?
            Precision = TruePositiveCount + FalsePositiveCount > 0
                ? (TruePositiveCount / (double)(TruePositiveCount + FalsePositiveCount)) * 100
                : 0;

            // Recall: What % of real issues did AI find?
            Recall = TruePositiveCount + FalseNegativeCount > 0
                ? (TruePositiveCount / (double)(TruePositiveCount + FalseNegativeCount)) * 100
                : 0;

            // F1: Harmonic mean of precision and recall
            F1Score = Precision + Recall > 0
                ? 2 * (Precision * Recall) / (Precision + Recall)
                : 0;

            // Simplified accuracy (TP / Total)
            Accuracy = TruePositiveCount + FalsePositiveCount + FalseNegativeCount > 0
                ? (TruePositiveCount / (double)(TruePositiveCount + FalsePositiveCount + FalseNegativeCount)) * 100
                : 0;
        }
    }

    /// <summary>
    /// Represents a match between an AI finding and ground truth issue
    /// </summary>
    public class FindingMatch
    {
        /// <summary>
        /// AI-generated finding
        /// </summary>
        public Finding AiFinding { get; set; } = new();

        /// <summary>
        /// Ground truth issue that matches
        /// </summary>
        public GroundTruthIssue GroundTruthIssue { get; set; } = new();

        /// <summary>
        /// Match confidence (0-100%)
        /// </summary>
        public double MatchConfidence { get; set; }

        /// <summary>
        /// Whether category matches
        /// </summary>
        public bool CategoryMatches { get; set; }

        /// <summary>
        /// Whether severity matches
        /// </summary>
        public bool SeverityMatches { get; set; }

        /// <summary>
        /// Whether location matches
        /// </summary>
        public bool LocationMatches { get; set; }

        /// <summary>
        /// Match type (Exact, Partial, Weak)
        /// </summary>
        public MatchType MatchType { get; set; }

        /// <summary>
        /// Notes about this match
        /// </summary>
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Type of match between AI finding and ground truth
    /// </summary>
    public enum MatchType
    {
        /// <summary>
        /// Exact match (category, severity, location all match)
        /// </summary>
        Exact,

        /// <summary>
        /// Partial match (category matches, but severity or location differ)
        /// </summary>
        Partial,

        /// <summary>
        /// Weak match (related issue detected, but not exact)
        /// </summary>
        Weak,

        /// <summary>
        /// No match
        /// </summary>
        None
    }

    /// <summary>
    /// Configuration for ground truth validation
    /// </summary>
    public class ValidationConfiguration
    {
        /// <summary>
        /// Minimum match confidence to consider a true positive (default: 70%)
        /// </summary>
        public double MinMatchConfidence { get; set; } = 70.0;

        /// <summary>
        /// Whether category must match exactly
        /// </summary>
        public bool RequireExactCategoryMatch { get; set; } = false;

        /// <summary>
        /// Whether severity must match exactly
        /// </summary>
        public bool RequireExactSeverityMatch { get; set; } = false;

        /// <summary>
        /// Allow severity to be within N levels (e.g., HIGH vs CRITICAL is 1 level)
        /// </summary>
        public int AllowedSeverityDifference { get; set; } = 1;

        /// <summary>
        /// Whether location must match exactly
        /// </summary>
        public bool RequireExactLocationMatch { get; set; } = false;

        /// <summary>
        /// Allow location to be within N lines
        /// </summary>
        public int AllowedLineNumberDifference { get; set; } = 5;

        /// <summary>
        /// Count partial matches as true positives
        /// </summary>
        public bool CountPartialMatchesAsTruePositives { get; set; } = true;

        /// <summary>
        /// Weight for category match (0-1)
        /// </summary>
        public double CategoryMatchWeight { get; set; } = 0.5;

        /// <summary>
        /// Weight for severity match (0-1)
        /// </summary>
        public double SeverityMatchWeight { get; set; } = 0.3;

        /// <summary>
        /// Weight for location match (0-1)
        /// </summary>
        public double LocationMatchWeight { get; set; } = 0.2;
    }
}

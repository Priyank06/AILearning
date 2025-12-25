using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;

namespace PoC1_LegacyAnalyzer_Web.Models.Determinism
{
    /// <summary>
    /// Result of measuring determinism across multiple analysis runs
    /// </summary>
    public class DeterminismResult
    {
        /// <summary>
        /// Unique identifier for this determinism test
        /// </summary>
        public string TestId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// When this test was performed
        /// </summary>
        public DateTime TestedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Number of analysis runs performed
        /// </summary>
        public int RunCount { get; set; }

        /// <summary>
        /// Overall determinism score (0-100%)
        /// Higher = more consistent across runs
        /// </summary>
        public double DeterminismScore { get; set; }

        /// <summary>
        /// Consistency level based on score
        /// </summary>
        public ConsistencyLevel ConsistencyLevel { get; set; }

        /// <summary>
        /// All analysis runs performed
        /// </summary>
        public List<AnalysisRun> Runs { get; set; } = new();

        /// <summary>
        /// Findings that appeared in ALL runs (100% consistent)
        /// </summary>
        public List<ConsistentFinding> ConsistentFindings { get; set; } = new();

        /// <summary>
        /// Findings that appeared in SOME runs (inconsistent)
        /// </summary>
        public List<InconsistentFinding> InconsistentFindings { get; set; } = new();

        /// <summary>
        /// Determinism metrics per agent
        /// </summary>
        public Dictionary<string, AgentDeterminismMetrics> MetricsByAgent { get; set; } = new();

        /// <summary>
        /// Determinism metrics per category
        /// </summary>
        public Dictionary<string, CategoryDeterminismMetrics> MetricsByCategory { get; set; } = new();

        /// <summary>
        /// Overall statistics
        /// </summary>
        public DeterminismStatistics Statistics { get; set; } = new();

        /// <summary>
        /// Configuration used for this test
        /// </summary>
        public DeterminismConfiguration Configuration { get; set; } = new();
    }

    /// <summary>
    /// Single analysis run in a determinism test
    /// </summary>
    public class AnalysisRun
    {
        /// <summary>
        /// Run number (1-based)
        /// </summary>
        public int RunNumber { get; set; }

        /// <summary>
        /// When this run started
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// Duration of this run
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Findings from this run
        /// </summary>
        public List<Finding> Findings { get; set; } = new();

        /// <summary>
        /// Number of findings in this run
        /// </summary>
        public int FindingCount { get; set; }

        /// <summary>
        /// Unique identifier for this run's output
        /// </summary>
        public string RunHash { get; set; } = string.Empty;
    }

    /// <summary>
    /// A finding that appeared consistently across runs
    /// </summary>
    public class ConsistentFinding
    {
        /// <summary>
        /// The finding (from first run where it appeared)
        /// </summary>
        public Finding Finding { get; set; } = new();

        /// <summary>
        /// How many runs this appeared in
        /// </summary>
        public int AppearanceCount { get; set; }

        /// <summary>
        /// Percentage of runs this appeared in (0-100%)
        /// </summary>
        public double AppearanceRate => RunCount > 0 ? (AppearanceCount / (double)RunCount) * 100 : 0;

        /// <summary>
        /// Total number of runs
        /// </summary>
        public int RunCount { get; set; }

        /// <summary>
        /// Severity consistency (if severity varied across runs)
        /// </summary>
        public List<string> SeveritiesAcrossRuns { get; set; } = new();

        /// <summary>
        /// Whether severity was consistent
        /// </summary>
        public bool SeverityConsistent => SeveritiesAcrossRuns.Distinct().Count() == 1;

        /// <summary>
        /// Description consistency score (0-100%)
        /// </summary>
        public double DescriptionConsistency { get; set; }
    }

    /// <summary>
    /// A finding that appeared in some but not all runs (inconsistent)
    /// </summary>
    public class InconsistentFinding
    {
        /// <summary>
        /// The finding
        /// </summary>
        public Finding Finding { get; set; } = new();

        /// <summary>
        /// Which runs this appeared in
        /// </summary>
        public List<int> AppearedInRuns { get; set; } = new();

        /// <summary>
        /// Percentage of runs this appeared in
        /// </summary>
        public double AppearanceRate => TotalRuns > 0 ? (AppearedInRuns.Count / (double)TotalRuns) * 100 : 0;

        /// <summary>
        /// Total runs
        /// </summary>
        public int TotalRuns { get; set; }

        /// <summary>
        /// Why this is inconsistent
        /// </summary>
        public string InconsistencyReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Determinism metrics for a specific agent
    /// </summary>
    public class AgentDeterminismMetrics
    {
        /// <summary>
        /// Agent name
        /// </summary>
        public string AgentName { get; set; } = string.Empty;

        /// <summary>
        /// Determinism score for this agent (0-100%)
        /// </summary>
        public double DeterminismScore { get; set; }

        /// <summary>
        /// Findings that were consistent
        /// </summary>
        public int ConsistentFindingCount { get; set; }

        /// <summary>
        /// Findings that were inconsistent
        /// </summary>
        public int InconsistentFindingCount { get; set; }

        /// <summary>
        /// Average number of findings per run
        /// </summary>
        public double AverageFindingsPerRun { get; set; }

        /// <summary>
        /// Standard deviation of finding count
        /// </summary>
        public double FindingCountStdDev { get; set; }
    }

    /// <summary>
    /// Determinism metrics for a category
    /// </summary>
    public class CategoryDeterminismMetrics
    {
        /// <summary>
        /// Category name
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Determinism score for this category
        /// </summary>
        public double DeterminismScore { get; set; }

        /// <summary>
        /// Number of consistent findings
        /// </summary>
        public int ConsistentFindings { get; set; }

        /// <summary>
        /// Number of inconsistent findings
        /// </summary>
        public int InconsistentFindings { get; set; }
    }

    /// <summary>
    /// Overall determinism statistics
    /// </summary>
    public class DeterminismStatistics
    {
        /// <summary>
        /// Total number of unique findings across all runs
        /// </summary>
        public int TotalUniqueFindings { get; set; }

        /// <summary>
        /// Findings that appeared in 100% of runs
        /// </summary>
        public int FullyConsistentFindings { get; set; }

        /// <summary>
        /// Findings that appeared in ≥80% of runs
        /// </summary>
        public int HighlyConsistentFindings { get; set; }

        /// <summary>
        /// Findings that appeared in 50-79% of runs
        /// </summary>
        public int ModeratelyConsistentFindings { get; set; }

        /// <summary>
        /// Findings that appeared in <50% of runs
        /// </summary>
        public int PoorlyConsistentFindings { get; set; }

        /// <summary>
        /// Average finding count per run
        /// </summary>
        public double AverageFindingsPerRun { get; set; }

        /// <summary>
        /// Standard deviation of finding count across runs
        /// </summary>
        public double FindingCountStdDev { get; set; }

        /// <summary>
        /// Minimum findings in any run
        /// </summary>
        public int MinFindings { get; set; }

        /// <summary>
        /// Maximum findings in any run
        /// </summary>
        public int MaxFindings { get; set; }
    }

    /// <summary>
    /// Configuration for determinism testing
    /// </summary>
    public class DeterminismConfiguration
    {
        /// <summary>
        /// Number of times to run the analysis
        /// Default: 10
        /// </summary>
        public int RunCount { get; set; } = 10;

        /// <summary>
        /// Minimum appearance rate to consider a finding consistent (0-100%)
        /// Default: 80% (must appear in 8/10 runs)
        /// </summary>
        public double ConsistencyThreshold { get; set; } = 80.0;

        /// <summary>
        /// Whether to compare finding descriptions for similarity
        /// </summary>
        public bool CompareDescriptions { get; set; } = true;

        /// <summary>
        /// Whether to run analyses in parallel (faster but uses more resources)
        /// </summary>
        public bool RunInParallel { get; set; } = false;

        /// <summary>
        /// Maximum degree of parallelism (if RunInParallel = true)
        /// </summary>
        public int MaxParallelism { get; set; } = 3;

        /// <summary>
        /// Whether to cache results (set to false for determinism testing)
        /// </summary>
        public bool DisableCaching { get; set; } = true;

        /// <summary>
        /// Delay between runs (in milliseconds) to avoid rate limiting
        /// </summary>
        public int DelayBetweenRunsMs { get; set; } = 1000;
    }

    /// <summary>
    /// Consistency level classification
    /// </summary>
    public enum ConsistencyLevel
    {
        /// <summary>
        /// 90-100% determinism - Excellent consistency
        /// </summary>
        Excellent,

        /// <summary>
        /// 80-89% determinism - Good consistency
        /// </summary>
        Good,

        /// <summary>
        /// 70-79% determinism - Moderate consistency
        /// </summary>
        Moderate,

        /// <summary>
        /// 60-69% determinism - Fair consistency
        /// </summary>
        Fair,

        /// <summary>
        /// <60% determinism - Poor consistency
        /// </summary>
        Poor
    }
}

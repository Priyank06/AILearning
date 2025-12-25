namespace PoC1_LegacyAnalyzer_Web.Models.GroundTruth
{
    /// <summary>
    /// Represents a complete benchmark dataset with known issues for validation
    /// </summary>
    public class GroundTruthDataset
    {
        /// <summary>
        /// Dataset identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Dataset name (e.g., "Legacy C# Benchmark v1.0")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Dataset version
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Description of the dataset
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// All ground truth issues in this dataset
        /// </summary>
        public List<GroundTruthIssue> Issues { get; set; } = new();

        /// <summary>
        /// Files included in this benchmark dataset
        /// </summary>
        public List<BenchmarkFile> Files { get; set; } = new();

        /// <summary>
        /// Statistics about this dataset
        /// </summary>
        public DatasetStatistics Statistics { get; set; } = new();

        /// <summary>
        /// When this dataset was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last updated timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Tags for categorizing datasets
        /// </summary>
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Represents a file in the benchmark dataset
    /// </summary>
    public class BenchmarkFile
    {
        /// <summary>
        /// File name
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Programming language
        /// </summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// File content
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// File path relative to dataset root
        /// </summary>
        public string RelativePath { get; set; } = string.Empty;

        /// <summary>
        /// Number of known issues in this file
        /// </summary>
        public int IssueCount { get; set; }

        /// <summary>
        /// Lines of code
        /// </summary>
        public int LineCount { get; set; }
    }

    /// <summary>
    /// Statistics about the ground truth dataset
    /// </summary>
    public class DatasetStatistics
    {
        /// <summary>
        /// Total number of files
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Total number of known issues
        /// </summary>
        public int TotalIssues { get; set; }

        /// <summary>
        /// Issues by severity
        /// </summary>
        public Dictionary<string, int> IssuesBySeverity { get; set; } = new();

        /// <summary>
        /// Issues by category
        /// </summary>
        public Dictionary<string, int> IssuesByCategory { get; set; } = new();

        /// <summary>
        /// Issues by programming language
        /// </summary>
        public Dictionary<string, int> IssuesByLanguage { get; set; } = new();

        /// <summary>
        /// Issues by difficulty level
        /// </summary>
        public Dictionary<string, int> IssuesByDifficulty { get; set; } = new();
    }
}

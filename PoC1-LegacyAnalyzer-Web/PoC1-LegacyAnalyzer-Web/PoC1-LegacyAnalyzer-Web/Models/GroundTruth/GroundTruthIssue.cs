namespace PoC1_LegacyAnalyzer_Web.Models.GroundTruth
{
    /// <summary>
    /// Represents a known, verified issue in a benchmark file (ground truth).
    /// Used to measure precision and recall of AI-generated findings.
    /// </summary>
    public class GroundTruthIssue
    {
        /// <summary>
        /// Unique identifier for this ground truth issue
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// File name where this issue exists
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Category of the issue (SQL Injection, N+1 Query, SOLID Violation, etc.)
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the issue
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Expected severity (CRITICAL, HIGH, MEDIUM, LOW)
        /// </summary>
        public string Severity { get; set; } = string.Empty;

        /// <summary>
        /// Location in code (class, method, or line number)
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// Line number where issue exists (if applicable)
        /// </summary>
        public int? LineNumber { get; set; }

        /// <summary>
        /// Code snippet showing the issue
        /// </summary>
        public string CodeSnippet { get; set; } = string.Empty;

        /// <summary>
        /// Which specialist agent(s) should detect this issue
        /// </summary>
        public List<string> ExpectedDetectorAgents { get; set; } = new();

        /// <summary>
        /// Tags for categorizing ground truth issues (legacy, security, performance, etc.)
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Why this is considered an issue (rationale for ground truth)
        /// </summary>
        public string Rationale { get; set; } = string.Empty;

        /// <summary>
        /// Whether this issue is mandatory to detect (affects recall calculation)
        /// </summary>
        public bool IsMandatory { get; set; } = true;

        /// <summary>
        /// Difficulty level for AI to detect (Easy, Medium, Hard)
        /// </summary>
        public string DifficultyLevel { get; set; } = "Medium";

        /// <summary>
        /// Date when this ground truth was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Who created/verified this ground truth (expert reviewer)
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;
    }
}

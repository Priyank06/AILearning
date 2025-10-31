namespace PoC1_LegacyAnalyzer_Web.Models.MultiAgent
{
    /// <summary>
    /// Individual code issue found during preprocessing
    /// </summary>
    public class CodeIssue
    {
        public string IssueType { get; set; } = string.Empty; // Security, Performance, Modernization
        public string Severity { get; set; } = string.Empty; // Critical, High, Medium, Low
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public string CodeSnippet { get; internal set; }
    }
}

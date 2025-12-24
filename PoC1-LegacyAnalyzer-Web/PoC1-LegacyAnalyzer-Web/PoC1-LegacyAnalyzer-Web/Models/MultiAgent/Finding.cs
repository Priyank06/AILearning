using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Models.MultiAgent
{
    public class Finding
    {
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public string Severity { get; set; } = "";
        public string Location { get; set; } = "";
        public List<string> Evidence { get; set; } = new();
        
        // Validation properties
        public FindingValidationResult? Validation { get; set; }
        
        // Dependency impact analysis
        public DependencyImpact? DependencyImpact { get; set; }
        
        // Explainability: confidence scores and reasoning
        public ExplainableFinding? Explainability { get; set; }
    }

    public enum FindingValidationStatus
    {
        Validated,      // ✅ All checks passed
        LowConfidence,  // ⚠️ Some warnings but no critical errors
        Failed          // ❌ Critical validation errors found
    }

    public class FindingValidationResult
    {
        public FindingValidationStatus Status { get; set; } = FindingValidationStatus.Validated;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, bool> Checks { get; set; } = new();
    }

    public class ProjectSummary
    {
        public int TotalFiles { get; set; }
        public int TotalLines { get; set; }
        public int TotalClasses { get; set; }
        public int TotalMethods { get; set; }
        public int TotalProperties { get; set; }
        public int TotalInterfaces { get; set; }
        public int TotalDependencies { get; set; }
        public int TotalComplexity { get; set; }
        public int TotalCodePatternAnalysis { get; set; }
        public string CompactSummary { get; set; } = "";
        // Language distribution
        public Dictionary<string, int> LanguageDistribution { get; set; } = new();

        // Pre-identified issues (found without AI)
        public List<CodeIssue> PreIdentifiedIssues { get; set; } = new();

        // Structured summary for AI (optimized format - ~500 tokens total)
        public string StructuredSummary { get; set; } = string.Empty;

        // File summaries (compact versions only) - using main Models.FileMetadata
        public List<FileMetadata> FileSummaries { get; set; } = new();
    }
    
    public class CodeIssue
    {
        public string IssueType { get; set; } = string.Empty; // Security, Performance, Quality
        public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string CodeSnippet { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }
}

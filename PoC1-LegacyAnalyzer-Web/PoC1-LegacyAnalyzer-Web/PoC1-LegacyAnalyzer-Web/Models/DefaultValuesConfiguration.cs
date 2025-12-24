namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Configuration for default string values used throughout the application.
    /// Centralizes hardcoded strings to appsettings.json for easier maintenance.
    /// </summary>
    public class DefaultValuesConfiguration
    {
        public LanguageDefaults Language { get; set; } = new();
        public StatusDefaults Status { get; set; } = new();
        public SeverityDefaults Severity { get; set; } = new();
        public CategoryDefaults Category { get; set; } = new();
        public TokenUsageDefaults TokenUsage { get; set; } = new();
        public FileNameDefaults FileNames { get; set; } = new();
    }

    public class LanguageDefaults
    {
        public string Default { get; set; } = "csharp";
        public string Unknown { get; set; } = "unknown";
    }

    public class StatusDefaults
    {
        public string Success { get; set; } = "Success";
        public string Error { get; set; } = "Error";
        public string Initializing { get; set; } = "Initializing";
        public string PreparingData { get; set; } = "Preparing data";
        public string FilteringMetadata { get; set; } = "Filtering metadata";
        public string AnalyzingCode { get; set; } = "Analyzing code";
        public string ProcessingAIResponse { get; set; } = "Processing AI response";
        public string Complete { get; set; } = "Complete";
        public string AnalysisFailed { get; set; } = "Analysis Failed";
        public string AnalysisCompleted { get; set; } = "Analysis Completed";
        public string AnalysisCompletedFallback { get; set; } = "Analysis Completed (Fallback)";
        public string AnalysisCompletedIndividualFallback { get; set; } = "Analysis Completed (Individual Fallback)";
        public string AnalysisInProgress { get; set; } = "Agent analysis in progress";
        public string ExtractingMetadata { get; set; } = "Extracting metadata and performing static analysis (no API calls)...";
        public string AnalyzingProjectStructure { get; set; } = "Analyzing project structure and dependencies";
        public string AnalyzingArchitecturalOrganization { get; set; } = "Analyzing architectural organization";
        public string EvaluatingSystemArchitecture { get; set; } = "Evaluating system architecture patterns";
        public string CalculatingBusinessValue { get; set; } = "Calculating business value and risk assessment";
        public string CreatingExecutiveSummary { get; set; } = "Creating executive summary and recommendations";
        public string AnalysisCompletedGeneratingInsights { get; set; } = "Analysis completed - generating insights...";
        public string AnalyzedBySingleAgent { get; set; } = "Analyzed by Single Agent";
    }

    public class SeverityDefaults
    {
        public string Critical { get; set; } = "Critical";
        public string High { get; set; } = "High";
        public string Medium { get; set; } = "Medium";
        public string Low { get; set; } = "Low";
    }

    public class CategoryDefaults
    {
        public string Error { get; set; } = "Error";
        public string Warning { get; set; } = "Warning";
        public string Info { get; set; } = "Info";
    }

    public class TokenUsageDefaults
    {
        public string DefaultProvider { get; set; } = "semantic-kernel";
        public string DefaultModel { get; set; } = "unknown";
    }

    public class FileNameDefaults
    {
        public string Unknown { get; set; } = "Unknown";
        public string UnknownFile { get; set; } = "unknown";
    }
}


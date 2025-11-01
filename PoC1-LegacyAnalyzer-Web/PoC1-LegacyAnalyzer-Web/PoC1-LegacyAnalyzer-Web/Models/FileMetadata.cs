namespace PoC1_LegacyAnalyzer_Web.Models
{
    public class FileMetadata
    {
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string Language { get; set; } = "csharp";
        public List<string> UsingDirectives { get; set; } = new();
        public List<string> Namespaces { get; set; } = new();
        public List<string> ClassSignatures { get; set; } = new();
        public List<string> MethodSignatures { get; set; } = new();
        public List<string> PropertySignatures { get; set; } = new();
        public ComplexityMetrics Complexity { get; set; } = new();
        public CodePatternAnalysis Patterns { get; set; } = new();
        public string PatternSummary { get; set; } = string.Empty;
        public string Status { get; set; } = "Success";
        public string ErrorMessage { get; set; } = string.Empty;
    }
}


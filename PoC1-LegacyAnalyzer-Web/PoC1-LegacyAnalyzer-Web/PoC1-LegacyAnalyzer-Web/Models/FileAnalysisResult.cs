namespace PoC1_LegacyAnalyzer_Web.Models
{
    public class FileAnalysisResult
    {
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public CodeAnalysisResult StaticAnalysis { get; set; } = new CodeAnalysisResult();
        public string AIInsight { get; set; } = string.Empty;
        public int ComplexityScore { get; set; }
        public LegacyPatternResult? LegacyPatternResult { get; set; }
        public DependencyImpact? DependencyImpact { get; set; } // Impact analysis for this file
        public SemanticAnalysisResult? SemanticAnalysis { get; set; } // Hybrid semantic analysis for non-C# languages
        public string Status { get; set; } = string.Empty; // Default should be set from configuration
        public string ErrorMessage { get; set; } = string.Empty;
    }
}

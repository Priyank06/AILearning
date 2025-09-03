namespace PoC1_LegacyAnalyzer_Web.Models
{
    public class FileAnalysisResult
    {
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public CodeAnalysisResult StaticAnalysis { get; set; } = new CodeAnalysisResult();
        public string AIInsight { get; set; } = string.Empty;
        public int ComplexityScore { get; set; }
        public string Status { get; set; } = "Success";
        public string ErrorMessage { get; set; } = string.Empty;
    }
}

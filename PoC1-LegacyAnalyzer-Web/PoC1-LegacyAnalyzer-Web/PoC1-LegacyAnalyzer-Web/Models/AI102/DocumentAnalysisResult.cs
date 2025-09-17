namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class DocumentAnalysisResult
    {
        public string FileName { get; set; } = string.Empty;
        public string ExtractedText { get; set; } = string.Empty;
        public Dictionary<string, string> KeyValuePairs { get; set; } = new();
        public List<TableData> Tables { get; set; } = new();
        public double ConfidenceScore { get; set; }
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    }
}
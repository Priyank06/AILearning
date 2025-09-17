namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class AnalysisCommand
    {
        public CommandType Type { get; set; }
        public Dictionary<string, string> Parameters { get; set; } = new();
        public double Confidence { get; set; }
        public string OriginalText { get; set; } = string.Empty;
    }
}
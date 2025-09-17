namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class CodeSearchResult
    {
        public CodeDocument Document { get; set; } = new();
        public double Score { get; set; }
        public Dictionary<string, List<string>> Highlights { get; set; } = new();
        public string SemanticCaption { get; set; } = string.Empty;
    }
}
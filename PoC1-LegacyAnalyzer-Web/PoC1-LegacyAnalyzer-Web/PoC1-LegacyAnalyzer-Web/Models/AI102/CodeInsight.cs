namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class CodeInsight
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public InsightType Type { get; set; }
        public int Impact { get; set; }
        public List<string> AffectedFiles { get; set; } = new();
    }
}
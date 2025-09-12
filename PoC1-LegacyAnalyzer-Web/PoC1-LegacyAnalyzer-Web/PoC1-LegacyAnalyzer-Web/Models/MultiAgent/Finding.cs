namespace PoC1_LegacyAnalyzer_Web.Models.MultiAgent
{
    public class Finding
    {
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public string Severity { get; set; } = "";
        public string Location { get; set; } = "";
        public List<string> Evidence { get; set; } = new();
    }
}

namespace PoC1_LegacyAnalyzer_Web.Models.MultiAgent
{
    public class Recommendation
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Implementation { get; set; } = "";
        public decimal EstimatedHours { get; set; }
        public string Priority { get; set; } = "";
        public List<string> Dependencies { get; set; } = new();
    }
}

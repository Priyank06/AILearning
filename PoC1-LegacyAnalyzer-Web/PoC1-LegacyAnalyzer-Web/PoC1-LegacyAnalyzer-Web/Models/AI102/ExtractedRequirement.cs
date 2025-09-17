namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class ExtractedRequirement
    {
        public string RequirementId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RequirementType Type { get; set; }
        public int Priority { get; set; }
        public double Confidence { get; set; }
    }
}
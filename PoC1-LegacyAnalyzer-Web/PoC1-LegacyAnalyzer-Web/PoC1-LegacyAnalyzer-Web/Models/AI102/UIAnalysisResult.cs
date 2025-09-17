namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class UIAnalysisResult
    {
        public List<UIElement> Elements { get; set; } = new();
        public UILayoutType Layout { get; set; }
        public double Confidence { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
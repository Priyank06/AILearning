namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class UIElement
    {
        public string ElementType { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public BoundingBox BoundingBox { get; set; } = new();
        public double Confidence { get; set; }
    }
}
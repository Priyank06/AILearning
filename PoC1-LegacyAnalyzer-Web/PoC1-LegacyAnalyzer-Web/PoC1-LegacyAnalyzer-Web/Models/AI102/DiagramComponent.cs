namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class DiagramComponent
    {
        public string Name { get; set; } = string.Empty;
        public ComponentType Type { get; set; }
        public BoundingBox BoundingBox { get; set; } = new();
        public double Confidence { get; set; }
    }
}
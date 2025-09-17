namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class DetectedObject
    {
        public string Name { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public BoundingBox BoundingBox { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}
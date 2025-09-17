namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class DiagramConnection
    {
        public string FromComponent { get; set; } = string.Empty;
        public string ToComponent { get; set; } = string.Empty;
        public ConnectionType Type { get; set; }
        public double Confidence { get; set; }
    }
}
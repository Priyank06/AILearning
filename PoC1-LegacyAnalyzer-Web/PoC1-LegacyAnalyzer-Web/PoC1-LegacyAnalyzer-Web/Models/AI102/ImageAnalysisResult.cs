namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class ImageAnalysisResult
    {
        public string Description { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public List<DetectedObject> Objects { get; set; } = new();
        public double Confidence { get; set; }
        public List<string> ExtractedText { get; set; } = new();
    }
}
namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class ClassificationResult
    {
        public PatternPrediction TopPrediction { get; set; } = new();
        public List<PatternPrediction> AllPredictions { get; set; } = new();
        public double OverallConfidence { get; set; }
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    }
}
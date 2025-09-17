namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class AnalysisIntent
    {
        public string IntentName { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public List<EntityResult> Entities { get; set; } = new();
        public string OriginalText { get; set; } = string.Empty;
    }
}
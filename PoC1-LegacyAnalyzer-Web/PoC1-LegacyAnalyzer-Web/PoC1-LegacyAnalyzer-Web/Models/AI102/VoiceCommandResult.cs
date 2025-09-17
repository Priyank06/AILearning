namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class VoiceCommandResult
    {
        public bool Success { get; set; }
        public string RecognizedText { get; set; } = string.Empty;
        public AnalysisCommand Command { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
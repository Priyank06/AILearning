namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// User preferences persisted in browser storage (last analysis type, thresholds, UI flags).
    /// </summary>
    public class UserPreferences
    {
        public string LastAnalysisType { get; set; } = "general";
        public int? ComplexityLowThreshold { get; set; }
        public int? ComplexityMediumThreshold { get; set; }
        public int? ComplexityHighThreshold { get; set; }
        public bool ClientPersistenceEnabled { get; set; } = true;
        public Dictionary<string, string> CustomKeys { get; set; } = new Dictionary<string, string>();
    }
}
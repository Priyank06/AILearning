namespace PoC1_LegacyAnalyzer_Web.Services.Persistence
{
    /// <summary>
    /// Configuration for client-side (browser) persistence.
    /// </summary>
    public class ClientPersistenceConfiguration
    {
        public bool Enabled { get; set; } = true;
        public string Engine { get; set; } = "sqljs";
        public int MaxSessionsToKeep { get; set; } = 50;
        public string PurgeStrategy { get; set; } = "oldest";
        /// <summary>Auto-remove analysis and agent sessions older than this many days (e.g. 60). Applied when loading history.</summary>
        public int RetentionDays { get; set; } = 60;
    }
}
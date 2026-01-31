using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.Persistence
{
    /// <summary>
    /// Persists and retrieves analysis sessions in browser SQLite. Hides SQLite and encryption details.
    /// </summary>
    public interface IBrowserAnalysisRepository
    {
        /// <summary>Save an analysis session (result + business metrics) with optional friendly name, severity, and analysis type.</summary>
        Task SaveSessionAsync(MultiFileAnalysisResult result, BusinessMetrics? businessMetrics, string? userFriendlyName = null, string? severityLevel = null, string? analysisType = null, CancellationToken cancellationToken = default);

        /// <summary>Get the most recent sessions (full results) up to take.</summary>
        Task<IReadOnlyList<SavedAnalysisSession>> GetRecentSessionsAsync(int take, CancellationToken cancellationToken = default);

        /// <summary>Get a single session by id.</summary>
        Task<SavedAnalysisSession?> GetSessionByIdAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>Delete a session by id.</summary>
        Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>Remove sessions (and their file results) older than cutoffUtc. Used for retention (e.g. 60 days).</summary>
        Task PurgeSessionsOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
    }

    /// <summary>A saved analysis session (result + metadata) for history and reload.</summary>
    public class SavedAnalysisSession
    {
        public string Id { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string AnalysisType { get; set; } = string.Empty;
        public string? UserFriendlyName { get; set; }
        public string? SeverityLevel { get; set; }
        public int SchemaVersion { get; set; }
        public MultiFileAnalysisResult Result { get; set; } = new MultiFileAnalysisResult();
        public BusinessMetrics? BusinessMetrics { get; set; }
    }
}
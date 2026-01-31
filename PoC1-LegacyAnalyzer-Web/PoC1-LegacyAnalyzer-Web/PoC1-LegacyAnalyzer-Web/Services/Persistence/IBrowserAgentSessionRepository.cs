using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;

namespace PoC1_LegacyAnalyzer_Web.Services.Persistence
{
    /// <summary>
    /// Persists and retrieves multi-agent analysis sessions in browser SQLite. Hides SQLite and encryption details.
    /// </summary>
    public interface IBrowserAgentSessionRepository
    {
        /// <summary>Save an agent session (team result + objective and selected agents).</summary>
        Task SaveAgentSessionAsync(TeamAnalysisResult result, string businessObjective, string? customObjective, IReadOnlyList<string> selectedAgents, string? userFriendlyName = null, CancellationToken cancellationToken = default);

        /// <summary>Get the most recent agent sessions up to take.</summary>
        Task<IReadOnlyList<SavedAgentSession>> GetRecentAgentSessionsAsync(int take, CancellationToken cancellationToken = default);

        /// <summary>Get a single agent session by id.</summary>
        Task<SavedAgentSession?> GetAgentSessionByIdAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>Delete an agent session by id.</summary>
        Task DeleteAgentSessionAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>Remove agent sessions older than cutoffUtc. Used for retention (e.g. 60 days).</summary>
        Task PurgeAgentSessionsOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
    }

    /// <summary>A saved agent session (team result + metadata) for history and reload.</summary>
    public class SavedAgentSession
    {
        public string Id { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string BusinessObjective { get; set; } = string.Empty;
        public string? CustomObjective { get; set; }
        public IReadOnlyList<string> SelectedAgents { get; set; } = Array.Empty<string>();
        public string? UserFriendlyName { get; set; }
        public int SchemaVersion { get; set; }
        public TeamAnalysisResult TeamResult { get; set; } = new TeamAnalysisResult();
    }
}

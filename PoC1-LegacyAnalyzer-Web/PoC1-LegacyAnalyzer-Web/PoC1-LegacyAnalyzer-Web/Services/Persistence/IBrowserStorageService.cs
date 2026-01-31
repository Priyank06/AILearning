using PoC1_LegacyAnalyzer_Web.Models;
using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;

namespace PoC1_LegacyAnalyzer_Web.Services.Persistence
{
    /// <summary>
    /// High-level operations over browser persistence: save/load sessions (multi-file and multi-agent) and preferences.
    /// </summary>
    public interface IBrowserStorageService
    {
        Task SaveSessionAsync(MultiFileAnalysisResult result, BusinessMetrics? businessMetrics, string? userFriendlyName = null, string? severityLevel = null, string? analysisType = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SavedAnalysisSession>> GetRecentSessionsAsync(int take, CancellationToken cancellationToken = default);
        Task<SavedAnalysisSession?> GetSessionByIdAsync(string sessionId, CancellationToken cancellationToken = default);
        Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);

        Task SaveAgentSessionAsync(TeamAnalysisResult result, string businessObjective, string? customObjective, IReadOnlyList<string> selectedAgents, string? userFriendlyName = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SavedAgentSession>> GetRecentAgentSessionsAsync(int take, CancellationToken cancellationToken = default);
        Task<SavedAgentSession?> GetAgentSessionByIdAsync(string sessionId, CancellationToken cancellationToken = default);
        Task DeleteAgentSessionAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>Remove analysis and agent sessions older than RetentionDays (e.g. 60 days). Call when loading history.</summary>
        Task PurgeExpiredSessionsAsync(CancellationToken cancellationToken = default);

        Task<UserPreferences> GetPreferencesAsync(CancellationToken cancellationToken = default);
        Task SavePreferencesAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
        Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    }
}
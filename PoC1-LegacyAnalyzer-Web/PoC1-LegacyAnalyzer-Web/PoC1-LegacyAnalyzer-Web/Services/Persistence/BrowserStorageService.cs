using Microsoft.Extensions.Options;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.Persistence
{
    /// <summary>
    /// High-level browser storage: delegates to analysis, agent session, and preferences repositories. No-ops when ClientPersistence:Enabled is false.
    /// </summary>
    public class BrowserStorageService : IBrowserStorageService
    {
        private readonly IBrowserAnalysisRepository _analysisRepo;
        private readonly IBrowserAgentSessionRepository _agentSessionRepo;
        private readonly IBrowserPreferencesRepository _preferencesRepo;
        private readonly ISecureClientInterop _interop;
        private readonly ClientPersistenceConfiguration _config;

        public BrowserStorageService(
            IBrowserAnalysisRepository analysisRepo,
            IBrowserAgentSessionRepository agentSessionRepo,
            IBrowserPreferencesRepository preferencesRepo,
            ISecureClientInterop interop,
            IOptions<ClientPersistenceConfiguration> config)
        {
            _analysisRepo = analysisRepo;
            _agentSessionRepo = agentSessionRepo;
            _preferencesRepo = preferencesRepo;
            _interop = interop;
            _config = config.Value;
        }

        public async Task SaveSessionAsync(MultiFileAnalysisResult result, BusinessMetrics? businessMetrics, string? userFriendlyName = null, string? severityLevel = null, string? analysisType = null, CancellationToken cancellationToken = default)
        {
            if (!_config.Enabled) return;
            await _analysisRepo.SaveSessionAsync(result, businessMetrics, userFriendlyName, severityLevel, analysisType, cancellationToken);
        }

        public async Task<IReadOnlyList<SavedAnalysisSession>> GetRecentSessionsAsync(int take, CancellationToken cancellationToken = default)
        {
            if (!_config.Enabled) return Array.Empty<SavedAnalysisSession>();
            return await _analysisRepo.GetRecentSessionsAsync(take, cancellationToken);
        }

        public async Task<SavedAnalysisSession?> GetSessionByIdAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (!_config.Enabled) return null;
            return await _analysisRepo.GetSessionByIdAsync(sessionId, cancellationToken);
        }

        public async Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (!_config.Enabled) return;
            await _analysisRepo.DeleteSessionAsync(sessionId, cancellationToken);
        }

        public async Task SaveAgentSessionAsync(Models.AgentCommunication.TeamAnalysisResult result, string businessObjective, string? customObjective, IReadOnlyList<string> selectedAgents, string? userFriendlyName = null, CancellationToken cancellationToken = default)
        {
            if (!_config.Enabled) return;
            await _agentSessionRepo.SaveAgentSessionAsync(result, businessObjective, customObjective, selectedAgents, userFriendlyName, cancellationToken);
        }

        public async Task<IReadOnlyList<SavedAgentSession>> GetRecentAgentSessionsAsync(int take, CancellationToken cancellationToken = default)
        {
            if (!_config.Enabled) return Array.Empty<SavedAgentSession>();
            return await _agentSessionRepo.GetRecentAgentSessionsAsync(take, cancellationToken);
        }

        public async Task<SavedAgentSession?> GetAgentSessionByIdAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (!_config.Enabled) return null;
            return await _agentSessionRepo.GetAgentSessionByIdAsync(sessionId, cancellationToken);
        }

        public async Task DeleteAgentSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (!_config.Enabled) return;
            await _agentSessionRepo.DeleteAgentSessionAsync(sessionId, cancellationToken);
        }

        public async Task PurgeExpiredSessionsAsync(CancellationToken cancellationToken = default)
        {
            if (!_config.Enabled) return;
            var days = _config.RetentionDays > 0 ? _config.RetentionDays : 60;
            var cutoff = DateTime.UtcNow.AddDays(-days);
            await _analysisRepo.PurgeSessionsOlderThanAsync(cutoff, cancellationToken);
            await _agentSessionRepo.PurgeAgentSessionsOlderThanAsync(cutoff, cancellationToken);
        }

        public async Task<UserPreferences> GetPreferencesAsync(CancellationToken cancellationToken = default)
        {
            if (!_config.Enabled) return new UserPreferences();
            return await _preferencesRepo.GetAsync(cancellationToken);
        }

        public async Task SavePreferencesAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
        {
            if (!_config.Enabled) return;
            await _preferencesRepo.SaveAsync(preferences, cancellationToken);
        }

        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            if (!_config.Enabled) return false;
            return await _interop.IsAvailableAsync(cancellationToken);
        }
    }
}
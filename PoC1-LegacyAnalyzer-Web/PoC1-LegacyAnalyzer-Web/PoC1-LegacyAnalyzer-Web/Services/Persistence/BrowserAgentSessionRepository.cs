using System.Text.Json;
using Microsoft.Extensions.Logging;
using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;

namespace PoC1_LegacyAnalyzer_Web.Services.Persistence
{
    /// <summary>
    /// Persists and retrieves multi-agent analysis sessions via browser SQLite. Maps domain models to/from SQL rows.
    /// </summary>
    public class BrowserAgentSessionRepository : IBrowserAgentSessionRepository
    {
        private readonly ISecureClientInterop _interop;
        private readonly ILogger<BrowserAgentSessionRepository> _logger;
        private const int SchemaVersion = 1;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };

        public BrowserAgentSessionRepository(ISecureClientInterop interop, ILogger<BrowserAgentSessionRepository> logger)
        {
            _interop = interop;
            _logger = logger;
        }

        public async Task SaveAgentSessionAsync(TeamAnalysisResult result, string businessObjective, string? customObjective, IReadOnlyList<string> selectedAgents, string? userFriendlyName = null, CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid().ToString("N");
            var createdAt = DateTime.UtcNow.ToString("O");
            var resultJson = JsonSerializer.Serialize(result, JsonOptions);
            var agentSpecialtiesJson = JsonSerializer.Serialize(selectedAgents ?? Array.Empty<string>(), JsonOptions);

            await _interop.InitializeSchemaAsync(cancellationToken);
            await _interop.LoadDatabaseAsync(cancellationToken);
            await _interop.ExecuteSqlAsync(
                "INSERT INTO AgentSession (Id, CreatedAt, BusinessObjective, CustomObjective, AgentSpecialtiesJson, UserFriendlyName, SchemaVersion, ResultJson) VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                new object[] { id, createdAt, businessObjective ?? "", customObjective ?? "", agentSpecialtiesJson, userFriendlyName ?? "", SchemaVersion, resultJson },
                cancellationToken);

            await _interop.SaveDatabaseAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<SavedAgentSession>> GetRecentAgentSessionsAsync(int take, CancellationToken cancellationToken = default)
        {
            await _interop.InitializeSchemaAsync(cancellationToken);
            await _interop.LoadDatabaseAsync(cancellationToken);
            var rows = await _interop.QueryAsync<AgentSessionRow>(
                "SELECT Id, CreatedAt, BusinessObjective, CustomObjective, AgentSpecialtiesJson, UserFriendlyName, SchemaVersion, ResultJson FROM AgentSession ORDER BY CreatedAt DESC LIMIT ?",
                new object[] { take },
                cancellationToken);

            var list = new List<SavedAgentSession>();
            foreach (var row in rows)
            {
                var session = RowToSavedSession(row);
                if (session != null)
                    list.Add(session);
            }
            return list;
        }

        public async Task<SavedAgentSession?> GetAgentSessionByIdAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            await _interop.InitializeSchemaAsync(cancellationToken);
            await _interop.LoadDatabaseAsync(cancellationToken);
            var rows = await _interop.QueryAsync<AgentSessionRow>(
                "SELECT Id, CreatedAt, BusinessObjective, CustomObjective, AgentSpecialtiesJson, UserFriendlyName, SchemaVersion, ResultJson FROM AgentSession WHERE Id = ?",
                new object[] { sessionId },
                cancellationToken);
            var row = rows.FirstOrDefault();
            return row != null ? RowToSavedSession(row) : null;
        }

        public async Task DeleteAgentSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            await _interop.InitializeSchemaAsync(cancellationToken);
            await _interop.LoadDatabaseAsync(cancellationToken);
            await _interop.ExecuteSqlAsync("DELETE FROM AgentSession WHERE Id = ?", new object[] { sessionId }, cancellationToken);
            await _interop.SaveDatabaseAsync(cancellationToken);
        }

        public async Task PurgeAgentSessionsOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
        {
            await _interop.InitializeSchemaAsync(cancellationToken);
            await _interop.LoadDatabaseAsync(cancellationToken);
            var cutoff = cutoffUtc.ToString("O");
            await _interop.ExecuteSqlAsync("DELETE FROM AgentSession WHERE CreatedAt < ?", new object[] { cutoff }, cancellationToken);
            await _interop.SaveDatabaseAsync(cancellationToken);
        }

        private SavedAgentSession? RowToSavedSession(AgentSessionRow row)
        {
            try
            {
                var teamResult = string.IsNullOrEmpty(row.ResultJson)
                    ? new TeamAnalysisResult()
                    : JsonSerializer.Deserialize<TeamAnalysisResult>(row.ResultJson, JsonOptions) ?? new TeamAnalysisResult();

                IReadOnlyList<string> selectedAgents = Array.Empty<string>();
                if (!string.IsNullOrEmpty(row.AgentSpecialtiesJson))
                {
                    var parsed = JsonSerializer.Deserialize<List<string>>(row.AgentSpecialtiesJson, JsonOptions);
                    if (parsed != null)
                        selectedAgents = parsed;
                }

                DateTime.TryParse(row.CreatedAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var createdAt);

                return new SavedAgentSession
                {
                    Id = row.Id ?? "",
                    CreatedAt = createdAt,
                    BusinessObjective = row.BusinessObjective ?? "",
                    CustomObjective = string.IsNullOrEmpty(row.CustomObjective) ? null : row.CustomObjective,
                    SelectedAgents = selectedAgents,
                    UserFriendlyName = string.IsNullOrEmpty(row.UserFriendlyName) ? null : row.UserFriendlyName,
                    SchemaVersion = row.SchemaVersion,
                    TeamResult = teamResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize agent session {Id}", row.Id);
                return null;
            }
        }

        private class AgentSessionRow
        {
            public string? Id { get; set; }
            public string? CreatedAt { get; set; }
            public string? BusinessObjective { get; set; }
            public string? CustomObjective { get; set; }
            public string? AgentSpecialtiesJson { get; set; }
            public string? UserFriendlyName { get; set; }
            public int SchemaVersion { get; set; }
            public string? ResultJson { get; set; }
        }
    }
}

using System.Text.Json;
using Microsoft.Extensions.Logging;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.Persistence
{
    /// <summary>
    /// Persists and retrieves analysis sessions via browser SQLite. Maps domain models to/from SQL rows.
    /// </summary>
    public class BrowserAnalysisRepository : IBrowserAnalysisRepository
    {
        private readonly ISecureClientInterop _interop;
        private readonly ILogger<BrowserAnalysisRepository> _logger;
        private const int SchemaVersion = 1;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };

        public BrowserAnalysisRepository(ISecureClientInterop interop, ILogger<BrowserAnalysisRepository> logger)
        {
            _interop = interop;
            _logger = logger;
        }

        public async Task SaveSessionAsync(MultiFileAnalysisResult result, BusinessMetrics? businessMetrics, string? userFriendlyName = null, string? severityLevel = null, string? analysisType = null, CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid().ToString("N");
            var createdAt = DateTime.UtcNow.ToString("O");
            var resultJson = JsonSerializer.Serialize(result, JsonOptions);
            var businessMetricsJson = businessMetrics != null ? JsonSerializer.Serialize(businessMetrics, JsonOptions) : null;
            var at = analysisType ?? "general";

            await _interop.InitializeSchemaAsync(cancellationToken);
            await _interop.LoadDatabaseAsync(cancellationToken);
            await _interop.ExecuteSqlAsync(
                "INSERT INTO AnalysisSession (Id, CreatedAt, AnalysisType, FileCount, OverallComplexityScore, OverallRiskLevel, SchemaVersion, UserFriendlyName, SeverityLevel, ResultJson, BusinessMetricsJson) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                new object[] { id, createdAt, at, result.TotalFiles, result.OverallComplexityScore, result.OverallRiskLevel ?? "", SchemaVersion, userFriendlyName ?? "", severityLevel ?? "", resultJson, businessMetricsJson ?? "" },
                cancellationToken);

            foreach (var file in result.FileResults ?? new List<FileAnalysisResult>())
            {
                var fileJson = JsonSerializer.Serialize(file, JsonOptions);
                await _interop.ExecuteSqlAsync(
                    "INSERT INTO AnalysisFileResult (SessionId, FileName, FileSize, ComplexityScore, Status, Hash, Ignored, Suppressed, ResultJson) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)",
                    new object[] { id, file.FileName ?? "", file.FileSize, file.ComplexityScore, file.Status ?? "", "", 0, 0, fileJson },
                    cancellationToken);
            }

            await _interop.SaveDatabaseAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<SavedAnalysisSession>> GetRecentSessionsAsync(int take, CancellationToken cancellationToken = default)
        {
            await _interop.InitializeSchemaAsync(cancellationToken);
            await _interop.LoadDatabaseAsync(cancellationToken);
            var rows = await _interop.QueryAsync<SessionRow>(
                "SELECT Id, CreatedAt, AnalysisType, FileCount, OverallComplexityScore, OverallRiskLevel, SchemaVersion, UserFriendlyName, SeverityLevel, ResultJson, BusinessMetricsJson FROM AnalysisSession ORDER BY CreatedAt DESC LIMIT ?",
                new object[] { take },
                cancellationToken);

            var list = new List<SavedAnalysisSession>();
            foreach (var row in rows)
            {
                var session = await RowToSavedSessionAsync(row, cancellationToken);
                if (session != null)
                    list.Add(session);
            }
            return list;
        }

        public async Task<SavedAnalysisSession?> GetSessionByIdAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            await _interop.InitializeSchemaAsync(cancellationToken);
            await _interop.LoadDatabaseAsync(cancellationToken);
            var rows = await _interop.QueryAsync<SessionRow>(
                "SELECT Id, CreatedAt, AnalysisType, FileCount, OverallComplexityScore, OverallRiskLevel, SchemaVersion, UserFriendlyName, SeverityLevel, ResultJson, BusinessMetricsJson FROM AnalysisSession WHERE Id = ?",
                new object[] { sessionId },
                cancellationToken);
            var row = rows.FirstOrDefault();
            return row != null ? await RowToSavedSessionAsync(row, cancellationToken) : null;
        }

        public async Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            await _interop.InitializeSchemaAsync(cancellationToken);
            await _interop.LoadDatabaseAsync(cancellationToken);
            await _interop.ExecuteSqlAsync("DELETE FROM AnalysisFileResult WHERE SessionId = ?", new object[] { sessionId }, cancellationToken);
            await _interop.ExecuteSqlAsync("DELETE FROM AnalysisSession WHERE Id = ?", new object[] { sessionId }, cancellationToken);
            await _interop.SaveDatabaseAsync(cancellationToken);
        }

        public async Task PurgeSessionsOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
        {
            await _interop.InitializeSchemaAsync(cancellationToken);
            await _interop.LoadDatabaseAsync(cancellationToken);
            var cutoff = cutoffUtc.ToString("O");
            await _interop.ExecuteSqlAsync("DELETE FROM AnalysisFileResult WHERE SessionId IN (SELECT Id FROM AnalysisSession WHERE CreatedAt < ?)", new object[] { cutoff }, cancellationToken);
            await _interop.ExecuteSqlAsync("DELETE FROM AnalysisSession WHERE CreatedAt < ?", new object[] { cutoff }, cancellationToken);
            await _interop.SaveDatabaseAsync(cancellationToken);
        }

        private async Task<SavedAnalysisSession?> RowToSavedSessionAsync(SessionRow row, CancellationToken cancellationToken)
        {
            try
            {
                var result = string.IsNullOrEmpty(row.ResultJson)
                    ? new MultiFileAnalysisResult()
                    : JsonSerializer.Deserialize<MultiFileAnalysisResult>(row.ResultJson, JsonOptions) ?? new MultiFileAnalysisResult();

                var fileRows = await _interop.QueryAsync<FileResultRow>(
                    "SELECT ResultJson FROM AnalysisFileResult WHERE SessionId = ?",
                    new object[] { row.Id },
                    cancellationToken);
                var fileResults = new List<FileAnalysisResult>();
                foreach (var fr in fileRows)
                {
                    if (!string.IsNullOrEmpty(fr.ResultJson))
                    {
                        var fa = JsonSerializer.Deserialize<FileAnalysisResult>(fr.ResultJson, JsonOptions);
                        if (fa != null)
                            fileResults.Add(fa);
                    }
                }
                result.FileResults = fileResults;

                BusinessMetrics? businessMetrics = null;
                if (!string.IsNullOrEmpty(row.BusinessMetricsJson))
                    businessMetrics = JsonSerializer.Deserialize<BusinessMetrics>(row.BusinessMetricsJson, JsonOptions);

                DateTime.TryParse(row.CreatedAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var createdAt);

                return new SavedAnalysisSession
                {
                    Id = row.Id ?? "",
                    CreatedAt = createdAt,
                    AnalysisType = row.AnalysisType ?? "general",
                    UserFriendlyName = string.IsNullOrEmpty(row.UserFriendlyName) ? null : row.UserFriendlyName,
                    SeverityLevel = string.IsNullOrEmpty(row.SeverityLevel) ? null : row.SeverityLevel,
                    SchemaVersion = row.SchemaVersion,
                    Result = result,
                    BusinessMetrics = businessMetrics
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize session {Id}", row.Id);
                return null;
            }
        }

        private class SessionRow
        {
            public string? Id { get; set; }
            public string? CreatedAt { get; set; }
            public string? AnalysisType { get; set; }
            public int FileCount { get; set; }
            public int OverallComplexityScore { get; set; }
            public string? OverallRiskLevel { get; set; }
            public int SchemaVersion { get; set; }
            public string? UserFriendlyName { get; set; }
            public string? SeverityLevel { get; set; }
            public string? ResultJson { get; set; }
            public string? BusinessMetricsJson { get; set; }
        }

        private class FileResultRow
        {
            public string? ResultJson { get; set; }
        }
    }
}
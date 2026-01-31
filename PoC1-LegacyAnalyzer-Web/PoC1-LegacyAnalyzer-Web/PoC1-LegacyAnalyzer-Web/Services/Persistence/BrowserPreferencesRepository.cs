using System.Text.Json;
using Microsoft.Extensions.Logging;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.Persistence
{
    /// <summary>
    /// Persists and retrieves user preferences via browser SQLite (ISecureClientInterop).
    /// </summary>
    public class BrowserPreferencesRepository : IBrowserPreferencesRepository
    {
        private readonly ISecureClientInterop _interop;
        private readonly ILogger<BrowserPreferencesRepository> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };

        public BrowserPreferencesRepository(ISecureClientInterop interop, ILogger<BrowserPreferencesRepository> logger)
        {
            _interop = interop;
            _logger = logger;
        }

        public async Task<UserPreferences> GetAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _interop.InitializeSchemaAsync(cancellationToken);
                await _interop.LoadDatabaseAsync(cancellationToken);
                var rows = await _interop.QueryAsync<PreferenceRow>("SELECT Key, Value FROM UserPreference", null, cancellationToken);
                var prefs = new UserPreferences();
                foreach (var row in rows)
                {
                    if (row.Key == null || row.Value == null) continue;
                    if (row.Key == "LastAnalysisType") prefs.LastAnalysisType = row.Value;
                    else if (row.Key == "ComplexityLowThreshold" && int.TryParse(row.Value, out var low)) prefs.ComplexityLowThreshold = low;
                    else if (row.Key == "ComplexityMediumThreshold" && int.TryParse(row.Value, out var med)) prefs.ComplexityMediumThreshold = med;
                    else if (row.Key == "ComplexityHighThreshold" && int.TryParse(row.Value, out var high)) prefs.ComplexityHighThreshold = high;
                    else if (row.Key == "ClientPersistenceEnabled") prefs.ClientPersistenceEnabled = row.Value == "1" || string.Equals(row.Value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (!string.IsNullOrEmpty(row.Key)) prefs.CustomKeys[row.Key] = row.Value;
                }
                return prefs;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Get preferences failed, returning defaults");
                return new UserPreferences();
            }
        }

        public async Task SaveAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
        {
            await _interop.InitializeSchemaAsync(cancellationToken);
            await _interop.LoadDatabaseAsync(cancellationToken);
            await _interop.ExecuteSqlAsync("DELETE FROM UserPreference", null, cancellationToken);
            var dict = new Dictionary<string, string>
            {
                ["LastAnalysisType"] = preferences.LastAnalysisType,
                ["ComplexityLowThreshold"] = preferences.ComplexityLowThreshold?.ToString() ?? "",
                ["ComplexityMediumThreshold"] = preferences.ComplexityMediumThreshold?.ToString() ?? "",
                ["ComplexityHighThreshold"] = preferences.ComplexityHighThreshold?.ToString() ?? "",
                ["ClientPersistenceEnabled"] = preferences.ClientPersistenceEnabled ? "1" : "0"
            };
            foreach (var kv in preferences.CustomKeys)
                dict[kv.Key] = kv.Value;
            foreach (var kv in dict)
            {
                await _interop.ExecuteSqlAsync(
                    "INSERT INTO UserPreference (Key, Value) VALUES (?, ?)",
                    new object[] { kv.Key, kv.Value },
                    cancellationToken);
            }
            await _interop.SaveDatabaseAsync(cancellationToken);
        }

        private class PreferenceRow
        {
            public string? Key { get; set; }
            public string? Value { get; set; }
        }
    }
}
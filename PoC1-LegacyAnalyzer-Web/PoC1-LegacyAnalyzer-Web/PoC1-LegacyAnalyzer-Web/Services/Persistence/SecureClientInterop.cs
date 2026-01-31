using System.Text.Json;
using Microsoft.JSInterop;

namespace PoC1_LegacyAnalyzer_Web.Services.Persistence
{
    /// <summary>
    /// Thin wrapper over JS persistence/crypto APIs. Envelope-aware only; never handles raw key material.
    /// </summary>
    public class SecureClientInterop : ISecureClientInterop
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<SecureClientInterop> _logger;
        private const string ObjectName = "legacyAnalyzerStorage";

        public SecureClientInterop(IJSRuntime jsRuntime, ILogger<SecureClientInterop> logger)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
        }

        public async Task ExecuteSqlAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync($"{ObjectName}.executeSql", cancellationToken, sql, ParametersToJs(parameters));
            }
            catch (JSException ex)
            {
                _logger.LogWarning(ex, "ExecuteSql failed: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                var raw = await _jsRuntime.InvokeAsync<JsonElement>($"{ObjectName}.query", cancellationToken, sql, ParametersToJs(parameters));
                var list = new List<T>();
                if (raw.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in raw.EnumerateArray())
                    {
                        var obj = JsonSerializer.Deserialize<T>(item.GetRawText());
                        if (obj != null)
                            list.Add(obj);
                    }
                }
                return list;
            }
            catch (JSException ex)
            {
                _logger.LogWarning(ex, "Query failed: {Message}", ex.Message);
                throw;
            }
        }

        public async Task SaveDatabaseAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync($"{ObjectName}.saveDb", cancellationToken);
            }
            catch (JSException ex)
            {
                _logger.LogWarning(ex, "SaveDb failed: {Message}", ex.Message);
                throw;
            }
        }

        public async Task LoadDatabaseAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync($"{ObjectName}.loadDb", cancellationToken);
            }
            catch (JSException ex)
            {
                _logger.LogWarning(ex, "LoadDb failed: {Message}", ex.Message);
                throw;
            }
        }

        public async Task InitializeSchemaAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync($"{ObjectName}.initializeSchema", cancellationToken);
            }
            catch (JSException ex)
            {
                _logger.LogWarning(ex, "InitializeSchema failed: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _jsRuntime.InvokeAsync<bool>($"{ObjectName}.isAvailable", cancellationToken);
            }
            catch
            {
                return false;
            }
        }

        public async Task<ExportBundle> ExportAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var raw = await _jsRuntime.InvokeAsync<JsonElement>($"{ObjectName}.exportBundle", cancellationToken);
                var payload = raw.GetProperty("encryptedDbPayload");
                var bytes = new List<byte>();
                foreach (var e in payload.EnumerateArray())
                    bytes.Add((byte)e.GetInt32());
                return new ExportBundle
                {
                    EncryptedDbPayload = bytes.ToArray(),
                    KeyEnvelopeJson = raw.GetProperty("keyEnvelopeJson").GetString() ?? "{}",
                    MetadataJson = raw.GetProperty("metadataJson").GetString() ?? "{}"
                };
            }
            catch (JSException ex)
            {
                _logger.LogWarning(ex, "Export failed: {Message}", ex.Message);
                throw;
            }
        }

        public async Task ImportAsync(ExportBundle bundle, CancellationToken cancellationToken = default)
        {
            try
            {
                var obj = new { encryptedDbPayload = bundle.EncryptedDbPayload, keyEnvelopeJson = bundle.KeyEnvelopeJson, metadataJson = bundle.MetadataJson };
                await _jsRuntime.InvokeVoidAsync($"{ObjectName}.importBundle", cancellationToken, obj);
            }
            catch (JSException ex)
            {
                _logger.LogWarning(ex, "Import failed: {Message}", ex.Message);
                throw;
            }
        }

        private static object? ParametersToJs(object? parameters)
        {
            if (parameters == null) return null;
            if (parameters is IEnumerable<object> arr) return arr;
            if (parameters is IDictionary<string, object?> dict) return dict.Values.ToArray();
            return parameters;
        }
    }
}

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Service for robustly extracting JSON from LLM responses that may contain markdown fences,
    /// explanatory text, or other non-JSON content.
    /// </summary>
    public interface IRobustJsonExtractor
    {
        /// <summary>
        /// Attempts to extract and parse JSON from LLM response using multiple fallback strategies.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="rawResponse">The raw LLM response that may contain JSON</param>
        /// <param name="options">Optional JSON serializer options</param>
        /// <returns>The deserialized object, or null if all strategies fail</returns>
        T? ExtractAndParse<T>(string rawResponse, System.Text.Json.JsonSerializerOptions? options = null) where T : class;

        /// <summary>
        /// Extracts the JSON string from raw response using multiple strategies.
        /// </summary>
        /// <param name="rawResponse">The raw LLM response</param>
        /// <returns>The extracted JSON string, or null if extraction fails</returns>
        string? ExtractJsonString(string rawResponse);
    }
}

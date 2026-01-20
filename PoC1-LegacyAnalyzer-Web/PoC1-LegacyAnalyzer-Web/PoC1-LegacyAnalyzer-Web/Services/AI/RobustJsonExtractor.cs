using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PoC1_LegacyAnalyzer_Web.Services.AI
{
    /// <summary>
    /// Robust JSON extractor with multiple fallback strategies to handle LLM responses
    /// that may contain markdown fences, explanatory text, or malformed JSON.
    /// </summary>
    public class RobustJsonExtractor : IRobustJsonExtractor
    {
        private readonly ILogger<RobustJsonExtractor> _logger;

        public RobustJsonExtractor(ILogger<RobustJsonExtractor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public T? ExtractAndParse<T>(string rawResponse, JsonSerializerOptions? options = null) where T : class
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                _logger.LogWarning("Empty or null raw response provided");
                return null;
            }

            // Use default options if none provided
            var jsonOptions = options ?? new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            // Strategy 1: Direct parse (fastest path)
            var result = TryDirectParse<T>(rawResponse, jsonOptions);
            if (result != null)
            {
                _logger.LogDebug("JSON extracted using Strategy 1: Direct parse");
                return result;
            }

            // Strategy 2: Extract from markdown code fences
            var jsonFromMarkdown = ExtractFromMarkdownFences(rawResponse);
            if (!string.IsNullOrEmpty(jsonFromMarkdown))
            {
                result = TryParseJson<T>(jsonFromMarkdown, jsonOptions);
                if (result != null)
                {
                    _logger.LogDebug("JSON extracted using Strategy 2: Markdown fences");
                    return result;
                }
            }

            // Strategy 3: Regex pattern matching for largest JSON object
            var jsonFromRegex = ExtractLargestJsonObject(rawResponse);
            if (!string.IsNullOrEmpty(jsonFromRegex))
            {
                result = TryParseJson<T>(jsonFromRegex, jsonOptions);
                if (result != null)
                {
                    _logger.LogDebug("JSON extracted using Strategy 3: Regex pattern matching");
                    return result;
                }
            }

            // Strategy 4: Clean and retry with aggressive cleaning
            var cleanedJson = AggressiveClean(rawResponse);
            if (!string.IsNullOrEmpty(cleanedJson))
            {
                result = TryParseJson<T>(cleanedJson, jsonOptions);
                if (result != null)
                {
                    _logger.LogDebug("JSON extracted using Strategy 4: Aggressive cleaning");
                    return result;
                }
            }

            _logger.LogWarning("All JSON extraction strategies failed for response (length: {Length})", rawResponse.Length);
            return null;
        }

        public string? ExtractJsonString(string rawResponse)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
                return null;

            // Try strategies in order
            if (IsValidJson(rawResponse))
                return rawResponse;

            var jsonFromMarkdown = ExtractFromMarkdownFences(rawResponse);
            if (!string.IsNullOrEmpty(jsonFromMarkdown) && IsValidJson(jsonFromMarkdown))
                return jsonFromMarkdown;

            var jsonFromRegex = ExtractLargestJsonObject(rawResponse);
            if (!string.IsNullOrEmpty(jsonFromRegex) && IsValidJson(jsonFromRegex))
                return jsonFromRegex;

            var cleanedJson = AggressiveClean(rawResponse);
            if (!string.IsNullOrEmpty(cleanedJson) && IsValidJson(cleanedJson))
                return cleanedJson;

            return null;
        }

        #region Strategy Implementations

        /// <summary>
        /// Strategy 1: Try direct parse without any preprocessing
        /// </summary>
        private T? TryDirectParse<T>(string rawResponse, JsonSerializerOptions options) where T : class
        {
            try
            {
                return JsonSerializer.Deserialize<T>(rawResponse, options);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Strategy 2: Extract JSON from markdown code fences (```json ... ``` or ``` ... ```)
        /// </summary>
        private string? ExtractFromMarkdownFences(string rawResponse)
        {
            // Pattern 1: ```json ... ```
            var jsonFencePattern = new Regex(@"```\s*json\s*\n?(.*?)```", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var jsonMatch = jsonFencePattern.Match(rawResponse);
            if (jsonMatch.Success && jsonMatch.Groups.Count > 1)
            {
                var extracted = jsonMatch.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(extracted))
                    return extracted;
            }

            // Pattern 2: ``` ... ``` (generic code fence)
            var genericFencePattern = new Regex(@"```\s*\w*\s*\n?(.*?)```", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var genericMatch = genericFencePattern.Match(rawResponse);
            if (genericMatch.Success && genericMatch.Groups.Count > 1)
            {
                var extracted = genericMatch.Groups[1].Value.Trim();
                // Check if it looks like JSON (starts with { or [)
                if (extracted.TrimStart().StartsWith("{") || extracted.TrimStart().StartsWith("["))
                    return extracted;
            }

            // Pattern 3: Look for JSON between any code fences
            var anyFencePattern = new Regex(@"```[^`]*```", RegexOptions.Singleline);
            var matches = anyFencePattern.Matches(rawResponse);
            foreach (Match match in matches)
            {
                var content = match.Value;
                // Remove the fences
                content = Regex.Replace(content, @"^```\s*\w*\s*", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                content = Regex.Replace(content, @"```\s*$", "", RegexOptions.Multiline);
                content = content.Trim();
                
                if ((content.StartsWith("{") || content.StartsWith("[")) && IsValidJson(content))
                    return content;
            }

            return null;
        }

        /// <summary>
        /// Strategy 3: Use balanced brace matching to find the largest valid JSON object
        /// </summary>
        private string? ExtractLargestJsonObject(string rawResponse)
        {
            var jsonObjects = new List<string>();

            // Find JSON objects by matching balanced braces (handles nested objects)
            var objectCandidates = ExtractBalancedBraces(rawResponse, '{', '}');
            foreach (var candidate in objectCandidates)
            {
                if (IsValidJson(candidate))
                {
                    jsonObjects.Add(candidate);
                }
            }

            // Find JSON arrays by matching balanced brackets
            var arrayCandidates = ExtractBalancedBraces(rawResponse, '[', ']');
            foreach (var candidate in arrayCandidates)
            {
                if (IsValidJson(candidate))
                {
                    jsonObjects.Add(candidate);
                }
            }

            // Return the largest valid JSON object
            if (jsonObjects.Any())
            {
                return jsonObjects.OrderByDescending(j => j.Length).First();
            }

            // Fallback: Find JSON between first { and last } (simple approach)
            var firstBrace = rawResponse.IndexOf('{');
            var lastBrace = rawResponse.LastIndexOf('}');
            
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                var candidate = rawResponse.Substring(firstBrace, lastBrace - firstBrace + 1);
                if (IsValidJson(candidate))
                    return candidate;
            }

            // Also try arrays
            var firstBracket = rawResponse.IndexOf('[');
            var lastBracket = rawResponse.LastIndexOf(']');
            
            if (firstBracket >= 0 && lastBracket > firstBracket)
            {
                var candidate = rawResponse.Substring(firstBracket, lastBracket - firstBracket + 1);
                if (IsValidJson(candidate))
                    return candidate;
            }

            return null;
        }

        /// <summary>
        /// Extracts strings with balanced opening and closing braces/brackets
        /// </summary>
        private List<string> ExtractBalancedBraces(string text, char openChar, char closeChar)
        {
            var results = new List<string>();
            var startIndices = new Stack<int>();
            var inString = false;
            var escapeNext = false;

            for (int i = 0; i < text.Length; i++)
            {
                var ch = text[i];

                if (escapeNext)
                {
                    escapeNext = false;
                    continue;
                }

                if (ch == '\\')
                {
                    escapeNext = true;
                    continue;
                }

                if (ch == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (inString)
                    continue;

                if (ch == openChar)
                {
                    startIndices.Push(i);
                }
                else if (ch == closeChar && startIndices.Count > 0)
                {
                    var start = startIndices.Pop();
                    if (startIndices.Count == 0) // Only extract top-level objects
                    {
                        var length = i - start + 1;
                        if (length > 0)
                        {
                            results.Add(text.Substring(start, length));
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Strategy 4: Aggressive cleaning with multiple passes
        /// </summary>
        private string AggressiveClean(string rawResponse)
        {
            var cleaned = rawResponse.Trim();

            // Step 1: Remove markdown fences
            cleaned = Regex.Replace(cleaned, @"^```\s*\w*\s*", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            cleaned = Regex.Replace(cleaned, @"```\s*$", "", RegexOptions.Multiline);

            // Step 2: Remove leading/trailing explanatory text
            var firstBrace = cleaned.IndexOf('{');
            var firstBracket = cleaned.IndexOf('[');
            var startIndex = firstBrace >= 0 && (firstBracket < 0 || firstBrace < firstBracket) 
                ? firstBrace 
                : (firstBracket >= 0 ? firstBracket : -1);

            if (startIndex > 0)
            {
                cleaned = cleaned.Substring(startIndex);
            }

            var lastBrace = cleaned.LastIndexOf('}');
            var lastBracket = cleaned.LastIndexOf(']');
            var endIndex = lastBrace >= 0 && (lastBracket < 0 || lastBrace > lastBracket)
                ? lastBrace + 1
                : (lastBracket >= 0 ? lastBracket + 1 : cleaned.Length);

            if (endIndex < cleaned.Length)
            {
                cleaned = cleaned.Substring(0, endIndex);
            }

            // Step 3: Remove trailing commas
            cleaned = Regex.Replace(cleaned, @",(\s*[\]}])", "$1");

            // Step 4: Fix common quote issues
            cleaned = cleaned.Replace("'", "\""); // Replace single quotes with double quotes (but be careful)
            // Only replace single quotes that are around property names or string values
            cleaned = Regex.Replace(cleaned, @"'([^']*)'", "\"$1\"", RegexOptions.Multiline);

            // Step 5: Remove comments (both // and /* */ style)
            cleaned = Regex.Replace(cleaned, @"//.*?$", "", RegexOptions.Multiline);
            cleaned = Regex.Replace(cleaned, @"/\*.*?\*/", "", RegexOptions.Singleline);

            // Step 6: Fix unescaped newlines in strings (common LLM mistake)
            // This is tricky - we'll be conservative and only fix obvious cases
            cleaned = Regex.Replace(cleaned, @":\s*""([^""]*)\n([^""]*)""", ":\"$1\\n$2\"", RegexOptions.Multiline);

            // Step 7: Remove any remaining non-JSON characters at start/end
            cleaned = cleaned.Trim();
            while (cleaned.Length > 0 && !cleaned.StartsWith("{") && !cleaned.StartsWith("["))
            {
                cleaned = cleaned.Substring(1);
            }
            while (cleaned.Length > 0 && !cleaned.EndsWith("}") && !cleaned.EndsWith("]"))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - 1);
            }

            return cleaned;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Attempts to parse JSON string to the specified type
        /// </summary>
        private T? TryParseJson<T>(string jsonString, JsonSerializerOptions options) where T : class
        {
            if (string.IsNullOrWhiteSpace(jsonString))
                return null;

            try
            {
                return JsonSerializer.Deserialize<T>(jsonString, options);
            }
            catch (JsonException ex)
            {
                _logger.LogDebug("JSON parsing failed: {Error}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Checks if a string is valid JSON by attempting to parse it
        /// </summary>
        private bool IsValidJson(string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                return false;

            candidate = candidate.Trim();
            if (!candidate.StartsWith("{") && !candidate.StartsWith("["))
                return false;

            try
            {
                using var doc = JsonDocument.Parse(candidate);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        #endregion
    }
}

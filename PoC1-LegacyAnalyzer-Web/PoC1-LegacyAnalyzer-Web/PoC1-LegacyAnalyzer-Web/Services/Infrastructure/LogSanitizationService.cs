using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PoC1_LegacyAnalyzer_Web.Models;
using System.Text.RegularExpressions;

namespace PoC1_LegacyAnalyzer_Web.Services.Infrastructure
{
    /// <summary>
    /// Service for sanitizing log messages to prevent sensitive data exposure.
    /// </summary>
    public interface ILogSanitizationService
    {
        /// <summary>
        /// Sanitizes a log message by redacting sensitive patterns and truncating long content.
        /// </summary>
        string SanitizeMessage(string message);

        /// <summary>
        /// Sanitizes an exception message and stack trace.
        /// </summary>
        string SanitizeException(Exception exception);

        /// <summary>
        /// Sanitizes a dictionary of log properties.
        /// </summary>
        Dictionary<string, object?> SanitizeProperties(Dictionary<string, object?> properties);

        /// <summary>
        /// Checks if a field name should be redacted.
        /// </summary>
        bool ShouldRedactField(string fieldName);
    }

    /// <summary>
    /// Implementation of log sanitization service.
    /// </summary>
    public class LogSanitizationService : ILogSanitizationService
    {
        private readonly LogSanitizationConfiguration _config;
        private readonly ILogger<LogSanitizationService> _logger;
        private readonly List<Regex> _redactionRegexes;

        public LogSanitizationService(
            IOptions<LogSanitizationConfiguration> config,
            ILogger<LogSanitizationService> logger)
        {
            _config = config?.Value ?? new LogSanitizationConfiguration();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Compile regex patterns for performance
            _redactionRegexes = _config.RedactionPatterns
                .Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled))
                .ToList();
        }

        public string SanitizeMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return message;

            if (!_config.Enabled)
                return message;

            var sanitized = message;

            // Apply pattern-based redaction
            foreach (var regex in _redactionRegexes)
            {
                sanitized = regex.Replace(sanitized, match =>
                {
                    // Replace the value part (group 2) with redaction marker
                    if (match.Groups.Count > 2)
                    {
                        return match.Groups[1].Value + ": " + _config.RedactionReplacement;
                    }
                    return _config.RedactionReplacement;
                });
            }

            // Truncate if too long
            if (sanitized.Length > _config.MaxLogMessageLength)
            {
                sanitized = sanitized.Substring(0, _config.MaxLogMessageLength) + "... [TRUNCATED]";
            }

            return sanitized;
        }

        public string SanitizeException(Exception exception)
        {
            if (exception == null)
                return string.Empty;

            if (!_config.Enabled)
                return exception.ToString();

            var message = exception.Message;
            var stackTrace = exception.StackTrace ?? string.Empty;

            // Sanitize exception message
            message = SanitizeMessage(message);

            // Sanitize stack trace (remove file paths and line numbers if configured)
            if (_config.RedactStackTrace)
            {
                stackTrace = "[STACK TRACE REDACTED]";
            }
            else
            {
                // Still sanitize sensitive patterns in stack trace
                stackTrace = SanitizeMessage(stackTrace);
            }

            var result = $"{exception.GetType().Name}: {message}";
            if (!string.IsNullOrWhiteSpace(stackTrace) && !_config.RedactStackTrace)
            {
                result += $"\n{stackTrace}";
            }

            // Handle inner exceptions
            if (exception.InnerException != null)
            {
                result += $"\nInner Exception: {SanitizeException(exception.InnerException)}";
            }

            return result;
        }

        public Dictionary<string, object?> SanitizeProperties(Dictionary<string, object?> properties)
        {
            if (properties == null || !_config.Enabled)
                return properties ?? new Dictionary<string, object?>();

            var sanitized = new Dictionary<string, object?>();

            foreach (var kvp in properties)
            {
                var key = kvp.Key;
                var value = kvp.Value;

                // Skip forbidden fields entirely
                if (ShouldRedactField(key))
                {
                    sanitized[key] = _config.RedactionReplacement;
                    continue;
                }

                // Sanitize string values
                if (value is string stringValue)
                {
                    // Check if it looks like code content
                    if (_config.RedactCodeSnippets && LooksLikeCode(stringValue))
                    {
                        sanitized[key] = TruncateCodeSnippet(stringValue);
                    }
                    else if (_config.RedactFileContents && LooksLikeFileContent(stringValue))
                    {
                        sanitized[key] = TruncateCodeSnippet(stringValue);
                    }
                    else
                    {
                        sanitized[key] = SanitizeMessage(stringValue);
                    }
                }
                // Sanitize exception values
                else if (value is Exception ex)
                {
                    sanitized[key] = SanitizeException(ex);
                }
                // Keep other types as-is (numbers, booleans, etc.)
                else
                {
                    sanitized[key] = value;
                }
            }

            return sanitized;
        }

        public bool ShouldRedactField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                return false;

            var lowerFieldName = fieldName.ToLowerInvariant();
            return _config.ForbiddenFields.Any(forbidden =>
                lowerFieldName.Contains(forbidden, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if a string looks like source code.
        /// </summary>
        private bool LooksLikeCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 20)
                return false;

            // Check for code-like patterns
            var codeIndicators = new[]
            {
                "{", "}", "(", ")", ";", "=", "var ", "function ", "class ", "public ", "private ",
                "using ", "import ", "def ", "return ", "if ", "for ", "while "
            };

            var codeIndicatorCount = codeIndicators.Count(indicator =>
                value.Contains(indicator, StringComparison.OrdinalIgnoreCase));

            // If it has multiple code indicators, it's likely code
            return codeIndicatorCount >= 3;
        }

        /// <summary>
        /// Checks if a string looks like file content.
        /// </summary>
        private bool LooksLikeFileContent(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 50)
                return false;

            // File content is usually longer and may contain newlines
            return value.Length > 200 || value.Contains('\n') || value.Contains('\r');
        }

        /// <summary>
        /// Truncates a code snippet to the maximum allowed length.
        /// </summary>
        private string TruncateCodeSnippet(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return code;

            if (code.Length <= _config.MaxCodeSnippetLength)
                return code;

            var truncated = code.Substring(0, _config.MaxCodeSnippetLength);
            return $"{truncated}... [CODE REDACTED - {code.Length} chars total]";
        }
    }
}


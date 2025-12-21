namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Configuration for log sanitization to prevent sensitive data exposure.
    /// </summary>
    public class LogSanitizationConfiguration
    {
        /// <summary>
        /// Enable or disable log sanitization.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Maximum length of code snippets to log. Longer snippets will be truncated.
        /// </summary>
        public int MaxCodeSnippetLength { get; set; } = 100;

        /// <summary>
        /// Patterns to redact from logs (e.g., API keys, passwords, tokens).
        /// </summary>
        public List<string> RedactionPatterns { get; set; } = new()
        {
            @"(api[_-]?key|apikey)\s*[:=]\s*[""']?([a-zA-Z0-9\-_]{20,})[""']?",
            @"(password|pwd|pass)\s*[:=]\s*[""']?([^""'\s]{3,})[""']?",
            @"(token|secret|secretkey)\s*[:=]\s*[""']?([a-zA-Z0-9\-_]{20,})[""']?",
            @"(connectionstring|connstr)\s*[:=]\s*[""']?([^""']{20,})[""']?",
            @"(authorization|bearer)\s+([a-zA-Z0-9\-_\.]{20,})",
            @"(x-api-key|x-auth-token)\s*[:=]\s*[""']?([a-zA-Z0-9\-_]{20,})[""']?"
        };

        /// <summary>
        /// Replacement text for redacted patterns.
        /// </summary>
        public string RedactionReplacement { get; set; } = "[REDACTED]";

        /// <summary>
        /// Whether to redact file contents from logs.
        /// </summary>
        public bool RedactFileContents { get; set; } = true;

        /// <summary>
        /// Whether to redact code snippets from logs.
        /// </summary>
        public bool RedactCodeSnippets { get; set; } = true;

        /// <summary>
        /// Whether to redact exception stack traces (can contain file paths and code).
        /// </summary>
        public bool RedactStackTrace { get; set; } = false; // Keep false for debugging, but sanitize sensitive parts

        /// <summary>
        /// Maximum length of any log message. Longer messages will be truncated.
        /// </summary>
        public int MaxLogMessageLength { get; set; } = 5000;

        /// <summary>
        /// Fields that should never be logged (will be completely removed).
        /// </summary>
        public List<string> ForbiddenFields { get; set; } = new()
        {
            "code",
            "codeContent",
            "fileContent",
            "fullCode",
            "sourceCode",
            "apiKey",
            "password",
            "token",
            "secret",
            "connectionString"
        };
    }
}


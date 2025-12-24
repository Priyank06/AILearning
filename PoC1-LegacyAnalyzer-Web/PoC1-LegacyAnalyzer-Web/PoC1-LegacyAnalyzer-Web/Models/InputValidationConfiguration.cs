namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Configuration for input validation and sanitization.
    /// </summary>
    public class InputValidationConfiguration
    {
        /// <summary>
        /// Enable or disable input validation.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Maximum length for business objective text.
        /// </summary>
        public int MaxBusinessObjectiveLength { get; set; } = 2000;

        /// <summary>
        /// Maximum length for custom objective text.
        /// </summary>
        public int MaxCustomObjectiveLength { get; set; } = 5000;

        /// <summary>
        /// Characters that are not allowed in business objectives (to prevent prompt injection).
        /// </summary>
        public List<string> ForbiddenCharacters { get; set; } = new() { "\0", "\r\n", "\n\r" };

        /// <summary>
        /// Patterns that indicate potential prompt injection attempts.
        /// </summary>
        public List<string> PromptInjectionPatterns { get; set; } = new()
        {
            @"ignore\s+(previous|above|all)\s+instructions",
            @"forget\s+(everything|all|previous)",
            @"you\s+are\s+now",
            @"system\s*[:=]\s*",
            @"assistant\s*[:=]\s*",
            @"user\s*[:=]\s*",
            @"<\|(system|user|assistant)\|>",
            @"\[INST\]",
            @"\[/INST\]"
        };

        /// <summary>
        /// Maximum file size in bytes.
        /// </summary>
        public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024; // 5MB

        /// <summary>
        /// [DEPRECATED] Maximum number of files allowed per analysis.
        /// NOTE: This limit is no longer enforced. Batching and preprocessing naturally handle any number of files.
        /// Files are processed in batches of 10, and preprocessing filters by priority, so there's no need for hard limits.
        /// Kept for backward compatibility only.
        /// </summary>
        [Obsolete("File count limits are no longer enforced. Batching handles any number of files.")]
        public int MaxFilesPerAnalysis { get; set; } = 50;

        /// <summary>
        /// Allowed file extensions (case-insensitive).
        /// </summary>
        public List<string> AllowedFileExtensions { get; set; } = new()
        {
            ".cs", ".py", ".js", ".ts", ".java", ".go", ".vb", ".fs", ".cpp", ".h"
        };

        /// <summary>
        /// File signatures (magic numbers) for validation.
        /// Key: extension, Value: list of allowed signatures (hex bytes).
        /// </summary>
        public Dictionary<string, List<string>> FileSignatures { get; set; } = new()
        {
            // C# files are plain text, no specific signature
            // Python files are plain text
            // JavaScript/TypeScript are plain text
            // Java files are plain text
            // Go files are plain text
            // Binary files would have signatures here
        };

        /// <summary>
        /// Whether to validate file signatures (magic numbers).
        /// </summary>
        public bool ValidateFileSignatures { get; set; } = false; // Disabled for text files

        /// <summary>
        /// Whether to scan for suspicious patterns in file content.
        /// </summary>
        public bool ScanForSuspiciousPatterns { get; set; } = true;

        /// <summary>
        /// Patterns that indicate potentially malicious or suspicious file content.
        /// </summary>
        public List<string> SuspiciousPatterns { get; set; } = new()
        {
            @"eval\s*\(",
            @"exec\s*\(",
            @"system\s*\(",
            @"shell_exec\s*\(",
            @"base64_decode\s*\(",
            @"gzinflate\s*\(",
            @"str_rot13\s*\(",
            @"<script[^>]*>",
            @"javascript:",
            @"onerror\s*=",
            @"onload\s*="
        };

        /// <summary>
        /// Maximum allowed file name length.
        /// </summary>
        public int MaxFileNameLength { get; set; } = 255;

        /// <summary>
        /// Characters not allowed in file names.
        /// </summary>
        public List<char> ForbiddenFileNameCharacters { get; set; } = new()
        {
            '<', '>', ':', '"', '/', '\\', '|', '?', '*', '\0'
        };
    }
}


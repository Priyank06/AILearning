namespace PoC1_LegacyAnalyzer_Web.Models
{
    public class CodeAnalysisResult
    {
        /// <summary>
        /// Strongly-typed language kind for this analysis summary.
        /// </summary>
        public LanguageKind LanguageKind { get; set; } = LanguageKind.Unknown;

        /// <summary>
        /// String language identifier (e.g. "csharp", "python") for backward compatibility.
        /// </summary>
        public string Language { get; set; } = string.Empty;

        public int ClassCount { get; set; }
        public int MethodCount { get; set; }
        public int PropertyCount { get; set; }
        public int UsingCount { get; set; }
        public List<string> Classes { get; set; } = new List<string>();
        public List<string> Methods { get; set; } = new List<string>();
        public List<string> UsingStatements { get; set; } = new List<string>();
    }
}

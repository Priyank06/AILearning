using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.Analysis
{
    /// <summary>
    /// Represents a single source file to be analyzed in a language-agnostic way.
    /// </summary>
    public class AnalyzableFile
    {
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public LanguageKind Language { get; set; } = LanguageKind.Unknown;
    }
}



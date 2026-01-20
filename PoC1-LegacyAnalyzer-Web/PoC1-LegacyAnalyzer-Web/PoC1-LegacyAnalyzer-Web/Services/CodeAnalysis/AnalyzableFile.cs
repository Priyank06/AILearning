using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.CodeAnalysis
{
    /// <summary>
    /// Represents a file that can be analyzed, with its content and metadata.
    /// </summary>
    public class AnalyzableFile
    {
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public LanguageKind Language { get; set; } = LanguageKind.Unknown;
        public long Size { get; set; }
        public string RelativePath { get; set; } = string.Empty;
    }
}


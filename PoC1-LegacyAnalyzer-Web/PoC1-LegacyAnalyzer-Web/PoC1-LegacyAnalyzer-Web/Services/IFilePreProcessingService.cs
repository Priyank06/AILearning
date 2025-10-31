using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Pre-processing service for extracting metadata and patterns from code files
    /// before sending to AI agents. Reduces token usage by 75-80%.
    /// </summary>
    public interface IFilePreProcessingService
    {
        /// <summary>
        /// Extract metadata from a single file using static analysis (no AI cost)
        /// </summary>
        Task<FileMetadata> ExtractMetadataAsync(IBrowserFile file, string languageHint = "csharp");

        /// <summary>
        /// Create a consolidated project summary from multiple file metadatas
        /// </summary>
        Task<ProjectSummary> CreateProjectSummaryAsync(List<FileMetadata> fileMetadatas);

        /// <summary>
        /// Detect common code patterns and anti-patterns without AI
        /// </summary>
        CodePatternAnalysis DetectPatterns(string code, string language);

        /// <summary>
        /// Calculate code complexity metrics locally
        /// </summary>
        ComplexityMetrics CalculateComplexity(string code, string language);

        /// <summary>
        /// Get supported languages for preprocessing
        /// </summary>
        List<string> GetSupportedLanguages();
    }
}
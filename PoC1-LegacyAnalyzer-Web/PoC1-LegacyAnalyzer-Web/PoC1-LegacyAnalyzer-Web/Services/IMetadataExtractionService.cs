using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Service for extracting metadata from code files using local Roslyn static analysis.
    /// </summary>
    public interface IMetadataExtractionService
    {
        /// <summary>
        /// Extracts rich metadata from a single code file using local Roslyn static analysis.
        /// </summary>
        /// <param name="file">The uploaded code file to analyze.</param>
        /// <param name="languageHint">Optional language hint (default: "csharp").</param>
        /// <returns>A <see cref="FileMetadata"/> object containing extracted metadata.</returns>
        Task<FileMetadata> ExtractMetadataAsync(IBrowserFile file, string languageHint = "csharp");

        /// <summary>
        /// Extracts metadata from multiple code files in parallel with configurable concurrency limit.
        /// </summary>
        /// <param name="files">A list of uploaded code files to analyze.</param>
        /// <param name="languageHint">Optional language hint (default: "csharp").</param>
        /// <param name="maxConcurrency">Maximum number of files to process concurrently (default: 5).</param>
        /// <returns>A list of <see cref="FileMetadata"/> objects.</returns>
        Task<List<FileMetadata>> ExtractMetadataParallelAsync(List<IBrowserFile> files, string languageHint = "csharp", int maxConcurrency = 5);
    }
}


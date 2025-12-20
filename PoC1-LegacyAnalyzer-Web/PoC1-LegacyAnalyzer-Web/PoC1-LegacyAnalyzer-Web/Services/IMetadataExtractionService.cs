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
        /// Extracts rich metadata from a single code file using unified analyzers (Roslyn for C#, TreeSitter for others).
        /// Language is auto-detected if languageHint is null or empty.
        /// </summary>
        /// <param name="file">The uploaded code file to analyze.</param>
        /// <param name="languageHint">Optional language hint. If null/empty, language is auto-detected from file extension.</param>
        /// <returns>A <see cref="FileMetadata"/> object containing extracted metadata.</returns>
        Task<FileMetadata> ExtractMetadataAsync(IBrowserFile file, string? languageHint = null);

        /// <summary>
        /// Extracts metadata from multiple code files in parallel with configurable concurrency limit.
        /// Language is auto-detected for each file if languageHint is null or empty.
        /// </summary>
        /// <param name="files">A list of uploaded code files to analyze.</param>
        /// <param name="languageHint">Optional language hint. If null/empty, language is auto-detected per file.</param>
        /// <param name="maxConcurrency">Maximum number of files to process concurrently (default: 5).</param>
        /// <returns>A list of <see cref="FileMetadata"/> objects.</returns>
        Task<List<FileMetadata>> ExtractMetadataParallelAsync(List<IBrowserFile> files, string? languageHint = null, int maxConcurrency = 5);
    }
}


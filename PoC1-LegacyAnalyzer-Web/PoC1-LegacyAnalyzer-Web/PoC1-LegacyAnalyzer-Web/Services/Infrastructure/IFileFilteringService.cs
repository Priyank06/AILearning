using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.Infrastructure
{
    /// <summary>
    /// Service for filtering file metadata based on various criteria.
    /// </summary>
    public interface IFileFilteringService
    {
        /// <summary>
        /// Filters file metadata to only those files with security risks.
        /// </summary>
        /// <param name="metadatas">List of <see cref="FileMetadata"/> objects to filter.</param>
        /// <returns>A new list containing only files with security findings.</returns>
        Task<List<FileMetadata>> FilterBySecurityRisks(List<FileMetadata> metadatas);

        /// <summary>
        /// Filters file metadata to only those files meeting complexity threshold.
        /// </summary>
        /// <param name="metadatas">List of <see cref="FileMetadata"/> objects to filter.</param>
        /// <param name="minComplexity">Minimum cyclomatic complexity threshold (default: 15).</param>
        /// <returns>A new list containing only files meeting the complexity threshold.</returns>
        Task<List<FileMetadata>> FilterByComplexity(List<FileMetadata> metadatas, int minComplexity = 15);

        /// <summary>
        /// Filters file metadata to only those files with performance issues.
        /// </summary>
        /// <param name="metadatas">List of <see cref="FileMetadata"/> objects to filter.</param>
        /// <returns>A new list containing only files with performance findings.</returns>
        Task<List<FileMetadata>> FilterByPerformanceIssues(List<FileMetadata> metadatas);
    }
}


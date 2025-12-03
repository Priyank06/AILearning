using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Service for filtering file metadata based on various criteria.
    /// </summary>
    public class FileFilteringService : IFileFilteringService
    {
        private readonly ILogger<FileFilteringService> _logger;
        private readonly FilePreProcessingOptions _options;

        public FileFilteringService(
            ILogger<FileFilteringService> logger,
            IOptions<FilePreProcessingOptions> options)
        {
            _logger = logger;
            _options = options?.Value ?? new FilePreProcessingOptions();
        }

        public async Task<List<FileMetadata>> FilterBySecurityRisks(List<FileMetadata> metadatas)
        {
            var totalCount = metadatas?.Count ?? 0;
            var filtered = metadatas.Where(m => m?.Patterns?.SecurityFindings != null && m.Patterns.SecurityFindings.Count > 0).ToList();
            var filteredCount = filtered.Count;
            var reductionPercentage = totalCount > 0 ? (double)(totalCount - filteredCount) / totalCount * 100.0 : 0.0;

            _logger?.LogInformation(
                "Filtered by security risks: {TotalBefore} files -> {FilteredAfter} files (Reduction: {ReductionPercentage:F1}%, Kept: {KeptPercentage:F1}%)",
                totalCount, filteredCount, reductionPercentage, 100.0 - reductionPercentage);

            return await Task.FromResult(filtered);
        }

        public async Task<List<FileMetadata>> FilterByComplexity(List<FileMetadata> metadatas, int minComplexity = 15)
        {
            // Use configured MinComplexityThreshold if parameter uses default, otherwise use provided value
            var effectiveThreshold = minComplexity == 15 ? _options.MinComplexityThreshold : minComplexity;

            var totalCount = metadatas?.Count ?? 0;
            var filtered = metadatas.Where(m => m?.Complexity != null && m.Complexity.CyclomaticComplexity >= effectiveThreshold).ToList();
            var filteredCount = filtered.Count;
            var reductionPercentage = totalCount > 0 ? (double)(totalCount - filteredCount) / totalCount * 100.0 : 0.0;

            _logger?.LogInformation(
                "Filtered by complexity (threshold: {MinComplexity}): {TotalBefore} files -> {FilteredAfter} files (Reduction: {ReductionPercentage:F1}%, Kept: {KeptPercentage:F1}%)",
                effectiveThreshold, totalCount, filteredCount, reductionPercentage, 100.0 - reductionPercentage);

            return await Task.FromResult(filtered);
        }

        public async Task<List<FileMetadata>> FilterByPerformanceIssues(List<FileMetadata> metadatas)
        {
            var totalCount = metadatas?.Count ?? 0;
            var filtered = metadatas.Where(m => m?.Patterns?.PerformanceFindings != null && m.Patterns.PerformanceFindings.Count > 0).ToList();
            var filteredCount = filtered.Count;
            var reductionPercentage = totalCount > 0 ? (double)(totalCount - filteredCount) / totalCount * 100.0 : 0.0;

            _logger?.LogInformation(
                "Filtered by performance issues: {TotalBefore} files -> {FilteredAfter} files (Reduction: {ReductionPercentage:F1}%, Kept: {KeptPercentage:F1}%)",
                totalCount, filteredCount, reductionPercentage, 100.0 - reductionPercentage);

            return await Task.FromResult(filtered);
        }
    }
}


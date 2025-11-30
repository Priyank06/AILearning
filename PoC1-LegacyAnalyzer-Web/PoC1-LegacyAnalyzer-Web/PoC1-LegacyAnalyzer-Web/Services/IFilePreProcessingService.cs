using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// PURE preprocessing service for extracting metadata and code patterns from source files before sending to AI agents.
    /// Achieves 75-80% token reduction by leveraging local Roslyn analysis and static pattern detection.
    /// No LLM or external AI calls are made; all analysis is performed locally for maximum performance.
    /// </summary>
    /// <remarks>
    /// Use this interface for all code preprocessing tasks prior to invoking LLMs or external AI services.
    /// Methods are optimized for parallel execution and can be cached for repeated analysis.
    /// </remarks>
    public interface IFilePreProcessingService
    {
        /// <summary>
        /// Extracts rich metadata from a single code file using local Roslyn static analysis.
        /// This method reduces token usage by 75-80% by summarizing code structure, patterns, and complexity before any LLM invocation.
        /// No external AI calls are made; all processing is local and highly performant.
        /// </summary>
        /// <param name="file">The uploaded code file to analyze (IBrowserFile).</param>
        /// <param name="languageHint">Optional language hint (default: "csharp").</param>
        /// <returns>A <see cref="FileMetadata"/> object containing extracted metadata, code patterns, and complexity metrics.</returns>
        /// <remarks>
        /// Use this method before sending code to any LLM or AI agent to minimize token usage and cost.
        /// Can be executed in parallel for batch file analysis. Results are suitable for caching.
        /// <para>Example usage:</para>
        /// <code>
        /// var metadata = await service.ExtractMetadataAsync(file, "csharp");
        /// </code>
        /// </remarks>
        Task<FileMetadata> ExtractMetadataAsync(IBrowserFile file, string languageHint = "csharp");

        /// <summary>
        /// Extracts metadata from multiple code files in parallel using local Roslyn static analysis, with a configurable concurrency limit.
        /// This method leverages Task.WhenAll for high throughput and processes files in parallel, but limits the number of concurrent file reads using SemaphoreSlim.
        /// Each file is processed independently; errors in one file do not affect others.
        /// </summary>
        /// <param name="files">A list of uploaded code files to analyze (IBrowserFile).</param>
        /// <param name="languageHint">Optional language hint (default: "csharp").</param>
        /// <param name="maxConcurrency">Maximum number of files to process concurrently (default: 5).</param>
        /// <returns>A list of <see cref="FileMetadata"/> objects containing extracted metadata, code patterns, and complexity metrics for each file.</returns>
        /// <remarks>
        /// Use this method for batch preprocessing before sending code to LLMs or AI agents. Optimized for parallel execution and suitable for caching.
        /// Concurrency is controlled to avoid overwhelming system resources. Example usage:
        /// <code>
        /// var metadatas = await preprocessor.ExtractMetadataParallelAsync(files, "csharp", maxConcurrency: 8);
        /// </code>
        /// </remarks>
        Task<List<FileMetadata>> ExtractMetadataParallelAsync(List<IBrowserFile> files, string languageHint = "csharp", int maxConcurrency = 5);

        /// <summary>
        /// Creates a consolidated project summary from a list of file metadata objects.
        /// Aggregates complexity, code patterns, and risk levels for the entire project.
        /// No LLM or external AI calls are made; all aggregation is local and efficient.
        /// </summary>
        /// <param name="fileMetadatas">A list of <see cref="FileMetadata"/> objects from prior preprocessing.</param>
        /// <returns>A <see cref="ProjectSummary"/> containing aggregate metrics and recommendations.</returns>
        /// <remarks>
        /// Use this method after preprocessing all files to generate a project-level summary for executive reporting or AI input.
        /// Can be executed in parallel and results are suitable for caching.
        /// <para>Example usage:</para>
        /// <code>
        /// var summary = await service.CreateProjectSummaryAsync(fileMetadatas);
        /// </code>
        /// </remarks>
        Task<ProjectSummary> CreateProjectSummaryAsync(List<FileMetadata> fileMetadatas);

        /// <summary>
        /// Detects common code patterns and anti-patterns in source code using local static analysis.
        /// No LLM or external AI calls are made; all detection is performed locally for speed and efficiency.
        /// </summary>
        /// <param name="code">The source code to analyze.</param>
        /// <param name="language">The language of the source code (e.g., "csharp").</param>
        /// <returns>A <see cref="CodePatternAnalysis"/> object containing detected patterns and anti-patterns.</returns>
        /// <remarks>
        /// Use for lightweight pattern detection before deeper analysis or LLM invocation. Suitable for parallel and cached execution.
        /// </remarks>
        CodePatternAnalysis DetectPatterns(string code, string language);

        /// <summary>
        /// Calculates code complexity metrics (cyclomatic, lines of code, etc.) using local static analysis.
        /// No LLM or external AI calls are made; all metrics are computed locally for optimal performance.
        /// </summary>
        /// <param name="code">The source code to analyze.</param>
        /// <param name="language">The language of the source code (e.g., "csharp").</param>
        /// <returns>A <see cref="ComplexityMetrics"/> object containing calculated complexity metrics.</returns>
        /// <remarks>
        /// Use for fast complexity estimation before sending code to LLMs or for project-level reporting. Suitable for parallel and cached execution.
        /// </remarks>
        ComplexityMetrics CalculateComplexity(string code, string language);

        /// <summary>
        /// Gets the list of supported languages for preprocessing.
        /// </summary>
        /// <returns>A list of supported language strings (e.g., "csharp").</returns>
        /// <remarks>
        /// Use to validate language support before preprocessing. No LLM or external calls are made.
        /// </remarks>
        List<string> GetSupportedLanguages();

        /// <summary>
        /// Filters the provided file metadata list to only those files that have security risks detected in their Patterns.
        /// </summary>
        /// <param name="metadatas">List of <see cref="FileMetadata"/> objects to filter.</param>
        /// <returns>A new list containing only files with security findings.</returns>
        /// <remarks>
        /// Use to route files with security risks to security specialist agents. Does not modify the input list.
        /// </remarks>
        Task<List<FileMetadata>> FilterBySecurityRisks(List<FileMetadata> metadatas);

        /// <summary>
        /// Filters the provided file metadata list to only those files with cyclomatic complexity greater than or equal to <paramref name="minComplexity"/>.
        /// </summary>
        /// <param name="metadatas">List of <see cref="FileMetadata"/> objects to filter.</param>
        /// <param name="minComplexity">Minimum cyclomatic complexity threshold (default: 15).</param>
        /// <returns>A new list containing only files meeting the complexity threshold.</returns>
        /// <remarks>
        /// Use to route complex files to performance or architecture specialist agents. Does not modify the input list.
        /// </remarks>
        Task<List<FileMetadata>> FilterByComplexity(List<FileMetadata> metadatas, int minComplexity = 15);

        /// <summary>
        /// Filters the provided file metadata list to only those files that have performance issues detected in their Patterns.
        /// </summary>
        /// <param name="metadatas">List of <see cref="FileMetadata"/> objects to filter.</param>
        /// <returns>A new list containing only files with performance findings.</returns>
        /// <remarks>
        /// Use to route files with performance issues to performance specialist agents. Does not modify the input list.
        /// </remarks>
        Task<List<FileMetadata>> FilterByPerformanceIssues(List<FileMetadata> metadatas);

        /// <summary>
        /// Prepares compact, agent-specific data summaries for specialist agents based on file metadata.
        /// Filters and summarizes files according to the agent specialty, reducing token usage by 75-80% compared to sending full file content.
        /// </summary>
        /// <param name="allMetadata">List of all <see cref="FileMetadata"/> objects for the project.</param>
        /// <param name="agentSpecialty">Specialist agent type: "security", "performance", or "architecture".</param>
        /// <returns>A compact string summary suitable for agent input, containing only relevant files and metrics.</returns>
        /// <remarks>
        /// <para>Example usage:</para>
        /// <code>
        /// var summary = await service.GetAgentSpecificData(allMetadata, "security");
        /// </code>
        /// <para>Security: Filters by security risks, returns summary of risky files only.</para>
        /// <para>Performance: Filters by complexity > 15, returns summary of complex files only.</para>
        /// <para>Architecture: Returns project-level summary (class count, dependency graph, patterns).</para>
        /// </remarks>
        Task<string> GetAgentSpecificData(List<FileMetadata> allMetadata, string agentSpecialty);

        /// <summary>
        /// Gets cache statistics including hit/miss rates, total requests, and current cache size.
        /// Useful for monitoring cache performance and effectiveness.
        /// </summary>
        /// <returns>A <see cref="CacheStatistics"/> object containing cache performance metrics.</returns>
        /// <remarks>
        /// Use this method to monitor cache effectiveness and tune cache TTL if needed.
        /// <para>Example usage:</para>
        /// <code>
        /// var stats = service.GetCacheStatistics();
        /// var hitRate = stats.HitRate;
        /// </code>
        /// </remarks>
        CacheStatistics GetCacheStatistics();

        /// <summary>
        /// Clears all cached file metadata from the cache.
        /// Useful for forcing fresh analysis of all files or when cache needs to be reset.
        /// </summary>
        /// <returns>The number of cache entries that were cleared.</returns>
        /// <remarks>
        /// This operation is thread-safe and logs the cache clearing action.
        /// Use when files have been updated outside the normal cache invalidation mechanism.
        /// <para>Example usage:</para>
        /// <code>
        /// var clearedCount = service.ClearCache();
        /// </code>
        /// </remarks>
        int ClearCache();

        /// <summary>
        /// Clears cached metadata for a specific file by name.
        /// Useful for invalidating cache when a specific file has been updated.
        /// </summary>
        /// <param name="fileName">The name of the file to remove from cache. Can be a partial match to clear all variants.</param>
        /// <returns>The number of cache entries that were cleared for the specified file.</returns>
        /// <remarks>
        /// This operation is thread-safe and logs the cache clearing action.
        /// The method searches for cache keys containing the specified file name, so it will clear
        /// all cached entries for that file regardless of size or last modified time.
        /// <para>Example usage:</para>
        /// <code>
        /// var clearedCount = service.ClearCacheForFile("MyClass.cs");
        /// </code>
        /// </remarks>
        int ClearCacheForFile(string fileName);

        /// <summary>
        /// Estimates the token count for a FileMetadata object.
        /// Calculates tokens based on PatternSummary length plus overhead from method signatures, class names, and complexity metrics.
        /// </summary>
        /// <param name="metadata">The FileMetadata object to estimate tokens for.</param>
        /// <returns>Estimated token count for the metadata. Accuracy: ±10%.</returns>
        /// <remarks>
        /// Token estimation formula: (PatternSummary.Length / 4) + overhead
        /// Overhead includes: method signatures, class names, complexity metrics, pattern counts.
        /// This method is used to validate the 75-80% token reduction claim.
        /// <para>Example usage:</para>
        /// <code>
        /// var tokens = service.EstimateTokenCount(metadata);
        /// </code>
        /// </remarks>
        int EstimateTokenCount(FileMetadata metadata);

        /// <summary>
        /// Compares token counts between metadata summary and full source code.
        /// Validates the 75-80% token reduction claim by comparing compact metadata vs full code.
        /// </summary>
        /// <param name="metadata">The FileMetadata object containing the preprocessed summary.</param>
        /// <param name="fullCode">The full source code content for comparison.</param>
        /// <returns>A <see cref="TokenEstimate"/> object containing both token counts and reduction percentage.</returns>
        /// <remarks>
        /// This method estimates tokens for both the metadata summary and full code, then calculates
        /// the reduction percentage. Results are logged to validate the 75-80% reduction claim.
        /// Accuracy: ±10% for token estimates.
        /// <para>Example usage:</para>
        /// <code>
        /// var comparison = service.CompareWithFullCode(metadata, fullCode);
        /// Console.WriteLine($"Reduction: {comparison.ReductionPercentage}%");
        /// </code>
        /// </remarks>
        TokenEstimate CompareWithFullCode(FileMetadata metadata, string fullCode);
    }
}
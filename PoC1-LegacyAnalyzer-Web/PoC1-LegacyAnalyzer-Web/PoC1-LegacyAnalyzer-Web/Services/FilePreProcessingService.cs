using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Facade service that orchestrates focused preprocessing services.
    /// Maintains backward compatibility with IFilePreProcessingService while delegating to specialized services.
    /// </summary>
    public class FilePreProcessingService : IFilePreProcessingService
    {
        private readonly ILogger<FilePreProcessingService> _logger;
        private readonly IMetadataExtractionService _metadataExtraction;
        private readonly IPatternDetectionService _patternDetection;
        private readonly IComplexityCalculationService _complexityCalculation;
        private readonly IFileFilteringService _fileFiltering;
        private readonly IFileCacheManager _cacheManager;
        private readonly FilePreProcessingOptions _options;

        public FilePreProcessingService(
            ILogger<FilePreProcessingService> logger,
            IMetadataExtractionService metadataExtraction,
            IPatternDetectionService patternDetection,
            IComplexityCalculationService complexityCalculation,
            IFileFilteringService fileFiltering,
            IFileCacheManager cacheManager,
            IOptions<FilePreProcessingOptions> options)
        {
            _logger = logger;
            _metadataExtraction = metadataExtraction;
            _patternDetection = patternDetection;
            _complexityCalculation = complexityCalculation;
            _fileFiltering = fileFiltering;
            _cacheManager = cacheManager;
            _options = options?.Value ?? new FilePreProcessingOptions();
        }

        // Delegate to MetadataExtractionService
        public Task<FileMetadata> ExtractMetadataAsync(IBrowserFile file, string? languageHint = null)
        {
            return _metadataExtraction.ExtractMetadataAsync(file, languageHint);
        }

        public Task<List<FileMetadata>> ExtractMetadataParallelAsync(List<IBrowserFile> files, string? languageHint = null, int maxConcurrency = 5)
        {
            return _metadataExtraction.ExtractMetadataParallelAsync(files, languageHint, maxConcurrency);
        }

        // Delegate to PatternDetectionService
        public CodePatternAnalysis DetectPatterns(string code, string language)
        {
            return _patternDetection.DetectPatterns(code, language);
        }

        // Delegate to ComplexityCalculationService
        public ComplexityMetrics CalculateComplexity(string code, string language)
        {
            return _complexityCalculation.CalculateComplexity(code, language);
        }

        // Delegate to FileFilteringService
        public Task<List<FileMetadata>> FilterBySecurityRisks(List<FileMetadata> metadatas)
        {
            return _fileFiltering.FilterBySecurityRisks(metadatas);
        }

        public Task<List<FileMetadata>> FilterByComplexity(List<FileMetadata> metadatas, int minComplexity = 15)
        {
            return _fileFiltering.FilterByComplexity(metadatas, minComplexity);
        }

        public Task<List<FileMetadata>> FilterByPerformanceIssues(List<FileMetadata> metadatas)
        {
            return _fileFiltering.FilterByPerformanceIssues(metadatas);
        }

        // Delegate to FileCacheManager
        public CacheStatistics GetCacheStatistics()
        {
            return _cacheManager.GetCacheStatistics();
        }

        public int ClearCache()
        {
            return _cacheManager.ClearCache();
        }

        public int ClearCacheForFile(string fileName)
        {
            return _cacheManager.ClearCacheForFile(fileName);
        }

        // Orchestration methods that combine multiple services
        public async Task<ProjectSummary> CreateProjectSummaryAsync(List<FileMetadata> fileMetadatas)
        {
            var summary = new ProjectSummary
            {
                TotalFiles = fileMetadatas.Count,
                TotalClasses = fileMetadatas.Sum(f => f.Complexity.ClassCount),
                TotalMethods = fileMetadatas.Sum(f => f.Complexity.MethodCount),
                TotalProperties = fileMetadatas.Sum(f => f.Complexity.PropertyCount),
                ComplexityScore = fileMetadatas.Sum(f => f.Complexity.CyclomaticComplexity)
            };

            summary.FileResults = fileMetadatas.Select(m => new FileAnalysisResult
            {
                FileName = m.FileName,
                FileSize = m.FileSize,
                ComplexityScore = m.Complexity.CyclomaticComplexity,
                StaticAnalysis = new CodeAnalysisResult
                {
                    ClassCount = m.Complexity.ClassCount,
                    MethodCount = m.Complexity.MethodCount,
                    PropertyCount = m.Complexity.PropertyCount,
                    UsingCount = m.UsingDirectives.Count,
                    Classes = m.ClassSignatures.ToList(),
                    Methods = m.MethodSignatures.ToList(),
                    UsingStatements = m.UsingDirectives.ToList()
                },
                AIInsight = string.Join("; ", m.Patterns.SecurityFindings.Concat(m.Patterns.PerformanceFindings).Concat(m.Patterns.ArchitectureFindings))
            }).ToList();

            // Simple overall assessment
            var securityCount = fileMetadatas.Sum(m => m.Patterns.SecurityFindings.Count);
            var perfCount = fileMetadatas.Sum(m => m.Patterns.PerformanceFindings.Count);
            var archCount = fileMetadatas.Sum(m => m.Patterns.ArchitectureFindings.Count);

            summary.RiskLevel = securityCount > 0 ? "High" : perfCount + archCount > 3 ? "Medium" : "Low";
            summary.OverallAssessment = $"Security:{securityCount}, Performance:{perfCount}, Architecture:{archCount}";

            summary.KeyRecommendations = new List<string>();
            if (securityCount > 0) summary.KeyRecommendations.Add("Address security risks identified in preprocessing.");
            if (perfCount > 0) summary.KeyRecommendations.Add("Optimize performance hotspots identified in preprocessing.");
            if (archCount > 0) summary.KeyRecommendations.Add("Refactor architecture anti-patterns identified in preprocessing.");

            await Task.CompletedTask;
            return summary;
        }

        public List<string> GetSupportedLanguages()
        {
            return _patternDetection.GetSupportedLanguages();
        }

        public async Task<string> GetAgentSpecificData(List<FileMetadata> allMetadata, string agentSpecialty)
        {
            if (allMetadata == null || string.IsNullOrWhiteSpace(agentSpecialty))
                return string.Empty;

            string result = string.Empty;
            int tokenEstimate = 0;

            if (agentSpecialty.Equals("security", StringComparison.OrdinalIgnoreCase))
            {
                var filtered = await _fileFiltering.FilterBySecurityRisks(allMetadata);
                if (!filtered.Any())
                {
                    _logger?.LogWarning("Security agent: No security patterns detected. Sending all {count} files for thorough review.", allMetadata.Count);
                    filtered = allMetadata;
                }
                var lines = filtered.Select(f =>
                    $"File: {f.FileName} | Risks: {string.Join(", ", f.Patterns?.SecurityFindings ?? new List<string>())} | Complexity: {f.Complexity?.CyclomaticComplexity ?? 0}");
                result = string.Join("\n", lines);
            }
            else if (agentSpecialty.Equals("performance", StringComparison.OrdinalIgnoreCase))
            {
                var filtered = await _fileFiltering.FilterByComplexity(allMetadata, 15);
                if (!filtered.Any())
                {
                    _logger?.LogWarning("Performance agent: No files met complexity threshold. Sending all {count} files.", allMetadata.Count);
                    filtered = allMetadata;
                }
                var lines = filtered.Select(f =>
                    $"File: {f.FileName} | PerfIssues: {string.Join(", ", f.Patterns?.PerformanceFindings ?? new List<string>())} | Complexity: {f.Complexity?.CyclomaticComplexity ?? 0}");
                result = string.Join("\n", lines);
            }
            else if (agentSpecialty.Equals("architecture", StringComparison.OrdinalIgnoreCase))
            {
                int totalClasses = allMetadata.Sum(f => f.Complexity?.ClassCount ?? 0);
                var patterns = allMetadata.SelectMany(f =>
                    (f.Patterns?.SecurityFindings ?? new List<string>())
                    .Concat(f.Patterns?.PerformanceFindings ?? new List<string>())
                ).Distinct();
                result = $"Project Summary:\nClasses: {totalClasses}\nPatterns: {string.Join(", ", patterns)}";
            }
            else
            {
                result = "Unsupported agent specialty.";
            }

            // Estimate token count (rough: 1 token per 4 chars)
            tokenEstimate = result.Length / 4;
            _logger?.LogInformation("Agent '{AgentSpecialty}' summary token estimate: {TokenEstimate}", agentSpecialty, tokenEstimate);
            return result;
        }

        public int EstimateTokenCount(FileMetadata metadata)
        {
            if (metadata == null)
            {
                _logger?.LogWarning("EstimateTokenCount called with null metadata");
                return 0;
            }

            // Base token count from PatternSummary (roughly 1 token per 4 characters)
            var patternSummaryTokens = string.IsNullOrEmpty(metadata.PatternSummary)
                ? 0
                : (int)Math.Ceiling(metadata.PatternSummary.Length / 4.0);

            // Overhead tokens for structured data
            var overheadTokens = 0;

            // Method signatures overhead (each signature ~10-20 tokens)
            overheadTokens += metadata.MethodSignatures?.Count * 15 ?? 0;

            // Class signatures overhead (each class ~5-10 tokens)
            overheadTokens += metadata.ClassSignatures?.Count * 7 ?? 0;

            // Property signatures overhead (each property ~3-5 tokens)
            overheadTokens += metadata.PropertySignatures?.Count * 4 ?? 0;

            // Complexity metrics overhead (~10 tokens)
            if (metadata.Complexity != null)
            {
                overheadTokens += 10;
            }

            // Pattern counts overhead (each finding category ~5 tokens)
            if (metadata.Patterns != null)
            {
                overheadTokens += (metadata.Patterns.SecurityFindings?.Count ?? 0) * 5;
                overheadTokens += (metadata.Patterns.PerformanceFindings?.Count ?? 0) * 5;
                overheadTokens += (metadata.Patterns.ArchitectureFindings?.Count ?? 0) * 5;
            }

            // Using directives overhead (each using ~2 tokens)
            overheadTokens += (metadata.UsingDirectives?.Count ?? 0) * 2;

            // Namespace overhead (~3 tokens per namespace)
            overheadTokens += (metadata.Namespaces?.Count ?? 0) * 3;

            var totalTokens = patternSummaryTokens + overheadTokens;

            _logger?.LogDebug(
                "Token estimation for {FileName}: PatternSummary={PatternSummaryTokens}, Overhead={OverheadTokens}, Total={TotalTokens}",
                metadata.FileName, patternSummaryTokens, overheadTokens, totalTokens);

            return totalTokens;
        }

        public TokenEstimate CompareWithFullCode(FileMetadata metadata, string fullCode)
        {
            if (metadata == null)
            {
                _logger?.LogWarning("CompareWithFullCode called with null metadata");
                return new TokenEstimate
                {
                    MetadataTokens = 0,
                    FullCodeTokens = 0,
                    ReductionPercentage = 0
                };
            }

            if (string.IsNullOrEmpty(fullCode))
            {
                _logger?.LogWarning("CompareWithFullCode called with null or empty fullCode for file: {FileName}", metadata.FileName);
                return new TokenEstimate
                {
                    MetadataTokens = EstimateTokenCount(metadata),
                    FullCodeTokens = 0,
                    ReductionPercentage = 0
                };
            }

            // Estimate metadata tokens
            var metadataTokens = EstimateTokenCount(metadata);

            // Estimate full code tokens (roughly 1 token per 4 characters for code)
            // Code has slightly more tokens per character due to syntax, so we use 3.5 as divisor
            var fullCodeTokens = (int)Math.Ceiling(fullCode.Length / 3.5);

            // Calculate reduction percentage
            var reductionPercentage = fullCodeTokens > 0
                ? ((double)(fullCodeTokens - metadataTokens) / fullCodeTokens) * 100.0
                : 0.0;

            var estimate = new TokenEstimate
            {
                MetadataTokens = metadataTokens,
                FullCodeTokens = fullCodeTokens,
                ReductionPercentage = Math.Round(reductionPercentage, 2)
            };

            // Log comparison results to validate 75-80% reduction claim
            _logger?.LogInformation(
                "Token reduction validation for {FileName}: Metadata={MetadataTokens} tokens, FullCode={FullCodeTokens} tokens, " +
                "Reduction={ReductionPercentage}% (Target: 75-80%, Accuracy: ±10%)",
                metadata.FileName, metadataTokens, fullCodeTokens, reductionPercentage);

            // Log warning if reduction is outside expected range
            if (reductionPercentage < 70)
            {
                _logger?.LogWarning(
                    "Token reduction for {FileName} is {ReductionPercentage}%, below expected 75-80% range. " +
                    "Consider reviewing preprocessing effectiveness.",
                    metadata.FileName, reductionPercentage);
            }
            else if (reductionPercentage > 85)
            {
                _logger?.LogInformation(
                    "Token reduction for {FileName} is {ReductionPercentage}%, exceeding expected 75-80% range. " +
                    "Excellent optimization achieved.",
                    metadata.FileName, reductionPercentage);
            }
            else
            {
                _logger?.LogDebug(
                    "Token reduction for {FileName} is {ReductionPercentage}%, within expected 75-80% range.",
                    metadata.FileName, reductionPercentage);
            }

            return estimate;
        }
    }
}

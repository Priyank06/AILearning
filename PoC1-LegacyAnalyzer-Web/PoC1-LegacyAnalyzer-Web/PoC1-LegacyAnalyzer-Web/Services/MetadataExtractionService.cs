using System.Text;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Service for extracting metadata from code files using local Roslyn static analysis.
    /// </summary>
    public class MetadataExtractionService : IMetadataExtractionService
    {
        private readonly ILogger<MetadataExtractionService> _logger;
        private readonly IFileCacheManager _cacheManager;
        private readonly IPatternDetectionService _patternDetection;
        private readonly IComplexityCalculationService _complexityCalculation;
        private readonly FilePreProcessingOptions _options;

        public MetadataExtractionService(
            ILogger<MetadataExtractionService> logger,
            IFileCacheManager cacheManager,
            IPatternDetectionService patternDetection,
            IComplexityCalculationService complexityCalculation,
            IOptions<FilePreProcessingOptions> options)
        {
            _logger = logger;
            _cacheManager = cacheManager;
            _patternDetection = patternDetection;
            _complexityCalculation = complexityCalculation;
            _options = options?.Value ?? new FilePreProcessingOptions();
        }

        public async Task<List<FileMetadata>> ExtractMetadataParallelAsync(List<IBrowserFile> files, string languageHint = "csharp", int maxConcurrency = 5)
        {
            var effectiveConcurrency = maxConcurrency == 5 ? _options.MaxConcurrentFiles : maxConcurrency;

            var totalStopwatch = Stopwatch.StartNew();
            var fileCount = files?.Count ?? 0;
            var totalFileSize = files?.Sum(f => f?.Size ?? 0) ?? 0;

            _logger?.LogInformation("Starting parallel metadata extraction for {FileCount} files with maxConcurrency={MaxConcurrency}, totalSize={TotalSize} bytes",
                fileCount, effectiveConcurrency, totalFileSize);

            var semaphore = new SemaphoreSlim(effectiveConcurrency, effectiveConcurrency);
            var completed = 0;
            var individualTimings = new ConcurrentBag<long>();

            var tasks = files.Select(async file =>
            {
                var fileStopwatch = Stopwatch.StartNew();
                await semaphore.WaitAsync();
                try
                {
                    var result = await ExtractMetadataAsync(file, languageHint);
                    fileStopwatch.Stop();
                    individualTimings.Add(fileStopwatch.ElapsedMilliseconds);
                    return result;
                }
                catch (Exception ex)
                {
                    fileStopwatch.Stop();
                    _logger?.LogError(ex, "Error extracting metadata for file: {FileName}", file?.Name);
                    return new FileMetadata
                    {
                        FileName = file?.Name ?? "Unknown",
                        FileSize = file?.Size ?? 0,
                        Language = languageHint,
                        Status = "Error",
                        ErrorMessage = ex.Message
                    };
                }
                finally
                {
                    semaphore.Release();
                    var done = Interlocked.Increment(ref completed);
                    _logger?.LogDebug("File {FileName} completed. Total completed={Completed}/{Total}", file?.Name, done, files.Count);
                }
            }).ToList();

            var results = await Task.WhenAll(tasks);
            totalStopwatch.Stop();
            semaphore.Dispose();

            // Calculate performance metrics
            var totalDurationMs = totalStopwatch.ElapsedMilliseconds;
            var avgTimePerFile = individualTimings.Count > 0 ? individualTimings.Average() : 0;
            var estimatedSequentialTime = individualTimings.Sum();
            var speedup = estimatedSequentialTime > 0 ? (double)estimatedSequentialTime / totalDurationMs : 1.0;
            var filesPerSecond = fileCount > 0 && totalDurationMs > 0 ? (fileCount * 1000.0) / totalDurationMs : 0;

            _logger?.LogInformation(
                "Completed parallel metadata extraction: {FileCount} files in {TotalDuration}ms " +
                "(Avg: {AvgTimePerFile}ms/file, Speedup: {Speedup:F2}x, Throughput: {FilesPerSecond:F2} files/sec, " +
                "Estimated sequential: {EstimatedSequentialTime}ms)",
                fileCount, totalDurationMs, avgTimePerFile, speedup, filesPerSecond, estimatedSequentialTime);

            return results.ToList();
        }

        public async Task<FileMetadata> ExtractMetadataAsync(IBrowserFile file, string languageHint = "csharp")
        {
            var stopwatch = Stopwatch.StartNew();
            var fileSize = file.Size;
            var maxFileSizeBytes = _options.MaxFileSizeMB * 1024 * 1024;

            // Check file size limit
            if (fileSize > maxFileSizeBytes)
            {
                _logger?.LogWarning("File {FileName} size {FileSize} bytes exceeds maximum {MaxFileSizeMB}MB, rejecting",
                    file.Name, fileSize, _options.MaxFileSizeMB);
                return new FileMetadata
                {
                    FileName = file.Name,
                    FileSize = fileSize,
                    Language = languageHint,
                    Status = "Error",
                    ErrorMessage = $"File size {fileSize} bytes exceeds maximum allowed size of {_options.MaxFileSizeMB}MB"
                };
            }

            // Generate cache key
            var cacheKey = $"{file.Name}_{file.Size}_{file.LastModified.Ticks}_{languageHint}";

            // Try to get from cache
            if (_cacheManager.TryGetCached(cacheKey, out var cachedMetadata) && cachedMetadata != null)
            {
                stopwatch.Stop();
                _logger?.LogDebug("Cache hit for file: {FileName} (Key: {CacheKey}, Duration: {Duration}ms)",
                    file.Name, cacheKey, stopwatch.ElapsedMilliseconds);
                return cachedMetadata;
            }

            _logger?.LogDebug("Cache miss for file: {FileName} (Key: {CacheKey})", file.Name, cacheKey);

            var metadata = new FileMetadata
            {
                FileName = file.Name,
                FileSize = file.Size,
                Language = languageHint
            };

            try
            {
                using var stream = file.OpenReadStream(maxAllowedSize: maxFileSizeBytes);
                using var reader = new StreamReader(stream, Encoding.UTF8, true, 8192, leaveOpen: false);
                var code = await reader.ReadToEndAsync();

                if (languageHint.Equals("csharp", StringComparison.OrdinalIgnoreCase))
                {
                    PopulateCSharpMetadata(code, metadata, _options.EnablePatternDetection);
                }
                else
                {
                    // Fallback lightweight parsing
                    if (_options.EnablePatternDetection)
                    {
                        metadata.Patterns = _patternDetection.DetectPatterns(code, languageHint);
                    }
                    else
                    {
                        metadata.Patterns = new CodePatternAnalysis();
                    }
                    metadata.Complexity = _complexityCalculation.CalculateComplexity(code, languageHint);
                }

                metadata.PatternSummary = BuildPatternSummary(metadata);

                // Log pattern detection metrics
                if (_options.EnablePatternDetection)
                {
                    var securityCount = metadata.Patterns?.SecurityFindings?.Count ?? 0;
                    var performanceCount = metadata.Patterns?.PerformanceFindings?.Count ?? 0;
                    var architectureCount = metadata.Patterns?.ArchitectureFindings?.Count ?? 0;
                    var totalPatterns = securityCount + performanceCount + architectureCount;

                    _logger?.LogDebug(
                        "Pattern detection for {FileName}: Security={SecurityCount}, Performance={PerformanceCount}, Architecture={ArchitectureCount}, Total={TotalPatterns}",
                        file.Name, securityCount, performanceCount, architectureCount, totalPatterns);
                }
            }
            catch (Exception ex)
            {
                metadata.Status = "Error";
                metadata.ErrorMessage = ex.Message;
                _logger?.LogError(ex, "Error extracting metadata for file: {FileName}", file.Name);
            }

            stopwatch.Stop();
            var complexityScore = metadata.Complexity?.CyclomaticComplexity ?? 0;

            // Cache the result
            if (_options.EnableCaching)
            {
                _cacheManager.SetCached(cacheKey, metadata, _options.CacheTTLMinutes);
            }

            _logger?.LogDebug("Extracted metadata for file: {FileName} in {Duration}ms (Size: {FileSize} bytes, Complexity: {ComplexityScore})",
                file.Name, stopwatch.ElapsedMilliseconds, fileSize, complexityScore);

            return metadata;
        }

        private void PopulateCSharpMetadata(string code, FileMetadata metadata, bool enablePatternDetection = true)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            metadata.UsingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
                .Select(u => u.ToString())
                .Distinct()
                .ToList();

            metadata.Namespaces = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>()
                .Select(n => n.Name.ToString())
                .Distinct()
                .ToList();

            var classDecls = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
            foreach (var cls in classDecls)
            {
                var modifiers = string.Join(" ", cls.Modifiers.Select(m => m.Text));
                metadata.ClassSignatures.Add($"{modifiers} class {cls.Identifier}");

                foreach (var method in cls.Members.OfType<MethodDeclarationSyntax>())
                {
                    var mmods = string.Join(" ", method.Modifiers.Select(m => m.Text));
                    var parameters = string.Join(", ", method.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"));
                    metadata.MethodSignatures.Add($"{mmods} {method.ReturnType} {cls.Identifier}.{method.Identifier}({parameters})");
                }

                foreach (var prop in cls.Members.OfType<PropertyDeclarationSyntax>())
                {
                    var pmods = string.Join(" ", prop.Modifiers.Select(m => m.Text));
                    metadata.PropertySignatures.Add($"{pmods} {prop.Type} {cls.Identifier}.{prop.Identifier}");
                }
            }

            if (enablePatternDetection)
            {
                metadata.Patterns = _patternDetection.DetectPatterns(code, "csharp");
            }
            else
            {
                metadata.Patterns = new CodePatternAnalysis();
            }
            metadata.Complexity = _complexityCalculation.CalculateComplexity(code, "csharp");
        }

        private string BuildPatternSummary(FileMetadata m)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"File: {m.FileName} ({m.FileSize} bytes)");
            if (m.UsingDirectives.Count > 0)
                sb.AppendLine($"Usings: {string.Join(", ", m.UsingDirectives.Take(8))}{(m.UsingDirectives.Count > 8 ? ", ..." : string.Empty)}");
            if (m.Namespaces.Count > 0)
                sb.AppendLine($"Namespaces: {string.Join(", ", m.Namespaces.Take(5))}{(m.Namespaces.Count > 5 ? ", ..." : string.Empty)}");

            if (m.ClassSignatures.Count > 0)
                sb.AppendLine($"Classes: {string.Join(" | ", m.ClassSignatures.Take(6))}{(m.ClassSignatures.Count > 6 ? " | ..." : string.Empty)}");

            if (m.MethodSignatures.Count > 0)
                sb.AppendLine($"Methods: {string.Join(" | ", m.MethodSignatures.Take(10))}{(m.MethodSignatures.Count > 10 ? " | ..." : string.Empty)}");

            var findings = new List<string>();
            findings.AddRange(m.Patterns.SecurityFindings);
            findings.AddRange(m.Patterns.PerformanceFindings);
            findings.AddRange(m.Patterns.ArchitectureFindings);
            if (findings.Count > 0)
                sb.AppendLine($"Risks: {string.Join("; ", findings.Take(6))}{(findings.Count > 6 ? "; ..." : string.Empty)}");

            sb.AppendLine($"Complexity: CC={m.Complexity.CyclomaticComplexity}, LOC={m.Complexity.LinesOfCode}, Classes={m.Complexity.ClassCount}, Methods={m.Complexity.MethodCount}");
            return sb.ToString();
        }
    }
}


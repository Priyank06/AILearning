using System.Text;
using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Models;
using PoC1_LegacyAnalyzer_Web.Services.Analysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Service for extracting metadata from code files using unified analyzers (Roslyn for C#, TreeSitter for others).
    /// </summary>
    public class MetadataExtractionService : IMetadataExtractionService
    {
        private readonly ILogger<MetadataExtractionService> _logger;
        private readonly IFileCacheManager _cacheManager;
        private readonly IPatternDetectionService _patternDetection;
        private readonly ILegacyPatternDetectionService? _legacyPatternDetection;
        private readonly IComplexityCalculationService _complexityCalculation;
        private readonly IAnalyzerRouter _analyzerRouter;
        private readonly ILanguageDetector _languageDetector;
        private readonly IHybridMultiLanguageAnalyzer? _hybridAnalyzer;
        private readonly FilePreProcessingOptions _options;
        private readonly DefaultValuesConfiguration _defaultValues;

        public MetadataExtractionService(
            ILogger<MetadataExtractionService> logger,
            IFileCacheManager cacheManager,
            IPatternDetectionService patternDetection,
            IComplexityCalculationService complexityCalculation,
            IAnalyzerRouter analyzerRouter,
            ILanguageDetector languageDetector,
            IOptions<FilePreProcessingOptions> options,
            IOptions<DefaultValuesConfiguration> defaultValues,
            ILegacyPatternDetectionService? legacyPatternDetection = null,
            IHybridMultiLanguageAnalyzer? hybridAnalyzer = null)
        {
            _logger = logger;
            _cacheManager = cacheManager;
            _patternDetection = patternDetection;
            _legacyPatternDetection = legacyPatternDetection;
            _complexityCalculation = complexityCalculation;
            _analyzerRouter = analyzerRouter;
            _languageDetector = languageDetector;
            _hybridAnalyzer = hybridAnalyzer;
            _options = options?.Value ?? new FilePreProcessingOptions();
            _defaultValues = defaultValues?.Value ?? new DefaultValuesConfiguration();
        }

        public async Task<List<FileMetadata>> ExtractMetadataParallelAsync(List<IBrowserFile> files, string? languageHint = null, int maxConcurrency = 5)
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
                    var result = await ExtractMetadataAsync(file, languageHint ?? null);
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
                        FileName = file?.Name ?? _defaultValues.FileNames.Unknown,
                        FileSize = file?.Size ?? 0,
                        Language = languageHint ?? _defaultValues.Language.Unknown,
                        Status = _defaultValues.Status.Error,
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

        public async Task<FileMetadata> ExtractMetadataAsync(IBrowserFile file, string? languageHint = null)
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
                    Language = languageHint ?? _defaultValues.Language.Unknown,
                    Status = _defaultValues.Status.Error,
                    ErrorMessage = $"File size {fileSize} bytes exceeds maximum allowed size of {_options.MaxFileSizeMB}MB"
                };
            }

            // Read file content
            using var stream = file.OpenReadStream(maxAllowedSize: maxFileSizeBytes);
            using var reader = new StreamReader(stream, Encoding.UTF8, true, 8192, leaveOpen: false);
            var code = await reader.ReadToEndAsync();

            // Detect language automatically (override hint if detection succeeds)
            var detectedLanguage = _languageDetector.DetectLanguage(file.Name, code);
            var actualLanguage = detectedLanguage != LanguageKind.Unknown 
                ? detectedLanguage.ToString().ToLowerInvariant() 
                : (languageHint ?? _defaultValues.Language.Unknown);

            // Generate cache key with detected language
            var cacheKey = $"{file.Name}_{file.Size}_{file.LastModified.Ticks}_{actualLanguage}";

            // Try to get from cache
            if (_cacheManager.TryGetCached(cacheKey, out var cachedMetadata) && cachedMetadata != null)
            {
                stopwatch.Stop();
                _logger?.LogDebug("Cache hit for file: {FileName} (Key: {CacheKey}, Duration: {Duration}ms)",
                    file.Name, cacheKey, stopwatch.ElapsedMilliseconds);
                return cachedMetadata;
            }

            _logger?.LogDebug("Cache miss for file: {FileName} (Key: {CacheKey}, Detected Language: {Language})", 
                file.Name, cacheKey, actualLanguage);

            var metadata = new FileMetadata
            {
                FileName = file.Name,
                FileSize = file.Size,
                Language = actualLanguage,
                Status = _defaultValues.Status.Success
            };

            try
            {
                // Use unified analyzer router (Roslyn for C#, TreeSitter for others)
                var analyzable = new AnalyzableFile
                {
                    FileName = file.Name,
                    Content = code,
                    Language = detectedLanguage
                };

                var (codeStructure, codeAnalysisResult) = await _analyzerRouter.AnalyzeAsync(analyzable);

                // Populate metadata from CodeStructure (works for all languages)
                PopulateMetadataFromCodeStructure(codeStructure, codeAnalysisResult, metadata);

                // Pattern detection (language-aware)
                if (_options.EnablePatternDetection)
                {
                    metadata.Patterns = _patternDetection.DetectPatterns(code, actualLanguage);
                }
                else
                {
                    metadata.Patterns = new CodePatternAnalysis();
                }

                // Legacy pattern detection
                if (_legacyPatternDetection != null)
                {
                    var legacyContext = new Models.LegacyContext
                    {
                        FileName = file.Name,
                        FileLastModified = file.LastModified.DateTime,
                        Language = actualLanguage,
                        LinesOfCode = metadata.LineCount
                    };
                    var legacyResult = _legacyPatternDetection.DetectLegacyPatterns(code, actualLanguage, legacyContext);
                    metadata.LegacyPatternResult = legacyResult;
                }

                // Complexity calculation (language-aware)
                metadata.Complexity = _complexityCalculation.CalculateComplexity(code, actualLanguage);

                // Hybrid semantic analysis for non-C# languages
                if (_hybridAnalyzer != null && detectedLanguage != LanguageKind.CSharp && detectedLanguage != LanguageKind.Unknown)
                {
                    try
                    {
                        metadata.SemanticAnalysis = await _hybridAnalyzer.AnalyzeAsync(
                            code, 
                            file.Name, 
                            detectedLanguage, 
                            null, 
                            CancellationToken.None);
                        
                        _logger?.LogDebug("Semantic analysis completed for {FileName}: {IssueCount} issues found", 
                            file.Name, metadata.SemanticAnalysis?.SemanticIssues?.Count ?? 0);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Semantic analysis failed for {FileName}, continuing without semantic analysis", file.Name);
                        // Continue without semantic analysis - not critical
                    }
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
                metadata.Status = _defaultValues.Status.Error;
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

            _logger?.LogDebug("Extracted metadata for file: {FileName} in {Duration}ms (Size: {FileSize} bytes, Complexity: {ComplexityScore}, Language: {Language})",
                file.Name, stopwatch.ElapsedMilliseconds, fileSize, complexityScore, actualLanguage);

            return metadata;
        }

        /// <summary>
        /// Populates FileMetadata from CodeStructure and CodeAnalysisResult (works for all languages via unified analyzers).
        /// </summary>
        private void PopulateMetadataFromCodeStructure(CodeStructure structure, CodeAnalysisResult analysis, FileMetadata metadata)
        {
            // Populate imports/using directives
            metadata.UsingDirectives = structure.Imports
                .Select(i => i.ModuleName + (i.ImportedSymbols.Any() ? $" ({string.Join(", ", i.ImportedSymbols)})" : ""))
                .ToList();

            // Populate namespaces/containers
            if (!string.IsNullOrEmpty(structure.ContainerName))
            {
                metadata.Namespaces.Add(structure.ContainerName);
            }

            // Populate class signatures
            foreach (var cls in structure.Classes)
            {
                var accessMod = cls.AccessModifier != AccessModifier.Unknown ? cls.AccessModifier.ToString().ToLower() : "";
                var baseTypes = cls.BaseTypes.Any() ? $" : {string.Join(", ", cls.BaseTypes)}" : "";
                metadata.ClassSignatures.Add($"{accessMod} class {cls.Name}{baseTypes}");
            }

            // Populate method signatures
            foreach (var cls in structure.Classes)
            {
                foreach (var method in cls.Methods)
                {
                    var accessMod = method.AccessModifier != AccessModifier.Unknown ? method.AccessModifier.ToString().ToLower() : "";
                    var staticMod = method.IsStatic ? "static" : "";
                    var asyncMod = method.IsAsync ? "async" : "";
                    var returnType = !string.IsNullOrEmpty(method.ReturnType) ? method.ReturnType : "void";
                    var parameters = string.Join(", ", method.Parameters.Select(p => 
                        $"{(!string.IsNullOrEmpty(p.Type) ? p.Type : "var")} {p.Name}"));
                    var mods = string.Join(" ", new[] { accessMod, staticMod, asyncMod }.Where(m => !string.IsNullOrEmpty(m)));
                    metadata.MethodSignatures.Add($"{mods} {returnType} {cls.Name}.{method.Name}({parameters})");
                }
            }

            // Populate top-level functions (not in classes)
            foreach (var func in structure.Functions)
            {
                var accessMod = func.AccessModifier != AccessModifier.Unknown ? func.AccessModifier.ToString().ToLower() : "";
                var staticMod = func.IsStatic ? "static" : "";
                var asyncMod = func.IsAsync ? "async" : "";
                var returnType = !string.IsNullOrEmpty(func.ReturnType) ? func.ReturnType : "void";
                var parameters = string.Join(", ", func.Parameters.Select(p => 
                    $"{(!string.IsNullOrEmpty(p.Type) ? p.Type : "var")} {p.Name}"));
                var mods = string.Join(" ", new[] { accessMod, staticMod, asyncMod }.Where(m => !string.IsNullOrEmpty(m)));
                metadata.MethodSignatures.Add($"{mods} {returnType} {func.Name}({parameters})");
            }

            // Populate property signatures
            foreach (var cls in structure.Classes)
            {
                foreach (var prop in cls.Properties)
                {
                    var accessMod = prop.AccessModifier != AccessModifier.Unknown ? prop.AccessModifier.ToString().ToLower() : "";
                    var propType = !string.IsNullOrEmpty(prop.Type) ? prop.Type : "var";
                    var getter = prop.HasGetter ? "get; " : "";
                    var setter = prop.HasSetter ? "set; " : "";
                    metadata.PropertySignatures.Add($"{accessMod} {propType} {cls.Name}.{prop.Name} {{ {getter}{setter}}}");
                }
            }
        }

        private string BuildPatternSummary(FileMetadata m)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"File: {m.FileName} ({m.FileSize} bytes) | Language: {m.Language}");
            
            if (m.UsingDirectives.Count > 0)
                sb.AppendLine($"Imports/Dependencies: {string.Join(", ", m.UsingDirectives.Take(8))}{(m.UsingDirectives.Count > 8 ? ", ..." : string.Empty)}");
            if (m.Namespaces.Count > 0)
                sb.AppendLine($"Namespaces/Modules: {string.Join(", ", m.Namespaces.Take(5))}{(m.Namespaces.Count > 5 ? ", ..." : string.Empty)}");

            if (m.ClassSignatures.Count > 0)
                sb.AppendLine($"Classes/Types: {string.Join(" | ", m.ClassSignatures.Take(6))}{(m.ClassSignatures.Count > 6 ? " | ..." : string.Empty)}");

            if (m.MethodSignatures.Count > 0)
                sb.AppendLine($"Methods/Functions: {string.Join(" | ", m.MethodSignatures.Take(10))}{(m.MethodSignatures.Count > 10 ? " | ..." : string.Empty)}");

            // Include pattern findings prominently - these are critical for AI analysis
            var securityFindings = m.Patterns?.SecurityFindings ?? new List<string>();
            var performanceFindings = m.Patterns?.PerformanceFindings ?? new List<string>();
            var architectureFindings = m.Patterns?.ArchitectureFindings ?? new List<string>();
            
            if (securityFindings.Count > 0)
                sb.AppendLine($"Security Risks ({securityFindings.Count}): {string.Join("; ", securityFindings.Take(5))}{(securityFindings.Count > 5 ? "; ..." : string.Empty)}");
            
            if (performanceFindings.Count > 0)
                sb.AppendLine($"Performance Issues ({performanceFindings.Count}): {string.Join("; ", performanceFindings.Take(5))}{(performanceFindings.Count > 5 ? "; ..." : string.Empty)}");
            
            if (architectureFindings.Count > 0)
                sb.AppendLine($"Architecture Concerns ({architectureFindings.Count}): {string.Join("; ", architectureFindings.Take(5))}{(architectureFindings.Count > 5 ? "; ..." : string.Empty)}");

            // If no specific findings, still show summary
            if (securityFindings.Count == 0 && performanceFindings.Count == 0 && architectureFindings.Count == 0)
            {
                sb.AppendLine("Pattern Analysis: No critical patterns detected.");
            }

            sb.AppendLine($"Complexity: CC={m.Complexity?.CyclomaticComplexity ?? 0}, LOC={m.Complexity?.LinesOfCode ?? 0}, Classes={m.Complexity?.ClassCount ?? 0}, Methods={m.Complexity?.MethodCount ?? 0}");
            return sb.ToString();
        }
    }
}


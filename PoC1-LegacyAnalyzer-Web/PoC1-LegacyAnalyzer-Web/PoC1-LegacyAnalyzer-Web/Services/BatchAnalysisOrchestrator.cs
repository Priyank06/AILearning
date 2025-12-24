using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using PoC1_LegacyAnalyzer_Web.Models;
using PoC1_LegacyAnalyzer_Web.Services.Analysis;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Orchestrates batch processing of files for analysis.
    /// Handles any number of files by processing them in sequential batches of 10.
    /// Files are intelligently grouped by module/namespace to keep related files together.
    /// No hard file count limits are enforced - batching naturally handles large projects.
    /// </summary>
    public class BatchAnalysisOrchestrator : IBatchAnalysisOrchestrator
    {
        private readonly ICodeAnalysisService _codeAnalysisService;
        private readonly IAIAnalysisService _aiAnalysisService;
        private readonly IComplexityCalculatorService _complexityCalculator;
        private readonly ITokenEstimationService _tokenEstimation;
        private readonly ILogger<BatchAnalysisOrchestrator> _logger;
        private readonly BatchProcessingConfig _batchConfig;
        private readonly FileAnalysisLimitsConfig _fileLimits;
        private readonly TokenEstimationConfig _tokenEstimationConfig;
        private readonly IAnalyzerRouter _analyzerRouter;
        private readonly ILanguageDetector _languageDetector;
        private readonly IFilePreProcessingService _preprocessing;

        public BatchAnalysisOrchestrator(
            ICodeAnalysisService codeAnalysisService,
            IAIAnalysisService aiAnalysisService,
            IComplexityCalculatorService complexityCalculator,
            ITokenEstimationService tokenEstimation,
            ILogger<BatchAnalysisOrchestrator> logger,
            IOptions<BatchProcessingConfig> batchOptions,
            IOptions<FileAnalysisLimitsConfig> fileLimitOptions,
            IOptions<TokenEstimationConfig> tokenEstimationOptions,
            IAnalyzerRouter analyzerRouter,
            ILanguageDetector languageDetector,
            IFilePreProcessingService preprocessing)
        {
            _codeAnalysisService = codeAnalysisService;
            _aiAnalysisService = aiAnalysisService;
            _complexityCalculator = complexityCalculator;
            _tokenEstimation = tokenEstimation;
            _logger = logger;
            _batchConfig = batchOptions.Value ?? new BatchProcessingConfig();
            _fileLimits = fileLimitOptions.Value ?? new FileAnalysisLimitsConfig();
            _tokenEstimationConfig = tokenEstimationOptions.Value ?? new TokenEstimationConfig();
            _analyzerRouter = analyzerRouter;
            _languageDetector = languageDetector;
            _preprocessing = preprocessing;
        }

        public async Task<List<FileAnalysisResult>> AnalyzeFilesInBatchesAsync(
            List<IBrowserFile> files,
            string analysisType,
            AnalysisProgress analysisProgress,
            IProgress<AnalysisProgress> progress)
        {
            var fileResults = new List<FileAnalysisResult>();

            // Step 1: Perform static analysis on all files first (fast, no API calls)
            var fileData = await PrepareFileDataAsync(files, analysisProgress, progress);
            
            if (!fileData.Any())
            {
                _logger.LogWarning("No files successfully prepared for batch analysis");
                return fileResults;
            }

            // Step 2: Group files intelligently by module/namespace, then into batches of 10
            var batches = GroupFilesIntelligentlyIntoBatches(fileData);

            var originalApiCalls = fileData.Count;
            var optimizedApiCalls = batches.Count;
            var reductionPercentage = originalApiCalls > 0 
                ? Math.Round((1.0 - (double)optimizedApiCalls / originalApiCalls) * 100, 1) 
                : 0;

            _logger.LogInformation("Grouped {FileCount} files into {BatchCount} batches (10 files per batch). API call reduction: {Reduction}% ({Original} → {Optimized})", 
                fileData.Count, batches.Count, reductionPercentage, originalApiCalls, optimizedApiCalls);

            // Step 3: Process batches SEQUENTIALLY (not in parallel) to avoid API limits
            analysisProgress.TotalBatches = batches.Count;
            analysisProgress.Status = $"Processing {batches.Count} batches sequentially (optimized from {originalApiCalls} individual calls, ~{reductionPercentage}% reduction)...";
            progress?.Report(analysisProgress);

            var totalFilesProcessed = 0;
            var batchStartTime = DateTime.Now;
            
            for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
            {
                var batch = batches[batchIndex];
                var batchNumber = batchIndex + 1;
                
                // Update batch progress
                analysisProgress.CurrentBatch = batchNumber;
                analysisProgress.FilesInCurrentBatch = batch.Count;
                analysisProgress.BatchStatus = $"Analyzing batch {batchNumber} of {batches.Count}...";
                analysisProgress.Status = $"Batch {batchNumber}/{batches.Count}: Processing {batch.Count} files...";
                
                // Calculate estimated time remaining
                if (batchNumber > 1)
                {
                    var elapsedTime = DateTime.Now - batchStartTime;
                    var avgTimePerBatch = elapsedTime.TotalMilliseconds / (batchNumber - 1);
                    var remainingBatches = batches.Count - batchNumber;
                    analysisProgress.EstimatedTimeRemaining = TimeSpan.FromMilliseconds(avgTimePerBatch * remainingBatches);
                }
                
                progress?.Report(analysisProgress);

                try
                {
                    var batchResult = await ProcessBatchAsync(batch, analysisType, analysisProgress, progress);
                    fileResults.AddRange(batchResult);
                    totalFilesProcessed += batchResult.Count;
                    
                    analysisProgress.CompletedFiles = totalFilesProcessed;
                    analysisProgress.Status = $"Completed batch {batchNumber}/{batches.Count} ({totalFilesProcessed}/{fileData.Count} files processed)...";
                    progress?.Report(analysisProgress);
                    
                    _logger.LogInformation("Completed batch {BatchNumber}/{TotalBatches} with {FileCount} files", 
                        batchNumber, batches.Count, batch.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Critical error in batch {BatchNumber}, falling back to individual processing", batchNumber);
                    // Isolated error handling - process batch files individually as fallback
                    var fallbackResult = await ProcessBatchWithFallbackAsync(batch, analysisType, analysisProgress, progress);
                    fileResults.AddRange(fallbackResult);
                    totalFilesProcessed += fallbackResult.Count;
                    
                    analysisProgress.CompletedFiles = totalFilesProcessed;
                    analysisProgress.Status = $"Completed batch {batchNumber}/{batches.Count} ({totalFilesProcessed}/{fileData.Count} files processed, fallback mode)...";
                    progress?.Report(analysisProgress);
                }
            }

            analysisProgress.BatchStatus = $"All {batches.Count} batches completed";
            analysisProgress.EstimatedTimeRemaining = TimeSpan.Zero;
            progress?.Report(analysisProgress);

            return fileResults;
        }

        private async Task<List<(IBrowserFile file, string metadataSummary, CodeAnalysisResult staticAnalysis, FileMetadata metadata)>> PrepareFileDataAsync(
            List<IBrowserFile> files,
            AnalysisProgress analysisProgress = null,
            IProgress<AnalysisProgress> progress = null)
        {
            var fileData = new List<(IBrowserFile file, string metadataSummary, CodeAnalysisResult staticAnalysis, FileMetadata metadata)>();
            
            if (analysisProgress != null)
            {
                analysisProgress.Status = "Extracting metadata and performing static analysis (no API calls)...";
                progress?.Report(analysisProgress);
            }

            // Extract metadata for all files (uses TreeSitter/Roslyn, achieves token optimization)
            var metadataList = await _preprocessing.ExtractMetadataParallelAsync(files);
            var metadataDict = metadataList.ToDictionary(m => m.FileName, m => m);

            foreach (var file in files)
            {
                try
                {
                    // Get metadata for this file
                    if (!metadataDict.TryGetValue(file.Name, out var metadata))
                    {
                        _logger.LogWarning("Metadata not found for {FileName}, skipping", file.Name);
                        continue;
                    }

                    // Still need static analysis for metrics
                    using var stream = file.OpenReadStream(_fileLimits.MaxFileSizeBytes);
                    using var reader = new StreamReader(stream);
                    var content = await reader.ReadToEndAsync();

                    var languageKind = _languageDetector.DetectLanguage(file.Name, content);
                    var analyzable = new AnalyzableFile
                    {
                        FileName = file.Name,
                        Content = content,
                        Language = languageKind
                    };

                    var (_, staticAnalysis) = await _analyzerRouter.AnalyzeAsync(analyzable);
                    staticAnalysis.LanguageKind = languageKind;
                    staticAnalysis.Language = languageKind.ToString().ToLowerInvariant();

                    // Use metadata summary instead of full code content
                    var metadataSummary = !string.IsNullOrEmpty(metadata.PatternSummary) 
                        ? metadata.PatternSummary 
                        : $"File: {file.Name} | Language: {metadata.Language} | Classes: {metadata.Complexity.ClassCount} | Methods: {metadata.Complexity.MethodCount}";

                    // Store metadata for module grouping
                    fileData.Add((file, metadataSummary, staticAnalysis, metadata));
                    _logger.LogDebug("Prepared file {FileName} with metadata summary (token optimized)", file.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to read or analyze file: {FileName}", file.Name);
                    // Continue processing other files - error isolation
                }
            }

            return fileData;
        }

        /// <summary>
        /// Intelligently groups files by module/namespace, then creates batches of 10 files.
        /// Related files (same namespace/module) are kept together in the same batch.
        /// </summary>
        private List<List<(IBrowserFile file, string metadataSummary, CodeAnalysisResult staticAnalysis, FileMetadata metadata)>> GroupFilesIntelligentlyIntoBatches(
            List<(IBrowserFile file, string metadataSummary, CodeAnalysisResult staticAnalysis, FileMetadata metadata)> fileData)
        {
            const int FilesPerBatch = 10;
            var batches = new List<List<(IBrowserFile file, string metadataSummary, CodeAnalysisResult staticAnalysis, FileMetadata metadata)>>();

            // Step 1: Group files by module/namespace (extract from file path or metadata)
            var fileGroups = GroupFilesByModule(fileData);
            
            _logger.LogInformation("Grouped {FileCount} files into {GroupCount} modules/namespaces", 
                fileData.Count, fileGroups.Count);

            // Step 2: Create batches of 10 files, trying to keep related files together
            var currentBatch = new List<(IBrowserFile file, string metadataSummary, CodeAnalysisResult staticAnalysis, FileMetadata metadata)>();
            
            foreach (var moduleGroup in fileGroups)
            {
                foreach (var fileInfo in moduleGroup.Value)
                {
                    // If current batch is full (10 files), start a new batch
                    if (currentBatch.Count >= FilesPerBatch)
                    {
                        batches.Add(new List<(IBrowserFile file, string metadataSummary, CodeAnalysisResult staticAnalysis, FileMetadata metadata)>(currentBatch));
                        _logger.LogDebug("Created batch with {FileCount} files", currentBatch.Count);
                        currentBatch.Clear();
                    }

                    currentBatch.Add(fileInfo);
                }
            }

            // Add the last batch if it has files
            if (currentBatch.Any())
            {
                batches.Add(currentBatch);
                _logger.LogDebug("Created final batch with {FileCount} files", currentBatch.Count);
            }

            return batches;
        }

        /// <summary>
        /// Groups files by module/namespace based on file path and namespace information from metadata.
        /// </summary>
        private Dictionary<string, List<(IBrowserFile file, string metadataSummary, CodeAnalysisResult staticAnalysis, FileMetadata metadata)>> GroupFilesByModule(
            List<(IBrowserFile file, string metadataSummary, CodeAnalysisResult staticAnalysis, FileMetadata metadata)> fileData)
        {
            var groups = new Dictionary<string, List<(IBrowserFile file, string metadataSummary, CodeAnalysisResult staticAnalysis, FileMetadata metadata)>>();

            foreach (var fileInfo in fileData)
            {
                // Try to extract module/namespace from metadata first (more accurate)
                var module = "";
                
                // Check if metadata has namespace information
                if (fileInfo.metadata.Namespaces != null && fileInfo.metadata.Namespaces.Any())
                {
                    // Use the first namespace, extract the module part (e.g., "Company.Project.Models" -> "Models")
                    var namespaceParts = fileInfo.metadata.Namespaces.First().Split('.');
                    if (namespaceParts.Length > 0)
                    {
                        module = namespaceParts.Last(); // Use last part as module name
                    }
                }
                
                // Fallback to path-based extraction if no namespace in metadata
                if (string.IsNullOrEmpty(module))
                {
                    module = ExtractModuleFromPath(fileInfo.file.Name);
                }
                
                // If still no module found, use "Other" as default
                if (string.IsNullOrEmpty(module))
                {
                    module = "Other";
                }

                if (!groups.ContainsKey(module))
                {
                    groups[module] = new List<(IBrowserFile file, string metadataSummary, CodeAnalysisResult staticAnalysis, FileMetadata metadata)>();
                }

                groups[module].Add(fileInfo);
            }

            // Sort groups by size (larger groups first) to optimize batching
            return groups.OrderByDescending(g => g.Value.Count)
                .ToDictionary(g => g.Key, g => g.Value);
        }

        /// <summary>
        /// Extracts module name from file path.
        /// Tries to identify common folder structures like Models, Services, Controllers, etc.
        /// </summary>
        private string ExtractModuleFromPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "";

            // Normalize path separators
            var normalizedPath = filePath.Replace('\\', '/');
            
            // Common module patterns in .NET projects
            var modulePatterns = new[]
            {
                "Models", "Model",
                "Services", "Service",
                "Controllers", "Controller",
                "Views", "View",
                "Data", "DataAccess", "DAL",
                "Business", "BusinessLogic", "BLL",
                "Common", "Shared", "Utilities", "Utils",
                "Infrastructure", "Infra",
                "Domain", "Entities", "Entity",
                "Repositories", "Repository", "Repo",
                "Interfaces", "Contracts",
                "Helpers", "Extensions"
            };

            // Check for module patterns in path
            foreach (var pattern in modulePatterns)
            {
                if (normalizedPath.Contains($"/{pattern}/", StringComparison.OrdinalIgnoreCase) ||
                    normalizedPath.Contains($"\\{pattern}\\", StringComparison.OrdinalIgnoreCase))
                {
                    return pattern;
                }
            }

            // If no pattern found, try to extract folder name before file
            var parts = normalizedPath.Split('/', '\\');
            if (parts.Length >= 2)
            {
                // Return second-to-last part (folder containing the file)
                return parts[parts.Length - 2];
            }

            return "";
        }

        /// <summary>
        /// Legacy method for backward compatibility - uses token-based batching.
        /// </summary>
        private List<List<(IBrowserFile file, string metadataSummary, CodeAnalysisResult staticAnalysis, FileMetadata metadata)>> GroupFilesIntoBatches(
            List<(IBrowserFile file, string metadataSummary, CodeAnalysisResult staticAnalysis, FileMetadata metadata)> fileData)
        {
            // Use intelligent grouping by default
            return GroupFilesIntelligentlyIntoBatches(fileData);
        }

        private async Task<List<FileAnalysisResult>> ProcessBatchAsync(
            List<(IBrowserFile file, string metadataSummary, CodeAnalysisResult staticAnalysis, FileMetadata metadata)> batch,
            string analysisType,
            AnalysisProgress analysisProgress,
            IProgress<AnalysisProgress> progress)
        {
            var results = new List<FileAnalysisResult>();

            try
            {
                // Prepare batch data for AI analysis using metadata summaries instead of full code
                var batchData = batch.Select(f => (f.file.Name, f.metadataSummary, f.staticAnalysis)).ToList();
                _logger.LogInformation("Processing batch with {FileCount} files using metadata summaries (token optimized)", batch.Count);

                // Call batch AI analysis (single API call for multiple files, using metadata summaries)
                var aiResults = await _aiAnalysisService.GetBatchAnalysisAsync(batchData, analysisType);

                // Create file results from batch AI response with error isolation
                foreach (var fileInfo in batch)
                {
                    try
                    {
                        var aiInsight = aiResults.GetValueOrDefault(fileInfo.file.Name, 
                            GenerateFallbackAssessment(fileInfo.staticAnalysis, analysisType));

                        var fileResult = new FileAnalysisResult
                        {
                            FileName = fileInfo.file.Name,
                            FileSize = fileInfo.file.Size,
                            StaticAnalysis = fileInfo.staticAnalysis,
                            AIInsight = aiInsight,
                            ComplexityScore = _complexityCalculator.CalculateFileComplexity(fileInfo.staticAnalysis),
                            LegacyPatternResult = fileInfo.metadata.LegacyPatternResult,
                            Status = "Analysis Completed"
                        };

                        results.Add(fileResult);

                        // Update progress
                        if (analysisProgress != null)
                        {
                            analysisProgress.CompletedFiles++;
                            analysisProgress.Status = $"Batch {analysisProgress.CurrentBatch}/{analysisProgress.TotalBatches}: Processed {analysisProgress.CompletedFiles}/{analysisProgress.TotalFiles} files...";
                            progress?.Report(analysisProgress);
                        }
                    }
                    catch (Exception fileEx)
                    {
                        // Isolated error handling - continue with other files in batch
                        _logger.LogWarning(fileEx, "Error processing file {FileName} in batch, using fallback", fileInfo.file.Name);
                        results.Add(new FileAnalysisResult
                        {
                            FileName = fileInfo.file.Name,
                            FileSize = fileInfo.file.Size,
                            StaticAnalysis = fileInfo.staticAnalysis,
                            AIInsight = GenerateFallbackAssessment(fileInfo.staticAnalysis, analysisType),
                            ComplexityScore = _complexityCalculator.CalculateFileComplexity(fileInfo.staticAnalysis),
                            Status = "Analysis Completed (Fallback)",
                            ErrorMessage = $"Partial error: {fileEx.Message}"
                        });
                    }
                }

                _logger.LogInformation("Successfully processed batch of {FileCount} files in single API call", batch.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Batch processing failed for {FileCount} files, falling back to individual processing", batch.Count);
                
                // Fallback: process files individually if batch fails
                return await ProcessBatchWithFallbackAsync(batch, analysisType, analysisProgress, progress);
            }

            return results;
        }

        private async Task<List<FileAnalysisResult>> ProcessBatchWithFallbackAsync(
            List<(IBrowserFile file, string metadataSummary, CodeAnalysisResult staticAnalysis, FileMetadata metadata)> batch,
            string analysisType,
            AnalysisProgress analysisProgress,
            IProgress<AnalysisProgress> progress)
        {
            var results = new List<FileAnalysisResult>();
            
            foreach (var fileInfo in batch)
            {
                try
                {
                    // Use metadata summary even in fallback mode (token optimized)
                    var analysisContent = fileInfo.metadataSummary;
                    _logger.LogDebug("Fallback: Using metadata summary for {FileName}", fileInfo.file.Name);
                    
                    var aiInsight = await _aiAnalysisService.GetAnalysisAsync(analysisContent, analysisType, fileInfo.staticAnalysis);
                    
                    results.Add(new FileAnalysisResult
                    {
                        FileName = fileInfo.file.Name,
                        FileSize = fileInfo.file.Size,
                        StaticAnalysis = fileInfo.staticAnalysis,
                        AIInsight = aiInsight,
                        ComplexityScore = _complexityCalculator.CalculateFileComplexity(fileInfo.staticAnalysis),
                        LegacyPatternResult = fileInfo.metadata.LegacyPatternResult,
                        Status = "Analysis Completed (Individual Fallback)"
                    });

                    if (analysisProgress != null)
                    {
                        analysisProgress.CompletedFiles++;
                        progress?.Report(analysisProgress);
                    }
                }
                catch (Exception fallbackEx)
                {
                    // Even fallback failed - use static analysis only
                    _logger.LogError(fallbackEx, "Individual fallback analysis also failed for: {FileName}", fileInfo.file.Name);
                    results.Add(new FileAnalysisResult
                    {
                        FileName = fileInfo.file.Name,
                        FileSize = fileInfo.file.Size,
                        Status = "Analysis Failed",
                        ErrorMessage = $"Batch and individual processing failed: {fallbackEx.Message}",
                        StaticAnalysis = fileInfo.staticAnalysis,
                        AIInsight = GenerateFallbackAssessment(fileInfo.staticAnalysis, analysisType),
                        ComplexityScore = _complexityCalculator.CalculateFileComplexity(fileInfo.staticAnalysis),
                        LegacyPatternResult = fileInfo.metadata.LegacyPatternResult
                    });
                }
            }

            return results;
        }

        private string GenerateFallbackAssessment(CodeAnalysisResult analysis, string analysisType)
        {
            return analysisType switch
            {
                "security" => $"Security review recommended for {analysis.ClassCount} classes. Verify input validation, authentication, and authorization implementations.",
                "performance" => $"Performance assessment indicates {analysis.MethodCount} methods require optimization analysis. Focus on database operations and algorithmic efficiency.",
                "migration" => $"Migration complexity assessment: {analysis.ClassCount} classes require modernization evaluation. Plan for framework compatibility and API updates.",
                _ => $"Code quality assessment shows {analysis.ClassCount} classes with {analysis.MethodCount} methods requiring structured modernization approach with quality assurance."
            };
        }
    }
}


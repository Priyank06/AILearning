using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Options;

namespace PoC1_LegacyAnalyzer_Web.Services
{
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

        public BatchAnalysisOrchestrator(
            ICodeAnalysisService codeAnalysisService,
            IAIAnalysisService aiAnalysisService,
            IComplexityCalculatorService complexityCalculator,
            ITokenEstimationService tokenEstimation,
            ILogger<BatchAnalysisOrchestrator> logger,
            IOptions<BatchProcessingConfig> batchOptions,
            IOptions<FileAnalysisLimitsConfig> fileLimitOptions,
            IOptions<TokenEstimationConfig> tokenEstimationOptions)
        {
            _codeAnalysisService = codeAnalysisService;
            _aiAnalysisService = aiAnalysisService;
            _complexityCalculator = complexityCalculator;
            _tokenEstimation = tokenEstimation;
            _logger = logger;
            _batchConfig = batchOptions.Value ?? new BatchProcessingConfig();
            _fileLimits = fileLimitOptions.Value ?? new FileAnalysisLimitsConfig();
            _tokenEstimationConfig = tokenEstimationOptions.Value ?? new TokenEstimationConfig();
        }

        public async Task<List<FileAnalysisResult>> AnalyzeFilesInBatchesAsync(
            List<IBrowserFile> files,
            string analysisType,
            AnalysisProgress analysisProgress,
            IProgress<AnalysisProgress> progress)
        {
            var fileResults = new List<FileAnalysisResult>();
            var semaphore = new SemaphoreSlim(_batchConfig.MaxConcurrentBatches, _batchConfig.MaxConcurrentBatches);

            // Step 1: Perform static analysis on all files first (fast, no API calls)
            var fileData = await PrepareFileDataAsync(files, analysisProgress, progress);
            
            if (!fileData.Any())
            {
                _logger.LogWarning("No files successfully prepared for batch analysis");
                return fileResults;
            }

            // Step 2: Group files into batches based on token limits and file count
            var batches = GroupFilesIntoBatches(fileData);

            var originalApiCalls = fileData.Count;
            var optimizedApiCalls = batches.Count;
            var reductionPercentage = originalApiCalls > 0 
                ? Math.Round((1.0 - (double)optimizedApiCalls / originalApiCalls) * 100, 1) 
                : 0;

            _logger.LogInformation("Grouped {FileCount} files into {BatchCount} batches. API call reduction: {Reduction}% ({Original} → {Optimized})", 
                fileData.Count, batches.Count, reductionPercentage, originalApiCalls, optimizedApiCalls);

            // Step 3: Process batches in parallel with concurrency control and error isolation
            analysisProgress.Status = $"Processing {batches.Count} batches in parallel (optimized from {originalApiCalls} individual calls, ~{reductionPercentage}% reduction)...";
            progress?.Report(analysisProgress);

            var completedBatches = 0;
            var totalFilesProcessed = 0;
            var batchTasks = batches.Select(async (batch, batchIndex) =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var batchNumber = batchIndex + 1;
                    analysisProgress.Status = $"Batch {batchNumber}/{batches.Count}: Processing {batch.Count} files (API call {batchNumber} of {batches.Count}, ~{reductionPercentage}% fewer calls)...";
                    progress?.Report(analysisProgress);

                    var batchResult = await ProcessBatchAsync(batch, analysisType, analysisProgress, progress);
                    
                    Interlocked.Increment(ref completedBatches);
                    Interlocked.Add(ref totalFilesProcessed, batchResult.Count);
                    analysisProgress.Status = $"Completed {completedBatches}/{batches.Count} batches ({totalFilesProcessed} files processed)...";
                    progress?.Report(analysisProgress);

                    return batchResult;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Critical error in batch {BatchIndex}, falling back to individual processing", batchIndex);
                    // Isolated error handling - process batch files individually as fallback
                    var fallbackResult = await ProcessBatchWithFallbackAsync(batch, analysisType, analysisProgress, progress);
                    
                    Interlocked.Increment(ref completedBatches);
                    Interlocked.Add(ref totalFilesProcessed, fallbackResult.Count);
                    analysisProgress.Status = $"Completed {completedBatches}/{batches.Count} batches ({totalFilesProcessed} files processed, fallback mode)...";
                    progress?.Report(analysisProgress);
                    
                    return fallbackResult;
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var batchResults = await Task.WhenAll(batchTasks);

            // Step 4: Flatten batch results and merge with file results
            foreach (var batchResult in batchResults)
            {
                fileResults.AddRange(batchResult);
            }

            return fileResults;
        }

        private async Task<List<(IBrowserFile file, string content, CodeAnalysisResult staticAnalysis)>> PrepareFileDataAsync(
            List<IBrowserFile> files,
            AnalysisProgress analysisProgress = null,
            IProgress<AnalysisProgress> progress = null)
        {
            var fileData = new List<(IBrowserFile file, string content, CodeAnalysisResult staticAnalysis)>();
            
            if (analysisProgress != null)
            {
                analysisProgress.Status = "Performing static code analysis (no API calls)...";
                progress?.Report(analysisProgress);
            }

            foreach (var file in files)
            {
                try
                {
                    using var stream = file.OpenReadStream(_fileLimits.MaxFileSizeBytes);
                    using var reader = new StreamReader(stream);
                    var content = await reader.ReadToEndAsync();
                    var staticAnalysis = await _codeAnalysisService.AnalyzeCodeAsync(content);
                    
                    fileData.Add((file, content, staticAnalysis));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to read or analyze file: {FileName}", file.Name);
                    // Continue processing other files - error isolation
                }
            }

            return fileData;
        }

        private List<List<(IBrowserFile file, string content, CodeAnalysisResult staticAnalysis)>> GroupFilesIntoBatches(
            List<(IBrowserFile file, string content, CodeAnalysisResult staticAnalysis)> fileData)
        {
            var batches = new List<List<(IBrowserFile file, string content, CodeAnalysisResult staticAnalysis)>>();
            var currentBatch = new List<(IBrowserFile file, string content, CodeAnalysisResult staticAnalysis)>();
            var currentBatchTokens = 0;
            
            // Calculate available tokens (reserve space for response and prompt overhead)
            var basePromptTokens = _tokenEstimation.EstimateBatchPromptOverhead(0);
            var availableTokens = _batchConfig.MaxTokensPerBatch - _batchConfig.ReserveTokensForResponse - basePromptTokens;

            foreach (var fileInfo in fileData)
            {
                var fileTokens = _tokenEstimation.EstimateTokens(fileInfo.content);
                var batchPromptOverhead = _tokenEstimation.EstimateBatchPromptOverhead(currentBatch.Count);
                var totalTokensIfAdded = currentBatchTokens + fileTokens + batchPromptOverhead;

                // Check if adding this file would exceed limits
                bool exceedsFileLimit = currentBatch.Count >= _batchConfig.MaxFilesPerBatch;
                bool exceedsTokenLimit = totalTokensIfAdded > availableTokens;

                if (exceedsFileLimit || exceedsTokenLimit)
                {
                    // Start a new batch if current batch has files
                    if (currentBatch.Any())
                    {
                        batches.Add(currentBatch);
                        _logger.LogDebug("Created batch with {FileCount} files, {TokenCount} tokens", 
                            currentBatch.Count, currentBatchTokens);
                    }
                    currentBatch = new List<(IBrowserFile file, string content, CodeAnalysisResult staticAnalysis)>();
                    currentBatchTokens = 0;
                }

                // Add file to current batch
                currentBatch.Add(fileInfo);
                currentBatchTokens += fileTokens;
            }

            // Add the last batch if it has files
            if (currentBatch.Any())
            {
                batches.Add(currentBatch);
                _logger.LogDebug("Created final batch with {FileCount} files, {TokenCount} tokens", 
                    currentBatch.Count, currentBatchTokens);
            }

            return batches;
        }

        private async Task<List<FileAnalysisResult>> ProcessBatchAsync(
            List<(IBrowserFile file, string content, CodeAnalysisResult staticAnalysis)> batch,
            string analysisType,
            AnalysisProgress analysisProgress,
            IProgress<AnalysisProgress> progress)
        {
            var results = new List<FileAnalysisResult>();

            try
            {
                // Prepare batch data for AI analysis
                var batchData = batch.Select(f => (f.file.Name, f.content, f.staticAnalysis)).ToList();

                // Call batch AI analysis (single API call for multiple files)
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
                            Status = "Analysis Completed"
                        };

                        results.Add(fileResult);

                        // Update progress
                        if (analysisProgress != null)
                        {
                            analysisProgress.CompletedFiles++;
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
            List<(IBrowserFile file, string content, CodeAnalysisResult staticAnalysis)> batch,
            string analysisType,
            AnalysisProgress analysisProgress,
            IProgress<AnalysisProgress> progress)
        {
            var results = new List<FileAnalysisResult>();
            
            foreach (var fileInfo in batch)
            {
                try
                {
                    var previewLength = _fileLimits.DefaultCodePreviewLength;
                    var analysisContent = fileInfo.content.Length > previewLength 
                        ? fileInfo.content.Substring(0, previewLength) + "..." 
                        : fileInfo.content;
                    
                    var aiInsight = await _aiAnalysisService.GetAnalysisAsync(analysisContent, analysisType, fileInfo.staticAnalysis);
                    
                    results.Add(new FileAnalysisResult
                    {
                        FileName = fileInfo.file.Name,
                        FileSize = fileInfo.file.Size,
                        StaticAnalysis = fileInfo.staticAnalysis,
                        AIInsight = aiInsight,
                        ComplexityScore = _complexityCalculator.CalculateFileComplexity(fileInfo.staticAnalysis),
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
                        ComplexityScore = _complexityCalculator.CalculateFileComplexity(fileInfo.staticAnalysis)
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


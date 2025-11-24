using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Configuration;
using System.Threading;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class MultiFileAnalysisService : IMultiFileAnalysisService
    {
        private readonly ICodeAnalysisService _codeAnalysisService;
        private readonly IAIAnalysisService _aiAnalysisService;
        private readonly ILogger<MultiFileAnalysisService> _logger;
        private readonly BusinessCalculationRules _businessRules;
        private readonly BatchProcessingConfig _batchConfig;
        private readonly FileAnalysisLimitsConfig _fileLimits;
        private readonly ComplexityThresholdsConfig _complexityThresholds;
        private readonly ScaleThresholdsConfig _scaleThresholds;
        private readonly TokenEstimationConfig _tokenEstimation;

        public MultiFileAnalysisService(
            ICodeAnalysisService codeAnalysisService,
            IAIAnalysisService aiAnalysisService,
            ILogger<MultiFileAnalysisService> logger,
            IConfiguration configuration)
        {
            _codeAnalysisService = codeAnalysisService;
            _aiAnalysisService = aiAnalysisService;
            _logger = logger;

            _businessRules = new BusinessCalculationRules();
            configuration.GetSection("BusinessCalculationRules").Bind(_businessRules);

            // Load batch processing configuration
            _batchConfig = new BatchProcessingConfig();
            configuration.GetSection("AzureOpenAI:BatchProcessing").Bind(_batchConfig);
            
            // Set defaults if not configured
            if (_batchConfig.MaxFilesPerBatch <= 0) _batchConfig.MaxFilesPerBatch = 3;
            if (_batchConfig.MaxTokensPerBatch <= 0) _batchConfig.MaxTokensPerBatch = 8000;
            if (_batchConfig.MaxConcurrentBatches <= 0) _batchConfig.MaxConcurrentBatches = 2;
            if (_batchConfig.TokenEstimationCharsPerToken <= 0) _batchConfig.TokenEstimationCharsPerToken = 4;
            if (_batchConfig.ReserveTokensForResponse <= 0) _batchConfig.ReserveTokensForResponse = 2000;

            // Load file analysis limits configuration
            _fileLimits = new FileAnalysisLimitsConfig();
            configuration.GetSection("FileAnalysisLimits").Bind(_fileLimits);

            // Load complexity thresholds configuration
            _complexityThresholds = new ComplexityThresholdsConfig();
            configuration.GetSection("ComplexityThresholds").Bind(_complexityThresholds);

            // Load scale thresholds configuration
            _scaleThresholds = new ScaleThresholdsConfig();
            configuration.GetSection("ScaleThresholds").Bind(_scaleThresholds);

            // Load token estimation configuration
            _tokenEstimation = new TokenEstimationConfig();
            configuration.GetSection("TokenEstimation").Bind(_tokenEstimation);
        }

        public async Task<MultiFileAnalysisResult> AnalyzeMultipleFilesAsync(List<IBrowserFile> files, string analysisType)
        {
            _logger.LogInformation("Initiating optimized batch analysis for {FileCount} files using {AnalysisType} methodology",
                files.Count, analysisType);

            var result = new MultiFileAnalysisResult
            {
                TotalFiles = files.Count
            };

            var filesToProcess = files.Take(_fileLimits.MaxFilesPerAnalysis).ToList(); // Performance limit: maximum 10 files
            var fileResults = new List<FileAnalysisResult>();

            // Use batch processing by default for efficiency (60-80% reduction in API calls)
            if (_batchConfig.Enabled && filesToProcess.Count > 1)
            {
                _logger.LogInformation("Using optimized batch processing: {FileCount} files will be processed in batches", filesToProcess.Count);
                
                var analysisProgress = new AnalysisProgress
                {
                    TotalFiles = filesToProcess.Count,
                    CurrentAnalysisType = analysisType,
                    StartTime = DateTime.Now
                };

                fileResults = await AnalyzeFilesInBatchesAsync(filesToProcess, analysisType, analysisProgress, null);
            }
            else
            {
                // Fallback to individual processing only if batch is disabled or single file
                _logger.LogInformation("Using individual file processing (batch disabled or single file)");
                foreach (var file in filesToProcess)
                {
                    try
                    {
                        var fileResult = await AnalyzeIndividualFileAsync(file, analysisType);
                        fileResults.Add(fileResult);

                        // Aggregate project metrics
                        result.TotalClasses += fileResult.StaticAnalysis.ClassCount;
                        result.TotalMethods += fileResult.StaticAnalysis.MethodCount;
                        result.TotalProperties += fileResult.StaticAnalysis.PropertyCount;
                        result.TotalUsingStatements += fileResult.StaticAnalysis.UsingCount;

                        _logger.LogDebug("Successfully analyzed file: {FileName}", file.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Analysis failed for file: {FileName}", file.Name);

                        fileResults.Add(new FileAnalysisResult
                        {
                            FileName = file.Name,
                            FileSize = file.Size,
                            Status = "Analysis Failed",
                            ErrorMessage = $"Processing error: {ex.Message}",
                            StaticAnalysis = new CodeAnalysisResult()
                        });
                    }
                }
            }

            // Aggregate metrics from all file results
            foreach (var fileResult in fileResults)
            {
                result.TotalClasses += fileResult.StaticAnalysis.ClassCount;
                result.TotalMethods += fileResult.StaticAnalysis.MethodCount;
                result.TotalProperties += fileResult.StaticAnalysis.PropertyCount;
                result.TotalUsingStatements += fileResult.StaticAnalysis.UsingCount;
            }

            result.FileResults = fileResults;
            result.OverallComplexityScore = CalculateProjectComplexity(result);
            result.OverallRiskLevel = DetermineRiskLevel(result.OverallComplexityScore);
            result.KeyRecommendations = GenerateStrategicRecommendations(result, analysisType);
            result.OverallAssessment = GenerateExecutiveAssessment(result, analysisType);
            result.ProjectSummary = GenerateProjectSummary(result);

            // Calculate API call reduction for logging
            string apiCallReduction = "No reduction (individual processing)";
            if (_batchConfig.Enabled && filesToProcess.Count > 1)
            {
                try
                {
                    var preparedData = await PrepareFileDataAsync(filesToProcess);
                    var batches = GroupFilesIntoBatches(preparedData);
                    var reduction = preparedData.Count > 0 
                        ? Math.Round((1.0 - (double)batches.Count / preparedData.Count) * 100, 1) 
                        : 0;
                    apiCallReduction = $"~{reduction}% reduction ({preparedData.Count} → {batches.Count} API calls)";
                }
                catch
                {
                    apiCallReduction = "Batch processing enabled";
                }
            }

            _logger.LogInformation("Analysis completed for {FileCount} files. Overall complexity: {ComplexityScore}, Risk level: {RiskLevel}. API call optimization: {Optimization}",
                result.TotalFiles, result.OverallComplexityScore, result.OverallRiskLevel, apiCallReduction);

            return result;
        }

        private async Task<FileAnalysisResult> AnalyzeIndividualFileAsync(IBrowserFile file, string analysisType)
        {
            const int maxFileSize = 512000; // 500KB security limit

            using var stream = file.OpenReadStream(maxFileSize);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            var staticAnalysis = await _codeAnalysisService.AnalyzeCodeAsync(content);

            // Generate professional assessment
            string professionalAssessment;
            try
            {
                var analysisContent = content.Length > 800 ? content.Substring(0, 800) + "..." : content;
                professionalAssessment = await _aiAnalysisService.GetAnalysisAsync(analysisContent, analysisType, staticAnalysis);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI analysis unavailable for {FileName}, using fallback assessment", file.Name);
                professionalAssessment = GenerateFallbackAssessment(staticAnalysis, analysisType);
            }

            return new FileAnalysisResult
            {
                FileName = file.Name,
                FileSize = file.Size,
                StaticAnalysis = staticAnalysis,
                AIInsight = professionalAssessment,
                ComplexityScore = CalculateFileComplexity(staticAnalysis),
                Status = "Analysis Completed"
            };
        }

        private int CalculateFileComplexity(CodeAnalysisResult analysis)
        {
            // Professional complexity calculation algorithm
            var structuralComplexity = analysis.ClassCount * 10;
            var behavioralComplexity = analysis.MethodCount * 2;
            var dependencyComplexity = analysis.UsingCount * 1;

            var totalComplexity = structuralComplexity + behavioralComplexity + dependencyComplexity;
            return Math.Min(100, Math.Max(0, totalComplexity));
        }

        private int CalculateProjectComplexity(MultiFileAnalysisResult result)
        {
            if (!result.FileResults.Any()) return 0;

            var averageFileComplexity = result.FileResults.Average(f => f.ComplexityScore);
            var scaleComplexityFactor = Math.Min(result.TotalFiles * 1.5, 20); // Project scale impact
            var architecturalComplexity = result.TotalClasses > 0 ? (double)result.TotalMethods / result.TotalClasses : 0;

            var overallComplexity = averageFileComplexity + scaleComplexityFactor + (architecturalComplexity * 2);
            return Math.Min(100, (int)Math.Max(0, overallComplexity));
        }

        private string DetermineRiskLevel(int complexityScore) => complexityScore switch
        {
            var score when score < _complexityThresholds.Low => "LOW",
            var score when score < _complexityThresholds.High => "MEDIUM",
            _ => "HIGH"
        };

        private List<string> GenerateStrategicRecommendations(MultiFileAnalysisResult result, string analysisType)
        {
            var recommendations = new List<string>();

            // Risk-based recommendations
            if (result.OverallComplexityScore > _complexityThresholds.VeryHigh)
            {
                recommendations.Add("High complexity project requires dedicated migration team with senior architect oversight");
                recommendations.Add("Implement phased migration approach to minimize business disruption and technical risk");
                recommendations.Add("Establish comprehensive testing strategy before initiating modernization activities");
            }
            else if (result.OverallComplexityScore > _complexityThresholds.Critical)
            {
                recommendations.Add("Moderate complexity project suitable for experienced development team");
                recommendations.Add("Plan structured migration timeline with 6-10 week implementation window");
                recommendations.Add("Implement code quality gates and automated testing during modernization");
            }
            else
            {
                recommendations.Add("Low complexity project appropriate for standard development practices");
                recommendations.Add("Excellent candidate for junior developer skill development and mentoring");
                recommendations.Add("Consider as pilot project for establishing modernization best practices");
            }

            // Scale-based recommendations
            if (result.TotalFiles > _scaleThresholds.LargeCodebaseFileCount)
            {
                recommendations.Add("Large codebase requires automated testing and continuous integration before migration");
                recommendations.Add("Implement code analysis tools and quality metrics tracking throughout modernization");
            }

            // Architecture-based recommendations
            var methodsPerClass = result.TotalClasses > 0 ? (double)result.TotalMethods / result.TotalClasses : 0;
            if (methodsPerClass > _scaleThresholds.HighMethodsPerClass)
            {
                recommendations.Add("High method-to-class ratio indicates potential architectural refactoring opportunities");
            }

            // Analysis-specific recommendations
            if (analysisType == "security")
            {
                recommendations.Add("Implement security code review process with focus on input validation and authentication");
            }
            else if (analysisType == "performance")
            {
                recommendations.Add("Establish performance baselines and monitoring before optimization activities");
            }

            return recommendations.Take(_fileLimits.MaxRecommendations).ToList();
        }

        private string GenerateExecutiveAssessment(MultiFileAnalysisResult result, string analysisType)
        {
            var assessment = $"Comprehensive {analysisType} analysis of {result.TotalFiles}-file enterprise project indicates {result.OverallRiskLevel.ToLower()} modernization complexity. ";

            assessment += analysisType switch
            {
                "security" => $"Security assessment identifies {result.FileResults.Count(f => f.ComplexityScore > _scaleThresholds.HighRiskComplexityScore)} files requiring immediate security review and remediation.",
                "performance" => $"Performance analysis reveals optimization opportunities across {result.TotalMethods} methods with potential for significant efficiency improvements.",
                "migration" => $"Migration assessment indicates {GetMigrationEffortEstimate(result.OverallComplexityScore)} effort requirement with structured implementation approach.",
                _ => $"Code quality assessment reveals {result.TotalClasses} classes requiring modernization attention with varying priority levels."
            };

            return assessment + $" Recommended approach: {GetRecommendedApproach(result.OverallComplexityScore)}.";
        }

        private string GenerateProjectSummary(MultiFileAnalysisResult result)
        {
            return $"Enterprise project analysis: {result.TotalFiles} source files containing {result.TotalClasses} classes, " +
                   $"{result.TotalMethods} methods, and {result.TotalProperties} properties. " +
                   $"Overall assessment: {result.OverallRiskLevel} risk level with complexity rating of {result.OverallComplexityScore}/100. " +
                   $"Project requires {GetResourceRequirement(result.OverallComplexityScore)} with {GetTimelineEstimate(result.OverallComplexityScore)} implementation timeline.";
        }

        private string GetMigrationEffortEstimate(int complexity) => complexity switch
        {
            var score when score < _complexityThresholds.Low => "minimal to moderate",
            var score when score < _complexityThresholds.High => "moderate to substantial",
            _ => "substantial to extensive"
        };

        private string GetRecommendedApproach(int complexityScore) => complexityScore switch
        {
            var score when score < _complexityThresholds.Low => "Agile development with standard practices",
            var score when score < _complexityThresholds.Medium => "Structured approach with experienced team",
            var score when score < _complexityThresholds.VeryHigh => "Phased migration with risk mitigation",
            _ => "Enterprise methodology with dedicated team"
        };

        private string GetResourceRequirement(int complexity) => complexity switch
        {
            var score when score < _complexityThresholds.Low => "standard development resources",
            var score when score < _complexityThresholds.High => "experienced development team with architectural guidance",
            _ => "senior development team with specialist migration expertise"
        };

        private string GetTimelineEstimate(int complexity) => complexity switch
        {
            var score when score < _complexityThresholds.Low => "2-4 week",
            var score when score < _complexityThresholds.High => "4-8 week",
            _ => "8-16 week"
        };

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

        public async Task<MultiFileAnalysisResult> AnalyzeMultipleFilesWithProgressAsync(List<IBrowserFile> files, string analysisType, IProgress<AnalysisProgress> progress = null)
        {
            _logger.LogInformation("Starting optimized batch analysis for {FileCount} files", files.Count);

            var result = new MultiFileAnalysisResult
            {
                TotalFiles = files.Count
            };

            var fileResults = new List<FileAnalysisResult>();
            var analysisProgress = new AnalysisProgress
            {
                TotalFiles = files.Count,
                CurrentAnalysisType = analysisType,
                StartTime = DateTime.Now
            };

            var filesToProcess = files.Take(_fileLimits.MaxFilesPerAnalysis).ToList();

            // Use batch processing if enabled
            if (_batchConfig.Enabled && filesToProcess.Count > 1)
            {
                _logger.LogInformation("Using optimized batch processing: {FileCount} files will be processed in batches", filesToProcess.Count);
                analysisProgress.Status = $"Preparing {filesToProcess.Count} files for batch analysis...";
                progress?.Report(analysisProgress);
                
                fileResults = await AnalyzeFilesInBatchesAsync(filesToProcess, analysisType, analysisProgress, progress);
            }
            else
            {
                // Fallback to individual processing
                _logger.LogInformation("Using individual file processing (batch disabled or single file)");
                for (int i = 0; i < filesToProcess.Count; i++)
                {
                    var file = filesToProcess[i];

                    analysisProgress.CurrentFile = file.Name;
                    analysisProgress.CompletedFiles = i;
                    analysisProgress.Status = $"Analyzing file {i + 1}/{filesToProcess.Count}: {file.Name}...";
                    progress?.Report(analysisProgress);

                    await Task.Delay(200);

                    try
                    {
                        var fileResult = await AnalyzeIndividualFileAsync(file, analysisType);
                        fileResults.Add(fileResult);

                        result.TotalClasses += fileResult.StaticAnalysis.ClassCount;
                        result.TotalMethods += fileResult.StaticAnalysis.MethodCount;
                        result.TotalProperties += fileResult.StaticAnalysis.PropertyCount;
                        result.TotalUsingStatements += fileResult.StaticAnalysis.UsingCount;

                        analysisProgress.CompletedFiles = i + 1;
                        progress?.Report(analysisProgress);

                        _logger.LogDebug("Completed analysis for: {FileName}", file.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Analysis failed for file: {FileName}", file.Name);
                        fileResults.Add(new FileAnalysisResult
                        {
                            FileName = file.Name,
                            FileSize = file.Size,
                            Status = "Analysis Failed",
                            ErrorMessage = $"Processing error: {ex.Message}",
                            StaticAnalysis = new CodeAnalysisResult()
                        });
                        
                        analysisProgress.CompletedFiles = i + 1;
                        progress?.Report(analysisProgress);
                    }
                }
            }

            // Final progress update
            analysisProgress.CompletedFiles = fileResults.Count;
            analysisProgress.Status = "Analysis completed - generating insights...";
            progress?.Report(analysisProgress);

            // Aggregate metrics from all file results
            foreach (var fileResult in fileResults)
            {
                result.TotalClasses += fileResult.StaticAnalysis.ClassCount;
                result.TotalMethods += fileResult.StaticAnalysis.MethodCount;
                result.TotalProperties += fileResult.StaticAnalysis.PropertyCount;
                result.TotalUsingStatements += fileResult.StaticAnalysis.UsingCount;
            }

            result.FileResults = fileResults;
            result.OverallComplexityScore = CalculateProjectComplexity(result);
            result.OverallRiskLevel = DetermineRiskLevel(result.OverallComplexityScore);
            result.KeyRecommendations = GenerateStrategicRecommendations(result, analysisType);
            result.OverallAssessment = GenerateExecutiveAssessment(result, analysisType);
            result.ProjectSummary = GenerateProjectSummary(result);

            _logger.LogInformation("Analysis completed for {FileCount} files. API calls optimized: {BatchMode}", 
                result.TotalFiles, _batchConfig.Enabled ? "Yes" : "No");
            return result;
        }

        /// <summary>
        /// Analyzes files in optimized batches with parallel processing and token management.
        /// Implements 60-80% reduction in API calls through intelligent batching.
        /// </summary>
        private async Task<List<FileAnalysisResult>> AnalyzeFilesInBatchesAsync(
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

        /// <summary>
        /// Prepares file data by reading content and performing static analysis.
        /// </summary>
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

        /// <summary>
        /// Synchronous helper for PrepareFileData (used in GroupFilesIntoBatches call).
        /// </summary>
        private List<(IBrowserFile file, string content, CodeAnalysisResult staticAnalysis)> PrepareFileData(
            List<IBrowserFile> files)
        {
            return PrepareFileDataAsync(files).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Groups files into batches based on token limits and max files per batch.
        /// Optimizes batch size to maximize API call reduction while staying within token limits.
        /// </summary>
        private List<List<(IBrowserFile file, string content, CodeAnalysisResult staticAnalysis)>> GroupFilesIntoBatches(
            List<(IBrowserFile file, string content, CodeAnalysisResult staticAnalysis)> fileData)
        {
            var batches = new List<List<(IBrowserFile file, string content, CodeAnalysisResult staticAnalysis)>>();
            var currentBatch = new List<(IBrowserFile file, string content, CodeAnalysisResult staticAnalysis)>();
            var currentBatchTokens = 0;
            
            // Calculate available tokens (reserve space for response and prompt overhead)
            var basePromptTokens = EstimateBatchPromptOverhead(0);
            var availableTokens = _batchConfig.MaxTokensPerBatch - _batchConfig.ReserveTokensForResponse - basePromptTokens;

            foreach (var fileInfo in fileData)
            {
                var fileTokens = EstimateTokens(fileInfo.content);
                var batchPromptOverhead = EstimateBatchPromptOverhead(currentBatch.Count);
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

        /// <summary>
        /// Processes a single batch of files using batch AI analysis.
        /// Implements error isolation - failures in one file don't break the entire batch.
        /// </summary>
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
                            ComplexityScore = CalculateFileComplexity(fileInfo.staticAnalysis),
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
                            ComplexityScore = CalculateFileComplexity(fileInfo.staticAnalysis),
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

        /// <summary>
        /// Fallback method to process batch files individually when batch processing fails.
        /// Ensures error in one file doesn't break processing of other files.
        /// </summary>
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
                        ComplexityScore = CalculateFileComplexity(fileInfo.staticAnalysis),
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
                        ComplexityScore = CalculateFileComplexity(fileInfo.staticAnalysis)
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// Estimates token count for content using improved accuracy.
        /// Accounts for code structure (more tokens per character than plain text).
        /// </summary>
        private int EstimateTokens(string content)
        {
            if (string.IsNullOrEmpty(content))
                return 0;

            // More accurate estimation: code has more tokens per character
            // C# code typically has ~3-4 chars per token, but we use configurable value
            var baseEstimate = content.Length / _batchConfig.TokenEstimationCharsPerToken;
            
            // Add overhead for code structure (brackets, keywords, etc. increase token density)
            var structureOverhead = (int)(baseEstimate * _tokenEstimation.CodeStructureOverheadPercentage);
            
            return baseEstimate + structureOverhead;
        }

        /// <summary>
        /// Estimates additional tokens needed for batch prompt overhead.
        /// Includes system prompt, JSON structure, and per-file separators.
        /// </summary>
        private int EstimateBatchPromptOverhead(int fileCount)
        {
            var baseOverhead = _tokenEstimation.BaseBatchPromptOverhead + _tokenEstimation.BatchJsonStructureOverhead;
            var perFileOverhead = fileCount * _tokenEstimation.PerFileBatchOverhead;
            
            return baseOverhead + perFileOverhead;
        }

        public BusinessMetrics CalculateBusinessMetrics(MultiFileAnalysisResult result)
        {
            // Use configuration for business metrics calculation
            var metricsConfig = _businessRules.AnalysisLimits.BusinessMetrics;
            var baseHours = result.TotalMethods * metricsConfig.BaseHoursPerMethod;
            var complexityMultiplier = (result.OverallComplexityScore / 100m) + metricsConfig.ComplexityMultiplierBase;
            var savedHours = baseHours * complexityMultiplier;

            // Compliance cost avoidance based on risk level from configuration
            var complianceConfig = _businessRules.ComplianceCost;
            var riskLevel = result.OverallRiskLevel ?? "DEFAULT";
            var complianceAvoidance = complianceConfig.CostAvoidanceByRiskLevel.TryGetValue(riskLevel, out var cost)
                ? cost
                : complianceConfig.CostAvoidanceByRiskLevel.GetValueOrDefault("DEFAULT", 1000m);

            var hourlyRate = _businessRules.CostCalculation.DefaultDeveloperHourlyRate;
            var metrics = new BusinessMetrics
            {
                EstimatedDeveloperHoursSaved = savedHours,
                AverageHourlyRate = hourlyRate,
                MigrationTimeline = GetMigrationTimeline(result.OverallComplexityScore),
                RiskMitigation = $"{result.OverallRiskLevel} risk level - {GetRiskMitigationStrategy(result.OverallRiskLevel)}",
                ComplianceCostAvoidance = complianceAvoidance,
                ProjectCostSavings = savedHours * hourlyRate,
                TotalROI = (savedHours * hourlyRate) + complianceAvoidance,
                ProjectSize = GetProjectSizeAssessment(result.TotalFiles, result.TotalClasses),
                RecommendedApproach = GetRecommendedApproach(result.OverallComplexityScore)
            };

            // Calculate computed values
            metrics.CalculateValues();

            return metrics;
        }

        private string GetMigrationTimeline(int complexityScore)
        {
            var thresholds = _businessRules.ComplexityThresholds;
            var timeline = _businessRules.TimelineEstimation;

            if (complexityScore < thresholds.Low)
                return timeline.ContainsKey("VeryLow") ? timeline["VeryLow"] : "2-4 weeks";
            if (complexityScore < thresholds.Medium)
                return timeline.ContainsKey("Low") ? timeline["Low"] : "4-8 weeks";
            if (complexityScore < thresholds.High)
                return timeline.ContainsKey("Medium") ? timeline["Medium"] : "8-12 weeks";
            return timeline.ContainsKey("High") ? timeline["High"] : "12+ weeks";
        }

        private string GetProjectSizeAssessment(int fileCount, int classCount)
        {
            foreach (var kvp in _businessRules.ProjectSizeClassification)
            {
                var config = kvp.Value;
                if (config.MaxFiles.HasValue && config.MaxClasses.HasValue)
                {
                    if (fileCount < config.MaxFiles.Value && classCount < config.MaxClasses.Value)
                        return config.Label;
                }
                else if (!config.MaxFiles.HasValue && !config.MaxClasses.HasValue)
                {
                    // Enterprise fallback
                    return config.Label;
                }
            }
            // If no match, fallback to "Enterprise Project"
            return _businessRules.ProjectSizeClassification.ContainsKey("Enterprise")
                ? _businessRules.ProjectSizeClassification["Enterprise"].Label
                : "Enterprise Project";
        }

        /// <summary>
        /// Returns a risk mitigation strategy string based on the provided risk level.
        /// Uses configuration if you wish, otherwise falls back to standard recommendations.
        /// </summary>
        /// <param name="riskLevel">The overall risk level ("HIGH", "MEDIUM", "LOW").</param>
        /// <returns>Recommended risk mitigation strategy.</returns>
        private string GetRiskMitigationStrategy(string riskLevel)
        {
            return riskLevel switch
            {
                "HIGH" => "Dedicated migration team with senior architect oversight required",
                "MEDIUM" => "Experienced development team with structured approach recommended",
                "LOW" => "Standard development practices with code review sufficient",
                _ => "Assessment in progress"
            };
        }
    }
}

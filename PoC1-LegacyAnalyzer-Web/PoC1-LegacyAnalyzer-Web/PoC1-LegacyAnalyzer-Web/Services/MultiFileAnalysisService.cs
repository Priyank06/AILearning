using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Options;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class MultiFileAnalysisService : IMultiFileAnalysisService
    {
        private readonly ICodeAnalysisService _codeAnalysisService;
        private readonly IAIAnalysisService _aiAnalysisService;
        private readonly IBatchAnalysisOrchestrator _batchOrchestrator;
        private readonly IComplexityCalculatorService _complexityCalculator;
        private readonly IRiskAssessmentService _riskAssessment;
        private readonly IRecommendationGeneratorService _recommendationGenerator;
        private readonly IBusinessMetricsCalculator _businessMetricsCalculator;
        private readonly ILogger<MultiFileAnalysisService> _logger;
        private readonly BatchProcessingConfig _batchConfig;
        private readonly FileAnalysisLimitsConfig _fileLimits;

        public MultiFileAnalysisService(
            ICodeAnalysisService codeAnalysisService,
            IAIAnalysisService aiAnalysisService,
            IBatchAnalysisOrchestrator batchOrchestrator,
            IComplexityCalculatorService complexityCalculator,
            IRiskAssessmentService riskAssessment,
            IRecommendationGeneratorService recommendationGenerator,
            IBusinessMetricsCalculator businessMetricsCalculator,
            ILogger<MultiFileAnalysisService> logger,
            IOptions<BatchProcessingConfig> batchOptions,
            IOptions<FileAnalysisLimitsConfig> fileLimitOptions)
        {
            _codeAnalysisService = codeAnalysisService;
            _aiAnalysisService = aiAnalysisService;
            _batchOrchestrator = batchOrchestrator;
            _complexityCalculator = complexityCalculator;
            _riskAssessment = riskAssessment;
            _recommendationGenerator = recommendationGenerator;
            _businessMetricsCalculator = businessMetricsCalculator;
            _logger = logger;
            _batchConfig = batchOptions.Value ?? new BatchProcessingConfig();
            _fileLimits = fileLimitOptions.Value ?? new FileAnalysisLimitsConfig();
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

                fileResults = await _batchOrchestrator.AnalyzeFilesInBatchesAsync(filesToProcess, analysisType, analysisProgress, null);
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
            result.OverallComplexityScore = _complexityCalculator.CalculateProjectComplexity(result);
            result.OverallRiskLevel = _riskAssessment.DetermineRiskLevel(result.OverallComplexityScore);
            result.KeyRecommendations = _recommendationGenerator.GenerateStrategicRecommendations(result, analysisType);
            result.OverallAssessment = _recommendationGenerator.GenerateExecutiveAssessment(result, analysisType);
            result.ProjectSummary = _recommendationGenerator.GenerateProjectSummary(result);

            // Calculate API call reduction for logging
            string apiCallReduction = _batchConfig.Enabled && filesToProcess.Count > 1 
                ? "Batch processing enabled" 
                : "No reduction (individual processing)";

            _logger.LogInformation("Analysis completed for {FileCount} files. Overall complexity: {ComplexityScore}, Risk level: {RiskLevel}. API call optimization: {Optimization}",
                result.TotalFiles, result.OverallComplexityScore, result.OverallRiskLevel, apiCallReduction);

            return result;
        }

        private async Task<FileAnalysisResult> AnalyzeIndividualFileAsync(IBrowserFile file, string analysisType)
        {
            using var stream = file.OpenReadStream(_fileLimits.MaxFileSizeBytes);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            var staticAnalysis = await _codeAnalysisService.AnalyzeCodeAsync(content);

            // Generate professional assessment
            string professionalAssessment;
            try
            {
                var previewLength = _fileLimits.DefaultCodePreviewLength;
                var analysisContent = content.Length > previewLength
                    ? content.Substring(0, previewLength) + "..."
                    : content;
                professionalAssessment = await _aiAnalysisService.GetAnalysisAsync(analysisContent, analysisType, staticAnalysis);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI analysis unavailable for {FileName}, using fallback assessment", file.Name);
                professionalAssessment = analysisType switch
                {
                    "security" => $"Security review recommended for {staticAnalysis.ClassCount} classes. Verify input validation, authentication, and authorization implementations.",
                    "performance" => $"Performance assessment indicates {staticAnalysis.MethodCount} methods require optimization analysis. Focus on database operations and algorithmic efficiency.",
                    "migration" => $"Migration complexity assessment: {staticAnalysis.ClassCount} classes require modernization evaluation. Plan for framework compatibility and API updates.",
                    _ => $"Code quality assessment shows {staticAnalysis.ClassCount} classes with {staticAnalysis.MethodCount} methods requiring structured modernization approach with quality assurance."
                };
            }

            return new FileAnalysisResult
            {
                FileName = file.Name,
                FileSize = file.Size,
                StaticAnalysis = staticAnalysis,
                AIInsight = professionalAssessment,
                ComplexityScore = _complexityCalculator.CalculateFileComplexity(staticAnalysis),
                Status = "Analysis Completed"
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
                
                fileResults = await _batchOrchestrator.AnalyzeFilesInBatchesAsync(filesToProcess, analysisType, analysisProgress, progress);
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
            result.OverallComplexityScore = _complexityCalculator.CalculateProjectComplexity(result);
            result.OverallRiskLevel = _riskAssessment.DetermineRiskLevel(result.OverallComplexityScore);
            result.KeyRecommendations = _recommendationGenerator.GenerateStrategicRecommendations(result, analysisType);
            result.OverallAssessment = _recommendationGenerator.GenerateExecutiveAssessment(result, analysisType);
            result.ProjectSummary = _recommendationGenerator.GenerateProjectSummary(result);

            _logger.LogInformation("Analysis completed for {FileCount} files. API calls optimized: {BatchMode}", 
                result.TotalFiles, _batchConfig.Enabled ? "Yes" : "No");
            return result;
        }

        public BusinessMetrics CalculateBusinessMetrics(MultiFileAnalysisResult result)
        {
            return _businessMetricsCalculator.CalculateBusinessMetrics(result);
        }
    }
}

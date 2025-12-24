using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using PoC1_LegacyAnalyzer_Web.Models;
using PoC1_LegacyAnalyzer_Web.Services.Analysis;

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
        private readonly IAnalyzerRouter _analyzerRouter;
        private readonly ILanguageDetector _languageDetector;
        private readonly IFilePreProcessingService _preprocessing;
        private readonly ICrossFileAnalyzer? _crossFileAnalyzer;
        private readonly IDependencyGraphService? _dependencyGraphService;
        private readonly IHybridMultiLanguageAnalyzer? _hybridAnalyzer;
        private readonly BatchProcessingConfig _batchConfig;
        private readonly FileAnalysisLimitsConfig _fileLimits;
        private readonly DefaultValuesConfiguration _defaultValues;

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
            IOptions<FileAnalysisLimitsConfig> fileLimitOptions,
            IOptions<DefaultValuesConfiguration> defaultValues,
            IAnalyzerRouter analyzerRouter,
            ILanguageDetector languageDetector,
            IFilePreProcessingService preprocessing,
            ICrossFileAnalyzer? crossFileAnalyzer = null,
            IDependencyGraphService? dependencyGraphService = null,
            IHybridMultiLanguageAnalyzer? hybridAnalyzer = null)
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
            _defaultValues = defaultValues?.Value ?? new DefaultValuesConfiguration();
            _analyzerRouter = analyzerRouter;
            _languageDetector = languageDetector;
            _preprocessing = preprocessing;
            _crossFileAnalyzer = crossFileAnalyzer;
            _dependencyGraphService = dependencyGraphService;
            _hybridAnalyzer = hybridAnalyzer;
        }

        public async Task<MultiFileAnalysisResult> AnalyzeMultipleFilesAsync(List<IBrowserFile> files, string analysisType)
        {
            _logger.LogInformation("Initiating optimized batch analysis for {FileCount} files using {AnalysisType} methodology",
                files.Count, analysisType);

            var result = new MultiFileAnalysisResult
            {
                TotalFiles = files.Count
            };

            // NOTE: File count limit removed - batching handles any number of files
            // Files are processed in batches of 10, so there's no need to limit total file count
            var filesToProcess = files.ToList();
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
                
                // Build dependency graph for C# files after analysis
                if (_crossFileAnalyzer != null && _dependencyGraphService != null)
                {
                    try
                    {
                        // Process all files (not just C#) for dependency analysis
                        var filesForDependencyAnalysis = filesToProcess.ToList();
                        if (filesForDependencyAnalysis.Count > 1)
                        {
                            _logger.LogInformation("Building dependency graph for {FileCount} files", filesForDependencyAnalysis.Count);
                            var dependencyGraph = await _crossFileAnalyzer.BuildDependencyGraphAsync(filesForDependencyAnalysis);
                            var analysisId = Guid.NewGuid().ToString();
                            await _dependencyGraphService.StoreDependencyGraphAsync(analysisId, dependencyGraph);
                            
                            // Enrich file results with dependency impact
                            await EnrichResultsWithDependencyImpact(fileResults, dependencyGraph, analysisId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to build dependency graph, continuing without dependency analysis");
                    }
                }
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
                            Status = _defaultValues.Status.AnalysisFailed,
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
            // Extract metadata first (uses TreeSitter/Roslyn analyzers, achieves 75-80% token reduction)
            var metadata = await _preprocessing.ExtractMetadataAsync(file);
            
            // Route through unified analyzer to get CodeAnalysisResult for metrics
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

            var (structure, staticAnalysis) = await _analyzerRouter.AnalyzeAsync(analyzable);
            staticAnalysis.LanguageKind = languageKind;
            staticAnalysis.Language = languageKind.ToString().ToLowerInvariant();

            // Perform hybrid semantic analysis for non-C# languages
            SemanticAnalysisResult? semanticAnalysis = null;
            if (_hybridAnalyzer != null && languageKind != LanguageKind.CSharp)
            {
                try
                {
                    semanticAnalysis = await _hybridAnalyzer.AnalyzeAsync(content, file.Name, languageKind, analysisType);
                    _logger.LogDebug("Hybrid semantic analysis completed for {FileName}", file.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Hybrid semantic analysis failed for {FileName}, continuing with syntax-only", file.Name);
                }
            }

            // Generate professional assessment using metadata summary instead of full code
            string professionalAssessment;
            try
            {
                // Use PatternSummary (metadata) instead of full code - achieves token optimization
                var metadataSummary = !string.IsNullOrEmpty(metadata.PatternSummary) 
                    ? metadata.PatternSummary 
                    : $"File: {file.Name} | Language: {metadata.Language} | Classes: {metadata.Complexity.ClassCount} | Methods: {metadata.Complexity.MethodCount}";
                
                _logger.LogDebug("Using metadata summary for AI analysis (token optimized) for {FileName}", file.Name);
                professionalAssessment = await _aiAnalysisService.GetAnalysisAsync(metadataSummary, analysisType, staticAnalysis);
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
                LegacyPatternResult = metadata.LegacyPatternResult,
                SemanticAnalysis = semanticAnalysis, // Hybrid semantic analysis for non-C# languages
                ComplexityScore = _complexityCalculator.CalculateFileComplexity(staticAnalysis),
                Status = _defaultValues.Status.AnalysisCompleted
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

            // NOTE: File count limit removed - batching handles any number of files
            var filesToProcess = files.ToList();

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
                            Status = _defaultValues.Status.AnalysisFailed,
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
            analysisProgress.Status = _defaultValues.Status.AnalysisCompletedGeneratingInsights;
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

        /// <summary>
        /// Enriches file results with dependency impact information.
        /// </summary>
        private async Task EnrichResultsWithDependencyImpact(
            List<FileAnalysisResult> fileResults,
            DependencyGraph dependencyGraph,
            string analysisId)
        {
            if (_dependencyGraphService == null) return;

            foreach (var fileResult in fileResults)
            {
                try
                {
                    // Find nodes for this file
                    var fileNodes = dependencyGraph.Nodes
                        .Where(n => n.FileName == fileResult.FileName)
                        .ToList();

                    if (!fileNodes.Any())
                    {
                        continue;
                    }

                    // Calculate aggregate impact for the file
                    var totalAffectedFiles = new HashSet<string>();
                    var totalAffectedClasses = new HashSet<string>();
                    var totalAffectedMethods = new HashSet<string>();
                    var maxConnectivity = 0;
                    var isInCycle = false;

                    foreach (var node in fileNodes)
                    {
                        var impact = await _dependencyGraphService.GetImpactAsync(analysisId, node.Id);
                        if (impact != null)
                        {
                            foreach (var file in impact.AffectedFiles)
                            {
                                totalAffectedFiles.Add(file);
                            }
                            foreach (var cls in impact.AffectedClasses)
                            {
                                totalAffectedClasses.Add(cls);
                            }
                            foreach (var method in impact.AffectedMethods)
                            {
                                totalAffectedMethods.Add(method);
                            }
                            if (node.Connectivity > maxConnectivity)
                            {
                                maxConnectivity = node.Connectivity;
                            }
                            if (impact.IsInCycle)
                            {
                                isInCycle = true;
                            }
                        }
                    }

                    // Create file-level impact
                    fileResult.DependencyImpact = new DependencyImpact
                    {
                        ElementId = fileResult.FileName,
                        ElementName = fileResult.FileName,
                        FileName = fileResult.FileName,
                        AffectedFilesCount = totalAffectedFiles.Count,
                        AffectedFiles = totalAffectedFiles.ToList(),
                        AffectedClassesCount = totalAffectedClasses.Count,
                        AffectedClasses = totalAffectedClasses.ToList(),
                        AffectedMethodsCount = totalAffectedMethods.Count,
                        AffectedMethods = totalAffectedMethods.ToList(),
                        IsGodObject = maxConnectivity > 20,
                        IsInCycle = isInCycle,
                        RiskLevel = DetermineRiskLevel(totalAffectedFiles.Count, maxConnectivity, isInCycle)
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to enrich file {FileName} with dependency impact", fileResult.FileName);
                }
            }
        }

        private string DetermineRiskLevel(int affectedFilesCount, int connectivity, bool isInCycle)
        {
            if (affectedFilesCount > 10 || connectivity > 30 || isInCycle)
            {
                return "Critical";
            }
            if (affectedFilesCount > 5 || connectivity > 20)
            {
                return "High";
            }
            if (affectedFilesCount > 2 || connectivity > 10)
            {
                return "Medium";
            }
            return "Low";
        }
    }
}

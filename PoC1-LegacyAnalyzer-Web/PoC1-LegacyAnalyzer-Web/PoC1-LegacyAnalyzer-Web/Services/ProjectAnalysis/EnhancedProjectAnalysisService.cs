using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Models;
using PoC1_LegacyAnalyzer_Web.Models.ProjectAnalysis;
using Microsoft.Extensions.Options;
using PoC1_LegacyAnalyzer_Web.Services.Business;
using PoC1_LegacyAnalyzer_Web.Services.CodeAnalysis;
using PoC1_LegacyAnalyzer_Web.Services.Orchestration;

namespace PoC1_LegacyAnalyzer_Web.Services.ProjectAnalysis
{
    public class EnhancedProjectAnalysisService : IEnhancedProjectAnalysisService
    {
        private readonly IMultiFileAnalysisService _multiFileAnalysis;
        private readonly IAgentOrchestrationService _agentOrchestration;
        private readonly ICodeAnalysisAgentService _singleAgent;
        private readonly IProjectMetadataService _projectMetadataService;
        private readonly IFolderAnalysisService _folderAnalysisService;
        private readonly IArchitectureAssessmentService _architectureAssessmentService;
        private readonly IBusinessImpactCalculator _businessImpactCalculator;
        private readonly IProjectInsightsGenerator _projectInsightsGenerator;
        private readonly ILogger<EnhancedProjectAnalysisService> _logger;
        private readonly BusinessCalculationRules _businessRules;

        public EnhancedProjectAnalysisService(
            IMultiFileAnalysisService multiFileAnalysis,
            IAgentOrchestrationService agentOrchestration,
            ICodeAnalysisAgentService singleAgent,
            IProjectMetadataService projectMetadataService,
            IFolderAnalysisService folderAnalysisService,
            IArchitectureAssessmentService architectureAssessmentService,
            IBusinessImpactCalculator businessImpactCalculator,
            IProjectInsightsGenerator projectInsightsGenerator,
            ILogger<EnhancedProjectAnalysisService> logger,
            IOptions<BusinessCalculationRules> businessRulesOptions)
        {
            _multiFileAnalysis = multiFileAnalysis;
            _agentOrchestration = agentOrchestration;
            _singleAgent = singleAgent;
            _projectMetadataService = projectMetadataService;
            _folderAnalysisService = folderAnalysisService;
            _architectureAssessmentService = architectureAssessmentService;
            _businessImpactCalculator = businessImpactCalculator;
            _projectInsightsGenerator = projectInsightsGenerator;
            _logger = logger;
            _businessRules = businessRulesOptions.Value ?? new BusinessCalculationRules();
        }

        public async Task<ProjectAnalysisResult> AnalyzeProjectAsync(ProjectAnalysisRequest request, IProgress<ProjectAnalysisProgress> progress = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting enhanced project analysis for {ProjectType} with {FileCount} files",
                request.ProjectType, request.Files.Count);

            var result = new ProjectAnalysisResult();
            var analysisProgress = new ProjectAnalysisProgress
            {
                TotalFiles = request.Files.Count,
                CurrentPhase = "Initialization"
            };

            try
            {
                // Phase 1: Extract project metadata
                analysisProgress.CurrentPhase = "Extracting Project Metadata";
                analysisProgress.Status = "Analyzing project structure and dependencies";
                progress?.Report(analysisProgress);

                result.ProjectInfo = await _projectMetadataService.ExtractProjectMetadataAsync(request.Files, cancellationToken);

                // Phase 2: Folder-level analysis
                analysisProgress.CurrentPhase = "Folder Structure Analysis";
                analysisProgress.Status = "Analyzing architectural organization";
                progress?.Report(analysisProgress);

                result.FolderAnalysis = await _folderAnalysisService.AnalyzeFolderStructureAsync(request.ProjectStructure, cancellationToken);

                // Phase 3: Detailed file analysis
                analysisProgress.CurrentPhase = "Detailed File Analysis";
                progress?.Report(analysisProgress);

                if (request.AnalysisMode == "multi-agent")
                {
                    result.DetailedFileAnalysis = await PerformMultiAgentFileAnalysisAsync(
                        request, analysisProgress, progress, cancellationToken);
                }
                else
                {
                    result.DetailedFileAnalysis = await PerformSingleAgentFileAnalysisAsync(
                        request, analysisProgress, progress, cancellationToken);
                }

                // Phase 4: Architecture assessment
                analysisProgress.CurrentPhase = "Architecture Assessment";
                analysisProgress.Status = "Evaluating system architecture patterns";
                progress?.Report(analysisProgress);

                result.Architecture = await _architectureAssessmentService.AssessProjectArchitectureAsync(result, cancellationToken);

                // Phase 5: Business impact analysis
                analysisProgress.CurrentPhase = "Business Impact Analysis";
                analysisProgress.Status = "Calculating business value and risk assessment";
                progress?.Report(analysisProgress);

                result.BusinessImpact = await _businessImpactCalculator.AssessBusinessImpactAsync(result, cancellationToken);

                // Phase 6: Executive summary generation
                analysisProgress.CurrentPhase = "Executive Summary Generation";
                analysisProgress.Status = "Creating executive summary and recommendations";
                progress?.Report(analysisProgress);

                result.ExecutiveSummary = await _projectInsightsGenerator.GenerateProjectInsightsAsync(result, request.ProjectType, cancellationToken);
                result.NextSteps = GenerateActionableNextSteps(result);

                analysisProgress.CurrentPhase = "Complete";
                analysisProgress.Status = $"Analysis complete for {request.ProjectType}";
                analysisProgress.ProcessedFiles = analysisProgress.TotalFiles;
                progress?.Report(analysisProgress);

                _logger.LogInformation("Enhanced project analysis completed successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enhanced project analysis failed");
                throw;
            }
        }

        public async Task<ProjectMetadata> ExtractProjectMetadataAsync(List<IBrowserFile> files, CancellationToken cancellationToken = default)
        {
            return await _projectMetadataService.ExtractProjectMetadataAsync(files, cancellationToken);
        }

        private async Task<List<FileAnalysisResult>> PerformMultiAgentFileAnalysisAsync(ProjectAnalysisRequest request, ProjectAnalysisProgress analysisProgress, IProgress<ProjectAnalysisProgress> progress, CancellationToken cancellationToken)
        {
            var results = new List<FileAnalysisResult>();

            // Use existing multi-file analysis service
            var multiFileResult = await _multiFileAnalysis.AnalyzeMultipleFilesWithProgressAsync(
                request.Files.Take(_businessRules.AnalysisLimits.ResultLimits.MaxFilesForMultiAgentAnalysis).ToList(),
                request.AnalysisType,
                new Progress<Models.AnalysisProgress>(p =>
                {
                    analysisProgress.ProcessedFiles = p.CompletedFiles;
                    analysisProgress.CurrentFile = p.CurrentFile;
                    analysisProgress.Status = p.Status;
                    progress?.Report(analysisProgress);
                }));

            return multiFileResult.FileResults;
        }

        private async Task<List<FileAnalysisResult>> PerformSingleAgentFileAnalysisAsync(ProjectAnalysisRequest request, ProjectAnalysisProgress analysisProgress, IProgress<ProjectAnalysisProgress> progress, CancellationToken cancellationToken)
        {
            var results = new List<FileAnalysisResult>();

            // Combine files for single agent analysis
            var combinedCode = new List<string>();
            foreach (var file in request.Files.Take(_businessRules.AnalysisLimits.ResultLimits.MaxFilesForSingleAgentAnalysis))
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();
                combinedCode.Add($"// File: {file.Name}\n{content}");
            }

            var businessGoal = GetBusinessGoalFromAnalysisType(request.AnalysisType);
            var agentResult = await _singleAgent.AnalyzeWithAgentAsync(
                string.Join("\n\n", combinedCode),
                businessGoal,
                request.ProjectType,
                cancellationToken);

            // Convert agent result to file analysis results (simplified)
            foreach (var file in request.Files.Take(_businessRules.AnalysisLimits.ResultLimits.MaxFilesForSingleAgentAnalysis))
            {
                results.Add(new FileAnalysisResult
                {
                    FileName = file.Name,
                    FileSize = file.Size,
                    AIInsight = agentResult.BusinessImpact,
                    ComplexityScore = _businessRules.AnalysisLimits.ResultLimits.DefaultComplexityScore, // Simplified - would normally analyze individually
                    Status = "Analyzed by Single Agent",
                    StaticAnalysis = new Models.CodeAnalysisResult()
                });

                analysisProgress.ProcessedFiles++;
                analysisProgress.CurrentFile = file.Name;
                progress?.Report(analysisProgress);
            }

            return results;
        }

        public async Task<string> GenerateProjectInsightsAsync(ProjectAnalysisResult analysis, string businessContext, CancellationToken cancellationToken = default)
        {
            return await _projectInsightsGenerator.GenerateProjectInsightsAsync(analysis, businessContext, cancellationToken);
        }

        private string GetBusinessGoalFromAnalysisType(string analysisType)
        {
            // If you want to use BusinessCalculationRules for effort estimation, you can add logic here.
            // Otherwise, fallback to default mapping.
            return analysisType switch
            {
                "security" => "Comprehensive security assessment and compliance validation",
                "performance" => "Performance optimization and scalability improvement",
                "architecture" => "Architectural modernization and technical debt reduction",
                _ => "Comprehensive code quality and modernization assessment"
            };
        }

        /// <summary>
        /// Generates a list of actionable next steps based on business impact and architecture assessment.
        /// </summary>
        /// <param name="result">The project analysis result.</param>
        /// <returns>List of recommended next steps.</returns>
        private List<string> GenerateActionableNextSteps(ProjectAnalysisResult result)
        {
            var nextSteps = new List<string>();

            // Priority based on business impact and risk
            if (result.BusinessImpact?.RiskLevel == "HIGH")
            {
                nextSteps.Add("URGENT: Schedule executive review meeting within 48 hours");
                nextSteps.Add("Assemble dedicated modernization team with senior architect");
                nextSteps.Add("Conduct detailed technical debt assessment");
            }
            else if (result.BusinessImpact?.RiskLevel == "MEDIUM")
            {
                nextSteps.Add("Schedule architecture review meeting within 2 weeks");
                nextSteps.Add("Assign experienced development team to project");
                nextSteps.Add("Create detailed modernization roadmap");
            }
            else
            {
                nextSteps.Add("Include in next quarterly planning cycle");
                nextSteps.Add("Monitor for increasing complexity trends");
                nextSteps.Add("Apply standard development best practices");
            }

            // Architecture-specific recommendations
            if (result.Architecture != null && result.Architecture.ArchitecturalDebtScore > _businessRules.ComplexityThresholds.High)
            {
                nextSteps.Add("Implement architectural refactoring program");
                nextSteps.Add("Establish coding standards and review processes");
            }

            // Testing recommendations
            if (result.Architecture != null && result.Architecture.TestCoverage != null && result.Architecture.TestCoverage.Contains("No test"))
            {
                nextSteps.Add("Establish comprehensive testing strategy");
                nextSteps.Add("Implement automated testing pipeline");
            }

            return nextSteps.Take(_businessRules.AnalysisLimits.ResultLimits.MaxNextSteps).ToList(); // Limit to most important actions
        }
    }
}

using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Models;
using PoC1_LegacyAnalyzer_Web.Models.ProjectAnalysis;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class EnhancedProjectAnalysisService : IEnhancedProjectAnalysisService
    {
        private readonly IMultiFileAnalysisService _multiFileAnalysis;
        private readonly IAgentOrchestrationService _agentOrchestration;
        private readonly ICodeAnalysisAgentService _singleAgent;
        private readonly ILogger<EnhancedProjectAnalysisService> _logger;
        private readonly AgentConfiguration _agentConfig;
        private readonly BusinessCalculationRules _businessRules;

        public EnhancedProjectAnalysisService(
            IMultiFileAnalysisService multiFileAnalysis,
            IAgentOrchestrationService agentOrchestration,
            ICodeAnalysisAgentService singleAgent,
            ILogger<EnhancedProjectAnalysisService> logger,
            IConfiguration configuration)
        {
            _multiFileAnalysis = multiFileAnalysis;
            _agentOrchestration = agentOrchestration;
            _singleAgent = singleAgent;
            _logger = logger;

            _agentConfig = new AgentConfiguration();
            configuration.GetSection("AgentConfiguration").Bind(_agentConfig);

            _businessRules = new BusinessCalculationRules();
            configuration.GetSection("BusinessCalculationRules").Bind(_businessRules);
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

                result.ProjectInfo = await ExtractProjectMetadataAsync(request.Files, cancellationToken);

                // Phase 2: Folder-level analysis
                analysisProgress.CurrentPhase = "Folder Structure Analysis";
                analysisProgress.Status = "Analyzing architectural organization";
                progress?.Report(analysisProgress);

                result.FolderAnalysis = await AnalyzeFolderStructureAsync(request.ProjectStructure, cancellationToken);

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

                result.Architecture = await AssessProjectArchitectureAsync(result, cancellationToken);

                // Phase 5: Business impact analysis
                analysisProgress.CurrentPhase = "Business Impact Analysis";
                analysisProgress.Status = "Calculating business value and risk assessment";
                progress?.Report(analysisProgress);

                result.BusinessImpact = await AssessBusinessImpactAsync(result, cancellationToken);

                // Phase 6: Executive summary generation
                analysisProgress.CurrentPhase = "Executive Summary Generation";
                analysisProgress.Status = "Creating executive summary and recommendations";
                progress?.Report(analysisProgress);

                result.ExecutiveSummary = await GenerateProjectInsightsAsync(result, request.ProjectType, cancellationToken);
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
            var metadata = new ProjectMetadata();

            // Analyze project files
            var projectFiles = files.Where(f => f.Name.EndsWith(".csproj") || f.Name.EndsWith(".sln")).ToList();
            metadata.ProjectFiles = projectFiles.Select(f => f.Name).ToList();

            // Extract solution name
            var solutionFile = files.FirstOrDefault(f => f.Name.EndsWith(".sln"));
            if (solutionFile != null)
            {
                metadata.SolutionName = Path.GetFileNameWithoutExtension(solutionFile.Name);
            }
            else
            {
                var projectFile = files.FirstOrDefault(f => f.Name.EndsWith(".csproj"));
                if (projectFile != null)
                {
                    metadata.SolutionName = Path.GetFileNameWithoutExtension(projectFile.Name);
                }
            }

            // Analyze C# files for namespaces and dependencies
            var csFiles = files.Where(f => f.Name.EndsWith(".cs")).ToList();
            var namespaces = new HashSet<string>();
            var dependencies = new HashSet<string>();

            foreach (var file in csFiles.Take(_businessRules.ProcessingLimits.MetadataSampleFileCount)) // Use config value
            {
                try
                {
                    using var stream = file.OpenReadStream();
                    using var reader = new StreamReader(stream);
                    var content = await reader.ReadToEndAsync();

                    // Extract namespaces
                    var namespaceMatches = Regex.Matches(content, @"namespace\s+([^\s{;]+)");
                    foreach (Match match in namespaceMatches)
                    {
                        namespaces.Add(match.Groups[1].Value);
                    }

                    // Extract using statements
                    var usingMatches = Regex.Matches(content, @"using\s+([^;]+);");
                    foreach (Match match in usingMatches)
                    {
                        var usingStatement = match.Groups[1].Value.Trim();
                        if (!usingStatement.StartsWith("System") && usingStatement.Contains("."))
                        {
                            dependencies.Add(usingStatement);
                        }
                    }

                    // Estimate lines (rough calculation)
                    metadata.TotalLines += content.Split('\n').Length;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to analyze file: {FileName}", file.Name);
                }
            }

            metadata.MainNamespaces = namespaces.Take(10).ToList();
            metadata.Dependencies = dependencies.Take(20).ToList();
            metadata.LastModified = DateTime.Now; // Simulate - in real scenario, use file dates

            return metadata;
        }

        private async Task<Dictionary<string, FolderAnalysisResult>> AnalyzeFolderStructureAsync(Dictionary<string, List<IBrowserFile>> projectStructure, CancellationToken cancellationToken)
        {
            var folderAnalysis = new Dictionary<string, FolderAnalysisResult>();

            foreach (var folder in projectStructure.Take(_businessRules.AnalysisLimits.ResultLimits.MaxFoldersToAnalyze))
            {
                var csFiles = folder.Value.Where(f => f.Name.EndsWith(".cs")).ToList();
                if (!csFiles.Any()) continue;

                var analysis = new FolderAnalysisResult
                {
                    FolderName = folder.Key,
                    FileCount = csFiles.Count,
                    Purpose = AnalyzeFolderPurpose(folder.Key, csFiles),
                    ArchitecturalRole = DetermineArchitecturalRole(folder.Key),
                    ComplexityScore = CalculateFolderComplexity(csFiles.Count)
                };

                // Extract key classes from folder
                analysis.KeyClasses = csFiles.Take(_businessRules.AnalysisLimits.ResultLimits.MaxKeyClassesPerFolder).Select(f => Path.GetFileNameWithoutExtension(f.Name)).ToList();

                folderAnalysis[folder.Key] = analysis;
            }

            return folderAnalysis;
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

        private async Task<ProjectArchitectureAssessment> AssessProjectArchitectureAsync(ProjectAnalysisResult result, CancellationToken cancellationToken)
        {
            var assessment = new ProjectArchitectureAssessment();

            // Analyze folder structure to determine architectural pattern
            var folderNames = result.FolderAnalysis.Keys.ToList();
            assessment.ArchitecturalPattern = DetermineArchitecturalPattern(folderNames);
            assessment.LayerIdentification = IdentifyArchitecturalLayers(folderNames);
            assessment.SeparationOfConcerns = AssessSeparationOfConcerns(result.FolderAnalysis);
            assessment.DesignPatterns = IdentifyDesignPatterns(result.DetailedFileAnalysis);
            assessment.TestCoverage = AssessTestCoverage(folderNames);
            assessment.ArchitecturalDebtScore = CalculateArchitecturalDebt(result);

            return assessment;
        }

        private async Task<BusinessImpactAssessment> AssessBusinessImpactAsync(ProjectAnalysisResult result, CancellationToken cancellationToken)
        {
            var assessment = new BusinessImpactAssessment();

            var totalFiles = result.DetailedFileAnalysis.Count;
            var totalComplexity = result.DetailedFileAnalysis.Sum(f => f.ComplexityScore);
            var avgComplexity = totalFiles > 0 ? totalComplexity / totalFiles : 0;

            // Calculate estimated value based on project size and complexity
            assessment.EstimatedValue = CalculateProjectValue(result.ProjectInfo, avgComplexity);
            assessment.RiskLevel = DetermineBusinessRiskLevel(avgComplexity, result.Architecture.ArchitecturalDebtScore);
            assessment.MaintenanceOverhead = AssessMaintenanceOverhead(result);
            assessment.BusinessCriticalAreas = IdentifyBusinessCriticalAreas(result.FolderAnalysis);
            assessment.RecommendedApproach = DetermineRecommendedApproach(assessment.RiskLevel, totalFiles);
            assessment.InvestmentPriority = DetermineInvestmentPriority(assessment);

            return assessment;
        }

        public async Task<string> GenerateProjectInsightsAsync(ProjectAnalysisResult analysis, string businessContext, CancellationToken cancellationToken = default)
        {
            var projectSummary = $"Project: {analysis.ProjectInfo.SolutionName}, " +
                               $"Files: {analysis.DetailedFileAnalysis.Count}, " +
                               $"Architecture: {analysis.Architecture.ArchitecturalPattern}, " +
                               $"Risk: {analysis.BusinessImpact.RiskLevel}, " +
                               $"Value: {analysis.BusinessImpact.EstimatedValue:C0}";

            return $@"# Enterprise Project Analysis Summary

## Project Overview
**Solution**: {analysis.ProjectInfo.SolutionName}  
**Type**: {businessContext}  
**Scale**: {analysis.DetailedFileAnalysis.Count} source files, ~{analysis.ProjectInfo.TotalLines:N0} lines of code  
**Architecture**: {analysis.Architecture.ArchitecturalPattern}  

## Key Findings
- **Business Risk Level**: {analysis.BusinessImpact.RiskLevel}
- **Estimated Project Value**: {analysis.BusinessImpact.EstimatedValue:C0}
- **Maintenance Overhead**: {analysis.BusinessImpact.MaintenanceOverhead}
- **Architectural Debt Score**: {analysis.Architecture.ArchitecturalDebtScore}/100

## Strategic Recommendations
{analysis.BusinessImpact.RecommendedApproach}

## Implementation Priority
{analysis.BusinessImpact.InvestmentPriority}

## Business Critical Areas
{string.Join(", ", analysis.BusinessImpact.BusinessCriticalAreas.Take(5))}

This analysis provides executive-level insights for strategic technology investment decisions.";
        }

        // Helper methods for architecture and business analysis
        private string AnalyzeFolderPurpose(string folderName, List<IBrowserFile> files)
        {
            return folderName.ToLower() switch
            {
                var name when name.Contains("controller") => "API Controllers and Web Endpoints",
                var name when name.Contains("service") => "Business Logic Services",
                var name when name.Contains("model") || name.Contains("dto") => "Data Models and Transfer Objects",
                var name when name.Contains("repository") || name.Contains("data") => "Data Access Layer",
                var name when name.Contains("view") => "User Interface Views",
                var name when name.Contains("test") => "Unit and Integration Tests",
                var name when name.Contains("util") || name.Contains("helper") => "Utility Functions and Helpers",
                _ => "Application Logic Components"
            };
        }

        private string DetermineArchitecturalRole(string folderName)
        {
            return folderName.ToLower() switch
            {
                var name when name.Contains("controller") => "Presentation Layer",
                var name when name.Contains("service") || name.Contains("business") => "Business Logic Layer",
                var name when name.Contains("repository") || name.Contains("data") => "Data Access Layer",
                var name when name.Contains("model") => "Domain Layer",
                var name when name.Contains("common") || name.Contains("shared") => "Cross-Cutting Concerns",
                _ => "Application Layer"
            };
        }

        private int CalculateFolderComplexity(int fileCount)
        {
            var config = _businessRules.AnalysisLimits.FolderComplexity;
            var complexity = fileCount * config.FileCountMultiplier;
            if (fileCount > config.FileCountThreshold)
            {
                complexity += config.AdditionalComplexityWhenThresholdExceeded;
            }
            return Math.Min(config.MaxComplexityScore, complexity);
        }

        private string DetermineArchitecturalPattern(List<string> folderNames)
        {
            var hasControllers = folderNames.Any(f => f.ToLower().Contains("controller"));
            var hasServices = folderNames.Any(f => f.ToLower().Contains("service"));
            var hasRepositories = folderNames.Any(f => f.ToLower().Contains("repository"));
            var hasModels = folderNames.Any(f => f.ToLower().Contains("model"));

            if (hasControllers && hasServices && hasRepositories)
                return "Layered Architecture (MVC/Clean Architecture)";
            else if (hasControllers && hasServices)
                return "Service-Oriented Architecture";
            else if (hasModels && hasServices)
                return "Domain-Driven Design";
            else
                return "Monolithic Architecture";
        }

        private List<string> IdentifyArchitecturalLayers(List<string> folderNames)
        {
            var layers = new List<string>();

            if (folderNames.Any(f => f.ToLower().Contains("controller") || f.ToLower().Contains("api")))
                layers.Add("Presentation Layer");
            if (folderNames.Any(f => f.ToLower().Contains("service") || f.ToLower().Contains("business")))
                layers.Add("Business Logic Layer");
            if (folderNames.Any(f => f.ToLower().Contains("repository") || f.ToLower().Contains("data")))
                layers.Add("Data Access Layer");
            if (folderNames.Any(f => f.ToLower().Contains("model") || f.ToLower().Contains("domain")))
                layers.Add("Domain Layer");

            return layers.Any() ? layers : new List<string> { "Single Layer Architecture" };
        }

        private string AssessSeparationOfConcerns(Dictionary<string, FolderAnalysisResult> folderAnalysis)
        {
            var layerCount = folderAnalysis.Values.Select(f => f.ArchitecturalRole).Distinct().Count();
            var config = _businessRules.AnalysisLimits.ArchitecturalAssessment;
            return layerCount switch
            {
                var count when count >= config.ExcellentSeparationLayerCount => "Excellent separation of concerns with clear architectural layers",
                var count when count >= config.GoodSeparationLayerCount => "Good separation with distinct business and data layers",
                var count when count >= config.BasicSeparationLayerCount => "Basic separation between presentation and logic",
                _ => "Poor separation - mixed concerns across components"
            };
        }

        private List<string> IdentifyDesignPatterns(List<FileAnalysisResult> fileResults)
        {
            var patterns = new List<string>();

            var fileNames = fileResults.Select(f => f.FileName.ToLower()).ToList();

            if (fileNames.Any(f => f.Contains("repository")))
                patterns.Add("Repository Pattern");
            if (fileNames.Any(f => f.Contains("service") || f.Contains("manager")))
                patterns.Add("Service Layer Pattern");
            if (fileNames.Any(f => f.Contains("factory")))
                patterns.Add("Factory Pattern");
            if (fileNames.Any(f => f.Contains("builder")))
                patterns.Add("Builder Pattern");
            if (fileNames.Any(f => f.Contains("adapter") || f.Contains("wrapper")))
                patterns.Add("Adapter Pattern");

            return patterns.Any() ? patterns : new List<string> { "No clear design patterns detected" };
        }

        private string AssessTestCoverage(List<string> folderNames)
        {
            var hasTestFolders = folderNames.Any(f => f.ToLower().Contains("test"));
            var testFolderCount = folderNames.Count(f => f.ToLower().Contains("test"));
            var config = _businessRules.AnalysisLimits.ArchitecturalAssessment;

            return hasTestFolders switch
            {
                true when testFolderCount >= config.GoodTestCoverageFolderCount => "Good test coverage with multiple test projects",
                true => "Basic test coverage detected",
                false => "No test projects detected - testing strategy needed"
            };
        }

        private int CalculateArchitecturalDebt(ProjectAnalysisResult result)
        {
            var debtConfig = _businessRules.ArchitecturalDebt;
            var debtFactors = 0;

            // Factor 1: Poor separation of concerns
            if (result.Architecture.SeparationOfConcerns.Contains("Poor"))
                debtFactors += debtConfig.PoorSeparationOfConcernsDebt;
            else if (result.Architecture.SeparationOfConcerns.Contains("Basic"))
                debtFactors += debtConfig.BasicSeparationOfConcernsDebt;

            // Factor 2: No clear architectural pattern
            if (result.Architecture.ArchitecturalPattern.Contains("Monolithic"))
                debtFactors += debtConfig.MonolithicArchitectureDebt;

            // Factor 3: No design patterns
            if (result.Architecture.DesignPatterns.Any(p => p.Contains("No clear")))
                debtFactors += debtConfig.NoDesignPatternsDebt;

            // Factor 4: No testing
            if (result.Architecture.TestCoverage.Contains("No test"))
                debtFactors += debtConfig.NoTestingDebt;

            return Math.Min(debtConfig.MaxDebtScore, debtFactors);
        }

        private decimal CalculateProjectValue(ProjectMetadata projectInfo, int avgComplexity)
        {
            // Base value on lines of code and complexity
            var baseValue = (decimal)projectInfo.TotalLines * _businessRules.CostCalculation.BaseValuePerLine;
            var complexityMultiplier = (avgComplexity / 100m) + _businessRules.CostCalculation.ComplexityMultiplierBase;
            return Math.Min(_businessRules.CostCalculation.MaxEstimatedValue, baseValue * complexityMultiplier);
        }

        private string DetermineBusinessRiskLevel(int avgComplexity, int architecturalDebt)
        {
            // Use configuration-based risk mapping if available
            if (_agentConfig?.BusinessImpactRules?.RiskLevelMapping != null)
            {
                var description = $"Complexity: {avgComplexity}, Debt: {architecturalDebt}";
                foreach (var mapping in _agentConfig.BusinessImpactRules.RiskLevelMapping)
                {
                    if (description.Contains(mapping.Pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        return mapping.RiskLevel;
                    }
                }
            }
            // Fallback logic
            var riskScore = (avgComplexity + architecturalDebt) / 2;
            return riskScore switch
            {
                var low when low < _businessRules.RiskThresholds.LowRiskMax => "LOW",
                var med when med < _businessRules.RiskThresholds.MediumRiskMax => "MEDIUM",
                _ => "HIGH"
            };
        }

        private string AssessMaintenanceOverhead(ProjectAnalysisResult result)
        {
            var overhead = result.Architecture.ArchitecturalDebtScore switch
            {
                var low when low < _businessRules.ComplexityThresholds.Low => "Low maintenance overhead with good architectural practices",
                var high when high < _businessRules.ComplexityThresholds.High => "Moderate maintenance overhead requiring structured approach",
                _ => "High maintenance overhead with significant refactoring needs"
            };
            return overhead;
        }

        private List<string> IdentifyBusinessCriticalAreas(Dictionary<string, FolderAnalysisResult> folderAnalysis)
        {
            return folderAnalysis.Values
                .Where(f => f.ComplexityScore > _businessRules.ComplexityThresholds.High || f.ArchitecturalRole.Contains("Business"))
                .Select(f => f.FolderName)
                .Take(5)
                .ToList();
        }

        private string DetermineRecommendedApproach(string riskLevel, int totalFiles)
        {
            return (riskLevel, totalFiles) switch
            {
                ("LOW", var veryLow) when veryLow < _businessRules.ComplexityThresholds.VeryLow => "Standard agile development with code review practices",
                ("LOW", _) => "Structured development with architectural guidance",
                ("MEDIUM", _) => "Phased modernization with risk mitigation strategies",
                ("HIGH", _) => "Comprehensive modernization program with dedicated architecture team",
                _ => throw new NotImplementedException()
            };
        }

        /// <summary>
        /// Determines the investment priority based on business impact assessment and configuration rules.
        /// </summary>
        /// <param name="assessment">The business impact assessment.</param>
        /// <returns>Recommended investment priority string.</returns>
        private string DetermineInvestmentPriority(BusinessImpactAssessment assessment)
        {
            // Use configuration-based investment priority rules if available
            if (_agentConfig?.BusinessImpactRules?.InvestmentPriorityRules != null)
            {
                foreach (var rule in _agentConfig.BusinessImpactRules.InvestmentPriorityRules)
                {
                    // Only support equality for now
                    if (rule.Condition.Contains("riskLevel =="))
                    {
                        var expected = rule.Condition.Split("==")[1].Trim(' ', '\'', '"');
                        if (assessment.RiskLevel.Equals(expected, StringComparison.OrdinalIgnoreCase))
                        {
                            return rule.Action;
                        }
                    }
                }
            }
            // Fallback logic
            var fallbackConfig = _businessRules.AnalysisLimits.InvestmentPriorityFallback;
            return (assessment.RiskLevel, assessment.EstimatedValue) switch
            {
                ("HIGH", var value) when value > fallbackConfig.HighRiskHighValueThreshold => "CRITICAL - Immediate executive attention required",
                ("HIGH", _) => "HIGH - Significant business risk requiring prompt action",
                ("MEDIUM", var value) when value > fallbackConfig.MediumRiskHighValueThreshold => "MEDIUM - Strategic investment opportunity",
                _ => "LOW - Include in regular development planning cycle"
            };
        }

        /// <summary>
        /// Gets the business goal string from the analysis type, using configuration if available.
        /// </summary>
        /// <param name="analysisType">The analysis type.</param>
        /// <returns>Business goal string.</returns>
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

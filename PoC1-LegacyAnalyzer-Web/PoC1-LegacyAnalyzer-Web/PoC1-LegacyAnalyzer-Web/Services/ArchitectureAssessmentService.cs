using PoC1_LegacyAnalyzer_Web.Models.ProjectAnalysis;
using Microsoft.Extensions.Options;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class ArchitectureAssessmentService : IArchitectureAssessmentService
    {
        private readonly ILogger<ArchitectureAssessmentService> _logger;
        private readonly BusinessCalculationRules _businessRules;

        public ArchitectureAssessmentService(
            ILogger<ArchitectureAssessmentService> logger,
            IOptions<BusinessCalculationRules> businessRulesOptions)
        {
            _logger = logger;
            _businessRules = businessRulesOptions.Value ?? new BusinessCalculationRules();
        }

        public async Task<ProjectArchitectureAssessment> AssessProjectArchitectureAsync(
            ProjectAnalysisResult result,
            CancellationToken cancellationToken = default)
        {
            var assessment = new ProjectArchitectureAssessment();

            // Analyze folder structure to determine architectural pattern
            var folderNames = result.FolderAnalysis.Keys.ToList();
            assessment.ArchitecturalPattern = DetermineArchitecturalPattern(folderNames);
            assessment.LayerIdentification = IdentifyArchitecturalLayers(folderNames);
            assessment.SeparationOfConcerns = AssessSeparationOfConcerns(result.FolderAnalysis);
            assessment.DesignPatterns = IdentifyDesignPatterns(result.DetailedFileAnalysis);
            assessment.TestCoverage = AssessTestCoverage(folderNames);
            assessment.ArchitecturalDebtScore = CalculateArchitecturalDebt(assessment);

            return await Task.FromResult(assessment);
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

        private int CalculateArchitecturalDebt(ProjectArchitectureAssessment assessment)
        {
            var debtConfig = _businessRules.ArchitecturalDebt;
            var debtFactors = 0;

            // Factor 1: Poor separation of concerns
            if (assessment.SeparationOfConcerns.Contains("Poor"))
                debtFactors += debtConfig.PoorSeparationOfConcernsDebt;
            else if (assessment.SeparationOfConcerns.Contains("Basic"))
                debtFactors += debtConfig.BasicSeparationOfConcernsDebt;

            // Factor 2: No clear architectural pattern
            if (assessment.ArchitecturalPattern.Contains("Monolithic"))
                debtFactors += debtConfig.MonolithicArchitectureDebt;

            // Factor 3: No design patterns
            if (assessment.DesignPatterns.Any(p => p.Contains("No clear")))
                debtFactors += debtConfig.NoDesignPatternsDebt;

            // Factor 4: No testing
            if (assessment.TestCoverage.Contains("No test"))
                debtFactors += debtConfig.NoTestingDebt;

            return Math.Min(debtConfig.MaxDebtScore, debtFactors);
        }
    }
}


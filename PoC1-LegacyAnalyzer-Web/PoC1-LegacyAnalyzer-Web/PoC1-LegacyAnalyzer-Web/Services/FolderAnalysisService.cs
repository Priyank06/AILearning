using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Models.ProjectAnalysis;
using Microsoft.Extensions.Options;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class FolderAnalysisService : IFolderAnalysisService
    {
        private readonly ILogger<FolderAnalysisService> _logger;
        private readonly BusinessCalculationRules _businessRules;

        public FolderAnalysisService(
            ILogger<FolderAnalysisService> logger,
            IOptions<BusinessCalculationRules> businessRulesOptions)
        {
            _logger = logger;
            _businessRules = businessRulesOptions.Value ?? new BusinessCalculationRules();
        }

        public async Task<Dictionary<string, FolderAnalysisResult>> AnalyzeFolderStructureAsync(
            Dictionary<string, List<IBrowserFile>> projectStructure,
            CancellationToken cancellationToken = default)
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
                analysis.KeyClasses = csFiles.Take(_businessRules.AnalysisLimits.ResultLimits.MaxKeyClassesPerFolder)
                    .Select(f => Path.GetFileNameWithoutExtension(f.Name)).ToList();

                folderAnalysis[folder.Key] = analysis;
            }

            return await Task.FromResult(folderAnalysis);
        }

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
    }
}


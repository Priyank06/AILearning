using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Models.ProjectAnalysis;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class ProjectMetadataService : IProjectMetadataService
    {
        private readonly ILogger<ProjectMetadataService> _logger;
        private readonly BusinessCalculationRules _businessRules;

        public ProjectMetadataService(
            ILogger<ProjectMetadataService> logger,
            IOptions<BusinessCalculationRules> businessRulesOptions)
        {
            _logger = logger;
            _businessRules = businessRulesOptions.Value ?? new BusinessCalculationRules();
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

            foreach (var file in csFiles.Take(_businessRules.ProcessingLimits.MetadataSampleFileCount))
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
    }
}


using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class MultiFileAnalysisService : IMultiFileAnalysisService
    {
        private readonly ICodeAnalysisService _codeAnalysisService;
        private readonly IAIAnalysisService _aiAnalysisService;

        public MultiFileAnalysisService(ICodeAnalysisService codeAnalysisService, IAIAnalysisService aiAnalysisService)
        {
            _codeAnalysisService = codeAnalysisService;
            _aiAnalysisService = aiAnalysisService;
        }

        public async Task<MultiFileAnalysisResult> AnalyzeMultipleFilesAsync(List<IBrowserFile> files, string analysisType)
        {
            var result = new MultiFileAnalysisResult
            {
                TotalFiles = files.Count
            };

            var fileResults = new List<FileAnalysisResult>();

            foreach (var file in files.Take(10)) // Limit to 10 files for performance
            {
                try
                {
                    var fileResult = await AnalyzeIndividualFileAsync(file, analysisType);
                    fileResults.Add(fileResult);

                    // Aggregate totals
                    result.TotalClasses += fileResult.StaticAnalysis.ClassCount;
                    result.TotalMethods += fileResult.StaticAnalysis.MethodCount;
                    result.TotalProperties += fileResult.StaticAnalysis.PropertyCount;
                    result.TotalUsingStatements += fileResult.StaticAnalysis.UsingCount;
                }
                catch (Exception ex)
                {
                    fileResults.Add(new FileAnalysisResult
                    {
                        FileName = file.Name,
                        FileSize = file.Size,
                        Status = "Error",
                        ErrorMessage = ex.Message,
                        StaticAnalysis = new CodeAnalysisResult()
                    });
                }
            }

            result.FileResults = fileResults;
            result.OverallComplexityScore = CalculateOverallComplexity(result);
            result.OverallRiskLevel = GetOverallRiskLevel(result.OverallComplexityScore);
            result.KeyRecommendations = GenerateKeyRecommendations(result);
            result.OverallAssessment = GenerateOverallAssessment(result, analysisType);
            result.ProjectSummary = GenerateProjectSummary(result);

            return result;
        }

        private async Task<FileAnalysisResult> AnalyzeIndividualFileAsync(IBrowserFile file, string analysisType)
        {
            const int maxFileSize = 512000; // 500KB limit

            using var stream = file.OpenReadStream(maxFileSize);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            var staticAnalysis = await _codeAnalysisService.AnalyzeCodeAsync(content);

            // Get quick AI insight (simplified to avoid quota issues)
            string aiInsight;
            try
            {
                aiInsight = await _aiAnalysisService.GetAnalysisAsync(
                    content.Length > 800 ? content.Substring(0, 800) + "..." : content,
                    analysisType,
                    staticAnalysis);
            }
            catch
            {
                aiInsight = GenerateFallbackInsight(staticAnalysis, analysisType);
            }

            return new FileAnalysisResult
            {
                FileName = file.Name,
                FileSize = file.Size,
                StaticAnalysis = staticAnalysis,
                AIInsight = aiInsight,
                ComplexityScore = CalculateFileComplexity(staticAnalysis),
                Status = "Success"
            };
        }

        private int CalculateFileComplexity(CodeAnalysisResult analysis)
        {
            var complexity = (analysis.ClassCount * 10) + (analysis.MethodCount * 2) + analysis.UsingCount;
            return Math.Min(100, Math.Max(0, complexity));
        }

        private int CalculateOverallComplexity(MultiFileAnalysisResult result)
        {
            if (!result.FileResults.Any()) return 0;

            var avgComplexity = result.FileResults.Average(f => f.ComplexityScore);
            var fileCountFactor = Math.Min(result.TotalFiles * 2, 20); // More files = higher complexity

            return Math.Min(100, (int)(avgComplexity + fileCountFactor));
        }

        private string GetOverallRiskLevel(int complexityScore)
        {
            return complexityScore switch
            {
                < 30 => "LOW",
                < 60 => "MEDIUM",
                _ => "HIGH"
            };
        }

        private List<string> GenerateKeyRecommendations(MultiFileAnalysisResult result)
        {
            var recommendations = new List<string>();

            if (result.OverallComplexityScore > 70)
            {
                recommendations.Add("High complexity project - recommend dedicated migration team");
                recommendations.Add("Consider phased migration approach to reduce risk");
            }
            else if (result.OverallComplexityScore > 40)
            {
                recommendations.Add("Moderate complexity - experienced developers required");
                recommendations.Add("Plan for 4-8 week migration timeline");
            }
            else
            {
                recommendations.Add("Low complexity - suitable for standard migration process");
                recommendations.Add("Good candidate for junior developer training");
            }

            if (result.TotalFiles > 20)
            {
                recommendations.Add("Large codebase - implement automated testing before migration");
            }

            var avgMethodsPerClass = result.TotalClasses > 0 ? (double)result.TotalMethods / result.TotalClasses : 0;
            if (avgMethodsPerClass > 8)
            {
                recommendations.Add("High method-to-class ratio detected - consider refactoring");
            }

            return recommendations.Take(5).ToList();
        }

        private string GenerateOverallAssessment(MultiFileAnalysisResult result, string analysisType)
        {
            var assessment = $"This {result.TotalFiles}-file project shows {result.OverallRiskLevel.ToLower()} migration complexity. ";

            assessment += analysisType switch
            {
                "security" => $"Security analysis reveals {result.FileResults.Count(f => f.ComplexityScore > 60)} high-risk files requiring immediate attention.",
                "performance" => $"Performance analysis identifies optimization opportunities across {result.TotalMethods} methods.",
                "migration" => $"Migration assessment indicates {GetMigrationTimeEstimate(result.OverallComplexityScore)} effort required.",
                _ => $"Code quality assessment shows {result.TotalClasses} classes with varying modernization needs."
            };

            return assessment;
        }

        private string GenerateProjectSummary(MultiFileAnalysisResult result)
        {
            return $"Project contains {result.TotalFiles} files with {result.TotalClasses} classes, " +
                   $"{result.TotalMethods} methods, and {result.TotalProperties} properties. " +
                   $"Overall complexity rated as {result.OverallRiskLevel} with risk score of {result.OverallComplexityScore}/100.";
        }

        private string GetMigrationTimeEstimate(int complexity)
        {
            return complexity switch
            {
                < 30 => "2-4 weeks",
                < 60 => "4-8 weeks",
                _ => "8-16 weeks"
            };
        }

        private string GenerateFallbackInsight(CodeAnalysisResult analysis, string analysisType)
        {
            return analysisType switch
            {
                "security" => $"Security review needed for {analysis.ClassCount} classes. Check for input validation and secure coding practices.",
                "performance" => $"Performance review recommended for {analysis.MethodCount} methods. Look for optimization opportunities.",
                "migration" => $"Migration complexity moderate with {analysis.ClassCount} classes to modernize.",
                _ => $"Code review shows {analysis.ClassCount} classes with {analysis.MethodCount} methods requiring modernization attention."
            };
        }
    }
}

using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class MultiFileAnalysisService : IMultiFileAnalysisService
    {
        private readonly ICodeAnalysisService _codeAnalysisService;
        private readonly IAIAnalysisService _aiAnalysisService;
        private readonly ILogger<MultiFileAnalysisService> _logger;

        public MultiFileAnalysisService(
            ICodeAnalysisService codeAnalysisService,
            IAIAnalysisService aiAnalysisService,
            ILogger<MultiFileAnalysisService> logger)
        {
            _codeAnalysisService = codeAnalysisService;
            _aiAnalysisService = aiAnalysisService;
            _logger = logger;
        }

        public async Task<MultiFileAnalysisResult> AnalyzeMultipleFilesAsync(List<IBrowserFile> files, string analysisType)
        {
            _logger.LogInformation("Initiating comprehensive analysis for {FileCount} files using {AnalysisType} methodology",
                files.Count, analysisType);

            var result = new MultiFileAnalysisResult
            {
                TotalFiles = files.Count
            };

            var fileResults = new List<FileAnalysisResult>();

            foreach (var file in files.Take(10)) // Performance limit: maximum 10 files
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

            result.FileResults = fileResults;
            result.OverallComplexityScore = CalculateProjectComplexity(result);
            result.OverallRiskLevel = DetermineRiskLevel(result.OverallComplexityScore);
            result.KeyRecommendations = GenerateStrategicRecommendations(result, analysisType);
            result.OverallAssessment = GenerateExecutiveAssessment(result, analysisType);
            result.ProjectSummary = GenerateProjectSummary(result);

            _logger.LogInformation("Analysis completed for {FileCount} files. Overall complexity: {ComplexityScore}, Risk level: {RiskLevel}",
                result.TotalFiles, result.OverallComplexityScore, result.OverallRiskLevel);

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
            return Math.Min(100, Math.Max(0, totalComplexity / 3));
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
            < 30 => "LOW",
            < 60 => "MEDIUM",
            _ => "HIGH"
        };

        private List<string> GenerateStrategicRecommendations(MultiFileAnalysisResult result, string analysisType)
        {
            var recommendations = new List<string>();

            // Risk-based recommendations
            if (result.OverallComplexityScore > 70)
            {
                recommendations.Add("High complexity project requires dedicated migration team with senior architect oversight");
                recommendations.Add("Implement phased migration approach to minimize business disruption and technical risk");
                recommendations.Add("Establish comprehensive testing strategy before initiating modernization activities");
            }
            else if (result.OverallComplexityScore > 40)
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
            if (result.TotalFiles > 25)
            {
                recommendations.Add("Large codebase requires automated testing and continuous integration before migration");
                recommendations.Add("Implement code analysis tools and quality metrics tracking throughout modernization");
            }

            // Architecture-based recommendations
            var methodsPerClass = result.TotalClasses > 0 ? (double)result.TotalMethods / result.TotalClasses : 0;
            if (methodsPerClass > 8)
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

            return recommendations.Take(6).ToList(); // Limit to most relevant recommendations
        }

        private string GenerateExecutiveAssessment(MultiFileAnalysisResult result, string analysisType)
        {
            var assessment = $"Comprehensive {analysisType} analysis of {result.TotalFiles}-file enterprise project indicates {result.OverallRiskLevel.ToLower()} modernization complexity. ";

            assessment += analysisType switch
            {
                "security" => $"Security assessment identifies {result.FileResults.Count(f => f.ComplexityScore > 60)} files requiring immediate security review and remediation.",
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
            < 30 => "minimal to moderate",
            < 60 => "moderate to substantial",
            _ => "substantial to extensive"
        };

        private string GetRecommendedApproach(int complexity) => complexity switch
        {
            < 30 => "standard development practices with code review",
            < 60 => "structured approach with experienced team and architectural oversight",
            _ => "enterprise-grade methodology with dedicated migration team and executive sponsorship"
        };

        private string GetResourceRequirement(int complexity) => complexity switch
        {
            < 30 => "standard development resources",
            < 60 => "experienced development team with architectural guidance",
            _ => "senior development team with specialist migration expertise"
        };

        private string GetTimelineEstimate(int complexity) => complexity switch
        {
            < 30 => "2-4 week",
            < 60 => "4-8 week",
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
    }
}

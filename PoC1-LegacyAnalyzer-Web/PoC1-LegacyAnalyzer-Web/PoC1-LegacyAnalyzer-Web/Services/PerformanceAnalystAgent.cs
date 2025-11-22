using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using System.Text.Json;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class PerformanceAnalystAgent : ISpecialistAgentService
    {
        private readonly Kernel _kernel;
        private readonly ILogger<PerformanceAnalystAgent> _logger;
        private readonly AgentConfiguration _agentConfig;
        private readonly PerformanceAnalystConfig _analysisConfig;

        public string AgentName => _agentConfig.AgentProfiles["performance"].AgentName;
        public string Specialty => _agentConfig.AgentProfiles["performance"].Specialty;
        public string AgentPersona => _agentConfig.AgentProfiles["performance"].AgentPersona;
        public int ConfidenceThreshold => _agentConfig.AgentProfiles["performance"].ConfidenceThreshold;

        public PerformanceAnalystAgent(Kernel kernel, ILogger<PerformanceAnalystAgent> logger, IConfiguration configuration)
        {
            _kernel = kernel;
            _logger = logger;
            _kernel.Plugins.AddFromObject(this, "PerformanceAnalyst");

            _agentConfig = new AgentConfiguration();
            configuration.GetSection("AgentConfiguration").Bind(_agentConfig);
            var profile = _agentConfig.AgentProfiles["performance"];

            _analysisConfig = new PerformanceAnalystConfig();
            configuration.GetSection("AgentAnalysisConfiguration:Performance").Bind(_analysisConfig);
        }

        [KernelFunction, Description("Analyze code for performance bottlenecks and optimization opportunities")]
        public async Task<string> AnalyzePerformanceBottlenecks(
            [Description("C# source code to analyze")] string code,
            [Description("Expected performance requirements")] string performanceRequirements,
            [Description("Scalability targets and constraints")] string scalabilityTargets)
        {
            var template = _agentConfig.AgentPromptTemplates["performance"].AnalysisPrompt;
            var prompt = template
                .Replace("{agentPersona}", AgentPersona)
                .Replace("{code}", code)
                .Replace("{performanceRequirements}", performanceRequirements)
                .Replace("{scalabilityTargets}", scalabilityTargets);

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? _agentConfig.AgentPromptTemplates["performance"].DefaultResponse;
        }

        public async Task<string> AnalyzeAsync(
            string code,
            string businessContext,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("PerformanceAnalyst starting analysis");

                var performanceAnalysis = await AnalyzePerformanceBottlenecks(
                    code,
                    "Sub-second response times, 1000+ concurrent users",
                    "10x user growth over 2 years, 99.9% availability");

                var result = new
                {
                    AgentName = AgentName,
                    Specialty = Specialty,
                    ConfidenceScore = CalculatePerformanceConfidence(performanceAnalysis),
                    BusinessImpact = ExtractPerformanceBusinessImpact(performanceAnalysis),
                    EstimatedEffort = EstimateOptimizationEffort(performanceAnalysis),
                    Priority = DeterminePerformancePriority(performanceAnalysis),
                    KeyFindings = ExtractPerformanceFindings(performanceAnalysis),
                    Recommendations = ExtractPerformanceRecommendations(performanceAnalysis),
                    SpecialtyMetrics = new Dictionary<string, object>
                    {
                        ["BottleneckCount"] = CountBottlenecks(performanceAnalysis),
                        ["OptimizationOpportunities"] = CountOptimizations(performanceAnalysis),
                        ["ScalabilityIssues"] = CountScalabilityIssues(performanceAnalysis)
                    }
                };

                return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PerformanceAnalyst analysis failed");
                return CreatePerformanceErrorResult(ex.Message);
            }
        }

        public async Task<string> ReviewPeerAnalysisAsync(
            string peerAnalysis,
            string originalCode,
            CancellationToken cancellationToken = default)
        {
            var template = _agentConfig.AgentPromptTemplates["performance"].PeerReviewPrompt;
            var prompt = template
                .Replace("{agentPersona}", AgentPersona)
                .Replace("{peerAnalysis}", peerAnalysis)
                .Replace("{originalCode}", originalCode)
                .Replace("{reviewFocus}", "Performance impact, scalability, resource utilization, optimization opportunities");

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? _agentConfig.AgentPromptTemplates["performance"].DefaultResponse;
        }

        // Helper methods for performance-specific analysis
        private int CalculatePerformanceConfidence(string analysis)
        {
            var performanceIndicators = new[]
            {
                analysis.Contains("bottleneck", StringComparison.OrdinalIgnoreCase),
                analysis.Contains("optimization", StringComparison.OrdinalIgnoreCase),
                analysis.Contains("scalability", StringComparison.OrdinalIgnoreCase),
                analysis.Contains("memory", StringComparison.OrdinalIgnoreCase),
                analysis.Contains("database", StringComparison.OrdinalIgnoreCase),
                analysis.Length > _analysisConfig.MinAnalysisLengthForConfidence
            };

            return performanceIndicators.Count(indicator => indicator) * _analysisConfig.ConfidenceScoreMultiplier;
        }

        private string ExtractPerformanceBusinessImpact(string analysis)
        {
            var performanceKeywords = new[] { "response time", "throughput", "scalability", "user experience", "cost", "efficiency" };
            var sentences = analysis.Split('.', StringSplitOptions.RemoveEmptyEntries);

            return sentences.FirstOrDefault(s =>
                performanceKeywords.Any(keyword => s.Contains(keyword, StringComparison.OrdinalIgnoreCase)))?.Trim()
                ?? "Performance optimization will improve user experience and system efficiency";
        }

        private decimal EstimateOptimizationEffort(string analysis)
        {
            var complexityIndicators = new[] { "refactor", "architecture", "database", "caching", "async" };
            var complexityCount = complexityIndicators.Count(indicator =>
                analysis.Contains(indicator, StringComparison.OrdinalIgnoreCase));

            // Use configuration, with fallback to highest value if complexity exceeds configured values
            if (_analysisConfig.EffortEstimationByComplexity.TryGetValue(complexityCount, out var effort))
            {
                return effort;
            }
            // If complexity count exceeds configured values, use the maximum configured value
            return _analysisConfig.EffortEstimationByComplexity.Values.DefaultIfEmpty(80m).Max();
        }

        private string DeterminePerformancePriority(string analysis)
        {
            if (analysis.Contains("critical", StringComparison.OrdinalIgnoreCase) ||
                analysis.Contains("severe bottleneck", StringComparison.OrdinalIgnoreCase))
                return "CRITICAL";

            if (analysis.Contains("significant", StringComparison.OrdinalIgnoreCase) ||
                analysis.Contains("major improvement", StringComparison.OrdinalIgnoreCase))
                return "HIGH";

            return "MEDIUM";
        }

        private List<object> ExtractPerformanceFindings(string analysis)
        {
            var findings = new List<object>();

            var performancePatterns = new[]
            {
                ("Database Performance", "database"),
                ("Memory Usage", "memory"),
                ("Algorithm Efficiency", "algorithm"),
                ("I/O Operations", "i/o"),
                ("Concurrency Issues", "concurrent")
            };

            foreach (var (category, pattern) in performancePatterns)
            {
                if (analysis.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    findings.Add(new
                    {
                        Category = category,
                        Description = $"{category} optimization opportunity identified",
                        Severity = DeterminePerformanceSeverity(analysis, pattern),
                        Location = "Code analysis - specific locations require detailed review"
                    });
                }
            }

            return findings;
        }

        private List<object> ExtractPerformanceRecommendations(string analysis)
        {
            var recommendations = new List<object>();

            if (analysis.Contains("database", StringComparison.OrdinalIgnoreCase))
            {
                recommendations.Add(new
                {
                    Title = "Optimize Database Queries",
                    Description = "Implement query optimization and indexing strategies",
                    Implementation = "Add proper indexing, use parameterized queries, implement connection pooling",
                    EstimatedHours = 24m,
                    Priority = "HIGH"
                });
            }

            if (analysis.Contains("async", StringComparison.OrdinalIgnoreCase))
            {
                recommendations.Add(new
                {
                    Title = "Implement Asynchronous Operations",
                    Description = "Convert blocking operations to async/await patterns",
                    Implementation = "Replace synchronous I/O with async operations, implement proper task management",
                    EstimatedHours = 16m,
                    Priority = "HIGH"
                });
            }

            return recommendations;
        }

        private string DeterminePerformanceSeverity(string analysis, string pattern)
        {
            var context = ExtractPatternContext(analysis, pattern);

            if (context.Contains("critical", StringComparison.OrdinalIgnoreCase) ||
                context.Contains("severe", StringComparison.OrdinalIgnoreCase))
                return "CRITICAL";

            return "HIGH";
        }

        private string ExtractPatternContext(string analysis, string pattern)
        {
            var sentences = analysis.Split('.', StringSplitOptions.RemoveEmptyEntries);
            return sentences.FirstOrDefault(s => s.Contains(pattern, StringComparison.OrdinalIgnoreCase)) ?? "";
        }

        private int CountBottlenecks(string analysis) =>
            CountOccurrences(analysis, "bottleneck") + CountOccurrences(analysis, "slow") + CountOccurrences(analysis, "inefficient");

        private int CountOptimizations(string analysis) =>
            CountOccurrences(analysis, "optimization") + CountOccurrences(analysis, "improve") + CountOccurrences(analysis, "enhance");

        private int CountScalabilityIssues(string analysis) =>
            CountOccurrences(analysis, "scalability") + CountOccurrences(analysis, "concurrent") + CountOccurrences(analysis, "load");

        private int CountOccurrences(string text, string keyword)
        {
            return (text.Length - text.Replace(keyword, "", StringComparison.OrdinalIgnoreCase).Length) / keyword.Length;
        }

        private string CreatePerformanceErrorResult(string errorMessage)
        {
            var errorResult = new
            {
                AgentName = AgentName,
                Specialty = Specialty,
                ConfidenceScore = 0,
                BusinessImpact = $"Performance analysis failed: {errorMessage}",
                KeyFindings = new List<object>
                {
                    new
                    {
                        Category = "Analysis Error",
                        Description = errorMessage,
                        Severity = "HIGH"
                    }
                }
            };

            return JsonSerializer.Serialize(errorResult, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}

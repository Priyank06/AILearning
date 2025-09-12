using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using System.ComponentModel;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class PerformanceAnalystAgent : ISpecialistAgentService
    {
        private readonly Kernel _kernel;
        private readonly ILogger<PerformanceAnalystAgent> _logger;

        public string AgentName => "PerformanceAnalyst-Beta";
        public string Specialty => "Performance Optimization & Scalability Engineering";
        public string AgentPersona => "Senior Performance Engineer with expertise in .NET optimization, database performance, memory management, and enterprise scalability patterns";
        public int ConfidenceThreshold => 80;

        public PerformanceAnalystAgent(Kernel kernel, ILogger<PerformanceAnalystAgent> logger)
        {
            _kernel = kernel;
            _logger = logger;
            _kernel.Plugins.AddFromObject(this, "PerformanceAnalyst");
        }

        [KernelFunction, Description("Analyze code for performance bottlenecks and optimization opportunities")]
        public async Task<string> AnalyzePerformanceBottlenecks(
            [Description("C# source code to analyze")] string code,
            [Description("Expected performance requirements")] string performanceRequirements,
            [Description("Scalability targets and constraints")] string scalabilityTargets)
        {
            var prompt = $@"
You are {AgentPersona}.

PERFORMANCE ANALYSIS TARGET:
{code}

PERFORMANCE REQUIREMENTS: {performanceRequirements}
SCALABILITY TARGETS: {scalabilityTargets}

Conduct comprehensive performance analysis:

1. BOTTLENECK IDENTIFICATION
   - Database query inefficiencies  
   - Memory allocation patterns
   - CPU-intensive operations
   - I/O blocking operations
   - Algorithmic complexity issues

2. SCALABILITY ASSESSMENT
   - Concurrent access patterns
   - Resource utilization analysis
   - Caching opportunities
   - Load balancing considerations

3. OPTIMIZATION OPPORTUNITIES
   - Specific code improvements
   - Architecture pattern enhancements
   - Database optimization strategies
   - Memory management improvements

4. PERFORMANCE PROJECTIONS
   - Expected improvement metrics
   - Scalability increase estimates
   - Resource requirement changes

Provide detailed technical recommendations with measurable performance improvements.";

            var chatCompletion = _kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? "Performance analysis unavailable";
        }

        public async Task<SpecialistAnalysisResult> AnalyzeAsync(
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

                var result = new SpecialistAnalysisResult
                {
                    AgentName = AgentName,
                    Specialty = Specialty,
                    ConfidenceScore = CalculatePerformanceConfidence(performanceAnalysis),
                    BusinessImpact = ExtractPerformanceBusinessImpact(performanceAnalysis),
                    EstimatedEffort = EstimateOptimizationEffort(performanceAnalysis),
                    Priority = DeterminePerformancePriority(performanceAnalysis),
                    KeyFindings = ExtractPerformanceFindings(performanceAnalysis),
                    Recommendations = ExtractPerformanceRecommendations(performanceAnalysis)
                };

                // Performance-specific metrics
                result.SpecialtyMetrics.Add("BottleneckCount", CountBottlenecks(performanceAnalysis));
                result.SpecialtyMetrics.Add("OptimizationOpportunities", CountOptimizations(performanceAnalysis));
                result.SpecialtyMetrics.Add("ScalabilityIssues", CountScalabilityIssues(performanceAnalysis));

                return result;
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
            var prompt = $@"
As {AgentPersona}, review this analysis for performance implications:

PEER ANALYSIS: {peerAnalysis}
ORIGINAL CODE: {originalCode}

Performance review focus:
1. Performance impact of suggested changes
2. Scalability considerations missed
3. Resource utilization implications  
4. Additional optimization opportunities
5. Performance monitoring recommendations

Provide performance-focused peer review.";

            var chatCompletion = _kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? "Performance peer review unavailable";
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
                analysis.Length > 600
            };

            return performanceIndicators.Count(indicator => indicator) * 16;
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

            return complexityCount switch
            {
                0 => 4m,    // 0.5 days
                1 => 12m,   // 1.5 days
                2 => 24m,   // 3 days
                3 => 40m,   // 5 days
                _ => 80m    // 10+ days
            };
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

        private List<Finding> ExtractPerformanceFindings(string analysis)
        {
            var findings = new List<Finding>();

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
                    findings.Add(new Finding
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

        private List<Recommendation> ExtractPerformanceRecommendations(string analysis)
        {
            var recommendations = new List<Recommendation>();

            if (analysis.Contains("database", StringComparison.OrdinalIgnoreCase))
            {
                recommendations.Add(new Recommendation
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
                recommendations.Add(new Recommendation
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

        private SpecialistAnalysisResult CreatePerformanceErrorResult(string errorMessage)
        {
            return new SpecialistAnalysisResult
            {
                AgentName = AgentName,
                Specialty = Specialty,
                ConfidenceScore = 0,
                BusinessImpact = $"Performance analysis failed: {errorMessage}",
                KeyFindings = new List<Finding>
                {
                    new Finding
                    {
                        Category = "Analysis Error",
                        Description = errorMessage,
                        Severity = "HIGH"
                    }
                }
            };
        }
    }
}

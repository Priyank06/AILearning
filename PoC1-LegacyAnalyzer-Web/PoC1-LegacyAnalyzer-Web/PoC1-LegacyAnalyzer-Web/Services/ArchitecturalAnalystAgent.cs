using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PoC1_LegacyAnalyzer_Web.Models;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class ArchitecturalAnalystAgent : ISpecialistAgentService
    {
        private readonly Kernel _kernel;
        private readonly ILogger<ArchitecturalAnalystAgent> _logger;
        private readonly AgentConfiguration _agentConfig;
        private readonly ArchitecturalAnalystConfig _analysisConfig;

        public string AgentName => _agentConfig.AgentProfiles["architecture"].AgentName;
        public string Specialty => _agentConfig.AgentProfiles["architecture"].Specialty;
        public string AgentPersona => _agentConfig.AgentProfiles["architecture"].AgentPersona;
        public int ConfidenceThreshold => _agentConfig.AgentProfiles["architecture"].ConfidenceThreshold;

        public ArchitecturalAnalystAgent(Kernel kernel, ILogger<ArchitecturalAnalystAgent> logger, IConfiguration configuration)
        {
            _kernel = kernel;
            _logger = logger;
            _kernel.Plugins.AddFromObject(this, "ArchitecturalAnalyst");

            _agentConfig = new AgentConfiguration();
            configuration.GetSection("AgentConfiguration").Bind(_agentConfig);
            var profile = _agentConfig.AgentProfiles["architecture"];

            _analysisConfig = new ArchitecturalAnalystConfig();
            configuration.GetSection("AgentAnalysisConfiguration:Architecture").Bind(_analysisConfig);
        }

        [KernelFunction, Description("Analyze software architecture and design patterns")]
        public async Task<string> AnalyzeArchitecturalDesign(
            [Description("C# source code to analyze")] string code,
            [Description("Target architecture style and patterns")] string targetArchitecture,
            [Description("Business domain and constraints")] string businessDomain)
        {
            var template = _agentConfig.AgentPromptTemplates["architecture"].AnalysisPrompt;
            var prompt = template
                .Replace("{agentPersona}", AgentPersona)
                .Replace("{code}", code)
                .Replace("{targetArchitecture}", targetArchitecture)
                .Replace("{businessDomain}", businessDomain);

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? _agentConfig.AgentPromptTemplates["architecture"].DefaultResponse;
        }

        public async Task<string> AnalyzeAsync(
            string code,
            string businessContext,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("ArchitecturalAnalyst starting analysis");

                var architecturalAnalysis = await AnalyzeArchitecturalDesign(
                    code,
                    "Clean Architecture, Domain-Driven Design, Microservices readiness",
                    businessContext);

                var result = new
                {
                    AgentName = AgentName,
                    Specialty = Specialty,
                    ConfidenceScore = CalculateArchitecturalConfidence(architecturalAnalysis),
                    BusinessImpact = ExtractArchitecturalBusinessImpact(architecturalAnalysis),
                    EstimatedEffort = EstimateArchitecturalEffort(architecturalAnalysis),
                    Priority = DetermineArchitecturalPriority(architecturalAnalysis),
                    KeyFindings = ExtractArchitecturalFindings(architecturalAnalysis),
                    Recommendations = ExtractArchitecturalRecommendations(architecturalAnalysis),
                    SpecialtyMetrics = new Dictionary<string, object>
                    {
                        ["DesignPatternViolations"] = CountPatternViolations(architecturalAnalysis),
                        ["ArchitecturalDebt"] = AssessArchitecturalDebt(architecturalAnalysis),
                        ["ModernizationComplexity"] = AssessModernizationComplexity(architecturalAnalysis)
                    }
                };

                return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ArchitecturalAnalyst analysis failed");
                return CreateArchitecturalErrorResult(ex.Message);
            }
        }

        public async Task<string> ReviewPeerAnalysisAsync(
            string peerAnalysis,
            string originalCode,
            CancellationToken cancellationToken = default)
        {
            var template = _agentConfig.AgentPromptTemplates["architecture"].PeerReviewPrompt;
            var prompt = template
                .Replace("{agentPersona}", AgentPersona)
                .Replace("{peerAnalysis}", peerAnalysis)
                .Replace("{originalCode}", originalCode)
                .Replace("{reviewFocus}", "Design pattern implications, maintainability, architectural principles, integration, technical debt");

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? _agentConfig.AgentPromptTemplates["architecture"].DefaultResponse;
        }

        // Helper methods unchanged...
        private int CalculateArchitecturalConfidence(string analysis)
        {
            var architecturalIndicators = new[]
            {
                analysis.Contains("pattern", StringComparison.OrdinalIgnoreCase),
                analysis.Contains("architecture", StringComparison.OrdinalIgnoreCase),
                analysis.Contains("design", StringComparison.OrdinalIgnoreCase),
                analysis.Contains("SOLID", StringComparison.OrdinalIgnoreCase),
                analysis.Contains("separation", StringComparison.OrdinalIgnoreCase),
                analysis.Contains("coupling", StringComparison.OrdinalIgnoreCase),
                analysis.Length > _analysisConfig.MinAnalysisLengthForConfidence
            };

            return Math.Min(100, architecturalIndicators.Count(indicator => indicator) * _analysisConfig.ConfidenceScoreMultiplier);
        }

        private string ExtractArchitecturalBusinessImpact(string analysis)
        {
            var businessKeywords = new[] { "maintainability", "scalability", "flexibility", "technical debt", "development velocity", "time to market" };
            var sentences = analysis.Split('.', StringSplitOptions.RemoveEmptyEntries);

            return sentences.FirstOrDefault(s =>
                businessKeywords.Any(keyword => s.Contains(keyword, StringComparison.OrdinalIgnoreCase)))?.Trim()
                ?? "Architectural improvements will enhance system maintainability and development velocity";
        }

        private decimal EstimateArchitecturalEffort(string analysis)
        {
            var complexityIndicators = new[] { "refactor", "restructure", "pattern", "architecture", "design", "framework" };
            var complexityCount = complexityIndicators.Count(indicator =>
                analysis.Contains(indicator, StringComparison.OrdinalIgnoreCase));

            // Use configuration, with fallback logic for values <= 1
            if (complexityCount <= 1)
            {
                return _analysisConfig.EffortEstimationByComplexity.TryGetValue(1, out var effort) ? effort : 16m;
            }
            if (_analysisConfig.EffortEstimationByComplexity.TryGetValue(complexityCount, out var configuredEffort))
            {
                return configuredEffort;
            }
            // If complexity count exceeds configured values, use the maximum configured value
            return _analysisConfig.EffortEstimationByComplexity.Values.DefaultIfEmpty(320m).Max();
        }

        private string DetermineArchitecturalPriority(string analysis)
        {
            if (analysis.Contains("fundamental", StringComparison.OrdinalIgnoreCase) ||
                analysis.Contains("architectural debt", StringComparison.OrdinalIgnoreCase))
                return "CRITICAL";

            if (analysis.Contains("significant", StringComparison.OrdinalIgnoreCase) ||
                analysis.Contains("important", StringComparison.OrdinalIgnoreCase))
                return "HIGH";

            return "MEDIUM";
        }

        private List<object> ExtractArchitecturalFindings(string analysis)
        {
            var findings = new List<object>();

            var architecturalPatterns = new[]
            {
                ("SOLID Principles", "SOLID"),
                ("Design Patterns", "pattern"),
                ("Separation of Concerns", "separation"),
                ("Dependency Management", "dependency"),
                ("Code Organization", "organization")
            };

            foreach (var (category, pattern) in architecturalPatterns)
            {
                if (analysis.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    findings.Add(new
                    {
                        Category = category,
                        Description = $"{category} analysis completed - improvements identified",
                        Severity = DetermineArchitecturalSeverity(analysis, pattern),
                        Location = "System architecture - multiple components affected"
                    });
                }
            }

            return findings;
        }

        private List<object> ExtractArchitecturalRecommendations(string analysis)
        {
            var recommendations = new List<object>();

            if (analysis.Contains("SOLID", StringComparison.OrdinalIgnoreCase))
            {
                recommendations.Add(new
                {
                    Title = "Implement SOLID Design Principles",
                    Description = "Refactor code to adhere to SOLID principles for better maintainability",
                    Implementation = "Apply Single Responsibility, Open/Closed, and Dependency Inversion principles systematically",
                    EstimatedHours = 60m,
                    Priority = "HIGH",
                    Dependencies = new List<string> { "Architecture review", "Refactoring plan", "Testing strategy" }
                });
            }

            if (analysis.Contains("pattern", StringComparison.OrdinalIgnoreCase))
            {
                recommendations.Add(new
                {
                    Title = "Introduce Appropriate Design Patterns",
                    Description = "Implement design patterns to improve code structure and reusability",
                    Implementation = "Apply Repository, Factory, Strategy, and Observer patterns where appropriate",
                    EstimatedHours = 40m,
                    Priority = "MEDIUM",
                    Dependencies = new List<string> { "Pattern selection", "Team training", "Code review process" }
                });
            }

            return recommendations;
        }

        private string DetermineArchitecturalSeverity(string analysis, string pattern)
        {
            var context = ExtractPatternContext(analysis, pattern);

            if (context.Contains("violation", StringComparison.OrdinalIgnoreCase) ||
                context.Contains("poor", StringComparison.OrdinalIgnoreCase))
                return "HIGH";

            if (context.Contains("improvement", StringComparison.OrdinalIgnoreCase))
                return "MEDIUM";

            return "LOW";
        }

        private string ExtractPatternContext(string analysis, string pattern)
        {
            var sentences = analysis.Split('.', StringSplitOptions.RemoveEmptyEntries);
            return sentences.FirstOrDefault(s => s.Contains(pattern, StringComparison.OrdinalIgnoreCase)) ?? "";
        }

        private int CountPatternViolations(string analysis)
        {
            var violationKeywords = new[] { "violation", "poor", "bad", "anti-pattern", "smell" };
            return violationKeywords.Sum(keyword => CountOccurrences(analysis, keyword));
        }

        private int AssessArchitecturalDebt(string analysis)
        {
            var debtKeywords = new[] { "debt", "refactor", "restructure", "legacy", "outdated" };
            return debtKeywords.Sum(keyword => CountOccurrences(analysis, keyword));
        }

        private int AssessModernizationComplexity(string analysis)
        {
            var complexityKeywords = new[] { "complex", "difficult", "challenging", "significant", "major" };
            return complexityKeywords.Sum(keyword => CountOccurrences(analysis, keyword));
        }

        private int CountOccurrences(string text, string keyword)
        {
            return (text.Length - text.Replace(keyword, "", StringComparison.OrdinalIgnoreCase).Length) / keyword.Length;
        }

        private string CreateArchitecturalErrorResult(string errorMessage)
        {
            var errorResult = new
            {
                AgentName = AgentName,
                Specialty = Specialty,
                ConfidenceScore = 0,
                BusinessImpact = $"Architectural analysis failed: {errorMessage}",
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

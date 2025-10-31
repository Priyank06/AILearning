using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using System.ComponentModel;
using System.Text.Json;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class ArchitecturalAnalystAgent : ISpecialistAgentService
    {
        private readonly Kernel _kernel;
        private readonly ILogger<ArchitecturalAnalystAgent> _logger;

        public string AgentName => "ArchitecturalAnalyst-Gamma";
        public string Specialty => "Software Architecture & Design Patterns Analysis";
        public string AgentPersona => "Principal Software Architect with 20+ years experience in enterprise architecture, design patterns, SOLID principles, and system modernization strategies";
        public int ConfidenceThreshold => 85;

        public ArchitecturalAnalystAgent(Kernel kernel, ILogger<ArchitecturalAnalystAgent> logger)
        {
            _kernel = kernel;
            _logger = logger;
            _kernel.Plugins.AddFromObject(this, "ArchitecturalAnalyst");
        }

        [KernelFunction, Description("Analyze software architecture and design patterns")]
        public async Task<string> AnalyzeArchitecturalDesign(
            [Description("C# source code to analyze")] string code,
            [Description("Target architecture style and patterns")] string targetArchitecture,
            [Description("Business domain and constraints")] string businessDomain)
        {
            var prompt = $@"
You are {AgentPersona}.

ARCHITECTURAL ANALYSIS TARGET:
{code}

TARGET ARCHITECTURE: {targetArchitecture}
BUSINESS DOMAIN: {businessDomain}

Conduct comprehensive architectural analysis:

1. DESIGN PATTERN ASSESSMENT
   - Current patterns used (or misused)
   - SOLID principle adherence
   - Dependency inversion opportunities
   - Single responsibility violations

2. ARCHITECTURAL QUALITY
   - Separation of concerns
   - Coupling and cohesion analysis
   - Layer architecture evaluation
   - Component boundaries assessment

3. MODERNIZATION STRATEGY
   - Refactoring priorities
   - Pattern introduction roadmap
   - Architecture evolution path
   - Migration risk assessment

4. ENTERPRISE INTEGRATION
   - Service boundaries definition
   - API design recommendations
   - Data architecture improvements
   - Cross-cutting concerns handling

Provide architectural recommendations with implementation strategies.";

            var chatCompletion = _kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? "Architectural analysis unavailable";
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

                // Create a structured result but return as JSON string to match interface
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
            var prompt = $@"
As {AgentPersona}, review this analysis from architectural perspective:

PEER ANALYSIS: {peerAnalysis}
ORIGINAL CODE: {originalCode}

Architectural review focus:
1. Design pattern implications of suggested changes
2. Long-term maintainability impact
3. Architectural principle violations or improvements
4. System integration and boundary considerations
5. Technical debt implications

Provide architecture-focused peer review with strategic recommendations.";

            var chatCompletion = _kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? "Architectural peer review unavailable";
        }

        // Keep all your existing helper methods unchanged...
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
                analysis.Length > 800
            };

            return Math.Min(100, architecturalIndicators.Count(indicator => indicator) * 14);
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

            return complexityCount switch
            {
                <= 1 => 16m,
                2 => 40m,
                3 => 80m,
                4 => 160m,
                _ => 320m
            };
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

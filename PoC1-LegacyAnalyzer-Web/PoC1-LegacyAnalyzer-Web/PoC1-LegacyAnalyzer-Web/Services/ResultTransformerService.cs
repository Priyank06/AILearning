using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PoC1_LegacyAnalyzer_Web.Models;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using System.Text.Json;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Centralized service for transforming raw LLM analysis results into structured SpecialistAnalysisResult objects.
    /// Extracts common parsing logic from individual agents including confidence scoring, finding extraction,
    /// recommendation parsing, and metric calculation.
    /// </summary>
    public class ResultTransformerService : IResultTransformerService
    {
        private readonly ILogger<ResultTransformerService> _logger;
        private readonly SecurityAnalystConfig _securityConfig;
        private readonly PerformanceAnalystConfig _performanceConfig;
        private readonly ArchitecturalAnalystConfig _architecturalConfig;

        public ResultTransformerService(ILogger<ResultTransformerService> logger, IConfiguration configuration)
        {
            _logger = logger;

            // Load configurations
            _securityConfig = new SecurityAnalystConfig();
            configuration.GetSection("AgentAnalysisConfiguration:Security").Bind(_securityConfig);

            _performanceConfig = new PerformanceAnalystConfig();
            configuration.GetSection("AgentAnalysisConfiguration:Performance").Bind(_performanceConfig);

            _architecturalConfig = new ArchitecturalAnalystConfig();
            configuration.GetSection("AgentAnalysisConfiguration:Architecture").Bind(_architecturalConfig);
        }

        public SpecialistAnalysisResult TransformToResult(string rawAnalysis, string agentName, string specialty)
        {
            try
            {
                var result = new SpecialistAnalysisResult
                {
                    AgentName = agentName,
                    Specialty = specialty,
                    AnalysisTimestamp = DateTime.UtcNow,
                    ConfidenceScore = CalculateConfidenceScore(rawAnalysis, specialty),
                    BusinessImpact = ExtractBusinessImpact(rawAnalysis, specialty),
                    EstimatedEffort = EstimateEffort(rawAnalysis, specialty),
                    Priority = DeterminePriority(rawAnalysis),
                    KeyFindings = ExtractFindings(rawAnalysis, specialty),
                    Recommendations = ExtractRecommendations(rawAnalysis, specialty),
                    RiskLevel = AssessRisk(rawAnalysis, specialty),
                    SpecialtyMetrics = CalculateSpecialtyMetrics(rawAnalysis, specialty)
                };

                _logger.LogInformation("Transformed analysis for {AgentName} with confidence: {Confidence}%", 
                    agentName, result.ConfidenceScore);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to transform analysis result for {AgentName}", agentName);
                return CreateErrorResult(ex.Message, agentName, specialty);
            }
        }

        public SpecialistAnalysisResult CreateErrorResult(string errorMessage, string agentName, string specialty)
        {
            return new SpecialistAnalysisResult
            {
                AgentName = agentName,
                Specialty = specialty,
                AnalysisTimestamp = DateTime.UtcNow,
                ConfidenceScore = 0,
                BusinessImpact = $"Analysis failed: {errorMessage}",
                Priority = "HIGH",
                KeyFindings = new List<Finding>
                {
                    new Finding
                    {
                        Category = "Analysis Error",
                        Description = errorMessage,
                        Severity = "HIGH",
                        Location = "System Error",
                        Evidence = new List<string> { "Analysis process failed" }
                    }
                },
                Recommendations = new List<Recommendation>(),
                RiskLevel = new RiskAssessment
                {
                    Level = "HIGH",
                    RiskFactors = new List<string> { "Analysis failure prevents proper risk assessment" },
                    MitigationStrategy = "Retry analysis or investigate system issues"
                },
                SpecialtyMetrics = new Dictionary<string, object>()
            };
        }

        private int CalculateConfidenceScore(string analysis, string specialty)
        {
            var baseIndicators = new[]
            {
                analysis.Contains("recommendation", StringComparison.OrdinalIgnoreCase),
                analysis.Contains("analysis", StringComparison.OrdinalIgnoreCase),
                analysis.Contains("finding", StringComparison.OrdinalIgnoreCase),
                !string.IsNullOrWhiteSpace(analysis)
            };

            var specialtyIndicators = specialty.ToLower() switch
            {
                "security" => new[]
                {
                    analysis.Contains("vulnerability", StringComparison.OrdinalIgnoreCase),
                    analysis.Contains("security", StringComparison.OrdinalIgnoreCase),
                    analysis.Contains("risk", StringComparison.OrdinalIgnoreCase),
                    analysis.Length > _securityConfig.MinAnalysisLengthForConfidence
                },
                "performance" => new[]
                {
                    analysis.Contains("bottleneck", StringComparison.OrdinalIgnoreCase),
                    analysis.Contains("optimization", StringComparison.OrdinalIgnoreCase),
                    analysis.Contains("scalability", StringComparison.OrdinalIgnoreCase),
                    analysis.Contains("memory", StringComparison.OrdinalIgnoreCase),
                    analysis.Contains("database", StringComparison.OrdinalIgnoreCase),
                    analysis.Length > _performanceConfig.MinAnalysisLengthForConfidence
                },
                "architecture" => new[]
                {
                    analysis.Contains("pattern", StringComparison.OrdinalIgnoreCase),
                    analysis.Contains("architecture", StringComparison.OrdinalIgnoreCase),
                    analysis.Contains("design", StringComparison.OrdinalIgnoreCase),
                    analysis.Contains("SOLID", StringComparison.OrdinalIgnoreCase),
                    analysis.Contains("separation", StringComparison.OrdinalIgnoreCase),
                    analysis.Contains("coupling", StringComparison.OrdinalIgnoreCase),
                    analysis.Length > _architecturalConfig.MinAnalysisLengthForConfidence
                },
                _ => new bool[0]
            };

            var allIndicators = baseIndicators.Concat(specialtyIndicators).ToArray();
            var multiplier = specialty.ToLower() switch
            {
                "security" => _securityConfig.ConfidenceScoreMultiplier,
                "performance" => _performanceConfig.ConfidenceScoreMultiplier,
                "architecture" => _architecturalConfig.ConfidenceScoreMultiplier,
                _ => 15
            };

            var score = allIndicators.Count(indicator => indicator) * multiplier;
            return Math.Min(100, Math.Max(0, score));
        }

        private string ExtractBusinessImpact(string analysis, string specialty)
        {
            var businessKeywords = specialty.ToLower() switch
            {
                "security" => new[] { "business", "cost", "risk", "compliance", "revenue", "reputation" },
                "performance" => new[] { "response time", "throughput", "scalability", "user experience", "cost", "efficiency" },
                "architecture" => new[] { "maintainability", "scalability", "flexibility", "technical debt", "development velocity", "time to market" },
                _ => new[] { "business", "cost", "efficiency", "value" }
            };

            var sentences = analysis.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var businessSentence = sentences.FirstOrDefault(s =>
                businessKeywords.Any(keyword => s.Contains(keyword, StringComparison.OrdinalIgnoreCase)));

            return businessSentence?.Trim() ?? specialty.ToLower() switch
            {
                "security" => "Security improvements will reduce compliance risks and protect business reputation",
                "performance" => "Performance optimization will improve user experience and system efficiency",
                "architecture" => "Architectural improvements will enhance system maintainability and development velocity",
                _ => "Analysis results will provide business value through system improvements"
            };
        }

        private decimal EstimateEffort(string analysis, string specialty)
        {
            var complexityKeywords = specialty.ToLower() switch
            {
                "security" => new[] { "refactor", "architecture", "framework", "migration", "comprehensive" },
                "performance" => new[] { "refactor", "architecture", "database", "caching", "async" },
                "architecture" => new[] { "refactor", "restructure", "pattern", "architecture", "design", "framework" },
                _ => new[] { "refactor", "change", "implement", "update" }
            };

            var complexityCount = complexityKeywords.Count(keyword =>
                analysis.Contains(keyword, StringComparison.OrdinalIgnoreCase));

            var effortMap = specialty.ToLower() switch
            {
                "security" => _securityConfig.EffortEstimationByComplexity,
                "performance" => _performanceConfig.EffortEstimationByComplexity,
                "architecture" => _architecturalConfig.EffortEstimationByComplexity,
                _ => new Dictionary<int, decimal> { { 0, 8m }, { 1, 16m }, { 2, 40m }, { 3, 80m } }
            };

            if (effortMap.TryGetValue(complexityCount, out var effort))
            {
                return effort;
            }

            // Use highest configured value if complexity exceeds map
            return effortMap.Values.DefaultIfEmpty(80m).Max();
        }

        private string DeterminePriority(string analysis)
        {
            if (analysis.Contains("critical", StringComparison.OrdinalIgnoreCase) ||
                analysis.Contains("severe", StringComparison.OrdinalIgnoreCase) ||
                analysis.Contains("urgent", StringComparison.OrdinalIgnoreCase))
                return "CRITICAL";

            if (analysis.Contains("high", StringComparison.OrdinalIgnoreCase) ||
                analysis.Contains("important", StringComparison.OrdinalIgnoreCase) ||
                analysis.Contains("significant", StringComparison.OrdinalIgnoreCase))
                return "HIGH";

            if (analysis.Contains("medium", StringComparison.OrdinalIgnoreCase) ||
                analysis.Contains("moderate", StringComparison.OrdinalIgnoreCase))
                return "MEDIUM";

            return "LOW";
        }

        private List<Finding> ExtractFindings(string analysis, string specialty)
        {
            var findings = new List<Finding>();

            var patterns = specialty.ToLower() switch
            {
                "security" => new[]
                {
                    ("SQL Injection", "injection"),
                    ("Authentication Flaw", "authentication"),
                    ("Authorization Issue", "authorization"),
                    ("Data Exposure", "exposure"),
                    ("Input Validation", "validation")
                },
                "performance" => new[]
                {
                    ("Database Performance", "database"),
                    ("Memory Usage", "memory"),
                    ("Algorithm Efficiency", "algorithm"),
                    ("I/O Operations", "i/o"),
                    ("Concurrency Issues", "concurrent")
                },
                "architecture" => new[]
                {
                    ("SOLID Principles", "SOLID"),
                    ("Design Patterns", "pattern"),
                    ("Separation of Concerns", "separation"),
                    ("Dependency Management", "dependency"),
                    ("Code Organization", "organization")
                },
                _ => new[] { ("General Finding", "finding") }
            };

            foreach (var (category, pattern) in patterns)
            {
                if (analysis.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    findings.Add(new Finding
                    {
                        Category = category,
                        Description = specialty.ToLower() switch
                        {
                            "security" => $"{category} vulnerability identified in code analysis",
                            "performance" => $"{category} optimization opportunity identified",
                            "architecture" => $"{category} analysis completed - improvements identified",
                            _ => $"{category} identified in analysis"
                        },
                        Severity = DetermineSeverity(analysis, pattern),
                        Location = GetLocationDescription(specialty),
                        Evidence = ExtractEvidence(analysis, pattern)
                    });
                }
            }

            return findings;
        }

        private List<Recommendation> ExtractRecommendations(string analysis, string specialty)
        {
            var recommendations = new List<Recommendation>();

            var recommendationPatterns = specialty.ToLower() switch
            {
                "security" => GetSecurityRecommendations(analysis),
                "performance" => GetPerformanceRecommendations(analysis),
                "architecture" => GetArchitecturalRecommendations(analysis),
                _ => new List<Recommendation>()
            };

            recommendations.AddRange(recommendationPatterns);
            return recommendations;
        }

        private List<Recommendation> GetSecurityRecommendations(string analysis)
        {
            var recommendations = new List<Recommendation>();

            if (analysis.Contains("parameterized", StringComparison.OrdinalIgnoreCase) ||
                analysis.Contains("injection", StringComparison.OrdinalIgnoreCase))
            {
                recommendations.Add(new Recommendation
                {
                    Title = "Implement Parameterized Queries",
                    Description = "Replace string concatenation with parameterized queries to prevent SQL injection",
                    Implementation = "Use SqlParameter objects for all database queries",
                    EstimatedHours = 16m,
                    Priority = "CRITICAL",
                    Dependencies = new List<string> { "Database access review", "Testing framework update" }
                });
            }

            if (analysis.Contains("authentication", StringComparison.OrdinalIgnoreCase))
            {
                recommendations.Add(new Recommendation
                {
                    Title = "Strengthen Authentication Mechanisms",
                    Description = "Implement robust authentication and session management",
                    Implementation = "Integrate enterprise authentication system (Azure AD/OAuth 2.0)",
                    EstimatedHours = 40m,
                    Priority = "HIGH",
                    Dependencies = new List<string> { "Identity provider configuration", "Security policy review" }
                });
            }

            return recommendations;
        }

        private List<Recommendation> GetPerformanceRecommendations(string analysis)
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
                    Priority = "HIGH",
                    Dependencies = new List<string> { "Database performance analysis", "Index design review" }
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
                    Priority = "HIGH",
                    Dependencies = new List<string> { "Async pattern training", "Task management review" }
                });
            }

            return recommendations;
        }

        private List<Recommendation> GetArchitecturalRecommendations(string analysis)
        {
            var recommendations = new List<Recommendation>();

            if (analysis.Contains("SOLID", StringComparison.OrdinalIgnoreCase))
            {
                recommendations.Add(new Recommendation
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
                recommendations.Add(new Recommendation
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

        private RiskAssessment AssessRisk(string analysis, string specialty)
        {
            var riskFactors = new List<string>();

            var riskKeywords = specialty.ToLower() switch
            {
                "security" => new[]
                {
                    ("injection", "SQL injection vulnerability allows data theft"),
                    ("authentication", "Weak authentication enables unauthorized access"),
                    ("encryption", "Inadequate encryption exposes sensitive data")
                },
                "performance" => new[]
                {
                    ("bottleneck", "Performance bottlenecks impact user experience"),
                    ("scalability", "Scalability issues limit system growth"),
                    ("memory", "Memory issues cause system instability")
                },
                "architecture" => new[]
                {
                    ("debt", "Technical debt increases maintenance costs"),
                    ("coupling", "High coupling reduces system flexibility"),
                    ("pattern", "Poor patterns impact code maintainability")
                },
                _ => new[] { ("issue", "General system issue identified") }
            };

            foreach (var (keyword, riskDescription) in riskKeywords)
            {
                if (analysis.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    riskFactors.Add(riskDescription);
                }
            }

            var riskLevel = specialty.ToLower() == "security" 
                ? DetermineSecurityRiskLevel(riskFactors.Count)
                : DetermineGeneralRiskLevel(riskFactors.Count);

            return new RiskAssessment
            {
                Level = riskLevel,
                RiskFactors = riskFactors,
                MitigationStrategy = specialty.ToLower() switch
                {
                    "security" => "Implement comprehensive security remediation plan with immediate focus on critical vulnerabilities",
                    "performance" => "Execute performance optimization strategy focusing on identified bottlenecks",
                    "architecture" => "Plan architectural refactoring to address design issues and technical debt",
                    _ => "Address identified issues according to priority and business impact"
                }
            };
        }

        private Dictionary<string, object> CalculateSpecialtyMetrics(string analysis, string specialty)
        {
            return specialty.ToLower() switch
            {
                "security" => new Dictionary<string, object>
                {
                    ["VulnerabilityCount"] = CountOccurrences(analysis, new[] { "vulnerability", "flaw", "weakness", "risk", "exposure" }),
                    ["ComplianceGaps"] = CountOccurrences(analysis, new[] { "compliance", "regulation", "standard", "requirement", "policy" }),
                    ["CriticalFindings"] = CountOccurrences(analysis, new[] { "critical", "severe", "dangerous", "urgent", "immediate" })
                },
                "performance" => new Dictionary<string, object>
                {
                    ["BottleneckCount"] = CountOccurrences(analysis, new[] { "bottleneck", "slow", "inefficient" }),
                    ["OptimizationOpportunities"] = CountOccurrences(analysis, new[] { "optimization", "improve", "enhance" }),
                    ["ScalabilityIssues"] = CountOccurrences(analysis, new[] { "scalability", "concurrent", "load" })
                },
                "architecture" => new Dictionary<string, object>
                {
                    ["DesignPatternViolations"] = CountOccurrences(analysis, new[] { "violation", "poor", "bad", "anti-pattern", "smell" }),
                    ["ArchitecturalDebt"] = CountOccurrences(analysis, new[] { "debt", "refactor", "restructure", "legacy", "outdated" }),
                    ["ModernizationComplexity"] = CountOccurrences(analysis, new[] { "complex", "difficult", "challenging", "significant", "major" })
                },
                _ => new Dictionary<string, object>()
            };
        }

        private string DetermineSecurityRiskLevel(int riskFactorCount)
        {
            return _securityConfig.RiskLevelByFactorCount.TryGetValue(riskFactorCount, out var level)
                ? level
                : riskFactorCount > _securityConfig.RiskLevelByFactorCount.Keys.DefaultIfEmpty(0).Max()
                    ? "CRITICAL"
                    : "LOW";
        }

        private string DetermineGeneralRiskLevel(int riskFactorCount)
        {
            return riskFactorCount switch
            {
                0 => "LOW",
                1 => "MEDIUM",
                2 => "HIGH",
                _ => "CRITICAL"
            };
        }

        private string DetermineSeverity(string analysis, string pattern)
        {
            var context = ExtractPatternContext(analysis, pattern);

            if (context.Contains("critical", StringComparison.OrdinalIgnoreCase) ||
                context.Contains("severe", StringComparison.OrdinalIgnoreCase) ||
                context.Contains("dangerous", StringComparison.OrdinalIgnoreCase))
                return "CRITICAL";

            if (context.Contains("high", StringComparison.OrdinalIgnoreCase) ||
                context.Contains("important", StringComparison.OrdinalIgnoreCase))
                return "HIGH";

            if (context.Contains("medium", StringComparison.OrdinalIgnoreCase) ||
                context.Contains("moderate", StringComparison.OrdinalIgnoreCase))
                return "MEDIUM";

            return "LOW";
        }

        private string GetLocationDescription(string specialty)
        {
            return specialty.ToLower() switch
            {
                "security" => "Multiple locations - detailed review required",
                "performance" => "Code analysis - specific locations require detailed review",
                "architecture" => "System architecture - multiple components affected",
                _ => "Analysis results - review required"
            };
        }

        private List<string> ExtractEvidence(string analysis, string pattern)
        {
            var evidence = new List<string>();
            var sentences = analysis.Split('.', StringSplitOptions.RemoveEmptyEntries);

            foreach (var sentence in sentences)
            {
                if (sentence.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    evidence.Add(sentence.Trim());
                }
            }

            var maxItems = _securityConfig.MaxEvidenceItems;
            return evidence.Take(maxItems).ToList();
        }

        private string ExtractPatternContext(string analysis, string pattern)
        {
            var sentences = analysis.Split('.', StringSplitOptions.RemoveEmptyEntries);
            return sentences.FirstOrDefault(s => s.Contains(pattern, StringComparison.OrdinalIgnoreCase)) ?? "";
        }

        private int CountOccurrences(string text, string[] keywords)
        {
            return keywords.Sum(keyword => CountOccurrences(text, keyword));
        }

        private int CountOccurrences(string text, string keyword)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword))
                return 0;
            
            return (text.Length - text.Replace(keyword, "", StringComparison.OrdinalIgnoreCase).Length) / keyword.Length;
        }
    }
}
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PoC1_LegacyAnalyzer_Web.Models;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting; // For IHostEnvironment

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
        private readonly IHostEnvironment? _env;
        private readonly IFindingValidationService? _validationService;
        private readonly IRobustJsonExtractor? _jsonExtractor;
        private readonly IConfidenceValidationService? _confidenceValidationService;

        public ResultTransformerService(
            ILogger<ResultTransformerService> logger,
            IConfiguration configuration,
            IHostEnvironment? env = null,
            IFindingValidationService? validationService = null,
            IRobustJsonExtractor? jsonExtractor = null,
            IConfidenceValidationService? confidenceValidationService = null)
        {
            _logger = logger;
            _env = env;
            _validationService = validationService;
            _jsonExtractor = jsonExtractor;
            _confidenceValidationService = confidenceValidationService;

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
            if (_env?.IsDevelopment() == true)
            {
                _logger.LogInformation("Raw LLM Analysis Response (truncated):\n{Raw}", Truncate(rawAnalysis, 8000));
            }

            try
            {
                LLMStructuredResponse? structured = null;

                // Use robust JSON extractor if available, otherwise fall back to legacy method
                if (_jsonExtractor != null)
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    };
                    structured = _jsonExtractor.ExtractAndParse<LLMStructuredResponse>(rawAnalysis, options);
                }
                else
                {
                    // Legacy fallback for backward compatibility
                    var cleanedJson = CleanPotentialJson(rawAnalysis);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    };
                    structured = JsonSerializer.Deserialize<LLMStructuredResponse>(cleanedJson, options);
                }

                if (structured != null && structured.ConfidenceScore > 0)
                {
                    // --- PATCH START ---
                    var findings = structured.Findings ?? new List<Finding>();
                    if (findings.Count == 0 && specialty.Equals("security", StringComparison.OrdinalIgnoreCase))
                    {
                        findings.Add(new Finding
                        {
                            Category = "Security",
                            Description = "No security issues were detected in the provided context.",
                            Severity = "LOW",
                            Location = "",
                            Evidence = new List<string>()
                        });
                    }
                    // --- PATCH END ---

                    // Validate findings if validation service is available
                    ValidateFindings(findings);
                    
                    // Validate and normalize explainability for each finding
                    ValidateExplainability(findings);

                    return new SpecialistAnalysisResult
                    {
                        AgentName = agentName,
                        Specialty = specialty,
                        ConfidenceScore = structured.ConfidenceScore,
                        BusinessImpact = !string.IsNullOrWhiteSpace(structured.BusinessImpact)
                            ? structured.BusinessImpact
                            : ExtractBusinessImpact(rawAnalysis, specialty),
                        KeyFindings = findings,
                        Recommendations = structured.Recommendations ?? new List<Recommendation>()
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse LLM response as JSON. Falling back to text parsing.");
            }

            // Fallback: Parse from unstructured text
            return ParseFromUnstructuredText(rawAnalysis, agentName, specialty);
        }

        // Helper: Clean LLM output for JSON parsing
        private string CleanPotentialJson(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "{}";
            var cleaned = input.Trim();

            // Remove Markdown code fences (```json ... ``')
            cleaned = Regex.Replace(cleaned, @"^```json\s*|```$", "", RegexOptions.IgnoreCase | RegexOptions.Multiline).Trim();
            cleaned = Regex.Replace(cleaned, @"^```[\w]*\s*|```$", "", RegexOptions.IgnoreCase | RegexOptions.Multiline).Trim();

            // Remove any leading/trailing non-JSON text
            var firstBrace = cleaned.IndexOf('{');
            var lastBrace = cleaned.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                cleaned = cleaned.Substring(firstBrace, lastBrace - firstBrace + 1);
            }

            // Remove trailing commas in objects/arrays
            cleaned = Regex.Replace(cleaned, @",(\s*[\]}])", "$1");

            // Optionally normalize quotes (rarely needed)
            // cleaned = cleaned.Replace("�", "\"").Replace("�", "\"").Replace("�", "'").Replace("�", "'");

            return cleaned;
        }

        // Helper: Fallback parsing from unstructured text
        private SpecialistAnalysisResult ParseFromUnstructuredText(string rawAnalysis, string agentName, string specialty)
        {
            var findings = ExtractFindingsFromText(rawAnalysis, specialty);
            var recommendations = ExtractRecommendations(rawAnalysis, specialty);

            // Validate findings if validation service is available
            ValidateFindings(findings);
            
            // Validate explainability for findings
            ValidateExplainability(findings);

            return new SpecialistAnalysisResult
            {
                AgentName = agentName,
                Specialty = specialty,
                ConfidenceScore = CalculateConfidenceScore(rawAnalysis, specialty),
                BusinessImpact = ExtractBusinessImpact(rawAnalysis, specialty),
                KeyFindings = findings,
                Recommendations = recommendations
            };
        }

        // Helper: Validate findings using validation service
        private void ValidateFindings(List<Finding> findings)
        {
            if (_validationService == null || findings == null || !findings.Any())
                return;

            try
            {
                // Validate without file content (will still check severity consistency and contradictions)
                var fileContents = new Dictionary<string, string>(); // Empty - validation will work with what it can
                var validationResults = _validationService.ValidateFindings(findings, fileContents);

                // Attach validation results to findings
                for (int i = 0; i < findings.Count && i < validationResults.Count; i++)
                {
                    findings[i].Validation = validationResults[i];
                }

                _logger.LogDebug("Validated {Count} findings", findings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during finding validation");
                // Don't fail the transformation if validation fails
            }
        }

        // Improved findings extraction: parse LLM prose for findings
        private List<Finding> ExtractFindingsFromText(string analysis, string specialty)
        {
            var findings = new List<Finding>();
            if (string.IsNullOrWhiteSpace(analysis)) return findings;

            // Patterns for findings
            var patterns = new[]
            {
                new { Regex = new Regex(@"Finding:\s*(.+?)(?=(?:\nFinding:|\nRecommendation:|\nIssue:|\nProblem:|$))", RegexOptions.Singleline | RegexOptions.IgnoreCase), Label = "Finding:" },
                new { Regex = new Regex(@"Issue:\s*(.+?)(?=(?:\nIssue:|\nFinding:|\nRecommendation:|\nProblem:|$))", RegexOptions.Singleline | RegexOptions.IgnoreCase), Label = "Issue:" },
                new { Regex = new Regex(@"Problem:\s*(.+?)(?=(?:\nProblem:|\nIssue:|\nFinding:|\nRecommendation:|$))", RegexOptions.Singleline | RegexOptions.IgnoreCase), Label = "Problem:" },
                new { Regex = new Regex(@"^\d+\.\s*(.+?)(?=(?:\n\d+\.|\nRecommendation:|$))", RegexOptions.Multiline | RegexOptions.Singleline), Label = "Numbered" },
                new { Regex = new Regex(@"^[-*]\s+(.+?)(?=(?:\n[-*]\s+|$))", RegexOptions.Multiline | RegexOptions.Singleline), Label = "Bullet" }
            };

            var matches = new List<string>();
            foreach (var pat in patterns)
            {
                foreach (Match m in pat.Regex.Matches(analysis))
                {
                    var text = m.Groups[1].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                        matches.Add(text);
                }
            }

            // If no matches, fallback to sentence-based heuristics
            if (matches.Count == 0)
            {
                // Split by sentences, look for "vulnerability", "issue", "problem", etc.
                var sentences = Regex.Split(analysis, @"(?<=[.!?])\s+");
                foreach (var s in sentences)
                {
                    if (Regex.IsMatch(s, @"vulnerability|issue|problem|risk|flaw|defect", RegexOptions.IgnoreCase))
                        matches.Add(s.Trim());
                }
            }

            foreach (var findingText in matches.Distinct())
            {
                findings.Add(new Finding
                {
                    Category = DetermineCategoryFromText(findingText, specialty),
                    Description = findingText,
                    Severity = DetermineSeverityFromText(findingText),
                    Location = ExtractLocationFromText(findingText),
                    Evidence = ExtractEvidenceFromText(findingText, analysis)
                });
            }

            return findings;
        }

        // Helper: Determine category from finding text
        private string DetermineCategoryFromText(string text, string specialty)
        {
            if (text.Contains("SQL injection", StringComparison.OrdinalIgnoreCase)) return "SQL Injection";
            if (text.Contains("authentication", StringComparison.OrdinalIgnoreCase)) return "Authentication";
            if (text.Contains("authorization", StringComparison.OrdinalIgnoreCase)) return "Authorization";
            if (text.Contains("performance", StringComparison.OrdinalIgnoreCase)) return "Performance";
            if (text.Contains("architecture", StringComparison.OrdinalIgnoreCase)) return "Architecture";
            if (text.Contains("pattern", StringComparison.OrdinalIgnoreCase)) return "Pattern";
            if (text.Contains("scalability", StringComparison.OrdinalIgnoreCase)) return "Scalability";
            if (text.Contains("memory", StringComparison.OrdinalIgnoreCase)) return "Memory";
            if (text.Contains("design", StringComparison.OrdinalIgnoreCase)) return "Design";
            // Fallback to specialty
            return specialty;
        }

        // Helper: Determine severity from finding text
        private string DetermineSeverityFromText(string text)
        {
            if (Regex.IsMatch(text, @"critical|severe|urgent|dangerous", RegexOptions.IgnoreCase)) return "CRITICAL";
            if (Regex.IsMatch(text, @"high|important|significant", RegexOptions.IgnoreCase)) return "HIGH";
            if (Regex.IsMatch(text, @"medium|moderate", RegexOptions.IgnoreCase)) return "MEDIUM";
            if (Regex.IsMatch(text, @"low|minor|trivial", RegexOptions.IgnoreCase)) return "LOW";
            return "MEDIUM";
        }

        // Helper: Extract location from finding text (file, line, class, method)
        private string ExtractLocationFromText(string text)
        {
            // Look for "in <FileName> line <number>" or "at <FileName>:<number>"
            var fileLine = Regex.Match(text, @"(\w+\.(cs|js|ts|py|java|cpp|h|cshtml))\s*(line|:)?\s*(\d+)?", RegexOptions.IgnoreCase);
            if (fileLine.Success)
            {
                var file = fileLine.Groups[1].Value;
                var line = fileLine.Groups[4].Success ? $" line {fileLine.Groups[4].Value}" : "";
                return $"{file}{line}".Trim();
            }
            // Look for class/method
            var classMatch = Regex.Match(text, @"class\s+(\w+)", RegexOptions.IgnoreCase);
            if (classMatch.Success) return $"Class {classMatch.Groups[1].Value}";
            var methodMatch = Regex.Match(text, @"method\s+(\w+)", RegexOptions.IgnoreCase);
            if (methodMatch.Success) return $"Method {methodMatch.Groups[1].Value}";
            return "";
        }

        // Helper: Extract evidence (first 200 chars or code block)
        private List<string> ExtractEvidenceFromText(string findingText, string fullText)
        {
            var evidence = new List<string>();
            // Try to find code block near findingText
            var idx = fullText.IndexOf(findingText, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                // Look for code block after finding
                var after = fullText.Substring(idx);
                var codeBlock = Regex.Match(after, @"```[a-zA-Z]*\s*([\s\S]+?)```", RegexOptions.Multiline);
                if (codeBlock.Success)
                {
                    evidence.Add(codeBlock.Groups[1].Value.Trim());
                }
            }
            // Always add a concise snippet
            evidence.Add(findingText.Length > 200 ? findingText.Substring(0, 200) : findingText);
            return evidence.Distinct().ToList();
        }

        // Truncate helper for logging
        private string Truncate(string input, int maxLen)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return input.Length <= maxLen ? input : input.Substring(0, maxLen) + "...";
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

        /// <summary>
        /// Validates and normalizes explainability for findings.
        /// </summary>
        private void ValidateExplainability(List<Finding> findings)
        {
            if (_confidenceValidationService == null || findings == null || !findings.Any())
            {
                _logger.LogDebug("ConfidenceValidationService not available or no findings to validate explainability.");
                return;
            }

            try
            {
                foreach (var finding in findings)
                {
                    if (finding.Explainability != null)
                    {
                        finding.Explainability = _confidenceValidationService.ValidateAndNormalize(finding.Explainability);
                    }
                }
                _logger.LogDebug("Validated explainability for {Count} findings.", findings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during explainability validation");
                // Don't fail the transformation if validation fails
            }
        }
    }
}
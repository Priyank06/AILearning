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
    public class SecurityAnalystAgent : ISpecialistAgentService
    {
        private readonly Kernel _kernel;
        private readonly ILogger<SecurityAnalystAgent> _logger;
        private readonly AgentConfiguration _agentConfig;

        public string AgentName => _agentConfig.AgentProfiles["security"].AgentName;
        public string Specialty => _agentConfig.AgentProfiles["security"].Specialty;
        public string AgentPersona => _agentConfig.AgentProfiles["security"].AgentPersona;
        public int ConfidenceThreshold => _agentConfig.AgentProfiles["security"].ConfidenceThreshold;

        public SecurityAnalystAgent(Kernel kernel, ILogger<SecurityAnalystAgent> logger, IConfiguration configuration)
        {
            _kernel = kernel;
            _logger = logger;
            _kernel.Plugins.AddFromObject(this, "SecurityAnalyst");

            _agentConfig = new AgentConfiguration();
            configuration.GetSection("AgentConfiguration").Bind(_agentConfig);
            var profile = _agentConfig.AgentProfiles["security"];
        }

        [KernelFunction, Description("Perform comprehensive security analysis of C# code")]
        public async Task<string> AnalyzeSecurityVulnerabilities(
            [Description("C# source code to analyze")] string code,
            [Description("Security compliance requirements (OWASP, PCI-DSS, etc.)")] string complianceStandards,
            [Description("Business context and risk tolerance")] string businessContext)
        {
            var template = _agentConfig.AgentPromptTemplates["security"].AnalysisPrompt;
            var prompt = template
                .Replace("{agentPersona}", AgentPersona)
                .Replace("{code}", code)
                .Replace("{complianceStandards}", complianceStandards)
                .Replace("{businessContext}", businessContext);

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? _agentConfig.AgentPromptTemplates["security"].DefaultResponse;
        }

        [KernelFunction, Description("Review another agent's analysis from security perspective")]
        public async Task<string> ReviewPeerAnalysisFromSecurityPerspective(
            [Description("Another agent's analysis to review")] string peerAnalysis,
            [Description("Original code being analyzed")] string originalCode,
            [Description("Security-specific concerns to validate")] string reviewFocus)
        {
            var template = _agentConfig.AgentPromptTemplates["security"].PeerReviewPrompt;
            var prompt = template
                .Replace("{agentPersona}", AgentPersona)
                .Replace("{peerAnalysis}", peerAnalysis)
                .Replace("{originalCode}", originalCode)
                .Replace("{reviewFocus}", reviewFocus);

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? _agentConfig.AgentPromptTemplates["security"].DefaultResponse;
        }

        public async Task<string> AnalyzeAsync(
            string code,
            string businessContext,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("SecurityAnalyst starting analysis for business context: {BusinessContext}", businessContext);

                var securityAnalysis = await AnalyzeSecurityVulnerabilities(
                    code,
                    "OWASP Top 10, PCI-DSS, SOX Compliance",
                    businessContext);

                var result = new
                {
                    AgentName = AgentName,
                    Specialty = Specialty,
                    ConfidenceScore = CalculateConfidenceScore(securityAnalysis),
                    BusinessImpact = ExtractBusinessImpact(securityAnalysis),
                    EstimatedEffort = EstimateRemediationEffort(securityAnalysis),
                    Priority = DeterminePriority(securityAnalysis),
                    KeyFindings = ExtractFindings(securityAnalysis),
                    Recommendations = ExtractRecommendations(securityAnalysis),
                    RiskLevel = AssessRisk(securityAnalysis),
                    SpecialtyMetrics = new Dictionary<string, object>
                    {
                        ["VulnerabilityCount"] = CountVulnerabilities(securityAnalysis),
                        ["ComplianceGaps"] = CountComplianceGaps(securityAnalysis),
                        ["CriticalFindings"] = CountCriticalFindings(securityAnalysis)
                    }
                };

                _logger.LogInformation("SecurityAnalyst completed analysis with confidence: {Confidence}%", result.ConfidenceScore);
                return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SecurityAnalyst analysis failed");
                return CreateErrorResult(ex.Message);
            }
        }

        public async Task<string> ReviewPeerAnalysisAsync(
            string peerAnalysis,
            string originalCode,
            CancellationToken cancellationToken = default)
        {
            return await ReviewPeerAnalysisFromSecurityPerspective(
                peerAnalysis,
                originalCode,
                "Data security, access controls, input validation, compliance requirements");
        }

        // Helper methods for parsing AI responses and calculating metrics
        private int CalculateConfidenceScore(string analysis)
        {
            var qualityIndicators = new[]
            {
                analysis.Contains("vulnerability", StringComparison.OrdinalIgnoreCase),
                analysis.Contains("security", StringComparison.OrdinalIgnoreCase),
                analysis.Contains("risk", StringComparison.OrdinalIgnoreCase),
                analysis.Contains("recommendation", StringComparison.OrdinalIgnoreCase),
                analysis.Length > 500
            };

            var score = qualityIndicators.Count(indicator => indicator) * 20;
            return Math.Min(100, Math.Max(0, score));
        }

        private string ExtractBusinessImpact(string analysis)
        {
            var businessKeywords = new[] { "business", "cost", "risk", "compliance", "revenue", "reputation" };
            var sentences = analysis.Split('.', StringSplitOptions.RemoveEmptyEntries);

            var businessSentence = sentences.FirstOrDefault(s =>
                businessKeywords.Any(keyword => s.Contains(keyword, StringComparison.OrdinalIgnoreCase)));

            return businessSentence?.Trim() ?? "Business impact assessment pending detailed analysis";
        }

        private decimal EstimateRemediationEffort(string analysis)
        {
            var complexityKeywords = new[] { "refactor", "architecture", "framework", "migration", "comprehensive" };
            var complexityCount = complexityKeywords.Count(keyword =>
                analysis.Contains(keyword, StringComparison.OrdinalIgnoreCase));

            return complexityCount switch
            {
                0 => 8m,
                1 => 16m,
                2 => 40m,
                >= 3 => 80m
            };
        }

        private string DeterminePriority(string analysis)
        {
            if (analysis.Contains("critical", StringComparison.OrdinalIgnoreCase) ||
                analysis.Contains("severe", StringComparison.OrdinalIgnoreCase))
                return "CRITICAL";

            if (analysis.Contains("high", StringComparison.OrdinalIgnoreCase) ||
                analysis.Contains("important", StringComparison.OrdinalIgnoreCase))
                return "HIGH";

            if (analysis.Contains("medium", StringComparison.OrdinalIgnoreCase) ||
                analysis.Contains("moderate", StringComparison.OrdinalIgnoreCase))
                return "MEDIUM";

            return "LOW";
        }

        private List<object> ExtractFindings(string analysis)
        {
            var findings = new List<object>();

            var vulnerabilityPatterns = new[]
            {
                ("SQL Injection", "injection"),
                ("Authentication Flaw", "authentication"),
                ("Authorization Issue", "authorization"),
                ("Data Exposure", "exposure"),
                ("Input Validation", "validation")
            };

            foreach (var (category, pattern) in vulnerabilityPatterns)
            {
                if (analysis.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    findings.Add(new
                    {
                        Category = category,
                        Description = $"{category} vulnerability identified in code analysis",
                        Severity = DetermineSeverity(analysis, pattern),
                        Location = "Multiple locations - detailed review required",
                        Evidence = ExtractEvidence(analysis, pattern)
                    });
                }
            }

            return findings;
        }

        private List<object> ExtractRecommendations(string analysis)
        {
            var recommendations = new List<object>();

            if (analysis.Contains("parameterized", StringComparison.OrdinalIgnoreCase))
            {
                recommendations.Add(new
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
                recommendations.Add(new
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

        private object AssessRisk(string analysis)
        {
            var riskFactors = new List<string>();

            if (analysis.Contains("injection", StringComparison.OrdinalIgnoreCase))
                riskFactors.Add("SQL injection vulnerability allows data theft");

            if (analysis.Contains("authentication", StringComparison.OrdinalIgnoreCase))
                riskFactors.Add("Weak authentication enables unauthorized access");

            if (analysis.Contains("encryption", StringComparison.OrdinalIgnoreCase))
                riskFactors.Add("Inadequate encryption exposes sensitive data");

            return new
            {
                Level = riskFactors.Count switch
                {
                    0 => "LOW",
                    1 => "MEDIUM",
                    2 => "HIGH",
                    _ => "CRITICAL"
                },
                RiskFactors = riskFactors,
                MitigationStrategy = "Implement comprehensive security remediation plan with immediate focus on critical vulnerabilities"
            };
        }

        private string DetermineSeverity(string analysis, string pattern)
        {
            var patternContext = ExtractPatternContext(analysis, pattern);

            if (patternContext.Contains("critical", StringComparison.OrdinalIgnoreCase) ||
                patternContext.Contains("severe", StringComparison.OrdinalIgnoreCase))
                return "CRITICAL";

            if (patternContext.Contains("high", StringComparison.OrdinalIgnoreCase) ||
                patternContext.Contains("dangerous", StringComparison.OrdinalIgnoreCase))
                return "HIGH";

            if (patternContext.Contains("medium", StringComparison.OrdinalIgnoreCase))
                return "MEDIUM";

            return "LOW";
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

            return evidence.Take(3).ToList();
        }

        private string ExtractPatternContext(string analysis, string pattern)
        {
            var sentences = analysis.Split('.', StringSplitOptions.RemoveEmptyEntries);
            return sentences.FirstOrDefault(s => s.Contains(pattern, StringComparison.OrdinalIgnoreCase)) ?? "";
        }

        private int CountVulnerabilities(string analysis)
        {
            var vulnerabilityKeywords = new[] { "vulnerability", "flaw", "weakness", "risk", "exposure" };
            return vulnerabilityKeywords.Sum(keyword => CountOccurrences(analysis, keyword));
        }

        private int CountComplianceGaps(string analysis)
        {
            var complianceKeywords = new[] { "compliance", "regulation", "standard", "requirement", "policy" };
            return complianceKeywords.Sum(keyword => CountOccurrences(analysis, keyword));
        }

        private int CountCriticalFindings(string analysis)
        {
            var criticalKeywords = new[] { "critical", "severe", "dangerous", "urgent", "immediate" };
            return criticalKeywords.Sum(keyword => CountOccurrences(analysis, keyword));
        }

        private int CountOccurrences(string text, string keyword)
        {
            return (text.Length - text.Replace(keyword, "", StringComparison.OrdinalIgnoreCase).Length) / keyword.Length;
        }

        private string CreateErrorResult(string errorMessage)
        {
            var errorResult = new
            {
                AgentName = AgentName,
                Specialty = Specialty,
                ConfidenceScore = 0,
                BusinessImpact = $"Analysis failed: {errorMessage}",
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

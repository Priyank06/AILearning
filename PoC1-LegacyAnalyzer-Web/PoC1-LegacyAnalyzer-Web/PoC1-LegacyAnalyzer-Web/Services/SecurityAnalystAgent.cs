using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using System.ComponentModel;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class SecurityAnalystAgent : ISpecialistAgentService
    {
        private readonly Kernel _kernel;
        private readonly ILogger<SecurityAnalystAgent> _logger;

        public string AgentName => "SecurityAnalyst-Alpha";
        public string Specialty => "Application Security & Compliance Analysis";
        public string AgentPersona => "Senior Application Security Engineer with 15+ years experience in enterprise security, OWASP expertise, and compliance frameworks (SOX, GDPR, PCI-DSS)";
        public int ConfidenceThreshold => 75;

        public SecurityAnalystAgent(Kernel kernel, ILogger<SecurityAnalystAgent> logger)
        {
            _kernel = kernel;
            _logger = logger;

            // Register agent functions with kernel
            _kernel.Plugins.AddFromObject(this, "SecurityAnalyst");
        }

        [KernelFunction, Description("Perform comprehensive security analysis of C# code")]
        public async Task<string> AnalyzeSecurityVulnerabilities(
            [Description("C# source code to analyze")] string code,
            [Description("Security compliance requirements (OWASP, PCI-DSS, etc.)")] string complianceStandards,
            [Description("Business context and risk tolerance")] string businessContext)
        {
            var prompt = $@"
You are {AgentPersona}.

ANALYSIS TARGET:
{code}

COMPLIANCE REQUIREMENTS: {complianceStandards}
BUSINESS CONTEXT: {businessContext}

Perform comprehensive security analysis:

1. VULNERABILITY ASSESSMENT
   - Identify SQL injection risks
   - Authentication/authorization flaws  
   - Input validation issues
   - Data exposure risks
   - Cryptographic weaknesses

2. COMPLIANCE EVALUATION
   - Map findings to compliance standards
   - Assess regulatory risk levels
   - Identify mandatory remediation items

3. BUSINESS RISK ANALYSIS
   - Quantify potential business impact
   - Prioritize fixes by risk level
   - Estimate remediation effort

4. ACTIONABLE RECOMMENDATIONS
   - Specific code changes required
   - Security architecture improvements
   - Process and policy updates

Provide detailed, actionable analysis with confidence scores.";

            var chatCompletion = _kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? "Security analysis unavailable";
        }

        [KernelFunction, Description("Review another agent's analysis from security perspective")]
        public async Task<string> ReviewPeerAnalysisFromSecurityPerspective(
            [Description("Another agent's analysis to review")] string peerAnalysis,
            [Description("Original code being analyzed")] string originalCode,
            [Description("Security-specific concerns to validate")] string securityFocus)
        {
            var prompt = $@"
As {AgentPersona}, review this colleague's analysis:

PEER ANALYSIS:
{peerAnalysis}

ORIGINAL CODE:
{originalCode}

SECURITY REVIEW FOCUS: {securityFocus}

Provide security-focused peer review:
1. Security aspects the peer analysis missed
2. Security implications of their recommendations  
3. Additional security measures needed
4. Risks introduced by suggested changes
5. Security best practices to incorporate

Be collaborative but thorough in identifying security gaps.";

            var chatCompletion = _kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? "Peer review unavailable";
        }

        public async Task<SpecialistAnalysisResult> AnalyzeAsync(
            string code,
            string businessContext,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("SecurityAnalyst starting analysis for business context: {BusinessContext}", businessContext);

                // Perform comprehensive security analysis
                var securityAnalysis = await AnalyzeSecurityVulnerabilities(
                    code,
                    "OWASP Top 10, PCI-DSS, SOX Compliance",
                    businessContext);

                // Parse AI response and structure results
                var result = new SpecialistAnalysisResult
                {
                    AgentName = AgentName,
                    Specialty = Specialty,
                    ConfidenceScore = CalculateConfidenceScore(securityAnalysis),
                    BusinessImpact = ExtractBusinessImpact(securityAnalysis),
                    EstimatedEffort = EstimateRemediationEffort(securityAnalysis),
                    Priority = DeterminePriority(securityAnalysis),
                    KeyFindings = ExtractFindings(securityAnalysis),
                    Recommendations = ExtractRecommendations(securityAnalysis),
                    RiskLevel = AssessRisk(securityAnalysis)
                };

                // Add security-specific metrics
                result.SpecialtyMetrics.Add("VulnerabilityCount", CountVulnerabilities(securityAnalysis));
                result.SpecialtyMetrics.Add("ComplianceGaps", CountComplianceGaps(securityAnalysis));
                result.SpecialtyMetrics.Add("CriticalFindings", CountCriticalFindings(securityAnalysis));

                _logger.LogInformation("SecurityAnalyst completed analysis with confidence: {Confidence}%", result.ConfidenceScore);
                return result;
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
            // Analyze response quality and completeness
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
            // Extract business impact statements from AI response
            var businessKeywords = new[] { "business", "cost", "risk", "compliance", "revenue", "reputation" };
            var sentences = analysis.Split('.', StringSplitOptions.RemoveEmptyEntries);

            var businessSentence = sentences.FirstOrDefault(s =>
                businessKeywords.Any(keyword => s.Contains(keyword, StringComparison.OrdinalIgnoreCase)));

            return businessSentence?.Trim() ?? "Business impact assessment pending detailed analysis";
        }

        private decimal EstimateRemediationEffort(string analysis)
        {
            // Basic effort estimation based on complexity indicators
            var complexityKeywords = new[] { "refactor", "architecture", "framework", "migration", "comprehensive" };
            var complexityCount = complexityKeywords.Count(keyword =>
                analysis.Contains(keyword, StringComparison.OrdinalIgnoreCase));

            return complexityCount switch
            {
                0 => 8m,    // 1 day
                1 => 16m,   // 2 days  
                2 => 40m,   // 1 week
                >= 3 => 80m // 2 weeks+
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

        private List<Finding> ExtractFindings(string analysis)
        {
            // Parse findings from AI response
            var findings = new List<Finding>();

            // Look for vulnerability patterns
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
                    findings.Add(new Finding
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

        private List<Recommendation> ExtractRecommendations(string analysis)
        {
            // Extract actionable recommendations
            var recommendations = new List<Recommendation>();

            if (analysis.Contains("parameterized", StringComparison.OrdinalIgnoreCase))
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

        private RiskAssessment AssessRisk(string analysis)
        {
            var riskFactors = new List<string>();

            if (analysis.Contains("injection", StringComparison.OrdinalIgnoreCase))
                riskFactors.Add("SQL injection vulnerability allows data theft");

            if (analysis.Contains("authentication", StringComparison.OrdinalIgnoreCase))
                riskFactors.Add("Weak authentication enables unauthorized access");

            if (analysis.Contains("encryption", StringComparison.OrdinalIgnoreCase))
                riskFactors.Add("Inadequate encryption exposes sensitive data");

            return new RiskAssessment
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
            // Extract evidence related to specific pattern
            var evidence = new List<string>();
            var sentences = analysis.Split('.', StringSplitOptions.RemoveEmptyEntries);

            foreach (var sentence in sentences)
            {
                if (sentence.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    evidence.Add(sentence.Trim());
                }
            }

            return evidence.Take(3).ToList(); // Limit to top 3 evidence items
        }

        private string ExtractPatternContext(string analysis, string pattern)
        {
            var sentences = analysis.Split('.', StringSplitOptions.RemoveEmptyEntries);
            return sentences.FirstOrDefault(s => s.Contains(pattern, StringComparison.OrdinalIgnoreCase)) ?? "";
        }

        private int CountVulnerabilities(string analysis)
        {
            var vulnerabilityKeywords = new[] { "vulnerability", "flaw", "weakness", "risk", "exposure" };
            return vulnerabilityKeywords.Sum(keyword =>
                CountOccurrences(analysis, keyword));
        }

        private int CountComplianceGaps(string analysis)
        {
            var complianceKeywords = new[] { "compliance", "regulation", "standard", "requirement", "policy" };
            return complianceKeywords.Sum(keyword =>
                CountOccurrences(analysis, keyword));
        }

        private int CountCriticalFindings(string analysis)
        {
            var criticalKeywords = new[] { "critical", "severe", "dangerous", "urgent", "immediate" };
            return criticalKeywords.Sum(keyword =>
                CountOccurrences(analysis, keyword));
        }

        private int CountOccurrences(string text, string keyword)
        {
            return (text.Length - text.Replace(keyword, "", StringComparison.OrdinalIgnoreCase).Length) / keyword.Length;
        }

        private SpecialistAnalysisResult CreateErrorResult(string errorMessage)
        {
            return new SpecialistAnalysisResult
            {
                AgentName = AgentName,
                Specialty = Specialty,
                ConfidenceScore = 0,
                BusinessImpact = $"Analysis failed: {errorMessage}",
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

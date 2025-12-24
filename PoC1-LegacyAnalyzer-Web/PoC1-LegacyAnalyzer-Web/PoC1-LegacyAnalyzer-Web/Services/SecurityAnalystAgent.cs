using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PoC1_LegacyAnalyzer_Web.Models;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Text.Json;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class SecurityAnalystAgent : ISpecialistAgentService
    {
        private readonly Kernel _kernel;
        private readonly ILogger<SecurityAnalystAgent> _logger;
        private readonly AgentConfiguration _agentConfig;
        private readonly IResultTransformerService _resultTransformer;
        private readonly AgentLegacyIndicatorsConfiguration _legacyIndicators;
        private readonly LegacyContextMessagesConfiguration _legacyContextMessages;

        public string AgentName => _agentConfig.AgentProfiles["security"].AgentName;
        public string Specialty => _agentConfig.AgentProfiles["security"].Specialty;
        public string AgentPersona => _agentConfig.AgentProfiles["security"].AgentPersona;
        public int ConfidenceThreshold => _agentConfig.AgentProfiles["security"].ConfidenceThreshold;

        public SecurityAnalystAgent(
            Kernel kernel, 
            ILogger<SecurityAnalystAgent> logger, 
            IConfiguration configuration,
            IResultTransformerService resultTransformer,
            IOptions<AgentLegacyIndicatorsConfiguration> legacyIndicators,
            IOptions<LegacyContextMessagesConfiguration> legacyContextMessages)
        {
            _kernel = kernel;
            _logger = logger;
            _resultTransformer = resultTransformer;
            _legacyIndicators = legacyIndicators?.Value ?? new AgentLegacyIndicatorsConfiguration();
            _legacyContextMessages = legacyContextMessages?.Value ?? new LegacyContextMessagesConfiguration();
            _kernel.Plugins.AddFromObject(this, "SecurityAnalyst");

            _agentConfig = new AgentConfiguration();
            configuration.GetSection("AgentConfiguration").Bind(_agentConfig);
        }

        [KernelFunction, Description("Perform comprehensive security analysis of source code")]
        public async Task<string> AnalyzeSecurityVulnerabilities(
            [Description("Source code to analyze (supports multiple languages)")] string code,
            [Description("Security compliance requirements (OWASP, PCI-DSS, etc.)")] string complianceStandards,
            [Description("Business context and risk tolerance")] string businessContext)
        {
            var template = _agentConfig.AgentPromptTemplates["security"].AnalysisPrompt;
            
            // Extract legacy context from code if available (check for legacy indicators in the metadata summary)
            var legacyContext = ExtractLegacyContextFromCode(code);
            
            var prompt = template
                .Replace("{agentPersona}", AgentPersona)
                .Replace("{code}", code)
                .Replace("{complianceStandards}", complianceStandards)
                .Replace("{businessContext}", businessContext)
                .Replace("{legacyContext}", legacyContext);

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
                return result.Content ?? _agentConfig.AgentPromptTemplates["security"].DefaultResponse;
        }

        private string ExtractLegacyContextFromCode(string code)
        {
            // Check if code contains legacy indicators (this is a metadata summary, not full code)
            // The actual legacy context should come from metadata, but we can detect some patterns here
            if (string.IsNullOrEmpty(code))
                return "";

            var legacyIndicators = new List<string>();

            // Check for legacy framework patterns
            if (code.Contains("System.Web.UI") || code.Contains("System.EnterpriseServices") || 
                code.Contains("System.Runtime.Remoting") || code.Contains("System.Web.Services"))
            {
                legacyIndicators.Add(_legacyIndicators.Security.AncientFramework);
            }

            // Check for global state patterns
            if (code.Contains("HttpContext.Current") || code.Contains("Application[") || code.Contains("Session["))
            {
                legacyIndicators.Add(_legacyIndicators.Security.GlobalState);
            }

            // Check for obsolete API patterns
            if (code.Contains("DataSet") || code.Contains("DataTable") || code.Contains("BinaryFormatter"))
            {
                legacyIndicators.Add(_legacyIndicators.Security.ObsoleteApis);
            }

            if (!legacyIndicators.Any())
                return "";

            return _legacyContextMessages.LegacyContextHeaderSimple + string.Join("\n", legacyIndicators) +
                   _legacyContextMessages.SecurityLegacyContextFooter;
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

                // Step 1: Get raw analysis from LLM
                var rawAnalysis = await AnalyzeSecurityVulnerabilities(
                    code,
                    "OWASP Top 10, PCI-DSS, SOX Compliance",
                    businessContext);

                // Step 2: Transform raw analysis to structured result
                var result = _resultTransformer.TransformToResult(rawAnalysis, AgentName, Specialty);

                _logger.LogInformation("SecurityAnalyst completed analysis with confidence: {Confidence}%", result.ConfidenceScore);
                return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SecurityAnalyst analysis failed");
                var errorResult = _resultTransformer.CreateErrorResult(ex.Message, AgentName, Specialty);
                return JsonSerializer.Serialize(errorResult, new JsonSerializerOptions { WriteIndented = true });
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
    }
}

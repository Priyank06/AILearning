using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PoC1_LegacyAnalyzer_Web.Models;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Text.Json;

namespace PoC1_LegacyAnalyzer_Web.Services.AI
{
    public class ArchitecturalAnalystAgent : ISpecialistAgentService
    {
        private readonly Kernel _kernel;
        private readonly ILogger<ArchitecturalAnalystAgent> _logger;
        private readonly AgentConfiguration _agentConfig;
        private readonly IResultTransformerService _resultTransformer;
        private readonly AgentLegacyIndicatorsConfiguration _legacyIndicators;
        private readonly LegacyContextMessagesConfiguration _legacyContextMessages;

        public string AgentName => _agentConfig.AgentProfiles["architecture"].AgentName;
        public string Specialty => _agentConfig.AgentProfiles["architecture"].Specialty;
        public string AgentPersona => _agentConfig.AgentProfiles["architecture"].AgentPersona;
        public int ConfidenceThreshold => _agentConfig.AgentProfiles["architecture"].ConfidenceThreshold;

        public ArchitecturalAnalystAgent(
            Kernel kernel, 
            ILogger<ArchitecturalAnalystAgent> logger, 
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
            _kernel.Plugins.AddFromObject(this, "ArchitecturalAnalyst");

            _agentConfig = new AgentConfiguration();
            configuration.GetSection("AgentConfiguration").Bind(_agentConfig);
        }

        [KernelFunction, Description("Analyze software architecture and design patterns")]
        public async Task<string> AnalyzeArchitecturalDesign(
            [Description("Source code to analyze (supports multiple languages)")] string code,
            [Description("Target architecture style and patterns")] string targetArchitecture,
            [Description("Business domain and constraints")] string businessDomain)
        {
            var template = _agentConfig.AgentPromptTemplates["architecture"].AnalysisPrompt;
            
            // Extract legacy context from code
            var legacyContext = ExtractLegacyContextFromCode(code);
            
            var prompt = template
                .Replace("{agentPersona}", AgentPersona)
                .Replace("{code}", code)
                .Replace("{targetArchitecture}", targetArchitecture)
                .Replace("{businessDomain}", businessDomain)
                .Replace("{legacyContext}", legacyContext);

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? _agentConfig.AgentPromptTemplates["architecture"].DefaultResponse;
        }

        private string ExtractLegacyContextFromCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                return "";

            var legacyIndicators = new List<string>();

            // Check for legacy framework patterns
            if (code.Contains("System.Web.UI") || code.Contains("System.EnterpriseServices") || 
                code.Contains("System.Runtime.Remoting") || code.Contains("System.Web.Services"))
            {
                legacyIndicators.Add(_legacyIndicators.Architecture.AncientFramework);
            }

            // Check for global state patterns
            if (code.Contains("HttpContext.Current") || code.Contains("Application[") || code.Contains("Session["))
            {
                legacyIndicators.Add(_legacyIndicators.Architecture.GlobalState);
            }

            // Check for God Objects (large class counts)
            if (code.Contains("Classes:") && int.TryParse(code.Split("Classes:")[1]?.Split(',')[0]?.Trim(), out var classCount) && classCount > 20)
            {
                legacyIndicators.Add(_legacyIndicators.Architecture.GodObjects);
            }

            if (!legacyIndicators.Any())
                return "";

            return _legacyContextMessages.LegacyContextHeaderSimple + string.Join("\n", legacyIndicators) +
                   _legacyContextMessages.ArchitectureLegacyContextFooter;
        }

        public async Task<string> AnalyzeAsync(
            string code,
            string businessContext,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("ArchitecturalAnalyst starting analysis");

                // Step 1: Get raw analysis from LLM
                var rawAnalysis = await AnalyzeArchitecturalDesign(
                    code,
                    "Clean Architecture, Domain-Driven Design, Microservices readiness",
                    businessContext);

                // Step 2: Transform raw analysis to structured result
                var result = _resultTransformer.TransformToResult(rawAnalysis, AgentName, Specialty);

                _logger.LogInformation("ArchitecturalAnalyst completed analysis with confidence: {Confidence}%", result.ConfidenceScore);
                return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ArchitecturalAnalyst analysis failed");
                var errorResult = _resultTransformer.CreateErrorResult(ex.Message, AgentName, Specialty);
                return JsonSerializer.Serialize(errorResult, new JsonSerializerOptions { WriteIndented = true });
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
    }
}


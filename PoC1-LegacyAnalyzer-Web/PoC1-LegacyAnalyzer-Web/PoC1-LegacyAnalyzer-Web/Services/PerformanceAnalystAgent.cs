using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Text.Json;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class PerformanceAnalystAgent : ISpecialistAgentService
    {
        private readonly Kernel _kernel;
        private readonly ILogger<PerformanceAnalystAgent> _logger;
        private readonly AgentConfiguration _agentConfig;
        private readonly IResultTransformerService _resultTransformer;
        private readonly AgentLegacyIndicatorsConfiguration _legacyIndicators;
        private readonly LegacyContextMessagesConfiguration _legacyContextMessages;

        public string AgentName => _agentConfig.AgentProfiles["performance"].AgentName;
        public string Specialty => _agentConfig.AgentProfiles["performance"].Specialty;
        public string AgentPersona => _agentConfig.AgentProfiles["performance"].AgentPersona;
        public int ConfidenceThreshold => _agentConfig.AgentProfiles["performance"].ConfidenceThreshold;

        public PerformanceAnalystAgent(
            Kernel kernel, 
            ILogger<PerformanceAnalystAgent> logger, 
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
            _kernel.Plugins.AddFromObject(this, "PerformanceAnalyst");

            _agentConfig = new AgentConfiguration();
            configuration.GetSection("AgentConfiguration").Bind(_agentConfig);
        }

        [KernelFunction, Description("Analyze code for performance bottlenecks and optimization opportunities")]
        public async Task<string> AnalyzePerformanceBottlenecks(
            [Description("Source code to analyze (supports multiple languages)")] string code,
            [Description("Expected performance requirements")] string performanceRequirements,
            [Description("Scalability targets and constraints")] string scalabilityTargets)
        {
            var template = _agentConfig.AgentPromptTemplates["performance"].AnalysisPrompt;
            
            // Extract legacy context from code
            var legacyContext = ExtractLegacyContextFromCode(code);
            
            var prompt = template
                .Replace("{agentPersona}", AgentPersona)
                .Replace("{code}", code)
                .Replace("{performanceRequirements}", performanceRequirements)
                .Replace("{scalabilityTargets}", scalabilityTargets)
                .Replace("{legacyContext}", legacyContext);

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? _agentConfig.AgentPromptTemplates["performance"].DefaultResponse;
        }

        private string ExtractLegacyContextFromCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                return "";

            var legacyIndicators = new List<string>();

            // Check for legacy data access patterns (often performance bottlenecks)
            if (code.Contains("DataSet") || code.Contains("DataTable") || code.Contains("SqlDataAdapter"))
            {
                legacyIndicators.Add(_legacyIndicators.Performance.LegacyDataAccess);
            }

            // Check for synchronous I/O patterns
            if (code.Contains("File.ReadAllText") || code.Contains("File.WriteAllText") || 
                code.Contains("HttpWebRequest") || code.Contains("WebClient"))
            {
                legacyIndicators.Add(_legacyIndicators.Performance.SynchronousIO);
            }

            // Check for legacy framework patterns
            if (code.Contains("System.Web.UI") || code.Contains("ViewState"))
            {
                legacyIndicators.Add(_legacyIndicators.Performance.LegacyWebForms);
            }

            if (!legacyIndicators.Any())
                return "";

            return _legacyContextMessages.LegacyContextHeaderSimple + string.Join("\n", legacyIndicators) +
                   _legacyContextMessages.PerformanceLegacyContextFooter;
        }

        public async Task<string> AnalyzeAsync(
            string code,
            string businessContext,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("PerformanceAnalyst starting analysis");

                // Step 1: Get raw analysis from LLM
                var rawAnalysis = await AnalyzePerformanceBottlenecks(
                    code,
                    "Sub-second response times, 1000+ concurrent users",
                    "10x user growth over 2 years, 99.9% availability");

                // Step 2: Transform raw analysis to structured result
                var result = _resultTransformer.TransformToResult(rawAnalysis, AgentName, Specialty);

                _logger.LogInformation("PerformanceAnalyst completed analysis with confidence: {Confidence}%", result.ConfidenceScore);
                return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PerformanceAnalyst analysis failed");
                var errorResult = _resultTransformer.CreateErrorResult(ex.Message, AgentName, Specialty);
                return JsonSerializer.Serialize(errorResult, new JsonSerializerOptions { WriteIndented = true });
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
    }
}

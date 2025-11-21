using Azure;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Provides AI-based code analysis using Azure OpenAI and configuration-driven prompt templates.
    /// </summary>
    public class AIAnalysisService : IAIAnalysisService
    {
        private readonly AzureOpenAIChatCompletionService _chatService;
        private readonly string _deploymentName;
        private readonly PromptConfiguration _promptConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="AIAnalysisService"/> class.
        /// Sets up Azure OpenAI and loads prompt configuration from appsettings.json.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if Azure OpenAI or prompt configuration is missing or invalid.
        /// </exception>
        public AIAnalysisService(IConfiguration configuration)
        {
            var endpoint = configuration["AzureOpenAI:Endpoint"]
                ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");

            var apiKey = configuration["AzureOpenAI:ApiKey"]
                ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");

            var deployment = configuration["AzureOpenAI:Deployment"]
                ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT")
                ?? "gpt-4"; // Update default to gpt-4

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new InvalidOperationException(
                    "Azure OpenAI endpoint is not configured. " +
                    "Set 'AzureOpenAI:Endpoint' in appsettings.json or AZURE_OPENAI_ENDPOINT environment variable.");
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Azure OpenAI API key is not configured. " +
                    "Set 'AzureOpenAI:ApiKey' in appsettings.json or AZURE_OPENAI_KEY environment variable. " +
                    "For development, use: dotnet user-secrets set 'AzureOpenAI:ApiKey' 'your-key'");
            }

            _deploymentName = deployment;
            _chatService = new AzureOpenAIChatCompletionService(
                deploymentName: deployment,
                endpoint: endpoint,
                apiKey: apiKey
            );

            // Bind PromptConfiguration section
            _promptConfig = new PromptConfiguration();
            configuration.GetSection("PromptConfiguration").Bind(_promptConfig);

            if (_promptConfig.SystemPrompts == null || _promptConfig.SystemPrompts.Count == 0)
            {
                throw new InvalidOperationException("PromptConfiguration is not properly configured in appsettings.json");
            }
        }

        /// <summary>
        /// Performs an AI-based analysis on the provided code using the specified analysis type and static analysis results.
        /// </summary>
        /// <param name="code">The source code to analyze.</param>
        /// <param name="analysisType">The type of analysis to perform.</param>
        /// <param name="staticAnalysis">The static analysis results to supplement the AI analysis.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the analysis output as a string.
        /// </returns>
        public async Task<string> GetAnalysisAsync(string code, string analysisType, CodeAnalysisResult staticAnalysis)
        {
            try
            {
                var prompt = BuildAnalysisPrompt(code, analysisType, staticAnalysis);
                var systemPrompt = GetSystemPrompt(analysisType);

                // Build chat history for the chat completion API
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage(systemPrompt);
                chatHistory.AddUserMessage(prompt);

                var result = await _chatService.GetChatMessageContentsAsync(chatHistory);
                return result?.FirstOrDefault()?.Content 
                    ?? _promptConfig.ErrorMessages.GetValueOrDefault("noResponse", "No response from AI.");
            }
            catch (RequestFailedException ex) when (ex.Status == 429)
            {
                return _promptConfig.ErrorMessages.GetValueOrDefault("rateLimitExceeded", "AI analysis is currently at capacity.");
            }
            catch (RequestFailedException ex) when (ex.Status == 401)
            {
                return _promptConfig.ErrorMessages.GetValueOrDefault("authenticationFailed", "AI service configuration error.");
            }
            catch
            {
                return _promptConfig.ErrorMessages.GetValueOrDefault("generalFailure", "AI analysis is temporarily unavailable.");
            }
        }

        /// <summary>
        /// Retrieves the system prompt associated with the specified analysis type from configuration.
        /// </summary>
        /// <param name="analysisType">The type of analysis for which to get the system prompt.</param>
        /// <returns>The system prompt as a string.</returns>
        public string GetSystemPrompt(string analysisType)
        {
            if (_promptConfig.SystemPrompts.TryGetValue(analysisType, out var prompt))
            {
                return prompt;
            }

            return _promptConfig.SystemPrompts.GetValueOrDefault(
                "general",
                "You are a senior software architect providing comprehensive code analysis."
            );
        }

        /// <summary>
        /// Builds the analysis prompt using configuration-based templates and code metrics.
        /// </summary>
        /// <param name="code">The source code to analyze.</param>
        /// <param name="analysisType">The type of analysis to perform.</param>
        /// <param name="analysis">The static analysis results.</param>
        /// <returns>The constructed prompt string for the AI model.</returns>
        private string BuildAnalysisPrompt(string code, string analysisType, CodeAnalysisResult analysis)
        {
            // Use configuration-based code preview length
            int maxLength = _promptConfig.CodePreviewMaxLength > 0 ? _promptConfig.CodePreviewMaxLength : 1200;
            var codePreview = code.Length > maxLength ? code.Substring(0, maxLength) + "..." : code;

            // Build base prompt from configuration template
            var basePrompt = _promptConfig.AnalysisPromptTemplates.BaseTemplate
                .Replace("{analysisType}", analysisType)
                .Replace("{classCount}", analysis.ClassCount.ToString())
                .Replace("{methodCount}", analysis.MethodCount.ToString())
                .Replace("{propertyCount}", analysis.PropertyCount.ToString())
                .Replace("{principalClasses}", string.Join(", ", analysis.Classes.Take(3)))
                .Replace("{codePreview}", codePreview);

            // Get analysis sections from configuration
            if (_promptConfig.AnalysisPromptTemplates.Templates.TryGetValue(analysisType, out var template))
            {
                var sections = string.Join("\n", template.Sections);
                return basePrompt + "\n" + sections;
            }
            else if (_promptConfig.AnalysisPromptTemplates.Templates.TryGetValue("general", out var generalTemplate))
            {
                var sections = string.Join("\n", generalTemplate.Sections);
                return basePrompt + "\n" + sections;
            }
            else
            {
                // Fallback if no template found
                return basePrompt;
            }
        }
    }
}
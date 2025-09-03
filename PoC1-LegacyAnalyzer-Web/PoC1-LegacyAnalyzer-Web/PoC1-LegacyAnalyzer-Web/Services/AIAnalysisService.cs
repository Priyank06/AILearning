using Azure;
using Azure.AI.OpenAI;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class AIAnalysisService : IAIAnalysisService
    {
        private readonly OpenAIClient _client;
        private readonly string _deploymentName;

        public AIAnalysisService(IConfiguration configuration)
        {
            var endpoint = configuration["AzureOpenAI:Endpoint"] ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            var apiKey = configuration["AzureOpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
            var deployment = configuration["AzureOpenAI:Deployment"] ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? "gpt-35-turbo";

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Azure OpenAI configuration missing. Please set AzureOpenAI:Endpoint and AzureOpenAI:ApiKey in appsettings.json");
            }

            _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
            _deploymentName = deployment;
        }

        public async Task<string> GetAnalysisAsync(string code, string analysisType, CodeAnalysisResult staticAnalysis)
        {
            try
            {
                var prompt = BuildAnalysisPrompt(code, analysisType, staticAnalysis);

                var chatCompletionsOptions = new ChatCompletionsOptions()
                {
                    Messages =
                    {
                        new ChatMessage(ChatRole.System, GetSystemPrompt(analysisType)),
                        new ChatMessage(ChatRole.User, prompt)
                    },
                    MaxTokens = 500,
                    Temperature = 0.3f
                };

                var response = await _client.GetChatCompletionsAsync(_deploymentName, chatCompletionsOptions);
                return response.Value.Choices[0].Message.Content;
            }
            catch (Exception ex)
            {
                return $"❌ AI Analysis temporarily unavailable: {ex.Message}\n💡 Please check your Azure OpenAI configuration and ensure the service is accessible.";
            }
        }

        public string GetSystemPrompt(string analysisType)
        {
            return analysisType switch
            {
                "security" => "You are a security expert providing actionable C# security recommendations.",
                "performance" => "You are a performance specialist identifying specific C# optimization opportunities.",
                "migration" => "You are a migration expert assessing C# code modernization requirements.",
                _ => "You are an expert C# architect providing practical modernization guidance."
            };
        }

        private string BuildAnalysisPrompt(string code, string analysisType, CodeAnalysisResult analysis)
        {
            var codePreview = code.Length > 1200 ? code.Substring(0, 1200) + "..." : code;

            var basePrompt = $@"Analyze this C# code for {analysisType}:

METRICS: {analysis.ClassCount} classes, {analysis.MethodCount} methods, {analysis.PropertyCount} properties
KEY CLASSES: {string.Join(", ", analysis.Classes.Take(3))}

CODE:
{codePreview}

Provide actionable insights:";

            return analysisType switch
            {
                "security" => basePrompt + @"
1. Security Score (1-10): [rating]
2. Top Security Risks: [specific vulnerabilities]
3. Immediate Actions: [concrete fixes needed]",

                "performance" => basePrompt + @"
1. Performance Score (1-10): [rating]  
2. Bottlenecks Found: [specific performance issues]
3. Optimization Actions: [concrete improvements]",

                "migration" => basePrompt + @"
1. Migration Readiness (1-10): [score]
2. Compatibility Issues: [specific .NET upgrade blockers]
3. Migration Strategy: [step-by-step approach]",

                _ => basePrompt + @"
1. Code Quality Score (1-10): [rating with explanation]
2. Priority Improvements: [top 3 actionable items]
3. Implementation Effort: [time estimate and approach]
4. Business Impact: [risk level and mitigation]"
            };
        }
    }
}

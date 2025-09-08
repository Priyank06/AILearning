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
                throw new InvalidOperationException("Azure OpenAI configuration missing. Please verify AzureOpenAI:Endpoint and AzureOpenAI:ApiKey settings.");
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
                    DeploymentName = _deploymentName,
                    Messages =
                    {
                        new ChatRequestSystemMessage(GetSystemPrompt(analysisType)),
                        new ChatRequestUserMessage(prompt)
                    },
                    MaxTokens = 500,
                    Temperature = 0.3f
                };

                var response = await _client.GetChatCompletionsAsync(chatCompletionsOptions);
                return response.Value.Choices[0].Message.Content;
            }
            catch (Exception ex)
            {
                return $"Intelligent analysis temporarily unavailable: {ex.Message}\nPlease verify Azure OpenAI service configuration and network connectivity.";
            }
        }

        public string GetSystemPrompt(string analysisType)
        {
            return analysisType switch
            {
                "security" => "You are a senior security architect providing actionable C# security assessment and recommendations.",
                "performance" => "You are a performance engineering specialist identifying specific C# optimization opportunities and bottlenecks.",
                "migration" => "You are a migration architect assessing C# code modernization requirements and providing strategic guidance.",
                _ => "You are a senior software architect providing comprehensive C# code modernization and quality assessment."
            };
        }

        private string BuildAnalysisPrompt(string code, string analysisType, CodeAnalysisResult analysis)
        {
            var codePreview = code.Length > 1200 ? code.Substring(0, 1200) + "..." : code;

            var basePrompt = $@"Conduct {analysisType} assessment for this C# code:

PROJECT METRICS: {analysis.ClassCount} classes, {analysis.MethodCount} methods, {analysis.PropertyCount} properties
PRINCIPAL CLASSES: {string.Join(", ", analysis.Classes.Take(3))}

SOURCE CODE:
{codePreview}

Provide professional assessment with actionable recommendations:";

            return analysisType switch
            {
                "security" => basePrompt + @"
1. Security Assessment Score (1-10): [rating with justification]
2. Critical Security Risks: [specific vulnerabilities requiring immediate attention]
3. Remediation Actions: [concrete steps to address security concerns]
4. Compliance Considerations: [relevant security standards and best practices]",

                "performance" => basePrompt + @"
1. Performance Assessment Score (1-10): [rating with technical justification]
2. Performance Bottlenecks: [specific areas impacting system performance]
3. Optimization Strategies: [concrete improvements with expected impact]
4. Scalability Considerations: [recommendations for handling increased load]",

                "migration" => basePrompt + @"
1. Migration Readiness Score (1-10): [assessment of modernization readiness]
2. Compatibility Analysis: [specific issues blocking .NET framework upgrade]
3. Migration Strategy: [phased approach with timeline recommendations]
4. Risk Mitigation: [strategies to minimize migration-related business disruption]",

                _ => basePrompt + @"
1. Code Quality Assessment (1-10): [overall rating with detailed justification]
2. Priority Modernization Areas: [top 3 actionable improvement opportunities]
3. Implementation Approach: [realistic timeline and resource requirements]
4. Business Impact Assessment: [risk level analysis and recommended mitigation strategies]"
            };
        }
    }
}

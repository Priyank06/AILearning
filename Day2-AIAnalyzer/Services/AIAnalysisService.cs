using Azure.AI.OpenAI;
using Azure;
using AICodeAnalyzer.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AICodeAnalyzer.Services
{
    public class AIAnalysisService : IAIAnalysisService
    {
        private readonly OpenAIClient _client;
        private readonly string _deploymentName;
        
        public AIAnalysisService(string endpoint, string apiKey, string deploymentName)
        {
            _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
            _deploymentName = deploymentName;
        }
        
        public async Task<string> GetAnalysisAsync(string code, string fileName, string analysisType, CodeAnalysisResult analysis)
        {
            try
            {
                var prompt = BuildAnalysisPrompt(code, fileName, analysisType, analysis);
                
                var chatCompletionsOptions = new ChatCompletionsOptions()
                {
                    DeploymentName = _deploymentName,
                    Messages =
                    {
                        new ChatRequestSystemMessage(GetSystemPrompt(analysisType)),
                        new ChatRequestUserMessage(prompt)
                    },
                    MaxTokens = 400,
                    Temperature = 0.3f
                };

                var response = await _client.GetChatCompletionsAsync(chatCompletionsOptions);
                return response.Value.Choices[0].Message.Content;
            }
            catch (Exception ex)
            {
                return $"❌ AI Analysis failed: {ex.Message}\n💡 Check your Azure OpenAI configuration and quota.";
            }
        }
        
        public async Task<string> GetQuickInsightAsync(string code, string fileName)
        {
            try
            {
                var codePreview = code.Length > 1000 ? code.Substring(0, 1000) + "..." : code;
                var prompt = $"Analyze this C# file '{fileName}' and provide ONE key modernization insight:\n\n{codePreview}\n\nRespond with one actionable sentence.";
                
                var chatCompletionsOptions = new ChatCompletionsOptions()
                {
                    DeploymentName = _deploymentName,
                    Messages =
                    {
                        new ChatRequestSystemMessage("You provide concise, actionable code modernization insights."),
                        new ChatRequestUserMessage(prompt)
                    },
                    MaxTokens = 60,
                    Temperature = 0.2f
                };

                var response = await _client.GetChatCompletionsAsync(chatCompletionsOptions);
                return response.Value.Choices[0].Message.Content.Trim();
            }
            catch (Exception ex)
            {
                return $"Insight unavailable: {ex.Message}";
            }
        }
        
        public string GetSystemPrompt(string analysisType)
        {
            return analysisType switch
            {
                "security" => "You are a security expert reviewing C# code for vulnerabilities and security best practices.",
                "performance" => "You are a performance specialist identifying bottlenecks and optimization opportunities.",
                "migration" => "You are a migration expert assessing code readiness for modern .NET versions.",
                _ => "You are an expert C# architect providing modernization recommendations."
            };
        }
        
        public string BuildAnalysisPrompt(string code, string fileName, string analysisType, CodeAnalysisResult analysis)
        {
            var codePreview = code.Length > 1200 ? code.Substring(0, 1200) + "..." : code;
            
            var basePrompt = $@"Analyze this C# file for {analysisType}:

FILE: {fileName}
METRICS: {analysis.ClassCount} classes, {analysis.MethodCount} methods
KEY CLASSES: {string.Join(", ", analysis.Classes.Take(3))}

CODE:
{codePreview}

Provide:";
            
            return analysisType switch
            {
                "security" => basePrompt + @"
1. Security Risk Level (1-10): [score]
2. Top 3 Security Concerns: [specific issues]
3. Immediate Actions: [critical fixes needed]",
                
                "performance" => basePrompt + @"
1. Performance Score (1-10): [rating]
2. Top 3 Bottlenecks: [specific performance issues]
3. Optimization Opportunities: [concrete improvements]",
                
                "migration" => basePrompt + @"
1. Migration Readiness (1-10): [compatibility score]
2. Breaking Changes: [specific issues for modern .NET]
3. Migration Effort: [estimated time/complexity]",
                
                _ => basePrompt + @"
1. Code Quality Score (1-10): [score with reason]
2. Top 3 Modernization Priorities: [actionable improvements]
3. Implementation Effort: [realistic time estimate]
4. Business Risk: [Low/Medium/High with mitigation]"
            };
        }
    }
}
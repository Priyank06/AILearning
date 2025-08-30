using Azure.AI.OpenAI;
using Azure;
using System;
using System.Threading.Tasks;

class AzureConnectionTest
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🔗 Day 2: Azure OpenAI Connection Test");
        Console.WriteLine("=====================================");
        
        try
        {
            // Get credentials from environment variables
            string? endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            string? apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
            string? deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");
            
            // Set default if not provided
            if (string.IsNullOrEmpty(deploymentName))
                deploymentName = "gpt-35-turbo";
            
            Console.WriteLine($" Endpoint: {endpoint?.Substring(0, Math.Min(50, endpoint?.Length ?? 0))}...");
            Console.WriteLine($" API Key: {(string.IsNullOrEmpty(apiKey) ? " Not found" : " Found")}");
            Console.WriteLine($" Deployment: {deploymentName}");
            Console.WriteLine();
            
            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine(" Missing required environment variables:");
                Console.WriteLine("   AZURE_OPENAI_ENDPOINT");
                Console.WriteLine("   AZURE_OPENAI_KEY");
                Console.WriteLine("   AZURE_OPENAI_DEPLOYMENT (optional, defaults to gpt-35-turbo)");
                Console.WriteLine();
                Console.WriteLine(" Set them in your shell profile:");
                Console.WriteLine("   export AZURE_OPENAI_ENDPOINT=\"https://your-resource.openai.azure.com/\"");
                Console.WriteLine("   export AZURE_OPENAI_KEY=\"your-api-key\"");
                Console.WriteLine("   export AZURE_OPENAI_DEPLOYMENT=\"your-deployment-name\"");
                return;
            }
            
            // Create client and test connection
            Console.WriteLine(" Testing connection...");
            var client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
            
            var chatOptions = new ChatCompletionsOptions()
            {
                DeploymentName = deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage("You are a helpful code analysis assistant."),
                    new ChatRequestUserMessage("Say 'Hello! I'm ready to analyze code.' in exactly 10 words.")
                },
                MaxTokens = 50,
                Temperature = 0.1f
            };
            
            var response = await client.GetChatCompletionsAsync(chatOptions);
            
            Console.WriteLine(" Connection successful!");
            Console.WriteLine($" AI Response: {response.Value.Choices[0].Message.Content}");
            Console.WriteLine($" Tokens used: {response.Value.Usage.TotalTokens}");
            Console.WriteLine();
            
            // Test code analysis prompt
            Console.WriteLine(" Testing code analysis prompt...");
            var analysisOptions = new ChatCompletionsOptions()
            {
                DeploymentName = deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage("You are an expert C# code analyst. Provide concise, actionable insights."),
                    new ChatRequestUserMessage(@"Analyze this C# method and provide 2 improvement suggestions:

public Customer FindById(int id)
{
    foreach(var customer in _customers)
    {
        if(customer.Id == id) return customer;
    }
    return null;
}

Format: 1. [suggestion] 2. [suggestion]")
                },
                MaxTokens = 100,
                Temperature = 0.3f
            };
            
            var codeAnalysisResponse = await client.GetChatCompletionsAsync(analysisOptions);
            
            Console.WriteLine(" Code analysis test successful!");
            Console.WriteLine($" Analysis: {codeAnalysisResponse.Value.Choices[0].Message.Content}");
            Console.WriteLine($" Tokens used: {codeAnalysisResponse.Value.Usage.TotalTokens}");
            Console.WriteLine();
            
            Console.WriteLine(" All tests passed! Ready to integrate with your code analyzer.");
            Console.WriteLine(" Next: Replace placeholder AI in your Day 1 analyzer with this real connection.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Connection failed: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("🔧 Troubleshooting:");
            Console.WriteLine("1. Verify your Azure OpenAI resource is deployed");
            Console.WriteLine("2. Check endpoint URL format: https://your-resource.openai.azure.com/");
            Console.WriteLine("3. Verify API key is correct");
            Console.WriteLine("4. Ensure deployment name matches your Azure model deployment");
            Console.WriteLine("5. Check Azure OpenAI service status");
            Console.WriteLine($"6. Full error details: {ex}");
        }
    }
}
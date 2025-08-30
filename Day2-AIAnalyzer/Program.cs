using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using AICodeAnalyzer.Services;
using System;
using System.Threading.Tasks;

namespace AICodeAnalyzer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Check Azure OpenAI configuration first
            if (!CheckAzureOpenAIConfiguration())
            {
                Console.WriteLine("❌ Azure OpenAI configuration missing. Please set environment variables:");
                Console.WriteLine("   AZURE_OPENAI_ENDPOINT");
                Console.WriteLine("   AZURE_OPENAI_KEY");
                Console.WriteLine("   AZURE_OPENAI_DEPLOYMENT (optional, defaults to gpt-35-turbo)");
                return;
            }

            var host = CreateHostBuilder(args).Build();
            var app = host.Services.GetRequiredService<ApplicationService>();
            await app.RunAsync(args);
        }
        
        static bool CheckAzureOpenAIConfiguration()
        {
            string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            string apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
            
            return !string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey);
        }
        
        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Register your services
                    services.AddSingleton<ICodeAnalysisService, CodeAnalysisService>();
                    services.AddSingleton<IAIAnalysisService>(provider =>
                    {
                        string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
                        string apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
                        string deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? "gpt-35-turbo";
                        
                        return new AIAnalysisService(endpoint, apiKey, deployment);
                    });
                    services.AddSingleton<IReportService, ReportService>();
                    services.AddTransient<ApplicationService>();
                });
    }
}
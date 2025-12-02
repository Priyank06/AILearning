using Microsoft.SemanticKernel;
using PoC1_LegacyAnalyzer_Web.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCodeAnalysisServices(this IServiceCollection services)
        {
            // Core analysis services (scoped for Blazor circuits)
            services.AddScoped<ICodeAnalysisService, CodeAnalysisService>();
            services.AddScoped<IAIAnalysisService, AIAnalysisService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IMultiFileAnalysisService, MultiFileAnalysisService>();
            services.AddScoped<IFileDownloadService, FileDownloadService>();
            services.AddScoped<ICodeAnalysisAgentService, CodeAnalysisAgentService>();
            services.AddScoped<IFilePreProcessingService, FilePreProcessingService>();
            
            // Helper services
            services.AddScoped<TokenEstimationService>();

            return services;
        }

        public static IServiceCollection AddMultiAgentOrchestration(this IServiceCollection services, IConfiguration configuration)
        {
            // Agent orchestration (scoped - each circuit gets fresh state)
            services.AddScoped<IAgentOrchestrationService>(sp =>
                new AgentOrchestrationService(
                    sp,
                    sp.GetRequiredService<Kernel>(),
                    sp.GetRequiredService<ILogger<AgentOrchestrationService>>(),
                    configuration,
                    sp.GetRequiredService<IFilePreProcessingService>()));

            services.AddScoped<IEnhancedProjectAnalysisService, EnhancedProjectAnalysisService>();

            // Specialist agents (scoped - isolated per analysis session)
            services.AddScoped<SecurityAnalystAgent>(sp =>
                new SecurityAnalystAgent(
                    sp.GetRequiredService<Kernel>(),
                    sp.GetRequiredService<ILogger<SecurityAnalystAgent>>(),
                    configuration));

            services.AddScoped<PerformanceAnalystAgent>(sp =>
                new PerformanceAnalystAgent(
                    sp.GetRequiredService<Kernel>(),
                    sp.GetRequiredService<ILogger<PerformanceAnalystAgent>>(),
                    configuration));

            services.AddScoped<ArchitecturalAnalystAgent>(sp =>
                new ArchitecturalAnalystAgent(
                    sp.GetRequiredService<Kernel>(),
                    sp.GetRequiredService<ILogger<ArchitecturalAnalystAgent>>(),
                    configuration));

            // Register as collection for orchestrator
            services.AddScoped<IEnumerable<ISpecialistAgentService>>(sp =>
            [
                sp.GetRequiredService<SecurityAnalystAgent>(),
            sp.GetRequiredService<PerformanceAnalystAgent>(),
            sp.GetRequiredService<ArchitecturalAnalystAgent>()
            ]);

            return services;
        }

        /// <summary>
        /// Configures Semantic Kernel and Azure OpenAI HttpClient with extended timeout (300 seconds) to prevent timeouts on long LLM calls.
        /// This ensures executive summaries and large completions do not fail due to default timeout.
        /// </summary>
        public static IServiceCollection AddSemanticKernel(this IServiceCollection services, IConfiguration configuration)
        {
            // Create HttpClient with 5 minute timeout for all LLM calls
            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(300) // 5 minutes
            };

            // Register HttpClient for Azure OpenAI
            services.AddSingleton<HttpClient>(httpClient);

            services.AddScoped<Kernel>(sp =>
            {
                var endpoint = configuration["AzureOpenAI:Endpoint"]
                    ?? throw new InvalidOperationException("Azure OpenAI endpoint not configured");
                var apiKey = configuration["AzureOpenAI:ApiKey"]
                    ?? throw new InvalidOperationException("Azure OpenAI API key not configured");
                var deployment = configuration["AzureOpenAI:Deployment"]
                    ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT")
                    ?? throw new InvalidOperationException("Azure OpenAI deployment not configured");

                var builder = Kernel.CreateBuilder();
                builder.AddAzureOpenAIChatCompletion(deployment, endpoint, apiKey);
                builder.Services.AddLogging();

                return builder.Build();
            });

            // Optionally, add a delegating handler to log slow requests
            httpClient.DefaultRequestHeaders.Add("X-SK-Timeout", "300s");

            // Register a handler to log if any LLM call exceeds 200 seconds
            services.AddSingleton<DelegatingHandler>(new LoggingTimeoutHandler(200));

            // Use the handler in your Kernel setup if supported
            // (If using KernelBuilder, pass the HttpClient and handler)

            return services;
        }

        public static IServiceCollection AddKeyVaultService(this IServiceCollection services)
        {
            services.AddSingleton<IKeyVaultService>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var clientOptions = provider.GetRequiredService<IOptions<KeyVaultClientOptions>>();
                var keyVaultEnabled = config.GetValue<bool>("KeyVault:Enabled");

                if (!keyVaultEnabled)
                {
                    var nullLogger = provider.GetRequiredService<ILogger<NullKeyVaultService>>();
                    return new NullKeyVaultService(nullLogger);
                }

                var logger = provider.GetRequiredService<ILogger<KeyVaultService>>();
                var vaultUri = config["KeyVault:VaultUri"];
                return new KeyVaultService(vaultUri, logger, clientOptions);
            });
            return services;
        }
    }
}

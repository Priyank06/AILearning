using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using PoC1_LegacyAnalyzer_Web.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PoC1_LegacyAnalyzer_Web.Models;
using System.Net.Http;

namespace PoC1_LegacyAnalyzer_Web
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCodeAnalysisServices(this IServiceCollection services)
        {
            // Core analysis services (scoped for Blazor circuits)
            services.AddScoped<ICodeAnalysisService, CodeAnalysisService>();
            services.AddScoped<IPromptBuilderService, PromptBuilderService>();
            services.AddScoped<IAIAnalysisService, AIAnalysisService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IMultiFileAnalysisService, MultiFileAnalysisService>();
            services.AddScoped<IFileDownloadService, FileDownloadService>();
            services.AddScoped<ICodeAnalysisAgentService, CodeAnalysisAgentService>();
            
            // File preprocessing focused services
            services.AddScoped<IFileCacheManager, FileCacheManager>();
            services.AddScoped<IComplexityCalculationService, ComplexityCalculationService>();
            services.AddScoped<IPatternDetectionService, PatternDetectionService>();
            services.AddScoped<IFileFilteringService, FileFilteringService>();
            services.AddScoped<IMetadataExtractionService, MetadataExtractionService>();
            
            // FilePreProcessingService as facade (maintains backward compatibility)
            services.AddScoped<IFilePreProcessingService, FilePreProcessingService>();
            
            // Multi-file analysis services
            services.AddScoped<IBatchAnalysisOrchestrator, BatchAnalysisOrchestrator>();
            services.AddScoped<IComplexityCalculatorService, ComplexityCalculatorService>();
            services.AddScoped<IRiskAssessmentService, RiskAssessmentService>();
            services.AddScoped<IRecommendationGeneratorService, RecommendationGeneratorService>();
            services.AddScoped<IBusinessMetricsCalculator, BusinessMetricsCalculator>();
            
            // Helper services
            services.AddScoped<ITokenEstimationService, TokenEstimationService>();

            // Analysis abstraction: Roslyn + Tree-sitter
            services.AddSingleton<Services.Analysis.ITreeSitterLanguageRegistry, Services.Analysis.TreeSitterLanguageRegistry>();
            services.AddSingleton<Services.Analysis.ILanguageDetector, Services.Analysis.LanguageDetector>();
            
            // Register analyzers for each language
            services.AddScoped<Services.Analysis.ICodeAnalyzer, Services.Analysis.RoslynCSharpAnalyzer>();
            services.AddScoped<Services.Analysis.ICodeAnalyzer, Services.Analysis.TreeSitterPythonAnalyzer>();
            services.AddScoped<Services.Analysis.ICodeAnalyzer, Services.Analysis.TreeSitterJavaScriptAnalyzer>();
            services.AddScoped<Services.Analysis.ICodeAnalyzer, Services.Analysis.TreeSitterTypeScriptAnalyzer>();
            services.AddScoped<Services.Analysis.ICodeAnalyzer, Services.Analysis.TreeSitterJavaAnalyzer>();
            services.AddScoped<Services.Analysis.ICodeAnalyzer, Services.Analysis.TreeSitterGoAnalyzer>();
            
            services.AddScoped<Services.Analysis.IAnalyzerRouter, Services.Analysis.AnalyzerRouter>();

            return services;
        }

        public static IServiceCollection AddMultiAgentOrchestration(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure options
            services.Configure<AgentConfiguration>(configuration.GetSection("AgentConfiguration"));
            services.Configure<BusinessCalculationRules>(configuration.GetSection("BusinessCalculationRules"));

            // Register result transformer service
            services.AddScoped<IResultTransformerService, ResultTransformerService>();

            // Register new services for DI
            services.AddScoped<IPeerReviewCoordinator, PeerReviewCoordinator>();
            services.AddScoped<IRecommendationSynthesizer, RecommendationSynthesizer>();
            services.AddScoped<IExecutiveSummaryGenerator, ExecutiveSummaryGenerator>();

            // Register new focused services
            services.AddScoped<IAgentRegistry, AgentRegistryService>();
            services.AddScoped<IConflictResolver, ConflictResolverService>();
            services.AddScoped<IConsensusCalculator, ConsensusCalculatorService>();
            services.AddScoped<IAgentCommunicationCoordinator, AgentCommunicationCoordinator>();

            // Project analysis services
            services.AddScoped<IProjectMetadataService, ProjectMetadataService>();
            services.AddScoped<IFolderAnalysisService, FolderAnalysisService>();
            services.AddScoped<IArchitectureAssessmentService, ArchitectureAssessmentService>();
            services.AddScoped<IBusinessImpactCalculator, BusinessImpactCalculator>();
            services.AddScoped<IProjectInsightsGenerator, ProjectInsightsGenerator>();

            // Agent orchestration (scoped - each circuit gets fresh state)
            services.AddScoped<IAgentOrchestrationService, AgentOrchestrationService>();

            services.AddScoped<IEnhancedProjectAnalysisService, EnhancedProjectAnalysisService>();

            // Specialist agents (scoped - isolated per analysis session)
            services.AddScoped<SecurityAnalystAgent>(sp =>
                new SecurityAnalystAgent(
                    sp.GetRequiredService<Kernel>(),
                    sp.GetRequiredService<ILogger<SecurityAnalystAgent>>(),
                    configuration,
                    sp.GetRequiredService<IResultTransformerService>()));

            services.AddScoped<PerformanceAnalystAgent>(sp =>
                new PerformanceAnalystAgent(
                    sp.GetRequiredService<Kernel>(),
                    sp.GetRequiredService<ILogger<PerformanceAnalystAgent>>(),
                    configuration,
                    sp.GetRequiredService<IResultTransformerService>()));

            services.AddScoped<ArchitecturalAnalystAgent>(sp =>
                new ArchitecturalAnalystAgent(
                    sp.GetRequiredService<Kernel>(),
                    sp.GetRequiredService<ILogger<ArchitecturalAnalystAgent>>(),
                    configuration,
                    sp.GetRequiredService<IResultTransformerService>()));

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
            // Configuration knobs
            int maxConnectionsPerServer = configuration.GetValue<int?>("AgentConfiguration:MaxConnectionsPerServer") ?? 8;
            TimeSpan httpTimeout = TimeSpan.FromSeconds(configuration.GetValue<int?>("AgentConfiguration:HttpTimeoutSeconds") ?? 300);

            // Register named HttpClients for each agent
            services.AddHttpClient("SecurityAgent", c => c.Timeout = httpTimeout)
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    MaxConnectionsPerServer = maxConnectionsPerServer
                });
            services.AddHttpClient("PerformanceAgent", c => c.Timeout = httpTimeout)
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    MaxConnectionsPerServer = maxConnectionsPerServer
                });
            services.AddHttpClient("ArchitectureAgent", c => c.Timeout = httpTimeout)
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    MaxConnectionsPerServer = maxConnectionsPerServer
                });

            // Register IChatCompletionService for AIAnalysisService (uses default client)
            services.AddScoped<IChatCompletionService>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var keyVaultService = sp.GetRequiredService<IKeyVaultService>();

                var endpoint = keyVaultService.GetSecretAsync("App--AzureOpenAI--Endpoint").GetAwaiter().GetResult()
                    ?? config["AzureOpenAI:Endpoint"]
                    ?? throw new InvalidOperationException("Azure OpenAI endpoint is not configured.");
                var apiKey = keyVaultService.GetSecretAsync("App--AzureOpenAI--ApiKey").GetAwaiter().GetResult()
                    ?? config["AzureOpenAI:ApiKey"]
                    ?? throw new InvalidOperationException("Azure OpenAI API key is not configured.");
                var deployment = keyVaultService.GetSecretAsync("App--AzureOpenAI--Deployment").GetAwaiter().GetResult()
                    ?? config["AzureOpenAI:Deployment"]
                    ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT")
                    ?? throw new InvalidOperationException("Azure OpenAI deployment is not configured.");

                // Use default HttpClientFactory (per-request)
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient();

                return new AzureOpenAIChatCompletionService(
                    deploymentName: deployment,
                    endpoint: endpoint,
                    apiKey: apiKey,
                    httpClient: httpClient
                );
            });

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

            // Remove singleton HttpClient registration

            // Optionally, add a delegating handler to log slow requests
            services.AddTransient<LoggingTimeoutHandler>();
            services.AddHttpClient("AzureOpenAI")
                .AddHttpMessageHandler<LoggingTimeoutHandler>();

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

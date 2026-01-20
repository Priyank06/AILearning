using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using PoC1_LegacyAnalyzer_Web.Services;
using PoC1_LegacyAnalyzer_Web.Services.AI;
using PoC1_LegacyAnalyzer_Web.Services.Business;
using PoC1_LegacyAnalyzer_Web.Services.Infrastructure;
using PoC1_LegacyAnalyzer_Web.Services.Reporting;
using PoC1_LegacyAnalyzer_Web.Services.Validation;
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
            services.AddScoped<Services.AI.IAIAnalysisService, Services.AI.AIAnalysisService>();
            services.AddScoped<Services.Reporting.IReportService, Services.Reporting.ReportService>();
            services.AddScoped<IMultiFileAnalysisService, MultiFileAnalysisService>();
            services.AddScoped<Services.Infrastructure.IFileDownloadService, Services.Infrastructure.FileDownloadService>();
            services.AddScoped<ICodeAnalysisAgentService, CodeAnalysisAgentService>();
            
            // File preprocessing focused services
            services.AddScoped<IFileCacheManager, FileCacheManager>();
            services.AddScoped<IComplexityCalculationService, ComplexityCalculationService>();
            services.AddScoped<IPatternDetectionService, PatternDetectionService>();
            services.AddScoped<ILegacyPatternDetectionService, LegacyPatternDetectionService>();
            services.AddScoped<IFileFilteringService, FileFilteringService>();
            services.AddScoped<IMetadataExtractionService>(sp => new MetadataExtractionService(
                sp.GetRequiredService<ILogger<MetadataExtractionService>>(),
                sp.GetRequiredService<IFileCacheManager>(),
                sp.GetRequiredService<IPatternDetectionService>(),
                sp.GetRequiredService<IComplexityCalculationService>(),
                sp.GetRequiredService<Services.Analysis.IAnalyzerRouter>(),
                sp.GetRequiredService<Services.Analysis.ILanguageDetector>(),
                sp.GetRequiredService<IOptions<FilePreProcessingOptions>>(),
                sp.GetRequiredService<IOptions<DefaultValuesConfiguration>>(),
                sp.GetService<ILegacyPatternDetectionService>(),
                sp.GetService<IHybridMultiLanguageAnalyzer>())); // Inject hybrid analyzer for semantic analysis
            
            // Legacy context formatter
            services.AddScoped<LegacyContextFormatter>();
            
            // FilePreProcessingService as facade (maintains backward compatibility)
            services.AddScoped<IFilePreProcessingService, FilePreProcessingService>();
            
            // Multi-file analysis services
            services.AddScoped<IBatchAnalysisOrchestrator, BatchAnalysisOrchestrator>();
            services.AddScoped<IComplexityCalculatorService, ComplexityCalculatorService>();
            services.AddScoped<Services.Business.IRiskAssessmentService, Services.Business.RiskAssessmentService>();
            services.AddScoped<Services.Business.IRecommendationGeneratorService, Services.Business.RecommendationGeneratorService>();
            services.AddScoped<Services.Business.IBusinessMetricsCalculator, Services.Business.BusinessMetricsCalculator>();
            
            // Helper services
            services.AddScoped<ITokenEstimationService, TokenEstimationService>();
            
            // Cross-file dependency analysis
            services.AddScoped<ICrossFileAnalyzer>(sp => new CrossFileAnalyzer(
                sp.GetRequiredService<ILogger<CrossFileAnalyzer>>(),
                sp.GetRequiredService<Services.Analysis.ILanguageDetector>(),
                sp.GetService<Services.Analysis.ITreeSitterLanguageRegistry>()));
            services.AddScoped<IDependencyGraphService, DependencyGraphService>();
            
            // Hybrid multi-language semantic analysis
            services.AddScoped<IHybridMultiLanguageAnalyzer>(sp => new HybridMultiLanguageAnalyzer(
                sp.GetRequiredService<Services.Analysis.ITreeSitterLanguageRegistry>(),
                sp.GetRequiredService<IAIAnalysisService>(),
                sp.GetRequiredService<Services.Analysis.ILanguageDetector>(),
                sp.GetRequiredService<ILogger<HybridMultiLanguageAnalyzer>>(),
                sp.GetRequiredService<IConfiguration>()));

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
            
            // Register finding validation service
            services.AddScoped<Services.Validation.IFindingValidationService, Services.Validation.FindingValidationService>();
            
            // Register robust JSON extractor service
            services.AddScoped<IRobustJsonExtractor, RobustJsonExtractor>();
            
            // Register confidence validation service
            services.AddScoped<Services.Validation.IConfidenceValidationService, Services.Validation.ConfidenceValidationService>();
            
            // Register agent rate limiter
            services.AddSingleton<IAgentRateLimiter>(sp => new AgentRateLimiter(
                sp.GetRequiredService<ILogger<AgentRateLimiter>>(),
                maxCallsPerMinute: 20)); // 20 calls per minute per agent

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
            services.AddScoped<Services.Business.IBusinessImpactCalculator, Services.Business.BusinessImpactCalculator>();
            services.AddScoped<IProjectInsightsGenerator, ProjectInsightsGenerator>();

            // Agent orchestration (scoped - each circuit gets fresh state)
            // Wrap in factory to catch configuration binding errors with better error messages
            services.AddScoped<IAgentOrchestrationService>(sp =>
            {
                try
                {
                    return new AgentOrchestrationService(
                        sp.GetRequiredService<Kernel>(),
                        sp.GetRequiredService<ILogger<AgentOrchestrationService>>(),
                        sp.GetRequiredService<IOptions<AgentConfiguration>>(),
                        sp.GetRequiredService<IOptions<DefaultValuesConfiguration>>(),
                        sp.GetRequiredService<IAgentRegistry>(),
                        sp.GetRequiredService<IAgentCommunicationCoordinator>(),
                        sp.GetRequiredService<IConsensusCalculator>(),
                        sp.GetRequiredService<IRecommendationSynthesizer>(),
                        sp.GetRequiredService<IExecutiveSummaryGenerator>(),
                        sp.GetRequiredService<IFilePreProcessingService>(),
                        sp.GetRequiredService<IInputValidationService>(),
                        sp.GetRequiredService<IErrorHandlingService>(),
                        sp.GetService<IRequestDeduplicationService>(),
                        sp.GetService<ICostTrackingService>(),
                        sp.GetService<ITracingService>(),
                        sp.GetService<IAgentRateLimiter>());
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is FormatException || ex.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
                {
                    var logger = sp.GetRequiredService<ILogger<AgentOrchestrationService>>();
                    logger.LogError(ex, "Failed to create AgentOrchestrationService due to configuration binding error: {ErrorMessage}", ex.Message);
                    throw new InvalidOperationException($"Configuration binding failed when creating AgentOrchestrationService. This is likely due to a duplicate key or invalid value in appsettings.json. Original error: {ex.Message}", ex);
                }
            });

            services.AddScoped<IEnhancedProjectAnalysisService, EnhancedProjectAnalysisService>();

            // Specialist agents (scoped - isolated per analysis session)
            services.AddScoped<Services.AI.SecurityAnalystAgent>(sp =>
                new Services.AI.SecurityAnalystAgent(
                    sp.GetRequiredService<Kernel>(),
                    sp.GetRequiredService<ILogger<Services.AI.SecurityAnalystAgent>>(),
                    configuration,
                    sp.GetRequiredService<IResultTransformerService>(),
                    sp.GetRequiredService<IOptions<AgentLegacyIndicatorsConfiguration>>(),
                    sp.GetRequiredService<IOptions<LegacyContextMessagesConfiguration>>()));

            services.AddScoped<Services.AI.PerformanceAnalystAgent>(sp =>
                new Services.AI.PerformanceAnalystAgent(
                    sp.GetRequiredService<Kernel>(),
                    sp.GetRequiredService<ILogger<Services.AI.PerformanceAnalystAgent>>(),
                    configuration,
                    sp.GetRequiredService<IResultTransformerService>(),
                    sp.GetRequiredService<IOptions<AgentLegacyIndicatorsConfiguration>>(),
                    sp.GetRequiredService<IOptions<LegacyContextMessagesConfiguration>>()));

            services.AddScoped<Services.AI.ArchitecturalAnalystAgent>(sp =>
                new Services.AI.ArchitecturalAnalystAgent(
                    sp.GetRequiredService<Kernel>(),
                    sp.GetRequiredService<ILogger<Services.AI.ArchitecturalAnalystAgent>>(),
                    configuration,
                    sp.GetRequiredService<IResultTransformerService>(),
                    sp.GetRequiredService<IOptions<AgentLegacyIndicatorsConfiguration>>(),
                    sp.GetRequiredService<IOptions<LegacyContextMessagesConfiguration>>()));

            // Register as collection for orchestrator
            services.AddScoped<IEnumerable<ISpecialistAgentService>>(sp =>
            [
                sp.GetRequiredService<Services.AI.SecurityAnalystAgent>(),
                sp.GetRequiredService<Services.AI.PerformanceAnalystAgent>(),
                sp.GetRequiredService<Services.AI.ArchitecturalAnalystAgent>()
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

            // Register IChatCompletionService with resilient wrapper (retry policies + circuit breaker)
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

                // Create inner service
                var innerService = new AzureOpenAIChatCompletionService(
                    deploymentName: deployment,
                    endpoint: endpoint,
                    apiKey: apiKey,
                    httpClient: httpClient
                );

                // Wrap with resilient service (retry + circuit breaker)
                var logger = sp.GetRequiredService<ILogger<Services.AI.ResilientChatCompletionService>>();
                var retryConfig = sp.GetRequiredService<IOptions<Models.RetryPolicyConfiguration>>();
                return new Services.AI.ResilientChatCompletionService(innerService, logger, retryConfig);
            });

            services.AddScoped<Kernel>(sp =>
            {
                // Create kernel using KernelBuilder (available in Semantic Kernel 1.x)
                // This allows us to properly configure the kernel with services
                var kernelBuilder = Kernel.CreateBuilder();
                
                // Get the resilient IChatCompletionService from DI (with retry policies and circuit breaker)
                var chatCompletionService = sp.GetRequiredService<IChatCompletionService>();
                
                // Add the chat completion service to the kernel's service collection
                kernelBuilder.Services.AddSingleton(chatCompletionService);
                
                // Also add logger factory so kernel can create loggers
                var loggerFactory = sp.GetService<ILoggerFactory>();
                if (loggerFactory != null)
                {
                    kernelBuilder.Services.AddSingleton(loggerFactory);
                }
                
                // Build the kernel
                return kernelBuilder.Build();
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

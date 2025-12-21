using PoC1_LegacyAnalyzer_Web.Models;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using PoC1_LegacyAnalyzer_Web;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using PoC1_LegacyAnalyzer_Web.Services;
using PoC1_LegacyAnalyzer_Web.Middleware;
using PoC1_LegacyAnalyzer_Web.Extensions;
using PoC1_LegacyAnalyzer_Web.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Application Insights
        // The Connection String or Instrumentation Key can be set via:
        // 1. Environment variable: APPLICATIONINSIGHTS_CONNECTION_STRING (preferred) or APPINSIGHTS_INSTRUMENTATIONKEY
        // 2. Configuration: ApplicationInsights:ConnectionString or ApplicationInsights:InstrumentationKey
        var appInsightsConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING") 
            ?? builder.Configuration["ApplicationInsights:ConnectionString"];
        var appInsightsInstrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY")
            ?? builder.Configuration["ApplicationInsights:InstrumentationKey"];

        // Configure Application Insights with telemetry for requests, dependencies, and exceptions
        if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
        {
            builder.Services.AddApplicationInsightsTelemetry(options =>
            {
                options.ConnectionString = appInsightsConnectionString;
                // Enable automatic collection of requests, dependencies, and exceptions
                options.EnableRequestTrackingTelemetryModule = true;
                options.EnableDependencyTrackingTelemetryModule = true;
                options.EnableEventCounterCollectionModule = true;
                options.EnablePerformanceCounterCollectionModule = true;
                options.EnableAppServicesHeartbeatTelemetryModule = true;
                options.EnableAzureInstanceMetadataTelemetryModule = true;
            });
        }
        else if (!string.IsNullOrWhiteSpace(appInsightsInstrumentationKey))
        {
            builder.Services.AddApplicationInsightsTelemetry(options =>
            {
                options.InstrumentationKey = appInsightsInstrumentationKey;
                // Enable automatic collection of requests, dependencies, and exceptions
                options.EnableRequestTrackingTelemetryModule = true;
                options.EnableDependencyTrackingTelemetryModule = true;
                options.EnableEventCounterCollectionModule = true;
                options.EnablePerformanceCounterCollectionModule = true;
                options.EnableAppServicesHeartbeatTelemetryModule = true;
                options.EnableAzureInstanceMetadataTelemetryModule = true;
            });
        }
        else
        {
            // Application Insights will be disabled if no connection string or instrumentation key is provided
            // But still configure with default settings for when configuration is available
            builder.Services.AddApplicationInsightsTelemetry();
        }

        // Bind configuration sections to options for IOptions pattern
        builder.Services.Configure<PromptConfiguration>(builder.Configuration.GetSection("PromptConfiguration"));
        builder.Services.Configure<AgentConfiguration>(builder.Configuration.GetSection("AgentConfiguration"));
        builder.Services.Configure<BusinessCalculationRules>(builder.Configuration.GetSection("BusinessCalculationRules"));
        builder.Services.Configure<FileAnalysisLimitsConfig>(builder.Configuration.GetSection("FileAnalysisLimits"));
        builder.Services.Configure<BatchProcessingConfig>(builder.Configuration.GetSection("AzureOpenAI:BatchProcessing"));
        builder.Services.Configure<ComplexityThresholdsConfig>(builder.Configuration.GetSection("ComplexityThresholds"));
        builder.Services.Configure<ScaleThresholdsConfig>(builder.Configuration.GetSection("ScaleThresholds"));
        builder.Services.Configure<TokenEstimationConfig>(builder.Configuration.GetSection("TokenEstimation"));
        builder.Services.Configure<KeyVaultConfiguration>(builder.Configuration.GetSection("KeyVault"));
        builder.Services.Configure<KeyVaultClientOptions>(builder.Configuration.GetSection("KeyVault:Client"));
        builder.Services.Configure<FilePreProcessingOptions>(builder.Configuration.GetSection("FilePreProcessing"));
        builder.Services.Configure<RetryPolicyConfiguration>(builder.Configuration.GetSection("RetryPolicy"));
        builder.Services.Configure<RateLimitConfiguration>(builder.Configuration.GetSection("RateLimit"));
        builder.Services.Configure<LogSanitizationConfiguration>(builder.Configuration.GetSection("LogSanitization"));
        builder.Services.Configure<InputValidationConfiguration>(builder.Configuration.GetSection("InputValidation"));
        builder.Services.Configure<ErrorHandlingConfiguration>(builder.Configuration.GetSection("ErrorHandling"));
        builder.Services.Configure<RequestDeduplicationConfiguration>(builder.Configuration.GetSection("RequestDeduplication"));
        builder.Services.Configure<CostTrackingConfiguration>(builder.Configuration.GetSection("CostTracking"));
        builder.Services.Configure<TracingConfiguration>(builder.Configuration.GetSection("Tracing"));

        // Add services to the container.
        // Configure memory cache with size limits for FilePreProcessingService
        var filePreProcessingOptions = builder.Configuration.GetSection("FilePreProcessing").Get<FilePreProcessingOptions>() ?? new FilePreProcessingOptions();
        
        // Validate FilePreProcessing configuration on startup
        var (isValid, errorMessage) = FilePreProcessingOptions.Validate(filePreProcessingOptions);
        if (!isValid)
        {
            var validationLoggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
            var startupLogger = validationLoggerFactory.CreateLogger<Program>();
            startupLogger.LogError("FilePreProcessing configuration validation failed: {ErrorMessage}", errorMessage);
            throw new InvalidOperationException($"Invalid FilePreProcessing configuration: {errorMessage}");
        }
        
        builder.Services.AddMemoryCache(options =>
        {
            if (filePreProcessingOptions.MaxCacheSize > 0)
            {
                options.SizeLimit = filePreProcessingOptions.MaxCacheSize;
            }
        });
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        
        // Add health checks
        builder.Services.AddHealthChecks()
            .AddCheck<AzureOpenAIHealthCheck>("azure-openai", tags: new[] { "external", "ai" })
            .AddCheck("memory", () => 
            {
                var memory = GC.GetTotalMemory(false);
                var maxMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
                var memoryUsagePercent = (double)memory / maxMemory * 100;
                
                if (memoryUsagePercent > 90)
                    return HealthCheckResult.Degraded($"High memory usage: {memoryUsagePercent:F1}%");
                
                return HealthCheckResult.Healthy($"Memory usage: {memoryUsagePercent:F1}%");
            }, tags: new[] { "internal" });
        
        // Configure logging to send exceptions and traces to Application Insights
        // Application Insights logging provider is automatically added by AddApplicationInsightsTelemetry()
        // Log levels are controlled by the "Logging" section in appsettings.json
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddApplicationInsights();

        builder.Services.AddKeyVaultService();
        
        // Register sanitized logging (must be before other services that use logging)
        builder.Services.AddSanitizedLogging();
        
        // Register your AI services
        builder.Services.AddCodeAnalysisServices();
        builder.Services.AddMultiAgentOrchestration(builder.Configuration);
        builder.Services.AddSemanticKernel(builder.Configuration);
        builder.Services.AddScoped<ITeamReportService, TeamReportService>();
        
        // Register rate limiting service
        builder.Services.AddSingleton<IRateLimitService, RateLimitService>();
        
        // Register input validation service
        builder.Services.AddScoped<IInputValidationService, InputValidationService>();
        
        // Register error handling service
        builder.Services.AddScoped<IErrorHandlingService, ErrorHandlingService>();
        
        // Register request deduplication service
        builder.Services.AddMemoryCache();
        builder.Services.AddScoped<IRequestDeduplicationService, RequestDeduplicationService>();
        
        // Register cost tracking service
        builder.Services.AddScoped<ICostTrackingService, CostTrackingService>();
        
        // Register tracing service
        builder.Services.AddSingleton<ITracingService, TracingService>();

        // Create logger using LoggerFactory with Application Insights support
        using var loggerFactory = LoggerFactory.Create(logging =>
        {
            logging.AddConsole();
            logging.AddApplicationInsights();
        });
        var logger = loggerFactory.CreateLogger<Program>();
        
        // Log FilePreProcessing configuration on startup
        logger.LogInformation(
            "FilePreProcessing configuration loaded: MaxConcurrentFiles={MaxConcurrentFiles}, CacheTTL={CacheTTL}min, " +
            "MaxCacheSize={MaxCacheSize}, EnableCaching={EnableCaching}, EnablePatternDetection={EnablePatternDetection}, " +
            "MaxFileSizeMB={MaxFileSizeMB}, LogDetailedMetrics={LogDetailedMetrics}",
            filePreProcessingOptions.MaxConcurrentFiles,
            filePreProcessingOptions.CacheTTLMinutes,
            filePreProcessingOptions.MaxCacheSize,
            filePreProcessingOptions.EnableCaching,
            filePreProcessingOptions.EnablePatternDetection,
            filePreProcessingOptions.MaxFileSizeMB,
            filePreProcessingOptions.Performance?.LogDetailedMetrics ?? false);

        try
        {
            // Configure Azure Key Vault if enabled
            ConfigureKeyVault(builder, logger);

            // Validate required secrets after configuration is loaded
            ValidateRequiredSecrets(builder.Configuration, logger);
        }
        catch (Exception ex)
        {
            // Log startup configuration errors to Application Insights
            logger.LogCritical(ex, "Application startup failed due to configuration error. Error: {ErrorMessage}", ex.Message);
            throw;
        }

        var promptConfig = builder.Configuration.GetSection("PromptConfiguration").Get<PromptConfiguration>();

        if (promptConfig == null)
        {
            logger.LogError("PromptConfiguration section is missing in appsettings.json.");
            throw new InvalidOperationException("PromptConfiguration section is missing in appsettings.json.");
        }
        if (promptConfig.SystemPrompts == null || promptConfig.SystemPrompts.Count == 0)
        {
            logger.LogError("PromptConfiguration.SystemPrompts is missing or empty in appsettings.json.");
            throw new InvalidOperationException("PromptConfiguration.SystemPrompts is missing or empty in appsettings.json.");
        }
        if (promptConfig.AnalysisPromptTemplates == null || promptConfig.AnalysisPromptTemplates.Templates == null)
        {
            logger.LogError("PromptConfiguration.AnalysisPromptTemplates.Templates is missing in appsettings.json.");
            throw new InvalidOperationException("PromptConfiguration.AnalysisPromptTemplates.Templates is missing in appsettings.json.");
        }

        // AgentConfiguration validation
        var agentConfig = builder.Configuration.GetSection("AgentConfiguration").Get<AgentConfiguration>();
        if (agentConfig == null)
        {
            logger.LogError("AgentConfiguration section is missing in appsettings.json.");
            throw new InvalidOperationException("AgentConfiguration section is missing in appsettings.json.");
        }
        if (agentConfig.AgentProfiles == null ||
            !agentConfig.AgentProfiles.ContainsKey("security") ||
            !agentConfig.AgentProfiles.ContainsKey("performance") ||
            !agentConfig.AgentProfiles.ContainsKey("architecture"))
        {
            logger.LogError("AgentConfiguration.AgentProfiles must contain 'security', 'performance', and 'architecture' profiles.");
            throw new InvalidOperationException("AgentConfiguration.AgentProfiles must contain 'security', 'performance', and 'architecture' profiles.");
        }
        if (agentConfig.AgentPromptTemplates == null ||
            !agentConfig.AgentPromptTemplates.ContainsKey("security") ||
            !agentConfig.AgentPromptTemplates.ContainsKey("performance") ||
            !agentConfig.AgentPromptTemplates.ContainsKey("architecture"))
        {
            logger.LogError("AgentConfiguration.AgentPromptTemplates must contain 'security', 'performance', and 'architecture' templates.");
            throw new InvalidOperationException("AgentConfiguration.AgentPromptTemplates must contain 'security', 'performance', and 'architecture' templates.");
        }
        if (agentConfig.OrchestrationPrompts == null)
        {
            logger.LogError("AgentConfiguration.OrchestrationPrompts is missing in appsettings.json.");
            throw new InvalidOperationException("AgentConfiguration.OrchestrationPrompts is missing in appsettings.json.");
        }

        // BusinessCalculationRules validation
        var businessRules = builder.Configuration.GetSection("BusinessCalculationRules").Get<BusinessCalculationRules>();
        if (businessRules == null)
        {
            logger.LogError("BusinessCalculationRules section is missing in appsettings.json.");
            throw new InvalidOperationException("BusinessCalculationRules section is missing in appsettings.json.");
        }
        if (businessRules.CostCalculation == null)
        {
            logger.LogError("BusinessCalculationRules.CostCalculation is missing.");
            throw new InvalidOperationException("BusinessCalculationRules.CostCalculation is missing.");
        }
        if (businessRules.CostCalculation.BaseValuePerLine <= 0)
        {
            logger.LogError("BusinessCalculationRules.CostCalculation.BaseValuePerLine must be greater than 0.");
            throw new InvalidOperationException("BusinessCalculationRules.CostCalculation.BaseValuePerLine must be greater than 0.");
        }
        if (businessRules.CostCalculation.MaxEstimatedValue <= 0)
        {
            logger.LogError("BusinessCalculationRules.CostCalculation.MaxEstimatedValue must be greater than 0.");
            throw new InvalidOperationException("BusinessCalculationRules.CostCalculation.MaxEstimatedValue must be greater than 0.");
        }
        if (businessRules.CostCalculation.DefaultDeveloperHourlyRate <= 0)
        {
            logger.LogError("BusinessCalculationRules.CostCalculation.DefaultDeveloperHourlyRate must be greater than 0.");
            throw new InvalidOperationException("BusinessCalculationRules.CostCalculation.DefaultDeveloperHourlyRate must be greater than 0.");
        }
        if (businessRules.ComplexityThresholds == null)
        {
            logger.LogError("BusinessCalculationRules.ComplexityThresholds is missing.");
            throw new InvalidOperationException("BusinessCalculationRules.ComplexityThresholds is missing.");
        }
        if (!(businessRules.ComplexityThresholds.VeryLow < businessRules.ComplexityThresholds.Low &&
              businessRules.ComplexityThresholds.Low < businessRules.ComplexityThresholds.Medium &&
              businessRules.ComplexityThresholds.Medium < businessRules.ComplexityThresholds.High &&
              businessRules.ComplexityThresholds.High < businessRules.ComplexityThresholds.VeryHigh))
        {
            logger.LogError("BusinessCalculationRules.ComplexityThresholds must be ordered: VeryLow < Low < Medium < High < VeryHigh.");
            throw new InvalidOperationException("BusinessCalculationRules.ComplexityThresholds must be ordered: VeryLow < Low < Medium < High < VeryHigh.");
        }
        if (businessRules.RiskThresholds == null)
        {
            logger.LogError("BusinessCalculationRules.RiskThresholds is missing.");
            throw new InvalidOperationException("BusinessCalculationRules.RiskThresholds is missing.");
        }
        if (!(businessRules.RiskThresholds.LowRiskMax < businessRules.RiskThresholds.MediumRiskMax &&
              businessRules.RiskThresholds.MediumRiskMax <= businessRules.RiskThresholds.HighRiskMin))
        {
            logger.LogError("BusinessCalculationRules.RiskThresholds must be logical: LowRiskMax < MediumRiskMax <= HighRiskMin.");
            throw new InvalidOperationException("BusinessCalculationRules.RiskThresholds must be logical: LowRiskMax < MediumRiskMax <= HighRiskMin.");
        }
        if (businessRules.ProcessingLimits == null)
        {
            logger.LogError("BusinessCalculationRules.ProcessingLimits is missing.");
            throw new InvalidOperationException("BusinessCalculationRules.ProcessingLimits is missing.");
        }
        if (businessRules.ProcessingLimits.MetadataSampleFileCount <= 0 ||
            businessRules.ProcessingLimits.CodeContextSummaryMaxLength <= 0 ||
            businessRules.ProcessingLimits.TokenEstimationCharsPerToken <= 0)
        {
            logger.LogError("BusinessCalculationRules.ProcessingLimits values must be positive.");
            throw new InvalidOperationException("BusinessCalculationRules.ProcessingLimits values must be positive.");
        }

        var app = builder.Build();
        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        
        // Add correlation ID middleware (first, to track all requests)
        app.UseMiddleware<CorrelationIdMiddleware>();
        
        // Add rate limiting middleware (before routing to catch all requests)
        app.UseMiddleware<RateLimitMiddleware>();
        
        app.UseRouting();

        // Map health check endpoints
        app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        data = e.Value.Data,
                        duration = e.Value.Duration.TotalMilliseconds
                    }),
                    totalDuration = report.TotalDuration.TotalMilliseconds
                });
                await context.Response.WriteAsync(result);
            }
        });
        
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("external")
        });
        
        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false // Liveness check - just returns OK if app is running
        });

        app.MapRazorPages();
        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        app.Run();
    }

    /// <summary>
    /// Configures Azure Key Vault as a configuration source if enabled, with structured logging.
    /// </summary>
    private static void ConfigureKeyVault(WebApplicationBuilder builder, ILogger logger)
    {
        var keyVaultConfig = builder.Configuration.GetSection("KeyVault").Get<KeyVaultConfiguration>();
        
        if (keyVaultConfig?.Enabled == true && !string.IsNullOrWhiteSpace(keyVaultConfig.VaultUri))
        {
            try
            {
                var vaultUri = new Uri(keyVaultConfig.VaultUri);
                
                // Configure credential based on environment
                DefaultAzureCredential credential;
                
                if (!string.IsNullOrWhiteSpace(keyVaultConfig.ManagedIdentityClientId))
                {
                    // Use user-assigned managed identity
                    credential = new DefaultAzureCredential(
                        new DefaultAzureCredentialOptions
                        {
                            ManagedIdentityClientId = keyVaultConfig.ManagedIdentityClientId
                        });
                }
                else if (!string.IsNullOrWhiteSpace(keyVaultConfig.TenantId))
                {
                    // Use specific tenant ID
                    credential = new DefaultAzureCredential(
                        new DefaultAzureCredentialOptions
                        {
                            TenantId = keyVaultConfig.TenantId
                        });
                }
                else
                {
                    // Use default credential chain (Managed Identity, Azure CLI, Visual Studio, etc.)
                    credential = new DefaultAzureCredential();
                }

                // Add Key Vault as configuration source (default retry policy)
                builder.Configuration.AddAzureKeyVault(
                    vaultUri,
                    credential,
                    new AzureKeyVaultConfigurationOptions
                    {
                        ReloadInterval = keyVaultConfig.ReloadIntervalSeconds > 0
                            ? TimeSpan.FromSeconds(keyVaultConfig.ReloadIntervalSeconds)
                            : null,
                        Manager = new PrefixKeyVaultSecretManager(keyVaultConfig.SecretPrefix ?? "App--")
                    });

                logger.LogInformation("Azure Key Vault configured: {VaultUri}", vaultUri);
            }
            catch (Exception ex)
            {
                // Log Key Vault integration failure to Application Insights with detailed information
                // This will be automatically captured by Application Insights as an exception telemetry
                logger.LogCritical(ex, 
                    "Failed to configure Azure Key Vault integration. VaultUri: {VaultUri}, ManagedIdentityClientId: {ManagedIdentityClientId}, TenantId: {TenantId}, " +
                    "ExceptionType: {ExceptionType}, StackTrace: {StackTrace}. " +
                    "Application will continue without Key Vault. Ensure secrets are available via other configuration sources.",
                    keyVaultConfig.VaultUri,
                    keyVaultConfig.ManagedIdentityClientId ?? "Not specified",
                    keyVaultConfig.TenantId ?? "Not specified",
                    ex.GetType().Name,
                    ex.StackTrace ?? "N/A");
                
                // Re-throw to ensure startup fails if Key Vault is critical
                // Comment out the throw if Key Vault is optional for your scenario
                // throw;
            }
        }
        else
        {
            logger.LogInformation("Azure Key Vault is not enabled or not configured. Using standard configuration sources.");
        }
    }

    /// <summary>
    /// Validates that required secrets are present in configuration or Key Vault.
    /// </summary>
    private static void ValidateRequiredSecrets(IConfiguration configuration, ILogger logger)
    {
        var requiredSecrets = new[]
        {
            "AzureOpenAI:ApiKey",
            "AzureOpenAI:Endpoint",
            "AzureOpenAI:Deployment"
        };
        var keyVaultPrefix = configuration["KeyVault:SecretPrefix"] ?? "App--";
        foreach (var secretKey in requiredSecrets)
        {
            var value = configuration[secretKey];
            var keyVaultKey = keyVaultPrefix + secretKey.Replace(":", "--");
            var keyVaultValue = configuration[keyVaultKey];
            if (string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(keyVaultValue))
            {
                logger.LogError("Required secret '{SecretKey}' is missing or empty in configuration and Key Vault.", secretKey);
                throw new InvalidOperationException($"Required secret '{secretKey}' is missing or empty in configuration and Key Vault.");
            }
            if (string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(keyVaultValue))
            {
                logger.LogWarning("Secret '{SecretKey}' is not set in config but is available in Key Vault as '{KeyVaultKey}'.", secretKey, keyVaultKey);
            }
        }
    }
}
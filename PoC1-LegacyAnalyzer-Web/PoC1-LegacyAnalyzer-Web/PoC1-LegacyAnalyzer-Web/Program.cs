using Microsoft.SemanticKernel;
using PoC1_LegacyAnalyzer_Web.Services;
using PoC1_LegacyAnalyzer_Web.Models;

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
                configuration));

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

    public static IServiceCollection AddSemanticKernel(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<Kernel>(sp =>
        {
            var endpoint = configuration["AzureOpenAI:Endpoint"]
                ?? throw new InvalidOperationException("Azure OpenAI endpoint not configured");
            var apiKey = configuration["AzureOpenAI:ApiKey"]
                ?? throw new InvalidOperationException("Azure OpenAI API key not configured");
            var deployment = configuration["AzureOpenAI:Deployment"] ?? "gpt-4";

            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(deployment, endpoint, apiKey);
            builder.Services.AddLogging();

            return builder.Build();
        });

        return services;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        // Register your AI services
        builder.Services.AddCodeAnalysisServices();
        builder.Services.AddMultiAgentOrchestration(builder.Configuration);
        builder.Services.AddSemanticKernel(builder.Configuration);

        var promptConfig = builder.Configuration.GetSection("PromptConfiguration").Get<PromptConfiguration>();

        if (promptConfig == null)
        {
            throw new InvalidOperationException("PromptConfiguration section is missing in appsettings.json.");
        }
        if (promptConfig.SystemPrompts == null || promptConfig.SystemPrompts.Count == 0)
        {
            throw new InvalidOperationException("PromptConfiguration.SystemPrompts is missing or empty in appsettings.json.");
        }
        if (promptConfig.AnalysisPromptTemplates == null || promptConfig.AnalysisPromptTemplates.Templates == null)
        {
            throw new InvalidOperationException("PromptConfiguration.AnalysisPromptTemplates.Templates is missing in appsettings.json.");
        }

        // AgentConfiguration validation
        var agentConfig = builder.Configuration.GetSection("AgentConfiguration").Get<AgentConfiguration>();
        if (agentConfig == null)
        {
            throw new InvalidOperationException("AgentConfiguration section is missing in appsettings.json.");
        }
        if (agentConfig.AgentProfiles == null ||
            !agentConfig.AgentProfiles.ContainsKey("security") ||
            !agentConfig.AgentProfiles.ContainsKey("performance") ||
            !agentConfig.AgentProfiles.ContainsKey("architecture"))
        {
            throw new InvalidOperationException("AgentConfiguration.AgentProfiles must contain 'security', 'performance', and 'architecture' profiles.");
        }
        if (agentConfig.AgentPromptTemplates == null ||
            !agentConfig.AgentPromptTemplates.ContainsKey("security") ||
            !agentConfig.AgentPromptTemplates.ContainsKey("performance") ||
            !agentConfig.AgentPromptTemplates.ContainsKey("architecture"))
        {
            throw new InvalidOperationException("AgentConfiguration.AgentPromptTemplates must contain 'security', 'performance', and 'architecture' templates.");
        }
        if (agentConfig.OrchestrationPrompts == null)
        {
            throw new InvalidOperationException("AgentConfiguration.OrchestrationPrompts is missing in appsettings.json.");
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
        app.UseRouting();

        app.MapRazorPages();
        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        app.Run();
    }
}
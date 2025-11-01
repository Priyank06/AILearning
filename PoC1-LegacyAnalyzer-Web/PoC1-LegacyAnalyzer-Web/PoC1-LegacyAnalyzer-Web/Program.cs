using Microsoft.SemanticKernel;
using PoC1_LegacyAnalyzer_Web.Services;

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

    public static IServiceCollection AddMultiAgentOrchestration(this IServiceCollection services)
    {
        // Agent orchestration (scoped - each circuit gets fresh state)
        services.AddScoped<IAgentOrchestrationService, AgentOrchestrationService>();
        services.AddScoped<IEnhancedProjectAnalysisService, EnhancedProjectAnalysisService>();

        // Specialist agents (scoped - isolated per analysis session)
        services.AddScoped<SecurityAnalystAgent>();
        services.AddScoped<PerformanceAnalystAgent>();
        services.AddScoped<ArchitecturalAnalystAgent>();

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
        builder.Services.AddMultiAgentOrchestration();
        builder.Services.AddSemanticKernel(builder.Configuration);

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
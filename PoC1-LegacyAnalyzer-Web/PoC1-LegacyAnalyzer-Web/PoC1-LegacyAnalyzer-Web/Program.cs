using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.SemanticKernel;
using PoC1_LegacyAnalyzer_Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
// Register your AI services
builder.Services.AddSingleton<ICodeAnalysisService, CodeAnalysisService>();
builder.Services.AddSingleton<IAIAnalysisService, AIAnalysisService>();
builder.Services.AddSingleton<IReportService, ReportService>();

builder.Services.AddScoped<IFileDownloadService, FileDownloadService>();
builder.Services.AddSingleton<IMultiFileAnalysisService, MultiFileAnalysisService>();

// Register new multi-agent services
builder.Services.AddSingleton<ISpecialistAgentService, SecurityAnalystAgent>();
builder.Services.AddSingleton<ISpecialistAgentService, PerformanceAnalystAgent>();
builder.Services.AddSingleton<ISpecialistAgentService, ArchitecturalAnalystAgent>();
builder.Services.AddSingleton<IAgentOrchestrationService, AgentOrchestrationService>();

// Register individual agents for dependency injection
builder.Services.AddSingleton<SecurityAnalystAgent>();
builder.Services.AddSingleton<PerformanceAnalystAgent>();
builder.Services.AddSingleton<ArchitecturalAnalystAgent>();

builder.Services.AddSingleton<IEnhancedProjectAnalysisService, EnhancedProjectAnalysisService>();

// Add new AI services to your existing Program.cs
builder.Services.AddSingleton<IFormRecognizerService, FormRecognizerService>();
builder.Services.AddSingleton<ICognitiveSearchService, CognitiveSearchService>();
builder.Services.AddSingleton<IComputerVisionService, ComputerVisionService>();
builder.Services.AddSingleton<ICustomVisionService, CustomVisionService>();
builder.Services.AddSingleton<ISpeechService, SpeechService>();
builder.Services.AddSingleton<ILanguageUnderstandingService, LanguageUnderstandingService>();
builder.Services.AddSingleton<IContentModerationService, ContentModerationService>();

// Register DocumentAnalystAgent with proper dependencies
builder.Services.AddSingleton<DocumentAnalystAgent>();

// Add DocumentAnalystAgent to the collection of specialist agents
builder.Services.AddSingleton<ISpecialistAgentService>(provider =>
    provider.GetRequiredService<DocumentAnalystAgent>());

// Keep your existing agent registrations:
builder.Services.AddSingleton<ISpecialistAgentService, SecurityAnalystAgent>();
builder.Services.AddSingleton<ISpecialistAgentService, PerformanceAnalystAgent>();
builder.Services.AddSingleton<ISpecialistAgentService, ArchitecturalAnalystAgent>();


builder.Services.AddSingleton<Kernel>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var endpoint = configuration["AzureOpenAI:Endpoint"] ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
    var apiKey = configuration["AzureOpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
    var deployment = configuration["AzureOpenAI:Deployment"] ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? "gpt-35-turbo";

    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.AddAzureOpenAIChatCompletion(deployment, endpoint, apiKey);

    return kernelBuilder.Build();
});

builder.Services.AddSingleton<ICodeAnalysisAgentService, CodeAnalysisAgentService>();

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
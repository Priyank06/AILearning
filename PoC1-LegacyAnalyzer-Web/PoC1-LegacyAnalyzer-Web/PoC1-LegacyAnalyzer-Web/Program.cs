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
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using PoC1_LegacyAnalyzer_Web.Models.AI102;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface IVisionAnalysisService
    {
        Task<List<ArchitecturalPattern>> AnalyzeSystemDiagramAsync(IFormFile imageFile);
        Task<CodeComplexityInsights> AnalyzeCodeScreenshotAsync(byte[] imageData);
        Task<List<string>> ExtractTextFromImageAsync(byte[] imageData);
    }
}
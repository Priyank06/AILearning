using PoC1_LegacyAnalyzer_Web.Models.AI102;
using CustomImageAnalysisResult = PoC1_LegacyAnalyzer_Web.Models.AI102.ImageAnalysisResult;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface IComputerVisionService
    {
        Task<CustomImageAnalysisResult> AnalyzeArchitectureDiagramAsync(Stream imageStream);
        Task<UIAnalysisResult> AnalyzeUIDesignAsync(Stream imageStream);
        Task<List<string>> ExtractTextFromImageAsync(Stream imageStream);
        Task<DiagramComponentsResult> IdentifyDiagramComponentsAsync(Stream imageStream);
        Task<string> AnalyzeProjectDiagramAsync(string imageUrl);
        Task<List<string>> DetectUIElementsAsync(string imageUrl);
        Task<List<string>> ExtractTextFromDocumentAsync(string documentPath);
    }

}
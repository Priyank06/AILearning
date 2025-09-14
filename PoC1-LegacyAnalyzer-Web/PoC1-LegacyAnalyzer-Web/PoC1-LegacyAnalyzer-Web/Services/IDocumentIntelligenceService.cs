using Azure.AI.FormRecognizer.DocumentAnalysis;
using PoC1_LegacyAnalyzer_Web.Models.AI102;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface IDocumentIntelligenceService
    {
        Task<DocumentAnalysisResult> AnalyzeProjectDocumentationAsync(IFormFile document);
        Task<List<string>> ExtractRequirementsFromDocumentAsync(byte[] documentData, string fileName);
        Task<ArchitectureDocumentInsights> AnalyzeArchitectureDocumentAsync(IFormFile document);
    }
}
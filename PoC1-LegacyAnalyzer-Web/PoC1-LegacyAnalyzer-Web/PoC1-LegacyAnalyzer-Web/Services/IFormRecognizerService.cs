using PoC1_LegacyAnalyzer_Web.Models.AI102;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface IFormRecognizerService
    {
        Task<DocumentAnalysisResult> AnalyzeDocumentAsync(Stream documentStream, string fileName);
        Task<List<ExtractedRequirement>> ExtractRequirementsAsync(Stream documentStream);
        Task<ArchitectureDiagramAnalysis> AnalyzeDiagramAsync(Stream imageStream);
    }
}
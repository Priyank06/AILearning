using PoC1_LegacyAnalyzer_Web.Models;
using PoC1_LegacyAnalyzer_Web.Models.AI102;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface ICognitiveSearchService
    {
        Task<bool> IndexCodebaseAsync(MultiFileAnalysisResult analysisResult);
        Task<List<CodeSearchResult>> SearchCodeAsync(string query, string[] filters = null);
        Task<List<CodeSearchResult>> SemanticSearchAsync(string naturalLanguageQuery);
        Task<CodeInsightsResult> GenerateCodeInsightsAsync(string searchQuery);
    }
}
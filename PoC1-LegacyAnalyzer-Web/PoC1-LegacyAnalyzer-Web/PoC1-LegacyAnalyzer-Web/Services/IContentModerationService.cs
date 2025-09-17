using PoC1_LegacyAnalyzer_Web.Models.AI102;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface IContentModerationService
    {
        Task<ContentModerationResult> ModerateCodeCommentsAsync(string codeWithComments);
        Task<DocumentationQualityResult> AnalyzeDocumentationQualityAsync(string documentation);
        Task<bool> IsContentAppropriateAsync(string content);
    }
}
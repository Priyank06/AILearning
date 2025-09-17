using PoC1_LegacyAnalyzer_Web.Models.AI102;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface ILanguageUnderstandingService
    {
        Task<AnalysisIntent> ParseAnalysisRequestAsync(string userInput);
        Task<List<EntityExtraction>> ExtractEntitiesAsync(string text);
        Task<ConversationContext> MaintainConversationContextAsync(string sessionId, string userInput);
    }
}
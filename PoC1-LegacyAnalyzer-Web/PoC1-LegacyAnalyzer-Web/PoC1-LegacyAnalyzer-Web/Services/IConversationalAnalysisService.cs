using PoC1_LegacyAnalyzer_Web.Models.AI102;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface IConversationalAnalysisService
    {
        Task<string> ProcessChatMessageAsync(string message, string userId);
        Task<ConversationContext> GetUserContextAsync(string userId);
        Task UpdateUserContextAsync(string userId, ConversationContext context);
    }
}
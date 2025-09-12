using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface IAgentOrchestrationService
    {
        Task<TeamAnalysisResult> CoordinateTeamAnalysisAsync(string code, string businessObjective, List<string> requiredSpecialties, CancellationToken cancellationToken = default);

        Task<AgentConversation> FacilitateAgentDiscussionAsync(string topic, List<SpecialistAnalysisResult> initialAnalyses, CancellationToken cancellationToken = default);

        Task<ConsolidatedRecommendations> SynthesizeRecommendationsAsync(List<SpecialistAnalysisResult> analyses, string businessContext, CancellationToken cancellationToken = default);

        Task<string> GenerateExecutiveSummaryAsync(TeamAnalysisResult teamResult, string businessObjective, CancellationToken cancellationToken = default);
    }
}

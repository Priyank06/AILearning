using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface ISpecialistAgentService
    {
        string AgentName { get; }
        string Specialty { get; }
        string AgentPersona { get; }
        int ConfidenceThreshold { get; }

        Task<string> AnalyzeAsync(string code, string businessContext, CancellationToken cancellationToken = default);

        Task<string> ReviewPeerAnalysisAsync(string peerAnalysis, string originalCode, CancellationToken cancellationToken = default);
    }
}

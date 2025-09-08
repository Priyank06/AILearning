using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface ICodeAnalysisAgentService
    {
        Task<AutonomousAnalysisResult> AnalyzeWithAgentAsync(
            string code,
            string businessGoal,
            string projectContext = "",
            CancellationToken cancellationToken = default);

        Task<string> GenerateStrategicPlanAsync(
            MultiFileAnalysisResult projectAnalysis,
            string businessObjective,
            CancellationToken cancellationToken = default);
    }
}

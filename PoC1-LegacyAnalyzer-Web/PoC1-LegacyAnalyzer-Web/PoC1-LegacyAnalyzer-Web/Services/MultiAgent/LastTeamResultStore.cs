using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;

namespace PoC1_LegacyAnalyzer_Web.Services.MultiAgent;

/// <summary>
/// Scoped store for the last completed team result. Allows the multi-agent page to load results
/// when the circuit doesn't push the update after a long-running analysis.
/// </summary>
public class LastTeamResultStore : ILastTeamResultStore
{
    private LastTeamResultEntry? _last;

    public void SetLastResult(TeamAnalysisResult result, string businessObjective, string? customObjective, IReadOnlyList<string> selectedAgents)
    {
        _last = new LastTeamResultEntry
        {
            Result = result,
            BusinessObjective = businessObjective ?? "",
            CustomObjective = customObjective,
            SelectedAgents = selectedAgents?.ToList() ?? new List<string>()
        };
    }

    public LastTeamResultEntry? TryGetLastResult()
    {
        return _last;
    }
}

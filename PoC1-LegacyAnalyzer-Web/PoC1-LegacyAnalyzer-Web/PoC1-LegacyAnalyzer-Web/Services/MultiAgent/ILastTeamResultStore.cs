using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;

namespace PoC1_LegacyAnalyzer_Web.Services.MultiAgent;

/// <summary>
/// Holds the last completed multi-agent result for the current circuit so the UI can load it
/// when the Blazor Server push after a long run doesn't reach the client.
/// </summary>
public interface ILastTeamResultStore
{
    void SetLastResult(TeamAnalysisResult result, string businessObjective, string? customObjective, IReadOnlyList<string> selectedAgents);
    LastTeamResultEntry? TryGetLastResult();
}

public class LastTeamResultEntry
{
    public TeamAnalysisResult Result { get; set; } = new();
    public string BusinessObjective { get; set; } = "";
    public string? CustomObjective { get; set; }
    public IReadOnlyList<string> SelectedAgents { get; set; } = Array.Empty<string>();
}

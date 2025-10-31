

namespace PoC1_LegacyAnalyzer_Web.Models.MultiAgent
{
    public class OrchestrationPlan
    {
        public string Objective { get; internal set; }
        public int TotalFiles { get; internal set; }
        public List<string> SelectedAgents { get; internal set; }
        public TimeSpan EstimatedDuration { get; internal set; }
        public string BusinessObjective { get; internal set; }
        public List<string> RequiredAgents { get; internal set; }
        public int EstimatedTokens { get; internal set; }
        public int EstimatedTimeSeconds { get; internal set; }
        public string Strategy { get; internal set; }
        public string AnalysisApproach { get; internal set; }
    }
}

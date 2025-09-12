namespace PoC1_LegacyAnalyzer_Web.Models.AgentCommunication
{
    public class TeamConsensusMetrics
    {
        public double AgreementPercentage { get; set; }
        public int TotalMessages { get; set; }
        public int ConflictCount { get; set; }
        public int ResolvedConflictCount { get; set; }
        public Dictionary<string, int> AgentParticipationScores { get; set; } = new();
        public TimeSpan DiscussionDuration { get; set; }
    }
}

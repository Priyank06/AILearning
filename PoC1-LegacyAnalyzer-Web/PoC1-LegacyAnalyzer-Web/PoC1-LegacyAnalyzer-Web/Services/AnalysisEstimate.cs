
namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Estimation for analysis cost and time
    /// </summary>
    public class AnalysisEstimate
    {
        public int EstimatedTokens { get; set; }
        public int EstimatedTimeSeconds { get; set; }
        public int NumberOfAgents { get; set; }
        public string OptimizationLevel { get; set; } = string.Empty;
        public decimal EstimatedCostUSD { get; set; }
        public Dictionary<string, int> TokenBreakdown { get; internal set; } = new();
    }
}
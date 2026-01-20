namespace PoC1_LegacyAnalyzer_Web.Services.AI
{
    public interface ITokenEstimationService
    {
        int EstimateTokens(string text);
        int EstimateTokensFromFiles(IEnumerable<long> fileSizes);
        int EstimateBatchPromptOverhead(int fileCount);
        decimal EstimateCost(int tokenCount, decimal costPerToken = 0.00003m);
        string FormatTokenCount(int tokenCount);
    }
}


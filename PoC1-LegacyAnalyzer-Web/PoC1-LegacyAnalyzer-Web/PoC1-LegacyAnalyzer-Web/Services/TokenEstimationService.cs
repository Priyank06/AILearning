namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Service for estimating token usage in AI operations.
    /// </summary>
    public class TokenEstimationService
    {
        private const int AVERAGE_CHARACTERS_PER_TOKEN = 4;

        /// <summary>
        /// Estimates token count from text length.
        /// </summary>
        /// <param name="text">Input text</param>
        /// <returns>Estimated token count</returns>
        public int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            return Math.Max(1, text.Length / AVERAGE_CHARACTERS_PER_TOKEN);
        }

        /// <summary>
        /// Estimates token count for multiple files.
        /// </summary>
        /// <param name="fileSizes">List of file sizes in bytes</param>
        /// <returns>Estimated token count</returns>
        public int EstimateTokensFromFiles(IEnumerable<long> fileSizes)
        {
            var totalBytes = fileSizes.Sum();
            // Assume average character is 1 byte for estimation
            return EstimateTokens(new string('x', (int)Math.Min(totalBytes, int.MaxValue)));
        }

        /// <summary>
        /// Estimates cost based on token usage (approximate).
        /// </summary>
        /// <param name="tokenCount">Number of tokens</param>
        /// <param name="costPerToken">Cost per token in USD (default: GPT-4 pricing)</param>
        /// <returns>Estimated cost in USD</returns>
        public decimal EstimateCost(int tokenCount, decimal costPerToken = 0.00003m)
        {
            return tokenCount * costPerToken;
        }

        /// <summary>
        /// Gets formatted token count with appropriate units.
        /// </summary>
        /// <param name="tokenCount">Number of tokens</param>
        /// <returns>Formatted string (e.g., "1.5K tokens", "2M tokens")</returns>
        public string FormatTokenCount(int tokenCount)
        {
            return tokenCount switch
            {
                < 1000 => $"{tokenCount} tokens",
                < 1000000 => $"{tokenCount / 1000.0:F1}K tokens",
                _ => $"{tokenCount / 1000000.0:F1}M tokens"
            };
        }
    }
}
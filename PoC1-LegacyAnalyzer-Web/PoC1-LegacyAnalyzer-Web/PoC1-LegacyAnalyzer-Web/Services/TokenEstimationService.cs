using Microsoft.Extensions.Options;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Service for estimating token usage in AI operations.
    /// </summary>
    public class TokenEstimationService : ITokenEstimationService
    {
        private const int AVERAGE_CHARACTERS_PER_TOKEN = 4;
        private readonly TokenEstimationConfig _tokenEstimation;
        private readonly BatchProcessingConfig _batchConfig;

        public TokenEstimationService(
            IOptions<TokenEstimationConfig> tokenEstimationOptions,
            IOptions<BatchProcessingConfig> batchOptions)
        {
            _tokenEstimation = tokenEstimationOptions.Value ?? new TokenEstimationConfig();
            _batchConfig = batchOptions.Value ?? new BatchProcessingConfig();
        }

        /// <summary>
        /// Estimates token count from text length.
        /// Accounts for code structure (more tokens per character than plain text).
        /// </summary>
        /// <param name="text">Input text</param>
        /// <returns>Estimated token count</returns>
        public int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            // More accurate estimation: code has more tokens per character
            // C# code typically has ~3-4 chars per token, but we use configurable value
            var baseEstimate = text.Length / _batchConfig.TokenEstimationCharsPerToken;
            
            // Add overhead for code structure (brackets, keywords, etc. increase token density)
            var structureOverhead = (int)(baseEstimate * _tokenEstimation.CodeStructureOverheadPercentage);
            
            return baseEstimate + structureOverhead;
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

        /// <summary>
        /// Estimates additional tokens needed for batch prompt overhead.
        /// Includes system prompt, JSON structure, and per-file separators.
        /// </summary>
        public int EstimateBatchPromptOverhead(int fileCount)
        {
            var baseOverhead = _tokenEstimation.BaseBatchPromptOverhead + _tokenEstimation.BatchJsonStructureOverhead;
            var perFileOverhead = fileCount * _tokenEstimation.PerFileBatchOverhead;
            
            return baseOverhead + perFileOverhead;
        }
    }
}
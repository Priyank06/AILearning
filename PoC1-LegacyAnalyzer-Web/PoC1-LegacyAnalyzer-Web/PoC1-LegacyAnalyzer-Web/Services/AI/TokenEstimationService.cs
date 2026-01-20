using Microsoft.Extensions.Options;
using PoC1_LegacyAnalyzer_Web.Models;
using SharpToken;

namespace PoC1_LegacyAnalyzer_Web.Services.AI
{
    /// <summary>
    /// Service for accurate token counting using tiktoken (SharpToken).
    /// Provides accurate token counts instead of estimates.
    /// </summary>
    public class TokenEstimationService : ITokenEstimationService
    {
        private const int AVERAGE_CHARACTERS_PER_TOKEN = 4;
        private readonly TokenEstimationConfig _tokenEstimation;
        private readonly BatchProcessingConfig _batchConfig;
        private readonly GptEncoding _encoding;
        private readonly ILogger<TokenEstimationService>? _logger;
        private readonly bool _useTiktoken;

        public TokenEstimationService(
            IOptions<TokenEstimationConfig> tokenEstimationOptions,
            IOptions<BatchProcessingConfig> batchOptions,
            ILogger<TokenEstimationService>? logger = null)
        {
            _tokenEstimation = tokenEstimationOptions.Value ?? new TokenEstimationConfig();
            _batchConfig = batchOptions.Value ?? new BatchProcessingConfig();
            _logger = logger;
            
            // Use tiktoken if enabled, otherwise fall back to estimation
            _useTiktoken = _tokenEstimation.UseTiktoken ?? true;
            
            try
            {
                // Initialize SharpToken with cl100k_base encoding (used by GPT-4)
                _encoding = GptEncoding.GetEncoding("cl100k_base");
                _logger?.LogInformation("TokenEstimationService initialized with tiktoken (cl100k_base encoding)");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to initialize tiktoken, falling back to estimation");
                _useTiktoken = false;
                _encoding = null!; // Will not be used if _useTiktoken is false
            }
        }

        /// <summary>
        /// Counts tokens accurately using tiktoken, or estimates if tiktoken is disabled.
        /// </summary>
        /// <param name="text">Input text</param>
        /// <returns>Token count (accurate if tiktoken enabled, estimated otherwise)</returns>
        public int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            // Use accurate tiktoken counting if enabled
            if (_useTiktoken && _encoding != null)
            {
                try
                {
                    var tokens = _encoding.Encode(text);
                    var count = tokens.Count;
                    _logger?.LogDebug("Counted {Count} tokens using tiktoken (text length: {Length})", count, text.Length);
                    return count;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Tiktoken encoding failed, falling back to estimation");
                    // Fall through to estimation
                }
            }

            // Fallback to estimation (original logic)
            var baseEstimate = text.Length / _batchConfig.TokenEstimationCharsPerToken;
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
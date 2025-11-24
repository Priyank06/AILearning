namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Configuration for batch processing of multiple files in a single API call.
    /// 
    /// Optimization Strategy:
    /// - Combines multiple files into single API calls to achieve 60-80% reduction
    /// - Uses intelligent token budgeting to maximize files per batch while staying within limits
    /// - Implements parallel batch processing with controlled concurrency
    /// - Provides error isolation so failures in one file don't break entire batch
    /// 
    /// Example: 10 files → 2-3 batches → 70-80% reduction in API calls
    /// </summary>
    public class BatchProcessingConfig
    {
        /// <summary>
        /// Whether batch processing is enabled.
        /// When enabled, multiple files are combined into single API calls for efficiency.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Maximum number of files to include in a single batch.
        /// Higher values = fewer API calls but more tokens per call.
        /// Recommended: 3-5 files per batch for optimal balance.
        /// </summary>
        public int MaxFilesPerBatch { get; set; } = 5;

        /// <summary>
        /// Maximum tokens allowed per batch (including response).
        /// Must account for: system prompt + file content + JSON structure + response.
        /// Recommended: 10000-15000 for GPT-3.5-turbo, 30000+ for GPT-4.
        /// </summary>
        public int MaxTokensPerBatch { get; set; } = 12000;

        /// <summary>
        /// Maximum number of batches to process concurrently.
        /// Higher values = faster processing but more API rate limit pressure.
        /// Recommended: 2-3 for Azure OpenAI standard tiers.
        /// </summary>
        public int MaxConcurrentBatches { get; set; } = 3;

        /// <summary>
        /// Characters per token for estimation (typically 4 for English code).
        /// Used to estimate token count before making API calls.
        /// C# code typically has 3-4 characters per token.
        /// </summary>
        public int TokenEstimationCharsPerToken { get; set; } = 4;

        /// <summary>
        /// Tokens to reserve for AI response (not used for input).
        /// Ensures enough space for comprehensive analysis responses.
        /// Recommended: 2000-4000 depending on desired response length.
        /// </summary>
        public int ReserveTokensForResponse { get; set; } = 3000;
    }
}


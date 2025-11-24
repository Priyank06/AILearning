namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Configuration for batch processing of multiple files in a single API call.
    /// </summary>
    public class BatchProcessingConfig
    {
        /// <summary>
        /// Enables or disables batch processing. When enabled, multiple files are processed together for efficiency.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Maximum number of files to include in a single batch. Adjust for optimal performance and resource usage.
        /// </summary>
        public int MaxFilesPerBatch { get; set; } = 5;

        /// <summary>
        /// Maximum tokens allowed per batch, including both input and response. Helps control request size.
        /// </summary>
        public int MaxTokensPerBatch { get; set; } = 12000;

        /// <summary>
        /// Maximum number of batches to process at the same time. Higher values may increase throughput but also resource usage.
        /// </summary>
        public int MaxConcurrentBatches { get; set; } = 3;

        /// <summary>
        /// Estimated number of characters per token, used for calculating batch sizes.
        /// </summary>
        public int TokenEstimationCharsPerToken { get; set; } = 4;

        /// <summary>
        /// Number of tokens reserved for the response to ensure enough space for results.
        /// </summary>
        public int ReserveTokensForResponse { get; set; } = 3000;
    }
}


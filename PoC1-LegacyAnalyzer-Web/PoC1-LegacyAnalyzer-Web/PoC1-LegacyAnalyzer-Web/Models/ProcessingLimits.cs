namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Represents processing limits configuration.
    /// </summary>
    public class ProcessingLimits
    {
        /// <summary>
        /// Metadata sample file count.
        /// </summary>
        public int MetadataSampleFileCount { get; set; }

        /// <summary>
        /// Maximum length for code context summary.
        /// </summary>
        public int CodeContextSummaryMaxLength { get; set; }

        /// <summary>
        /// Estimated characters per token.
        /// </summary>
        public int TokenEstimationCharsPerToken { get; set; }
    }
}
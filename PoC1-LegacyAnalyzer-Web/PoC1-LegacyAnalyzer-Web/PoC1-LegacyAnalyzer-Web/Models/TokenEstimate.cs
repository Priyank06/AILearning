namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Token estimation comparison between metadata summary and full source code.
    /// Used to validate the 75-80% token reduction claim of the preprocessing service.
    /// </summary>
    public class TokenEstimate
    {
        /// <summary>
        /// Estimated token count for the metadata summary (PatternSummary + overhead).
        /// This represents the compact representation used for AI agent input.
        /// </summary>
        public int MetadataTokens { get; set; }

        /// <summary>
        /// Estimated token count for the full source code.
        /// This represents the original file content that would be sent without preprocessing.
        /// </summary>
        public int FullCodeTokens { get; set; }

        /// <summary>
        /// Token reduction percentage achieved by using metadata instead of full code.
        /// Calculated as: ((FullCodeTokens - MetadataTokens) / FullCodeTokens) * 100
        /// </summary>
        public double ReductionPercentage { get; set; }

        /// <summary>
        /// Number of tokens saved by using metadata instead of full code.
        /// </summary>
        public int TokensSaved => FullCodeTokens - MetadataTokens;
    }
}


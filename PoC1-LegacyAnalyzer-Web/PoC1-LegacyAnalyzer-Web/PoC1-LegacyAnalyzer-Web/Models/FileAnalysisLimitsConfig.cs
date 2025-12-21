namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Configuration for file analysis limits and thresholds.
    /// Replaces hard-coded values for flexibility and maintainability.
    /// </summary>
    public class FileAnalysisLimitsConfig
    {
        /// <summary>
        /// Maximum file size in bytes for analysis (default: 500KB).
        /// Files larger than this will be rejected for security and performance reasons.
        /// </summary>
        public int MaxFileSizeBytes { get; set; } = 512000;

        /// <summary>
        /// Maximum number of files to process in a single analysis session.
        /// Prevents performance issues and API rate limiting.
        /// </summary>
        public int MaxFilesPerAnalysis { get; set; } = 10;

        /// <summary>
        /// Maximum number of recommendations to return per analysis.
        /// Limits output to most relevant recommendations.
        /// </summary>
        public int MaxRecommendations { get; set; } = 6;

        /// <summary>
        /// Default code preview length in characters for individual file analysis.
        /// Used when CodePreviewMaxLength is not configured.
        /// </summary>
        public int DefaultCodePreviewLength { get; set; } = 800;

        /// <summary>
        /// Minimum code preview length in characters for batch analysis.
        /// Used when batch size requires shorter previews.
        /// </summary>
        public int MinCodePreviewLength { get; set; } = 400;

        /// <summary>
        /// Batch size threshold for reducing code preview length.
        /// When batch size exceeds this, preview length is reduced.
        /// </summary>
        public int BatchSizeThresholdForPreviewReduction { get; set; } = 3;

        /// <summary>
        /// Code preview length divisor for large batches.
        /// Preview length = baseLength / (batchSize / divisor).
        /// </summary>
        public int BatchPreviewLengthDivisor { get; set; } = 2;

        /// <summary>
        /// Maximum number of top classes to display in analysis.
        /// </summary>
        public int MaxTopClassesToDisplay { get; set; } = 3;

        /// <summary>
        /// Minimum word count for AI analysis assessment.
        /// </summary>
        public int MinAnalysisWordCount { get; set; } = 200;

        /// <summary>
        /// Maximum word count for AI analysis assessment.
        /// </summary>
        public int MaxAnalysisWordCount { get; set; } = 500;
    }

    /// <summary>
    /// Configuration for complexity and risk thresholds.
    /// Replaces hard-coded threshold values.
    /// </summary>
    public class ComplexityThresholdsConfig
    {
        /// <summary>
        /// Low complexity threshold (below this is very low).
        /// </summary>
        public int Low { get; set; } = 30;

        /// <summary>
        /// Medium complexity threshold (between Low and Medium).
        /// </summary>
        public int Medium { get; set; } = 50;

        /// <summary>
        /// High complexity threshold (between Medium and High).
        /// </summary>
        public int High { get; set; } = 60;

        /// <summary>
        /// Very high complexity threshold (above this is very high).
        /// </summary>
        public int VeryHigh { get; set; } = 70;

        /// <summary>
        /// Critical complexity threshold for high-risk assessments.
        /// </summary>
        public int Critical { get; set; } = 40;
    }

    /// <summary>
    /// Configuration for file count and scale thresholds.
    /// </summary>
    public class ScaleThresholdsConfig
    {
        /// <summary>
        /// File count threshold for large codebase recommendations.
        /// </summary>
        public int LargeCodebaseFileCount { get; set; } = 25;

        /// <summary>
        /// Methods per class threshold for architectural recommendations.
        /// </summary>
        public int HighMethodsPerClass { get; set; } = 8;

        /// <summary>
        /// Complexity score threshold for high-risk file identification.
        /// </summary>
        public int HighRiskComplexityScore { get; set; } = 60;
    }

    /// <summary>
    /// Configuration for token estimation and overhead calculations.
    /// </summary>
    public class TokenEstimationConfig
    {
        /// <summary>
        /// Whether to use tiktoken (SharpToken) for accurate token counting.
        /// If false, falls back to estimation based on character count.
        /// </summary>
        public bool? UseTiktoken { get; set; } = true;

        /// <summary>
        /// Structure overhead percentage for code (default: 10%).
        /// Code has more tokens per character than plain text.
        /// Only used when UseTiktoken is false.
        /// </summary>
        public double CodeStructureOverheadPercentage { get; set; } = 0.1;

        /// <summary>
        /// Base prompt overhead in tokens for batch processing.
        /// </summary>
        public int BaseBatchPromptOverhead { get; set; } = 400;

        /// <summary>
        /// JSON structure overhead in tokens for batch processing.
        /// </summary>
        public int BatchJsonStructureOverhead { get; set; } = 200;

        /// <summary>
        /// Per-file overhead in tokens for batch processing.
        /// </summary>
        public int PerFileBatchOverhead { get; set; } = 80;
    }
}


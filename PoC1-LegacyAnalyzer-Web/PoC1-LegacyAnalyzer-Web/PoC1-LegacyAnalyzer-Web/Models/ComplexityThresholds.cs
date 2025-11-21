namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Represents complexity thresholds.
    /// </summary>
    public class ComplexityThresholds
    {
        /// <summary>
        /// Very low complexity threshold.
        /// </summary>
        public int VeryLow { get; set; }

        /// <summary>
        /// Low complexity threshold.
        /// </summary>
        public int Low { get; set; }

        /// <summary>
        /// Medium complexity threshold.
        /// </summary>
        public int Medium { get; set; }

        /// <summary>
        /// High complexity threshold.
        /// </summary>
        public int High { get; set; }

        /// <summary>
        /// Very high complexity threshold.
        /// </summary>
        public int VeryHigh { get; set; }
    }
}
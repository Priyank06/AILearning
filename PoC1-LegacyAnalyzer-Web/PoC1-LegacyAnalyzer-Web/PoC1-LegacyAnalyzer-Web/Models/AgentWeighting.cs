namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Represents agent weighting configuration.
    /// </summary>
    public class AgentWeighting
    {
        /// <summary>
        /// Security agent weighting.
        /// </summary>
        public double SecurityWeight { get; set; }

        /// <summary>
        /// Performance agent weighting.
        /// </summary>
        public double PerformanceWeight { get; set; }

        /// <summary>
        /// Architecture agent weighting.
        /// </summary>
        public double ArchitectureWeight { get; set; }

        /// <summary>
        /// Default agent weighting.
        /// </summary>
        public double DefaultWeight { get; set; }
    }
}
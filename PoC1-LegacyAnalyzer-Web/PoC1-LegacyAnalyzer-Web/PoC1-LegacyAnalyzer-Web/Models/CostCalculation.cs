namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Represents cost calculation parameters.
    /// </summary>
    public class CostCalculation
    {
        /// <summary>
        /// Base value per line of code.
        /// </summary>
        public decimal BaseValuePerLine { get; set; } = 0.10m;

        /// <summary>
        /// Maximum estimated value.
        /// </summary>
        public decimal MaxEstimatedValue { get; set; } = 1000000m;

        /// <summary>
        /// Default developer hourly rate.
        /// </summary>
        public decimal DefaultDeveloperHourlyRate { get; set; } = 125m;

        /// <summary>
        /// Base multiplier for complexity.
        /// </summary>
        public decimal ComplexityMultiplierBase { get; set; } = 0.5m;
    }
}
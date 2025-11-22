namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Configuration for architectural debt calculation.
    /// </summary>
    public class ArchitecturalDebtConfiguration
    {
        /// <summary>
        /// Debt factor for poor separation of concerns.
        /// </summary>
        public int PoorSeparationOfConcernsDebt { get; set; } = 30;

        /// <summary>
        /// Debt factor for basic separation of concerns.
        /// </summary>
        public int BasicSeparationOfConcernsDebt { get; set; } = 15;

        /// <summary>
        /// Debt factor for monolithic architecture.
        /// </summary>
        public int MonolithicArchitectureDebt { get; set; } = 20;

        /// <summary>
        /// Debt factor for no design patterns.
        /// </summary>
        public int NoDesignPatternsDebt { get; set; } = 25;

        /// <summary>
        /// Debt factor for no testing.
        /// </summary>
        public int NoTestingDebt { get; set; } = 25;

        /// <summary>
        /// Maximum architectural debt score.
        /// </summary>
        public int MaxDebtScore { get; set; } = 100;
    }
}


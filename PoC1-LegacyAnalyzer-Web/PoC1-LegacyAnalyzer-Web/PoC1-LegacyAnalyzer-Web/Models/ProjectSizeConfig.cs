namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Represents project size configuration.
    /// </summary>
    public class ProjectSizeConfig
    {
        /// <summary>
        /// Maximum number of files for the project size.
        /// </summary>
        public int? MaxFiles { get; set; }

        /// <summary>
        /// Maximum number of classes for the project size.
        /// </summary>
        public int? MaxClasses { get; set; }

        /// <summary>
        /// Label for the project size.
        /// </summary>
        public string Label { get; set; }
    }
}
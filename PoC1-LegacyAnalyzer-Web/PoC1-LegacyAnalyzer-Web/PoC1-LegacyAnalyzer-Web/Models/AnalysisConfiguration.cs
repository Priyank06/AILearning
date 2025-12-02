using Microsoft.AspNetCore.Components.Forms;

namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Configuration for analysis requests from the UI.
    /// </summary>
    public class AnalysisConfiguration
    {
        /// <summary>
        /// Business objective for the analysis.
        /// </summary>
        public string BusinessObjective { get; set; } = string.Empty;

        /// <summary>
        /// List of selected agent types (security, performance, architecture).
        /// </summary>
        public List<string> SelectedAgents { get; set; } = new();

        /// <summary>
        /// Files to be analyzed.
        /// </summary>
        public List<IBrowserFile> Files { get; set; } = new();

        /// <summary>
        /// Custom business objective if "custom" is selected.
        /// </summary>
        public string? CustomObjective { get; set; }

        /// <summary>
        /// Gets the effective business objective (custom if specified, otherwise the selected predefined one).
        /// </summary>
        public string EffectiveObjective => 
            !string.IsNullOrWhiteSpace(CustomObjective) ? CustomObjective : BusinessObjective;

        /// <summary>
        /// Validates the configuration.
        /// </summary>
        public bool IsValid => 
            !string.IsNullOrWhiteSpace(EffectiveObjective) && 
            SelectedAgents.Any() && 
            Files.Any();

        /// <summary>
        /// Gets validation errors if any.
        /// </summary>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();
            
            if (string.IsNullOrWhiteSpace(EffectiveObjective))
                errors.Add("Business objective is required.");
                
            if (!SelectedAgents.Any())
                errors.Add("At least one agent must be selected.");
                
            if (!Files.Any())
                errors.Add("At least one file must be selected for analysis.");
                
            return errors;
        }
    }
}
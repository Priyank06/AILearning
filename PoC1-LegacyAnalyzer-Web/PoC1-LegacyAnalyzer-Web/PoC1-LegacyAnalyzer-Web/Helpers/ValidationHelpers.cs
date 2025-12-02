namespace PoC1_LegacyAnalyzer_Web.Helpers
{
    /// <summary>
    /// Helper methods for input validation in the UI.
    /// </summary>
    public static class ValidationHelpers
    {
        /// <summary>
        /// Validates that at least one agent is selected.
        /// </summary>
        /// <param name="selectedAgents">List of selected agent types</param>
        /// <returns>True if at least one agent is selected</returns>
        public static bool IsAgentSelectionValid(List<string> selectedAgents)
        {
            return selectedAgents?.Any() == true;
        }

        /// <summary>
        /// Validates that at least one file is provided for analysis.
        /// </summary>
        /// <param name="files">List of selected files</param>
        /// <returns>True if at least one file is provided</returns>
        public static bool IsFileSelectionValid(ICollection<Microsoft.AspNetCore.Components.Forms.IBrowserFile> files)
        {
            return files?.Any() == true;
        }

        /// <summary>
        /// Validates that a business objective is provided.
        /// </summary>
        /// <param name="objective">Selected objective</param>
        /// <param name="customObjective">Custom objective text</param>
        /// <returns>True if objective is valid</returns>
        public static bool IsObjectiveValid(string objective, string? customObjective)
        {
            if (string.IsNullOrWhiteSpace(objective))
                return false;

            // If custom is selected, custom objective must be provided
            if (objective == "custom")
                return !string.IsNullOrWhiteSpace(customObjective);

            return true;
        }

        /// <summary>
        /// Gets all validation errors for the analysis configuration.
        /// </summary>
        /// <param name="objective">Selected objective</param>
        /// <param name="customObjective">Custom objective text</param>
        /// <param name="selectedAgents">Selected agents</param>
        /// <param name="files">Selected files</param>
        /// <returns>List of validation error messages</returns>
        public static List<string> GetValidationErrors(
            string objective, 
            string? customObjective, 
            List<string> selectedAgents, 
            ICollection<Microsoft.AspNetCore.Components.Forms.IBrowserFile> files)
        {
            var errors = new List<string>();

            if (!IsObjectiveValid(objective, customObjective))
            {
                if (objective == "custom")
                    errors.Add("Custom business objective is required when 'Custom Objective' is selected.");
                else
                    errors.Add("Business objective is required.");
            }

            if (!IsAgentSelectionValid(selectedAgents))
                errors.Add("At least one specialist agent must be selected.");

            if (!IsFileSelectionValid(files))
                errors.Add("At least one file must be selected for analysis.");

            return errors;
        }

        /// <summary>
        /// Validates file size against the specified limit.
        /// </summary>
        /// <param name="file">File to validate</param>
        /// <param name="maxSizeMB">Maximum size in MB</param>
        /// <returns>True if file size is valid</returns>
        public static bool IsFileSizeValid(Microsoft.AspNetCore.Components.Forms.IBrowserFile file, int maxSizeMB)
        {
            var maxSizeBytes = maxSizeMB * 1024L * 1024L;
            return file.Size <= maxSizeBytes;
        }

        /// <summary>
        /// Validates that the file is a C# source file.
        /// </summary>
        /// <param name="file">File to validate</param>
        /// <returns>True if file is a valid C# source file</returns>
        public static bool IsCSharpSourceFile(Microsoft.AspNetCore.Components.Forms.IBrowserFile file)
        {
            return file.Name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) &&
                   !file.Name.Contains(".Designer.") &&
                   !file.Name.Contains(".g.cs") &&
                   !file.Name.Contains("AssemblyInfo.cs");
        }
    }
}
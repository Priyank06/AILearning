using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.Orchestration
{
    /// <summary>
    /// Defines a service for analyzing project files and generating a summary report.
    /// </summary>
    public interface IProjectAnalysisService
    {
        /// <summary>
        /// Analyzes the provided project files and returns a summary of the analysis.
        /// </summary>
        /// <param name="files">The list of project files to analyze.</param>
        /// <param name="analysisType">The type of analysis to perform.</param>
        /// <param name="progress">An optional progress reporter for tracking analysis progress.</param>
        /// <returns>A <see cref="ProjectSummary"/> containing the results of the analysis.</returns>
        Task<ProjectSummary> AnalyzeProjectFilesAsync(List<IBrowserFile> files, string analysisType, IProgress<int> progress = null);
    }

    /// <summary>
    /// Represents a summary of the analysis performed on a set of project files.
    /// </summary>
    public class ProjectSummary
    {
        /// <summary>
        /// Gets or sets the total number of files analyzed.
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Gets or sets the total number of classes found in the project.
        /// </summary>
        public int TotalClasses { get; set; }

        /// <summary>
        /// Gets or sets the total number of methods found in the project.
        /// </summary>
        public int TotalMethods { get; set; }

        /// <summary>
        /// Gets or sets the total number of properties found in the project.
        /// </summary>
        public int TotalProperties { get; set; }

        /// <summary>
        /// Gets or sets the analysis results for each file.
        /// </summary>
        public List<FileAnalysisResult> FileResults { get; set; } = new();

        /// <summary>
        /// Gets or sets the overall assessment of the project.
        /// </summary>
        public string OverallAssessment { get; set; } = "";

        /// <summary>
        /// Gets or sets the overall complexity score of the project.
        /// </summary>
        public int ComplexityScore { get; set; }

        /// <summary>
        /// Gets or sets the risk level determined for the project.
        /// </summary>
        public string RiskLevel { get; set; } = "";

        /// <summary>
        /// Gets or sets the key recommendations based on the analysis.
        /// </summary>
        public List<string> KeyRecommendations { get; set; } = new();
    }
}

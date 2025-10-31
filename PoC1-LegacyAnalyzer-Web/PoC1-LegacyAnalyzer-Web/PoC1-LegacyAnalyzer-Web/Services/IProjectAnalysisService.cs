using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Models;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;

namespace PoC1_LegacyAnalyzer_Web.Services
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
}

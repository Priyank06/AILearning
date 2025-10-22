using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Provides methods for analyzing multiple files and calculating business metrics.
    /// </summary>
    public interface IMultiFileAnalysisService
    {
        /// <summary>
        /// Analyzes multiple files asynchronously based on the specified analysis type.
        /// </summary>
        /// <param name="files">The list of files to analyze.</param>
        /// <param name="analysisType">The type of analysis to perform.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="MultiFileAnalysisResult"/> with the analysis details.
        /// </returns>
        Task<MultiFileAnalysisResult> AnalyzeMultipleFilesAsync(List<IBrowserFile> files, string analysisType);

        /// <summary>
        /// Analyzes multiple files asynchronously with progress reporting.
        /// </summary>
        /// <param name="files">The list of files to analyze.</param>
        /// <param name="analysisType">The type of analysis to perform.</param>
        /// <param name="progress">An optional progress reporter for analysis progress updates.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="MultiFileAnalysisResult"/> with the analysis details.
        /// </returns>
        Task<MultiFileAnalysisResult> AnalyzeMultipleFilesWithProgressAsync(List<IBrowserFile> files, string analysisType, IProgress<AnalysisProgress> progress = null);

        /// <summary>
        /// Calculates business metrics based on the results of a multi-file analysis.
        /// </summary>
        /// <param name="result">The result of the multi-file analysis.</param>
        /// <returns>
        /// A <see cref="BusinessMetrics"/> object containing calculated business metrics.
        /// </returns>
        BusinessMetrics CalculateBusinessMetrics(MultiFileAnalysisResult result);
    }
}

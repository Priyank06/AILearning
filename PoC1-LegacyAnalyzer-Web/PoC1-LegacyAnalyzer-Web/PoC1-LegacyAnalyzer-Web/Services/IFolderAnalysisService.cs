using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Models.ProjectAnalysis;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface IFolderAnalysisService
    {
        Task<Dictionary<string, FolderAnalysisResult>> AnalyzeFolderStructureAsync(
            Dictionary<string, List<IBrowserFile>> projectStructure,
            CancellationToken cancellationToken = default);
    }
}


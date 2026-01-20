using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Models.ProjectAnalysis;

namespace PoC1_LegacyAnalyzer_Web.Services.ProjectAnalysis
{
    public interface IProjectMetadataService
    {
        Task<ProjectMetadata> ExtractProjectMetadataAsync(List<IBrowserFile> files, CancellationToken cancellationToken = default);
    }
}


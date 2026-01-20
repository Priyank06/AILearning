using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.Orchestration
{
    public interface IBatchAnalysisOrchestrator
    {
        Task<List<FileAnalysisResult>> AnalyzeFilesInBatchesAsync(
            List<IBrowserFile> files,
            string analysisType,
            AnalysisProgress analysisProgress,
            IProgress<AnalysisProgress> progress);
    }
}


using AICodeAnalyzer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AICodeAnalyzer.Services
{
    public interface IReportService
    {
        Task GenerateReportAsync(CodeAnalysisResult analysis, string aiAnalysis, string fileName, string analysisType);
        void ShowProjectSummary(List<ProjectFileAnalysis> projectFiles, int totalClasses, int totalMethods);
        void ShowUsage();
    }
}
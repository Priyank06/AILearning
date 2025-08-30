using AICodeAnalyzer.Models;
using System.Threading.Tasks;

namespace AICodeAnalyzer.Services
{
    public interface IAIAnalysisService
    {
        Task<string> GetAnalysisAsync(string code, string fileName, string analysisType, CodeAnalysisResult analysis);
        Task<string> GetQuickInsightAsync(string code, string fileName);
        string GetSystemPrompt(string analysisType);
        string BuildAnalysisPrompt(string code, string fileName, string analysisType, CodeAnalysisResult analysis);
    }
}
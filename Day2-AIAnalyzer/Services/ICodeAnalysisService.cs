using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AICodeAnalyzer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AICodeAnalyzer.Services
{
    public interface ICodeAnalysisService  
    {
        Task<CodeAnalysisResult> AnalyzeFileAsync(string filePath);
        Task<List<ProjectFileAnalysis>> AnalyzeProjectAsync(string projectPath, int maxFiles = 5);
        CodeAnalysisResult PerformBasicAnalysis(SyntaxNode root);
        void DisplayBasicResults(CodeAnalysisResult result);
    }
}
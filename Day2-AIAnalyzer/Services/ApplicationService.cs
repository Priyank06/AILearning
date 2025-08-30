using AICodeAnalyzer.Models;
using AICodeAnalyzer.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AICodeAnalyzer.Services
{
    public class ApplicationService
    {
        private readonly ICodeAnalysisService _codeAnalysis;
        private readonly IAIAnalysisService _aiAnalysis;
        private readonly IReportService _reportService;
        
        public ApplicationService(
            ICodeAnalysisService codeAnalysis,
            IAIAnalysisService aiAnalysis, 
            IReportService reportService)
        {
            _codeAnalysis = codeAnalysis;
            _aiAnalysis = aiAnalysis;
            _reportService = reportService;
        }
        
        public async Task RunAsync(string[] args)
        {
            Console.WriteLine("🤖 AI-Enhanced Code Analyzer - Day 3 (Architected)");
            Console.WriteLine("===================================================");
            
            if (args.Length > 0 && args[0] == "--help")
            {
                _reportService.ShowUsage();
                return;
            }
            
            var (analysisType, targetPath) = ParseArguments(args);
            
            Console.WriteLine($"🔍 Analysis Type: {analysisType}");
            Console.WriteLine($"📁 Target: {targetPath}");
            Console.WriteLine();
            
            if (Directory.Exists(targetPath))
            {
                await AnalyzeProject(targetPath, analysisType);
            }
            else if (File.Exists(targetPath))
            {
                await AnalyzeFile(targetPath, analysisType);
            }
            else
            {
                Console.WriteLine($"❌ Path not found: {targetPath}");
                Console.WriteLine("Creating sample analysis...");
                await AnalyzeFile("TestCode.cs", analysisType);
            }
        }
        
        private (string analysisType, string targetPath) ParseArguments(string[] args)
        {
            string analysisType = "general";
            string targetPath = "TestCode.cs";
            
            if (args.Length > 0 && args[0] == "--type" && args.Length > 1)
            {
                analysisType = args[1];
                if (args.Length > 2)
                    targetPath = args[2];
            }
            else if (args.Length > 0)
            {
                targetPath = args[0];
            }
            
            return (analysisType, targetPath);
        }
        
        private async Task AnalyzeFile(string filePath, string analysisType)
        {
            var analysis = await _codeAnalysis.AnalyzeFileAsync(filePath);
            var code = await File.ReadAllTextAsync(filePath);
            
            Console.WriteLine($"📁 Analyzing: {filePath}");
            Console.WriteLine($"📊 File size: {code.Length} characters\n");
            
            _codeAnalysis.DisplayBasicResults(analysis);
            
            Console.WriteLine($"\n🤖 AI-Powered {analysisType} Analysis:");
            Console.WriteLine(new string('=', 50));
            
            var aiAnalysis = await _aiAnalysis.GetAnalysisAsync(code, Path.GetFileName(filePath), analysisType, analysis);
            Console.WriteLine(aiAnalysis);
            
            await _reportService.GenerateReportAsync(analysis, aiAnalysis, Path.GetFileName(filePath), analysisType);
        }
        
        private async Task AnalyzeProject(string projectPath, string analysisType)
        {
            Console.WriteLine($"🔍 Project Analysis: {projectPath}");
            Console.WriteLine("===================================");
            
            var projectFiles = await _codeAnalysis.AnalyzeProjectAsync(projectPath, 5);
            Console.WriteLine($"Found {projectFiles.Count} C# files to analyze\n");
            
            int totalClasses = 0, totalMethods = 0;
            
            foreach (var fileAnalysis in projectFiles)
            {
                try
                {
                    Console.WriteLine($"📄 {fileAnalysis.FileName}:");
                    
                    totalClasses += fileAnalysis.Analysis.ClassCount;
                    totalMethods += fileAnalysis.Analysis.MethodCount;
                    
                    Console.WriteLine($"   📊 Classes: {fileAnalysis.Analysis.ClassCount}, Methods: {fileAnalysis.Analysis.MethodCount}");
                    
                    if (fileAnalysis.Analysis.ClassCount > 0)
                    {
                        var code = await File.ReadAllTextAsync(fileAnalysis.FilePath);
                        fileAnalysis.QuickInsight = await _aiAnalysis.GetQuickInsightAsync(code, fileAnalysis.FileName);
                        Console.WriteLine($"   🤖 {fileAnalysis.QuickInsight}");
                    }
                    
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ Error: {ex.Message}");
                }
            }
            
            _reportService.ShowProjectSummary(projectFiles, totalClasses, totalMethods);
        }
    }
}
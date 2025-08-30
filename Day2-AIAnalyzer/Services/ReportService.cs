using AICodeAnalyzer.Models;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace AICodeAnalyzer.Services
{
    public class ReportService : IReportService
    {
        public async Task GenerateReportAsync(CodeAnalysisResult analysis, string aiAnalysis, string fileName, string analysisType)
        {
            var report = new StringBuilder();
            
            report.AppendLine($"# {analysisType.ToUpper()} Analysis Report");
            report.AppendLine($"*File: {fileName} | Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}*");
            report.AppendLine();
            
            report.AppendLine("## Static Analysis Results");
            report.AppendLine($"- **Classes**: {analysis.ClassCount}");
            report.AppendLine($"- **Methods**: {analysis.MethodCount}");
            report.AppendLine($"- **Properties**: {analysis.PropertyCount}");
            report.AppendLine($"- **Dependencies**: {analysis.UsingCount} using statements");
            report.AppendLine();
            
            report.AppendLine("## AI Analysis");
            report.AppendLine(aiAnalysis);
            report.AppendLine();
            
            report.AppendLine("## Next Steps");
            report.AppendLine("1. Address high-priority items identified above");
            report.AppendLine("2. Plan implementation timeline");
            report.AppendLine("3. Set up monitoring for improvements");
            
            var reportFileName = $"{analysisType}-analysis-{Path.GetFileNameWithoutExtension(fileName)}-{DateTime.Now:yyyyMMdd-HHmmss}.md";
            await File.WriteAllTextAsync(reportFileName, report.ToString());
            Console.WriteLine($"\n📄 Report generated: {reportFileName}");
        }
        
        public void ShowProjectSummary(List<ProjectFileAnalysis> projectFiles, int totalClasses, int totalMethods)
        {
            Console.WriteLine("🧠 Project Summary:");
            Console.WriteLine("===================");
            Console.WriteLine($"Total Classes: {totalClasses}");
            Console.WriteLine($"Total Methods: {totalMethods}");
            Console.WriteLine($"Files Analyzed: {projectFiles.Count}");
            Console.WriteLine();
            Console.WriteLine("Key Insights:");
            
            foreach (var file in projectFiles.Where(f => !string.IsNullOrEmpty(f.QuickInsight)))
            {
                Console.WriteLine($"- {file.FileName}: {file.QuickInsight}");
            }
        }
        
        public void ShowUsage()
        {
            Console.WriteLine("📖 USAGE:");
            Console.WriteLine("dotnet run [file-or-directory]");
            Console.WriteLine("dotnet run --type [analysis-type] [file-or-directory]");
            Console.WriteLine();
            Console.WriteLine("Analysis Types:");
            Console.WriteLine("  general      - General modernization");
            Console.WriteLine("  security     - Security audit");
            Console.WriteLine("  performance  - Performance optimization");
            Console.WriteLine("  migration    - Migration readiness");
        }
    }
}
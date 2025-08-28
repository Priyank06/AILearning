using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine(" Simple AI Code Analyzer - Day 1");
        Console.WriteLine("==================================");

        if (args.Length > 0 && Directory.Exists(args[0]))
        {
            await AnalyzeProject(args[0]);
        }
        else
        {
            Console.WriteLine("Analyzing sample file...\n");
            await AnalyzeCodeFile("/Users/priyankghorecha/AILearning/Day1-SimpleAnalyzer/Sample/TestCode.cs");
            
        }
    }

    static async Task AnalyzeCodeFile(string fileName)
    {
        // Step 1: Read and parse the file
        if (!File.Exists(fileName))
        {
            Console.WriteLine($" File {fileName} not found!");
            return;
        }

        string code = File.ReadAllText(fileName);
        Console.WriteLine($"📁 Analyzing: {fileName}");
        Console.WriteLine($"📊 File size: {code.Length} characters\n");

        // Step 2: Parse with Roslyn
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var root = syntaxTree.GetRoot();

        // Step 3: Extract basic metrics
        var analysis = PerformBasicAnalysis(root);
        DisplayBasicResults(analysis);

        // Step 4: Simulated AI enhancement for Day 1
        await SimulateAIEnhancement(analysis);

        ReportGenerator.GenerateReport(analysis);
    }

    static CodeAnalysisResult PerformBasicAnalysis(SyntaxNode root)
    {
        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
        var properties = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().ToList();
        var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToList();

        return new CodeAnalysisResult
        {
            ClassCount = classes.Count,
            MethodCount = methods.Count,
            PropertyCount = properties.Count,
            UsingCount = usingDirectives.Count,
            Classes = classes.Select(c => c.Identifier.ValueText).ToList(),
            Methods = methods.Select(m => $"{GetClassName(m)}.{m.Identifier.ValueText}").ToList(),
            UsingStatements = usingDirectives.Select(u => u.Name?.ToString() ?? "Unknown").ToList()
        };
    }

    static string GetClassName(SyntaxNode method)
    {
        var classDeclaration = method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        return classDeclaration?.Identifier.ValueText ?? "Unknown";
    }

    static void DisplayBasicResults(CodeAnalysisResult result)
    {
        Console.WriteLine(" Code Analysis Results:");
        Console.WriteLine("========================");
        Console.WriteLine($"Classes: {result.ClassCount}");
        Console.WriteLine($"Methods: {result.MethodCount}");
        Console.WriteLine($"Properties: {result.PropertyCount}");
        Console.WriteLine($"Using Statements: {result.UsingCount}");

        Console.WriteLine($"\n Classes Found:");
        foreach (var className in result.Classes)
        {
            Console.WriteLine($"   - {className}");
        }

        Console.WriteLine($"\n🔧 Methods Found:");
        foreach (var method in result.Methods.Take(5)) // Limit to first 5
        {
            Console.WriteLine($"   - {method}");
        }
        if (result.Methods.Count > 5)
        {
            Console.WriteLine($"   ... and {result.Methods.Count - 5} more");
        }
    }

    static async Task SimulateAIEnhancement(CodeAnalysisResult analysis)
    {
        // Simulate processing delay
        await Task.Delay(1000);

        Console.WriteLine("\n AI Enhancement (Simulated):");
        Console.WriteLine("===============================");

        // Simple rule-based recommendations based on analysis
        var recommendations = GenerateRecommendations(analysis);

        foreach (var recommendation in recommendations)
        {
            Console.WriteLine($" {recommendation}");
        }

        Console.WriteLine($"\n Migration Priority: {GetPriority(analysis)}");
        Console.WriteLine($" Estimated Effort: {GetEffortEstimate(analysis)}");
        Console.WriteLine($" Code Quality Score: {GetQualityScore(analysis)}/10");
    }

    static List<string> GenerateRecommendations(CodeAnalysisResult analysis)
    {
        var recommendations = new List<string>();

        if (analysis.Methods.Any(m => m.Contains("FindById")))
        {
            recommendations.Add("Consider using LINQ instead of manual loops for data queries");
        }

        if (analysis.UsingStatements.Contains("System.Collections.Generic"))
        {
            recommendations.Add("Good use of generic collections - consider async patterns for I/O");
        }

        if (analysis.ClassCount > 5)
        {
            recommendations.Add("Large number of classes - consider dependency injection");
        }
        else
        {
            recommendations.Add("Moderate complexity - good candidate for incremental migration");
        }

        recommendations.Add("Add proper error handling and logging");
        recommendations.Add("Consider implementing interfaces for better testability");

        return recommendations.Take(3).ToList();
    }

    static string GetPriority(CodeAnalysisResult analysis)
    {
        if (analysis.ClassCount > 10) return "High";
        if (analysis.ClassCount > 3) return "Medium";
        return "Low";
    }

    static string GetEffortEstimate(CodeAnalysisResult analysis)
    {
        var days = Math.Max(1, analysis.ClassCount / 2);
        return $"{days}-{days + 1} days";
    }

    static int GetQualityScore(CodeAnalysisResult analysis)
    {
        int score = 5; // Base score

        if (analysis.UsingStatements.Count < 10) score += 1; // Not too many dependencies
        if (analysis.MethodCount > 0) score += 1; // Has methods
        if (analysis.PropertyCount > 0) score += 1; // Uses properties
        if (analysis.ClassCount > 1) score += 1; // Good separation

        return Math.Min(10, score);
    }
    
    static async Task AnalyzeProject(string projectPath)
{
    Console.WriteLine($" Analyzing Project: {projectPath}");
    Console.WriteLine("====================================");
    
    var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
        .Where(f => !f.Contains("/bin/") && !f.Contains("/obj/"))
        .ToList();
    
    Console.WriteLine($"Found {csFiles.Count} C# files\n");
    
    var totalAnalysis = new CodeAnalysisResult();
    
    foreach (var file in csFiles.Take(5)) // Limit for Day 1
    {
        try
        {
            Console.WriteLine($" {Path.GetFileName(file)}:");
            
            var code = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();
            
            var fileAnalysis = PerformBasicAnalysis(root);
            
            // Accumulate totals
            totalAnalysis.ClassCount += fileAnalysis.ClassCount;
            totalAnalysis.MethodCount += fileAnalysis.MethodCount;
            totalAnalysis.PropertyCount += fileAnalysis.PropertyCount;
            
            Console.WriteLine($"   Classes: {fileAnalysis.ClassCount}, Methods: {fileAnalysis.MethodCount}, Properties: {fileAnalysis.PropertyCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Error analyzing {file}: {ex.Message}");
        }
    }
    
    Console.WriteLine($"\n Project Summary:");
    Console.WriteLine($"==================");
    Console.WriteLine($"Total Classes: {totalAnalysis.ClassCount}");
    Console.WriteLine($"Total Methods: {totalAnalysis.MethodCount}");
    Console.WriteLine($"Total Properties: {totalAnalysis.PropertyCount}");
    
    // Simple recommendations based on size
    if (totalAnalysis.ClassCount > 20)
    {
        Console.WriteLine($"\n Recommendations:");
        Console.WriteLine($"   - Large codebase detected ({totalAnalysis.ClassCount} classes)");
        Console.WriteLine($"   - Consider phased migration approach");
        Console.WriteLine($"   - Start with core business logic classes");
    }
}
}

// Simple data model for results
public class CodeAnalysisResult
{
    public int ClassCount { get; set; }
    public int MethodCount { get; set; }
    public int PropertyCount { get; set; }
    public int UsingCount { get; set; }
    public List<string> Classes { get; set; } = new();
    public List<string> Methods { get; set; } = new();
    public List<string> UsingStatements { get; set; } = new();
}


public class ReportGenerator
{
    public static void GenerateReport(CodeAnalysisResult analysis, string outputPath = "analysis-report.md")
    {
        var report = new StringBuilder();
        
        report.AppendLine("# Code Analysis Report");
        report.AppendLine($"*Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}*");
        report.AppendLine();
        
        report.AppendLine("## Summary");
        report.AppendLine($"- **Classes**: {analysis.ClassCount}");
        report.AppendLine($"- **Methods**: {analysis.MethodCount}");
        report.AppendLine($"- **Properties**: {analysis.PropertyCount}");
        report.AppendLine($"- **Using Statements**: {analysis.UsingCount}");
        report.AppendLine();
        
        if (analysis.Classes.Any())
        {
            report.AppendLine("## Classes Found");
            foreach (var className in analysis.Classes)
            {
                report.AppendLine($"- `{className}`");
            }
            report.AppendLine();
        }
        
        report.AppendLine("## AI Recommendations (Placeholder)");
        report.AppendLine("- Consider modernizing legacy patterns");
        report.AppendLine("- Add async/await for I/O operations");
        report.AppendLine("- Implement proper error handling");
        report.AppendLine("- Consider dependency injection patterns");
        report.AppendLine();
        
        report.AppendLine("## Next Steps");
        report.AppendLine("1. Set up Azure OpenAI for detailed analysis");
        report.AppendLine("2. Analyze critical business logic classes first");
        report.AppendLine("3. Create migration timeline based on dependencies");
        report.AppendLine("4. Plan testing strategy for migrated code");
        
        File.WriteAllText(outputPath, report.ToString());
        Console.WriteLine($"📄 Report generated: {outputPath}");
    }
}
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AICodeAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AICodeAnalyzer.Services
{
    public class CodeAnalysisService : ICodeAnalysisService
    {
        public async Task<CodeAnalysisResult> AnalyzeFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File {filePath} not found!");
            }
            
            var code = await File.ReadAllTextAsync(filePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var root = syntaxTree.GetRoot();
            
            return PerformBasicAnalysis(root);
        }
        
        public async Task<List<ProjectFileAnalysis>> AnalyzeProjectAsync(string projectPath, int maxFiles = 5)
        {
            var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("/bin/") && !f.Contains("/obj/") && 
                           !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
                .Take(maxFiles)
                .ToList();

            var results = new List<ProjectFileAnalysis>();

            foreach (var file in csFiles)
            {
                try
                {
                    var analysis = await AnalyzeFileAsync(file);
                    results.Add(new ProjectFileAnalysis
                    {
                        FilePath = file,
                        FileName = Path.GetFileName(file),
                        Analysis = analysis,
                        QuickInsight = "" // Will be populated by AI service
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error analyzing {file}: {ex.Message}");
                }
            }

            return results;
        }
        
        public CodeAnalysisResult PerformBasicAnalysis(SyntaxNode root)
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
                UsingStatements = usingDirectives.Select(u => u.Name.ToString()).ToList()
            };
        }
        
        public void DisplayBasicResults(CodeAnalysisResult result)
        {
            Console.WriteLine("📊 Roslyn Static Analysis:");
            Console.WriteLine("===========================");
            Console.WriteLine($"Classes: {result.ClassCount}");
            Console.WriteLine($"Methods: {result.MethodCount}");
            Console.WriteLine($"Properties: {result.PropertyCount}");
            Console.WriteLine($"Using Statements: {result.UsingCount}");
            
            if (result.Classes.Any())
            {
                Console.WriteLine($"\n📝 Key Classes:");
                foreach (var className in result.Classes.Take(3))
                {
                    Console.WriteLine($"   - {className}");
                }
                if (result.Classes.Count > 3)
                    Console.WriteLine($"   ... and {result.Classes.Count - 3} more");
            }
        }
        
        private string GetClassName(SyntaxNode method)
        {
            var classDeclaration = method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            return classDeclaration?.Identifier.ValueText ?? "Unknown";
        }
    }
}
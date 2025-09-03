using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class CodeAnalysisService : ICodeAnalysisService
    {
        public async Task<CodeAnalysisResult> AnalyzeCodeAsync(string code)
        {
            return await Task.Run(() =>
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var root = syntaxTree.GetRoot();
                return PerformBasicAnalysis(root);
            });
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
                UsingStatements = usingDirectives.Select(u => u.Name?.ToString() ?? "").ToList()
            };
        }

        private string GetClassName(SyntaxNode method)
        {
            var classDeclaration = method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            return classDeclaration?.Identifier.ValueText ?? "Unknown";
        }
    }
}

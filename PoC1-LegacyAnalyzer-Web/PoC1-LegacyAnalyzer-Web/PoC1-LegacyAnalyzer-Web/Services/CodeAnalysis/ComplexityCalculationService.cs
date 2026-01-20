using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PoC1_LegacyAnalyzer_Web.Services.CodeAnalysis
{
    /// <summary>
    /// Service for calculating code complexity metrics using local static analysis.
    /// </summary>
    public class ComplexityCalculationService : IComplexityCalculationService
    {
        public ComplexityMetrics CalculateComplexity(string code, string language)
        {
            if (!string.Equals(language, "csharp", StringComparison.OrdinalIgnoreCase))
            {
                // Lightweight fallback
                var loc = code.Split('\n').Length;
                return new ComplexityMetrics
                {
                    CyclomaticComplexity = Math.Max(1, Regex.Matches(code, @"\b(if|for|foreach|while|case|catch|&&|\|\|)\b").Count + 1),
                    LinesOfCode = loc
                };
            }

            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            var classCount = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Count();
            var methodNodes = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
            var propertyCount = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().Count();

            var complexity = 0;
            foreach (var method in methodNodes)
            {
                var text = method.Body?.ToString() ?? method.ExpressionBody?.ToString() ?? string.Empty;
                var c = Regex.Matches(text, @"\b(if|for|foreach|while|case|catch|\?\:|&&|\|\|)\b").Count + 1;
                complexity += c;
            }

            return new ComplexityMetrics
            {
                CyclomaticComplexity = Math.Max(1, complexity),
                LinesOfCode = code.Split('\n').Length,
                ClassCount = classCount,
                MethodCount = methodNodes.Count,
                PropertyCount = propertyCount
            };
        }
    }
}


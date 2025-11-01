using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class FilePreProcessingService : IFilePreProcessingService
    {
        public async Task<FileMetadata> ExtractMetadataAsync(IBrowserFile file, string languageHint = "csharp")
        {
            var metadata = new FileMetadata
            {
                FileName = file.Name,
                FileSize = file.Size,
                Language = languageHint
            };

            try
            {
                using var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
                using var reader = new StreamReader(stream, Encoding.UTF8, true, 8192, leaveOpen: false);
                var code = await reader.ReadToEndAsync();

                if (languageHint.Equals("csharp", StringComparison.OrdinalIgnoreCase))
                {
                    PopulateCSharpMetadata(code, metadata);
                }
                else
                {
                    // Fallback lightweight parsing
                    metadata.Patterns = DetectPatterns(code, languageHint);
                    metadata.Complexity = CalculateComplexity(code, languageHint);
                }

                metadata.PatternSummary = BuildPatternSummary(metadata);
            }
            catch (Exception ex)
            {
                metadata.Status = "Error";
                metadata.ErrorMessage = ex.Message;
            }

            return metadata;
        }

        public async Task<ProjectSummary> CreateProjectSummaryAsync(List<FileMetadata> fileMetadatas)
        {
            var summary = new ProjectSummary
            {
                TotalFiles = fileMetadatas.Count,
                TotalClasses = fileMetadatas.Sum(f => f.Complexity.ClassCount),
                TotalMethods = fileMetadatas.Sum(f => f.Complexity.MethodCount),
                TotalProperties = fileMetadatas.Sum(f => f.Complexity.PropertyCount),
                ComplexityScore = fileMetadatas.Sum(f => f.Complexity.CyclomaticComplexity)
            };

            summary.FileResults = fileMetadatas.Select(m => new FileAnalysisResult
            {
                FileName = m.FileName,
                FileSize = m.FileSize,
                ComplexityScore = m.Complexity.CyclomaticComplexity,
                StaticAnalysis = new CodeAnalysisResult
                {
                    ClassCount = m.Complexity.ClassCount,
                    MethodCount = m.Complexity.MethodCount,
                    PropertyCount = m.Complexity.PropertyCount,
                    UsingCount = m.UsingDirectives.Count,
                    Classes = m.ClassSignatures.ToList(),
                    Methods = m.MethodSignatures.ToList(),
                    UsingStatements = m.UsingDirectives.ToList()
                },
                AIInsight = string.Join("; ", m.Patterns.SecurityFindings.Concat(m.Patterns.PerformanceFindings).Concat(m.Patterns.ArchitectureFindings))
            }).ToList();

            // Simple overall assessment
            var securityCount = fileMetadatas.Sum(m => m.Patterns.SecurityFindings.Count);
            var perfCount = fileMetadatas.Sum(m => m.Patterns.PerformanceFindings.Count);
            var archCount = fileMetadatas.Sum(m => m.Patterns.ArchitectureFindings.Count);

            summary.RiskLevel = securityCount > 0 ? "High" : perfCount + archCount > 3 ? "Medium" : "Low";
            summary.OverallAssessment = $"Security:{securityCount}, Performance:{perfCount}, Architecture:{archCount}";

            summary.KeyRecommendations = new List<string>();
            if (securityCount > 0) summary.KeyRecommendations.Add("Address security risks identified in preprocessing.");
            if (perfCount > 0) summary.KeyRecommendations.Add("Optimize performance hotspots identified in preprocessing.");
            if (archCount > 0) summary.KeyRecommendations.Add("Refactor architecture anti-patterns identified in preprocessing.");

            await Task.CompletedTask;
            return summary;
        }

        public CodePatternAnalysis DetectPatterns(string code, string language)
        {
            var analysis = new CodePatternAnalysis();

            if (!string.Equals(language, "csharp", StringComparison.OrdinalIgnoreCase))
            {
                return analysis;
            }

            if (Regex.IsMatch(code, @"SELECT\s+.*\+\s*", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("Potential SQL injection via string concatenation");
            }
            if (Regex.IsMatch(code, @"(Password|PWD)\s*=\s*""[^""]*""", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("Hardcoded credentials detected");
            }
            if (Regex.IsMatch(code, @"new\s+SqlConnection\s*\(", RegexOptions.IgnoreCase))
            {
                analysis.ArchitectureFindings.Add("Tight coupling to SQL connection inside business logic");
            }
            if (Regex.IsMatch(code, @"Task\.Delay\(|Thread\.Sleep\(", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("Blocking or artificial delays present");
            }
            if (Regex.IsMatch(code, @"\.Result|\.Wait\(\)", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("Synchronous wait on async calls");
            }

            return analysis;
        }

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

        public List<string> GetSupportedLanguages()
        {
            return new List<string> { "csharp" };
        }

        private static void PopulateCSharpMetadata(string code, FileMetadata metadata)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            metadata.UsingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
                .Select(u => u.ToString())
                .Distinct()
                .ToList();

            metadata.Namespaces = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>()
                .Select(n => n.Name.ToString())
                .Distinct()
                .ToList();

            var classDecls = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
            foreach (var cls in classDecls)
            {
                var modifiers = string.Join(" ", cls.Modifiers.Select(m => m.Text));
                metadata.ClassSignatures.Add($"{modifiers} class {cls.Identifier}");

                foreach (var method in cls.Members.OfType<MethodDeclarationSyntax>())
                {
                    var mmods = string.Join(" ", method.Modifiers.Select(m => m.Text));
                    var parameters = string.Join(", ", method.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"));
                    metadata.MethodSignatures.Add($"{mmods} {method.ReturnType} {cls.Identifier}.{method.Identifier}({parameters})");
                }

                foreach (var prop in cls.Members.OfType<PropertyDeclarationSyntax>())
                {
                    var pmods = string.Join(" ", prop.Modifiers.Select(m => m.Text));
                    metadata.PropertySignatures.Add($"{pmods} {prop.Type} {cls.Identifier}.{prop.Identifier}");
                }
            }

            metadata.Patterns = new FilePreProcessingService().DetectPatterns(code, "csharp");
            metadata.Complexity = new FilePreProcessingService().CalculateComplexity(code, "csharp");
        }

        private static string BuildPatternSummary(FileMetadata m)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"File: {m.FileName} ({m.FileSize} bytes)");
            if (m.UsingDirectives.Count > 0)
                sb.AppendLine($"Usings: {string.Join(", ", m.UsingDirectives.Take(8))}{(m.UsingDirectives.Count > 8 ? ", ..." : string.Empty)}");
            if (m.Namespaces.Count > 0)
                sb.AppendLine($"Namespaces: {string.Join(", ", m.Namespaces.Take(5))}{(m.Namespaces.Count > 5 ? ", ..." : string.Empty)}");

            if (m.ClassSignatures.Count > 0)
                sb.AppendLine($"Classes: {string.Join(" | ", m.ClassSignatures.Take(6))}{(m.ClassSignatures.Count > 6 ? " | ..." : string.Empty)}");

            if (m.MethodSignatures.Count > 0)
                sb.AppendLine($"Methods: {string.Join(" | ", m.MethodSignatures.Take(10))}{(m.MethodSignatures.Count > 10 ? " | ..." : string.Empty)}");

            var findings = new List<string>();
            findings.AddRange(m.Patterns.SecurityFindings);
            findings.AddRange(m.Patterns.PerformanceFindings);
            findings.AddRange(m.Patterns.ArchitectureFindings);
            if (findings.Count > 0)
                sb.AppendLine($"Risks: {string.Join("; ", findings.Take(6))}{(findings.Count > 6 ? "; ..." : string.Empty)}");

            sb.AppendLine($"Complexity: CC={m.Complexity.CyclomaticComplexity}, LOC={m.Complexity.LinesOfCode}, Classes={m.Complexity.ClassCount}, Methods={m.Complexity.MethodCount}");
            return sb.ToString();
        }
    }
}


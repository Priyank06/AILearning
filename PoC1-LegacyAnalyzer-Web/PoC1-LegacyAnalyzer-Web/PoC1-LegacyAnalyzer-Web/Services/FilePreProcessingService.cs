using Microsoft.AspNetCore.Components.Forms;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using System.Text;
using System.Text.RegularExpressions;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Pre-processing service that extracts metadata and patterns from code files
    /// WITHOUT using AI, reducing token costs by 75-80%
    /// </summary>
    public class FilePreProcessingService : IFilePreProcessingService
    {
        private readonly ILogger<FilePreProcessingService> _logger;
        private readonly Dictionary<string, ILanguageAnalyzer> _languageAnalyzers = new();

        public FilePreProcessingService(ILogger<FilePreProcessingService> logger)
        {
            _logger = logger;
            _languageAnalyzers["csharp"] = new CSharpAnalyzer();
        }

        public async Task<FileMetadata> ExtractMetadataAsync(IBrowserFile file, string languageHint = "csharp")
        {
            try
            {
                _logger.LogInformation($"Pre-processing file: {file.Name} ({file.Size} bytes)");

                // Read file content
                using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10MB limit
                using var reader = new StreamReader(stream);
                var code = await reader.ReadToEndAsync();

                var metadata = new FileMetadata
                {
                    FileName = file.Name,
                    Language = languageHint.ToLower(),
                    FileSize = file.Size,
                    LineCount = code.Split('\n').Length,
                    NonEmptyLineCount = code.Split('\n').Count(line => !string.IsNullOrWhiteSpace(line)),
                    CommentLineCount = CountCommentLines(code, languageHint)
                };

                // Use language-specific analyzer
                if (_languageAnalyzers.TryGetValue(languageHint.ToLower(), out var analyzer))
                {
                    await analyzer.AnalyzeAsync(code, metadata);
                }
                else
                {
                    _logger.LogWarning($"No analyzer found for language: {languageHint}. Using fallback.");
                    await PerformBasicAnalysisAsync(code, metadata);
                }

                // Detect patterns
                metadata.PatternAnalysis = DetectPatterns(code, languageHint);

                // Calculate complexity
                metadata.Complexity = CalculateComplexity(code, languageHint);

                // Create compact summary for AI
                metadata.CompactSummary = CreateCompactSummary(metadata, code);

                _logger.LogInformation($"Pre-processing complete: {metadata.Classes.Count} classes, " +
                    $"{metadata.Methods.Count} methods, complexity: {metadata.Complexity.ComplexityLevel}");

                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error pre-processing file: {file.Name}");
                return new FileMetadata
                {
                    FileName = file.Name,
                    Language = languageHint,
                    FileSize = file.Size,
                    CompactSummary = $"Error processing file: {ex.Message}"
                };
            }
        }

        public async Task<ProjectSummary> CreateProjectSummaryAsync(List<FileMetadata> fileMetadatas)
        {
            var summary = new ProjectSummary
            {
                TotalFiles = fileMetadatas.Count,
                TotalLines = fileMetadatas.Sum(f => f.LineCount),
                TotalClasses = fileMetadatas.Sum(f => f.Classes.Count),
                TotalMethods = fileMetadatas.Sum(f => f.Methods.Count),
                AverageComplexityScore = fileMetadatas.Any() ? fileMetadatas.Average(f => f.Complexity.CyclomaticComplexity) : 0,
                FileSummaries = fileMetadatas
            };

            // Find top complex files
            summary.TopComplexFiles = fileMetadatas
                .OrderByDescending(f => f.Complexity.CyclomaticComplexity)
                .Take(5)
                .Select(f => $"{f.FileName} (complexity: {f.Complexity.CyclomaticComplexity})")
                .ToList();

            // Find common dependencies
            var dependencyCounts = fileMetadatas
                .SelectMany(f => f.Dependencies)
                .GroupBy(d => d)
                .OrderByDescending(g => g.Count())
                .Take(10);

            summary.CommonDependencies = dependencyCounts
                .Select(g => $"{g.Key} (used in {g.Count()} files)")
                .ToList();

            // Aggregate pre-identified issues
            summary.PreIdentifiedIssues = AggregateIssues(fileMetadatas);

            summary.OverallComplexity = CalculateOverallComplexity(summary.AverageComplexityScore, fileMetadatas);

            // Create structured summary for AI
            summary.StructuredSummary = CreateStructuredSummary(summary, fileMetadatas);

            return summary;
        }        

        public CodePatternAnalysis DetectPatterns(string code, string language)
        {
            var analysis = new CodePatternAnalysis();

            if (language.ToLower() == "csharp")
            {
                // Design patterns
                analysis.DesignPatterns = DetectDesignPatterns(code);

                // Anti-patterns
                analysis.AntiPatterns = DetectAntiPatterns(code);

                // Code smells
                analysis.CodeSmells = DetectCodeSmells(code);

                // Security indicators
                analysis.HasSqlConcatenation = DetectSqlConcatenation(code);
                analysis.HasHardcodedCredentials = DetectHardcodedCredentials(code);
                analysis.HasDeprecatedCryptoAlgorithms = DetectDeprecatedCrypto(code);

                // Performance indicators
                analysis.HasSynchronousIO = DetectSynchronousIO(code);
                analysis.HasPotentialMemoryLeaks = DetectMemoryLeaks(code);
                analysis.HasIneffcientLoops = DetectInefficientLoops(code);

                // Modernization indicators
                analysis.UsesDeprecatedApis = DetectDeprecatedApis(code);
                analysis.MissingAsyncAwait = DetectMissingAsync(code);
                analysis.HasMagicNumbers = DetectMagicNumbers(code);
            }

            return analysis;
        }

        public ComplexityMetrics CalculateComplexity(string code, string language)
        {
            var metrics = new ComplexityMetrics();

            if (language.ToLower() == "csharp")
            {
                try
                {
                    var tree = CSharpSyntaxTree.ParseText(code);
                    var root = tree.GetRoot();

                    // Cyclomatic complexity
                    metrics.CyclomaticComplexity = CalculateCyclomaticComplexity(root);

                    // Max method lines
                    var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
                    metrics.MaxMethodLines = methods.Any() ? methods.Max(m => m.GetText().Lines.Count) : 0;

                    // Max parameter count
                    metrics.MaxParameterCount = methods.Any() ? methods.Max(m => m.ParameterList.Parameters.Count) : 0;

                    // Nesting depth
                    metrics.NestingDepth = CalculateMaxNestingDepth(root);

                    // Determine complexity level
                    metrics.ComplexityLevel = DetermineComplexityLevel(metrics);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Error calculating complexity: {ex.Message}");
                    metrics.ComplexityLevel = "Unknown";
                }
            }

            return metrics;
        }

        private string CalculateOverallComplexity(double averageComplexity, List<FileMetadata> fileMetadatas)
        {
            // Calculate complexity based on multiple factors
            var maxComplexity = fileMetadatas.Any() ? fileMetadatas.Max(f => f.Complexity.CyclomaticComplexity) : 0;
            var highComplexityFiles = fileMetadatas.Count(f => f.Complexity.ComplexityLevel == "High" || f.Complexity.ComplexityLevel == "Very High");
            var highComplexityPercentage = fileMetadatas.Any() ? (double)highComplexityFiles / fileMetadatas.Count * 100 : 0;

            // Determine overall level based on average and distribution
            if (averageComplexity > 50 || maxComplexity > 100 || highComplexityPercentage > 50)
                return "Very High";
            if (averageComplexity > 30 || maxComplexity > 50 || highComplexityPercentage > 30)
                return "High";
            if (averageComplexity > 15 || maxComplexity > 25 || highComplexityPercentage > 15)
                return "Medium";
            return "Low";
        }

        public List<string> GetSupportedLanguages()
        {
            return _languageAnalyzers.Keys.ToList();
        }

        #region Private Helper Methods

        private int CountCommentLines(string code, string language)
        {
            if (language.ToLower() == "csharp")
            {
                var singleLineComments = Regex.Matches(code, @"//.*$", RegexOptions.Multiline).Count;
                var multiLineComments = Regex.Matches(code, @"/\*[\s\S]*?\*/").Count;
                return singleLineComments + multiLineComments;
            }
            return 0;
        }

        private async Task PerformBasicAnalysisAsync(string code, FileMetadata metadata)
        {
            // Basic fallback analysis for unsupported languages
            metadata.Classes = Regex.Matches(code, @"class\s+(\w+)")
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .ToList();

            metadata.Methods = Regex.Matches(code, @"(?:public|private|protected)\s+\w+\s+(\w+)\s*\(")
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .ToList();

            await Task.CompletedTask;
        }

        private string CreateCompactSummary(FileMetadata metadata, string code)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"File: {metadata.FileName}");
            sb.AppendLine($"Lines: {metadata.LineCount} ({metadata.NonEmptyLineCount} code, {metadata.CommentLineCount} comments)");

            if (metadata.Classes.Any())
                sb.AppendLine($"Classes: {string.Join(", ", metadata.Classes.Take(3))}{(metadata.Classes.Count > 3 ? "..." : "")}");

            if (metadata.Methods.Any())
                sb.AppendLine($"Methods: {metadata.Methods.Count} total");

            sb.AppendLine($"Complexity: {metadata.Complexity.ComplexityLevel} (cyclomatic: {metadata.Complexity.CyclomaticComplexity})");

            if (metadata.PatternAnalysis.DesignPatterns.Any())
                sb.AppendLine($"Patterns: {string.Join(", ", metadata.PatternAnalysis.DesignPatterns)}");

            if (metadata.PatternAnalysis.CodeSmells.Any())
                sb.AppendLine($"Code Smells: {metadata.PatternAnalysis.CodeSmells.Count} detected");

            return sb.ToString();
        }

        private string CreateStructuredSummary(ProjectSummary summary, List<FileMetadata> fileMetadatas)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"PROJECT SUMMARY:");
            sb.AppendLine($"Files: {summary.TotalFiles}, Lines: {summary.TotalLines:N0}");
            sb.AppendLine($"Classes: {summary.TotalClasses}, Methods: {summary.TotalMethods}");
            sb.AppendLine($"Average Complexity: {summary.AverageComplexityScore:F1}");
            sb.AppendLine();

            if (summary.PreIdentifiedIssues.Any())
            {
                sb.AppendLine($"PRE-IDENTIFIED ISSUES ({summary.PreIdentifiedIssues.Count} total):");
                var criticalIssues = summary.PreIdentifiedIssues.Where(i => i.Severity == "Critical").ToList();
                var highIssues = summary.PreIdentifiedIssues.Where(i => i.Severity == "High").ToList();

                if (criticalIssues.Any())
                    sb.AppendLine($"  - Critical: {criticalIssues.Count} ({string.Join(", ", criticalIssues.Select(i => i.IssueType).Distinct())})");

                if (highIssues.Any())
                    sb.AppendLine($"  - High: {highIssues.Count} ({string.Join(", ", highIssues.Select(i => i.IssueType).Distinct())})");

                sb.AppendLine();
            }

            if (summary.TopComplexFiles.Any())
            {
                sb.AppendLine("MOST COMPLEX FILES:");
                foreach (var file in summary.TopComplexFiles.Take(3))
                    sb.AppendLine($"  - {file}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private int CalculateCyclomaticComplexity(SyntaxNode root)
        {
            int complexity = 1; // Base complexity

            // Count decision points
            complexity += root.DescendantNodes().OfType<IfStatementSyntax>().Count();
            complexity += root.DescendantNodes().OfType<WhileStatementSyntax>().Count();
            complexity += root.DescendantNodes().OfType<ForStatementSyntax>().Count();
            complexity += root.DescendantNodes().OfType<ForEachStatementSyntax>().Count();
            complexity += root.DescendantNodes().OfType<SwitchStatementSyntax>().Count();
            complexity += root.DescendantNodes().OfType<CaseSwitchLabelSyntax>().Count();
            complexity += root.DescendantNodes().OfType<CatchClauseSyntax>().Count();

            // Count logical operators
            var binaryExpressions = root.DescendantNodes().OfType<BinaryExpressionSyntax>();
            complexity += binaryExpressions.Count(be =>
                be.IsKind(SyntaxKind.LogicalAndExpression) ||
                be.IsKind(SyntaxKind.LogicalOrExpression));

            return complexity;
        }

        private int CalculateMaxNestingDepth(SyntaxNode root)
        {
            return root.DescendantNodes()
                .Max(node => node.Ancestors().Count(a =>
                    a is IfStatementSyntax ||
                    a is WhileStatementSyntax ||
                    a is ForStatementSyntax ||
                    a is ForEachStatementSyntax));
        }

        private string DetermineComplexityLevel(ComplexityMetrics metrics)
        {
            if (metrics.CyclomaticComplexity > 50 || metrics.MaxMethodLines > 300 || metrics.NestingDepth > 6)
                return "Very High";
            if (metrics.CyclomaticComplexity > 20 || metrics.MaxMethodLines > 150 || metrics.NestingDepth > 4)
                return "High";
            if (metrics.CyclomaticComplexity > 10 || metrics.MaxMethodLines > 75 || metrics.NestingDepth > 3)
                return "Medium";
            return "Low";
        }

        #endregion

        #region Security Detection

        private bool DetectSqlConcatenation(string code)
        {
            var patterns = new[]
            {
                @"(?:ExecuteQuery|ExecuteNonQuery|ExecuteReader)\s*\(\s*[""'].*?\+",
                @"[""']SELECT\s+.*?\+.*?FROM",
                @"CommandText\s*=\s*[""'].*?\+"
            };

            return patterns.Any(pattern => Regex.IsMatch(code, pattern, RegexOptions.IgnoreCase));
        }

        private bool DetectHardcodedCredentials(string code)
        {
            var patterns = new[]
            {
                @"(?:password|pwd|passwd)\s*=\s*[""'][^""']{3,}[""']",
                @"(?:apikey|api_key|secret)\s*=\s*[""'][^""']{10,}[""']",
                @"ConnectionString\s*=\s*[""'].*?password=[^;]+[""']"
            };

            return patterns.Any(pattern => Regex.IsMatch(code, pattern, RegexOptions.IgnoreCase));
        }

        private bool DetectDeprecatedCrypto(string code)
        {
            var deprecatedAlgorithms = new[] { "MD5", "SHA1", "DES", "TripleDES", "RC2" };
            return deprecatedAlgorithms.Any(algo => code.Contains(algo));
        }

        #endregion

        #region Performance Detection

        private bool DetectSynchronousIO(string code)
        {
            var patterns = new[]
            {
                @"File\.ReadAllText\(",
                @"File\.WriteAllText\(",
                @"StreamReader\.ReadToEnd\(",
                @"HttpClient\..*(?<!Async)\(",
                @"\.Result\b",
                @"\.Wait\(\)"
            };

            return patterns.Any(pattern => Regex.IsMatch(code, pattern));
        }

        private bool DetectMemoryLeaks(string code)
        {
            var hasDisposable = Regex.IsMatch(code, @":\s*IDisposable");
            var hasUsing = Regex.IsMatch(code, @"\busing\s*\(");
            return hasDisposable && !hasUsing;
        }

        private bool DetectInefficientLoops(string code)
        {
            // Detect nested loops with potential N+1 queries
            var nestedLoops = Regex.Matches(code, @"for(?:each)?\s*\([^)]+\)\s*\{[^}]*for(?:each)?\s*\(");
            return nestedLoops.Count > 2;
        }

        #endregion

        #region Modernization Detection

        private bool DetectDeprecatedApis(string code)
        {
            var deprecatedApis = new[]
            {
                "WebClient", "HttpWebRequest", "BinaryFormatter",
                "AppDomain.CurrentDomain.SetData", "Thread.Suspend"
            };

            return deprecatedApis.Any(api => code.Contains(api));
        }

        private bool DetectMissingAsync(string code)
        {
            var hasIOOperations = Regex.IsMatch(code, @"File\.|Stream\.|HttpClient\.");
            var hasAsyncMethods = Regex.IsMatch(code, @"\basync\s+Task");
            return hasIOOperations && !hasAsyncMethods;
        }

        private bool DetectMagicNumbers(string code)
        {
            var pattern = @"\b(?<![\w\.])\d{2,}(?![\w\.\:])\b(?![eE][+-]?\d)(?![xX][0-9a-fA-F])\b(?![0-9])\b";
            var matches = Regex.Matches(code, pattern);
            return matches.Count > 10;
        }

        private bool DetectEmptyCatchBlocks(string code)
        {
            var pattern = @"catch\s*\([^)]*\)\s*\{\s*\}";
            return Regex.IsMatch(code, pattern);
        }

        private bool DetectTodoComments(string code)
        {
            var pattern = @"//\s*(TODO|FIXME|HACK|XXX):";
            return Regex.IsMatch(code, pattern, RegexOptions.IgnoreCase);
        }

        #endregion

        #region Architecture Pattern Detection

        private List<string> DetectDesignPatterns(string code)
        {
            var patterns = new List<string>();

            if (Regex.IsMatch(code, @"class\s+\w+Factory\b", RegexOptions.IgnoreCase))
                patterns.Add("Factory Pattern");

            if (Regex.IsMatch(code, @"class\s+\w+Singleton\b", RegexOptions.IgnoreCase))
                patterns.Add("Singleton Pattern");

            if (Regex.IsMatch(code, @"interface\s+I\w+Observer\b"))
                patterns.Add("Observer Pattern");

            if (Regex.IsMatch(code, @"class\s+\w+Builder\b"))
                patterns.Add("Builder Pattern");

            if (Regex.IsMatch(code, @"interface\s+I\w+Strategy\b"))
                patterns.Add("Strategy Pattern");

            return patterns;
        }

        private List<string> DetectAntiPatterns(string code)
        {
            var antiPatterns = new List<string>();

            if (Regex.IsMatch(code, @"class\s+\w*God\w*Class", RegexOptions.IgnoreCase))
                antiPatterns.Add("God Class");

            if (Regex.IsMatch(code, @"public\s+static\s+\w+\s+\w+\s*;", RegexOptions.Multiline))
                antiPatterns.Add("Global State");

            if (DetectEmptyCatchBlocks(code))
                antiPatterns.Add("Swallowing Exceptions");

            return antiPatterns;
        }

        private List<CodeSmell> DetectCodeSmells(string code)
        {
            var smells = new List<CodeSmell>();

            // Long parameter lists
            var longParamMatches = Regex.Matches(code, @"(?<method>\w+)\s*\((?<params>[^)]{100,})\)");
            foreach (Match match in longParamMatches)
            {
                smells.Add(new CodeSmell
                {
                    Type = "Long Parameter List",
                    Description = $"Method '{match.Groups["method"].Value}' has an excessive number of parameters",
                    Severity = "Medium",
                    Location = $"Method: {match.Groups["method"].Value}"
                });
            }

            // Large classes
            var classMatches = Regex.Matches(code, @"class\s+(\w+)\s*(?:\:\s*[\w,\s]+)?\s*\{([^}]{5000,})\}");
            foreach (Match match in classMatches)
            {
                smells.Add(new CodeSmell
                {
                    Type = "Large Class",
                    Description = $"Class '{match.Groups[1].Value}' is excessively large (potential God Object)",
                    Severity = "High",
                    Location = $"Class: {match.Groups[1].Value}"
                });
            }

            // TODO comments
            if (DetectTodoComments(code))
            {
                smells.Add(new CodeSmell
                {
                    Type = "Incomplete Implementation",
                    Description = "Code contains TODO/FIXME comments indicating unfinished work",
                    Severity = "Low"
                });
            }

            return smells;
        }

        private List<CodeIssue> AggregateIssues(List<FileMetadata> fileMetadatas)
        {
            var issues = new List<CodeIssue>();

            foreach (var file in fileMetadatas)
            {
                var patterns = file.PatternAnalysis;

                // Security issues
                if (patterns.HasSqlConcatenation)
                {
                    issues.Add(new CodeIssue
                    {
                        IssueType = "Security",
                        Severity = "Critical",
                        Title = "SQL Injection Risk",
                        Description = "String concatenation used in SQL queries",
                        FileName = file.FileName,
                        Recommendation = "Use parameterized queries or ORM"
                    });
                }

                if (patterns.HasHardcodedCredentials)
                {
                    issues.Add(new CodeIssue
                    {
                        IssueType = "Security",
                        Severity = "Critical",
                        Title = "Hardcoded Credentials",
                        Description = "Passwords or API keys found in source code",
                        FileName = file.FileName,
                        Recommendation = "Move to secure configuration storage"
                    });
                }

                if (patterns.HasDeprecatedCryptoAlgorithms)
                {
                    issues.Add(new CodeIssue
                    {
                        IssueType = "Security",
                        Severity = "High",
                        Title = "Weak Cryptography",
                        Description = "Uses deprecated cryptographic algorithms",
                        FileName = file.FileName,
                        Recommendation = "Upgrade to modern algorithms (SHA256, AES)"
                    });
                }

                // Performance issues
                if (patterns.HasSynchronousIO)
                {
                    issues.Add(new CodeIssue
                    {
                        IssueType = "Performance",
                        Severity = "Medium",
                        Title = "Blocking I/O Operations",
                        Description = "Synchronous file/network operations block threads",
                        FileName = file.FileName,
                        Recommendation = "Convert to async/await pattern"
                    });
                }

                if (patterns.HasPotentialMemoryLeaks)
                {
                    issues.Add(new CodeIssue
                    {
                        IssueType = "Performance",
                        Severity = "High",
                        Title = "Potential Memory Leak",
                        Description = "IDisposable objects not properly disposed",
                        FileName = file.FileName,
                        Recommendation = "Implement using statements or Dispose pattern"
                    });
                }

                // Complexity issues
                if (file.Complexity.ComplexityLevel == "Very High")
                {
                    issues.Add(new CodeIssue
                    {
                        IssueType = "Maintainability",
                        Severity = "High",
                        Title = "High Complexity",
                        Description = $"File has very high cyclomatic complexity ({file.Complexity.CyclomaticComplexity})",
                        FileName = file.FileName,
                        Recommendation = "Refactor into smaller methods"
                    });
                }

                if (patterns.UsesDeprecatedApis)
                {
                    issues.Add(new CodeIssue
                    {
                        IssueType = "Modernization",
                        Severity = "Medium",
                        Title = "Deprecated APIs",
                        Description = "Uses deprecated .NET APIs",
                        FileName = file.FileName,
                        Recommendation = "Migrate to modern alternatives"
                    });
                }
            }

            return issues.OrderByDescending(i => i.Severity).ToList();
        }

        #endregion
    }

    #region Language Analyzer Interface

    public interface ILanguageAnalyzer
    {
        Task AnalyzeAsync(string code, FileMetadata metadata);
    }

    public class CSharpAnalyzer : ILanguageAnalyzer
    {
        public async Task AnalyzeAsync(string code, FileMetadata metadata)
        {
            try
            {
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetRoot();

                metadata.Classes = root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Select(c => c.Identifier.ValueText)
                    .ToList();

                metadata.Interfaces = root.DescendantNodes()
                    .OfType<InterfaceDeclarationSyntax>()
                    .Select(i => i.Identifier.ValueText)
                    .ToList();

                metadata.Methods = root.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Select(m =>
                    {
                        var className = GetClassName(m);
                        return $"{className}.{m.Identifier.ValueText}";
                    })
                    .ToList();

                metadata.Properties = root.DescendantNodes()
                    .OfType<PropertyDeclarationSyntax>()
                    .Select(p =>
                    {
                        var className = GetClassName(p);
                        return $"{className}.{p.Identifier.ValueText}";
                    })
                    .ToList();

                metadata.Dependencies = root.DescendantNodes()
                    .OfType<UsingDirectiveSyntax>()
                    .Select(u => u.Name?.ToString() ?? "")
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()
                    .ToList();

                await Task.CompletedTask;
            }
            catch (Exception)
            {
                metadata.CompactSummary = "Partial analysis (parse error)";
            }
        }

        private string GetClassName(SyntaxNode node)
        {
            var classDeclaration = node.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            return classDeclaration?.Identifier.ValueText ?? "Unknown";
        }
    }

    #endregion
}
using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Service for detecting legacy-specific anti-patterns: God Objects, Magic Numbers,
    /// Cyclic Dependencies, Dead Code, Obsolete APIs, Empty Catch Blocks.
    /// </summary>
    public class LegacyPatternDetectionService : ILegacyPatternDetectionService
    {
        private readonly ILogger<LegacyPatternDetectionService> _logger;
        private readonly LegacyPatternRegistry _registry;

        // Obsolete .NET Framework APIs and patterns
        private static readonly string[] ObsoleteFrameworkPatterns = new[]
        {
            @"System\.Web\.UI\.",
            @"System\.Data\.OleDb\.",
            @"System\.Data\.OracleClient\.",
            @"System\.EnterpriseServices\.",
            @"System\.Runtime\.Remoting\.",
            @"System\.Web\.Services\.",
            @"System\.Web\.HttpContext\.Current",
            @"DataSet\s+",
            @"DataTable\s+",
            @"SqlDataAdapter\s+",
            @"WebRequest\s+",
            @"WebClient\s+",
            @"HttpWebRequest\s+",
            @"HttpWebResponse\s+",
            @"ServiceController\s+",
            @"Process\.Start\s*\(",
            @"Thread\.Abort\s*\(",
            @"Thread\.Suspend\s*\(",
            @"Thread\.Resume\s*\(",
            @"AppDomain\.Unload\s*\(",
            @"Assembly\.LoadFrom\s*\(",
            @"CodeAccessPermission\.",
            @"PrincipalPermission\.",
            @"SecurityPermission\.",
            @"Registry\.(GetValue|SetValue|CreateKey|DeleteKey)",
            @"FileIOPermission\.",
            @"IsolatedStorageFile\.",
            @"SoapFormatter\s+",
            @"BinaryFormatter\s+",
            @"RemotingConfiguration\.",
            @"RemotingServices\.",
            @"ContextBoundObject\s+",
            @"MarshalByRefObject\s+",
            @"\[Obsolete\(",
            @"\[System\.Obsolete"
        };

        // Ancient .NET Framework version indicators
        private static readonly string[] AncientFrameworkIndicators = new[]
        {
            @"using\s+System\.Web\.UI",
            @"using\s+System\.EnterpriseServices",
            @"using\s+System\.Runtime\.Remoting",
            @"using\s+System\.Web\.Services",
            @"using\s+Microsoft\.VisualBasic",
            @"using\s+System\.Data\.OleDb",
            @"using\s+System\.Data\.OracleClient",
            @"\.NET\s+Framework\s+[12]\.",
            @"\.NET\s+Framework\s+3\.[0-4]",
            @"\.NET\s+Framework\s+4\.[0-5]"
        };

        // Global state patterns
        private static readonly string[] GlobalStatePatterns = new[]
        {
            @"public\s+static\s+(class|readonly|const)\s+",
            @"public\s+static\s+\w+\s+\w+\s*\{",
            @"HttpContext\.Current\.",
            @"Application\[",
            @"Session\[",
            @"Cache\[",
            @"static\s+readonly\s+\w+\s+\w+\s*=",
            @"public\s+static\s+\w+\s+\w+\s*=",
            @"Global\.",
            @"AppDomain\.CurrentDomain\.",
            @"ThreadStatic\s+",
            @"\[ThreadStatic\]"
        };

        public LegacyPatternDetectionService(ILogger<LegacyPatternDetectionService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registry = new LegacyPatternRegistry();
        }

        public LegacyPatternResult DetectLegacyPatterns(string code, string language, LegacyContext? context = null)
        {
            var result = new LegacyPatternResult
            {
                Context = context ?? new LegacyContext()
            };

            if (string.IsNullOrWhiteSpace(code))
            {
                _logger.LogWarning("DetectLegacyPatterns called with null or empty code");
                return result;
            }

            var languageKey = language?.ToLower() ?? "unknown";

            // Detect patterns based on language
            if (languageKey == "csharp" || languageKey == "cs")
            {
                DetectCSharpLegacyPatterns(code, result);
            }
            else
            {
                DetectGenericLegacyPatterns(code, result);
            }

            // Get legacy indicators
            if (context != null)
            {
                result.Indicators = GetLegacyIndicators(context);
            }

            _logger.LogDebug("Legacy pattern detection completed: {IssueCount} issues found", result.Issues.Count);
            return result;
        }

        public LegacyIndicators GetLegacyIndicators(LegacyContext context)
        {
            var indicators = new LegacyIndicators();

            // Check if code is very old (based on last modified date)
            if (context.FileLastModified.HasValue)
            {
                var yearsSinceModified = (DateTime.Now - context.FileLastModified.Value).TotalDays / 365.0;
                indicators.IsVeryOldCode = yearsSinceModified > 5;
                indicators.EstimatedFileAgeYears = (int)yearsSinceModified;
            }

            // Check framework version
            if (!string.IsNullOrEmpty(context.FrameworkVersion))
            {
                indicators.FrameworkVersion = context.FrameworkVersion;
                // Check if it's an ancient framework version
                if (Regex.IsMatch(context.FrameworkVersion, @"\.NET\s+Framework\s+[123]\.") ||
                    Regex.IsMatch(context.FrameworkVersion, @"\.NET\s+Framework\s+4\.[0-5]"))
                {
                    indicators.IsAncientFramework = true;
                }
            }

            return indicators;
        }

        private void DetectCSharpLegacyPatterns(string code, LegacyPatternResult result)
        {
            try
            {
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetRoot();
                
                DetectGodObjects(root, code, result);
                DetectMagicNumbers(root, code, result);
                DetectCyclicDependencies(root, code, result);
                DetectDeadCode(root, code, result);
                DetectObsoleteApis(root, code, result);
                DetectEmptyCatchBlocks(root, code, result);
                DetectGlobalState(root, code, result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse C# code with Roslyn, falling back to regex-based detection");
                DetectGenericLegacyPatterns(code, result);
            }
        }

        private void DetectGenericLegacyPatterns(string code, LegacyPatternResult result)
        {
            DetectMagicNumbersRegex(code, result);
            DetectEmptyCatchBlocksRegex(code, result);
            DetectObsoleteApisRegex(code, result);
            DetectGlobalStateRegex(code, result);
        }

        #region God Objects Detection

        private void DetectGodObjects(SyntaxNode root, string code, LegacyPatternResult result)
        {
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();

            foreach (var cls in classes)
            {
                var methods = cls.Members.OfType<MethodDeclarationSyntax>().Count();
                var properties = cls.Members.OfType<PropertyDeclarationSyntax>().Count();
                var fields = cls.Members.OfType<FieldDeclarationSyntax>().Count();
                var totalMembers = methods + properties + fields;

                // God Object: Class with excessive responsibilities (20+ methods OR 30+ total members)
                if (methods >= 20 || totalMembers >= 30)
                {
                    var location = GetLocation(cls);
                    result.Issues.Add(new LegacyIssue
                    {
                        PatternType = "GodObject",
                        Severity = "High",
                        Description = $"God Object detected: '{cls.Identifier}' has {methods} methods and {totalMembers} total members. This class likely violates Single Responsibility Principle.",
                        Location = location,
                        LineNumber = cls.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        Recommendation = "Apply Extract Class refactoring. Split into focused classes with single responsibility. Consider using Facade Pattern for backward compatibility.",
                        CodeSnippet = cls.Identifier.ToString()
                    });
                }
            }
        }

        #endregion

        #region Magic Numbers Detection

        private void DetectMagicNumbers(SyntaxNode root, string code, LegacyPatternResult result)
        {
            var literals = root.DescendantNodes()
                .OfType<LiteralExpressionSyntax>()
                .Where(lit => lit.Kind() == SyntaxKind.NumericLiteralExpression)
                .ToList();

            var magicNumberPatterns = new Dictionary<string, string>
            {
                { "0", "Zero literal (consider using named constant)" },
                { "1", "One literal (consider using named constant)" },
                { "2", "Two literal (consider using named constant)" },
                { "24", "Hours in day (consider using TimeSpan or constant)" },
                { "60", "Seconds/minutes (consider using named constant)" },
                { "100", "Percentage base (consider using named constant)" },
                { "1024", "Bytes in KB (consider using named constant)" },
                { "3600", "Seconds in hour (consider using named constant)" },
                { "86400", "Seconds in day (consider using named constant)" }
            };

            var suspiciousNumbers = new HashSet<int> { 0, 1, 2, 24, 60, 100, 1024, 3600, 86400, 7, 30, 31, 365 };

            foreach (var literal in literals)
            {
                if (int.TryParse(literal.Token.ValueText, out var value))
                {
                    // Skip if it's in a simple assignment or comparison (might be legitimate)
                    var parent = literal.Parent;
                    if (parent is BinaryExpressionSyntax || parent is AssignmentExpressionSyntax)
                    {
                        // Check if it's a suspicious magic number
                        if (suspiciousNumbers.Contains(value) && value > 2)
                        {
                            var location = GetLocation(literal);
                            result.Issues.Add(new LegacyIssue
                            {
                                PatternType = "MagicNumber",
                                Severity = "Medium",
                                Description = $"Magic number detected: {value}. {magicNumberPatterns.GetValueOrDefault(value.ToString(), "Consider using a named constant.")}",
                                Location = location,
                                LineNumber = literal.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                                Recommendation = "Replace magic numbers with named constants or configuration values. This improves readability and maintainability.",
                                CodeSnippet = literal.ToString()
                            });
                        }
                    }
                    else if (value > 100 && value != 1024 && value != 3600 && value != 86400)
                    {
                        // Large numbers that aren't common constants
                        var location = GetLocation(literal);
                        result.Issues.Add(new LegacyIssue
                        {
                            PatternType = "MagicNumber",
                            Severity = "Low",
                            Description = $"Large magic number detected: {value}. Consider using a named constant.",
                            Location = location,
                            LineNumber = literal.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                            Recommendation = "Replace with named constant or configuration value.",
                            CodeSnippet = literal.ToString()
                        });
                    }
                }
            }
        }

        private void DetectMagicNumbersRegex(string code, LegacyPatternResult result)
        {
            // Detect common magic numbers in code
            var magicNumberPattern = new Regex(@"\b(24|60|100|1024|3600|86400|7|30|31|365)\b(?!\s*[=:]\s*['""]|\s*[=:]\s*\w+|\s*[=:]\s*\[)", RegexOptions.IgnoreCase);
            var matches = magicNumberPattern.Matches(code);
            
            foreach (Match match in matches)
            {
                var lineNumber = GetLineNumber(code, match.Index);
                result.Issues.Add(new LegacyIssue
                {
                    PatternType = "MagicNumber",
                    Severity = "Medium",
                    Description = $"Magic number detected: {match.Value}. Consider using a named constant.",
                    Location = $"Line {lineNumber}",
                    LineNumber = lineNumber,
                    Recommendation = "Replace magic numbers with named constants.",
                    CodeSnippet = match.Value
                });
            }
        }

        #endregion

        #region Cyclic Dependencies Detection

        private void DetectCyclicDependencies(SyntaxNode root, string code, LegacyPatternResult result)
        {
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
            var usingDirectives = root.DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(u => u.Name?.ToString() ?? "")
                .Where(u => !string.IsNullOrEmpty(u))
                .ToList();

            // Heuristic: If there are many using statements and many classes, potential for cycles
            if (classes.Count > 3 && usingDirectives.Count > classes.Count * 2)
            {
                // Check for circular references in using statements
                var namespaceGroups = usingDirectives
                    .GroupBy(u => u.Split('.').FirstOrDefault() ?? "")
                    .Where(g => g.Count() > 2)
                    .ToList();

                if (namespaceGroups.Any())
                {
                    result.Issues.Add(new LegacyIssue
                    {
                        PatternType = "CyclicDependency",
                        Severity = "High",
                        Description = $"Potential cyclic dependencies detected: {usingDirectives.Count} using statements across {classes.Count} classes. Multiple namespaces with high coupling.",
                        Location = "File level",
                        Recommendation = "Introduce dependency inversion using interfaces. Break cycles by extracting shared interfaces. Consider using Mediator Pattern or Event Bus.",
                        CodeSnippet = string.Join(", ", namespaceGroups.Select(g => g.Key))
                    });
                }
            }

            // Check for direct circular references in class dependencies
            foreach (var cls in classes)
            {
                var className = cls.Identifier.ToString();
                var classReferences = root.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Where(id => id.Identifier.ToString() == className)
                    .Count();

                // If class references itself many times, might indicate tight coupling
                if (classReferences > 10)
                {
                    var location = GetLocation(cls);
                    result.Issues.Add(new LegacyIssue
                    {
                        PatternType = "CyclicDependency",
                        Severity = "Medium",
                        Description = $"Class '{className}' has high self-references ({classReferences}), indicating potential tight coupling or circular dependencies.",
                        Location = location,
                        LineNumber = cls.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        Recommendation = "Review dependencies and consider breaking into smaller, focused classes.",
                        CodeSnippet = className
                    });
                }
            }
        }

        #endregion

        #region Dead Code Detection

        private void DetectDeadCode(SyntaxNode root, string code, LegacyPatternResult result)
        {
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();

            foreach (var method in methods)
            {
                var methodName = method.Identifier.ToString();
                
                // Check if method is private and never called
                var isPrivate = method.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword));
                
                if (isPrivate)
                {
                    // Count references to this method
                    var references = root.DescendantNodes()
                        .OfType<InvocationExpressionSyntax>()
                        .Where(inv => inv.Expression.ToString().Contains(methodName))
                        .Count();

                    if (references == 0)
                    {
                        var location = GetLocation(method);
                        result.Issues.Add(new LegacyIssue
                        {
                            PatternType = "DeadCode",
                            Severity = "Low",
                            Description = $"Dead code detected: Private method '{methodName}' is never called.",
                            Location = location,
                            LineNumber = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                            Recommendation = "Remove unused methods to reduce maintenance burden. If method might be used via reflection, add [Obsolete] attribute with explanation.",
                            CodeSnippet = methodName
                        });
                    }
                }

                // Check for commented-out code blocks
                var methodBody = method.Body?.ToString() ?? "";
                if (methodBody.Contains("// TODO") || methodBody.Contains("// FIXME") || methodBody.Contains("// HACK"))
                {
                    var location = GetLocation(method);
                    result.Issues.Add(new LegacyIssue
                    {
                        PatternType = "DeadCode",
                        Severity = "Low",
                        Description = $"Method '{methodName}' contains TODO/FIXME/HACK comments, indicating incomplete or temporary code.",
                        Location = location,
                        LineNumber = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        Recommendation = "Review and complete TODO items or remove obsolete code.",
                        CodeSnippet = methodName
                    });
                }
            }
        }

        #endregion

        #region Obsolete APIs Detection

        private void DetectObsoleteApis(SyntaxNode root, string code, LegacyPatternResult result)
        {
            // Check for [Obsolete] attributes
            var obsoleteAttributes = root.DescendantNodes()
                .OfType<AttributeSyntax>()
                .Where(attr => attr.Name.ToString().Contains("Obsolete", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var attr in obsoleteAttributes)
            {
                var location = GetLocation(attr);
                result.Issues.Add(new LegacyIssue
                {
                    PatternType = "ObsoleteApi",
                    Severity = "High",
                    Description = "Obsolete API usage detected. This API is marked as obsolete and may be removed in future versions.",
                    Location = location,
                    LineNumber = attr.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    Recommendation = "Replace with recommended alternative API. Check Microsoft documentation for migration path.",
                    CodeSnippet = attr.ToString()
                });
            }

            // Check for obsolete framework patterns
            DetectObsoleteApisRegex(code, result);
        }

        private void DetectObsoleteApisRegex(string code, LegacyPatternResult result)
        {
            foreach (var pattern in ObsoleteFrameworkPatterns)
            {
                try
                {
                    var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                    var matches = regex.Matches(code);
                    
                    foreach (Match match in matches)
                    {
                        var lineNumber = GetLineNumber(code, match.Index);
                        result.Issues.Add(new LegacyIssue
                        {
                            PatternType = "ObsoleteApi",
                            Severity = "High",
                            Description = $"Obsolete or deprecated API detected: {match.Value}. This API is from an older .NET Framework version and may not be supported in modern .NET.",
                            Location = $"Line {lineNumber}",
                            LineNumber = lineNumber,
                            Recommendation = "Migrate to modern .NET APIs. Check Microsoft migration guides for alternatives.",
                            CodeSnippet = match.Value
                        });
                    }
                }
                catch (RegexParseException ex)
                {
                    _logger.LogWarning(ex, "Invalid regex pattern '{Pattern}' in ObsoleteFrameworkPatterns, skipping", pattern);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error matching pattern '{Pattern}' in ObsoleteFrameworkPatterns, skipping", pattern);
                }
            }
        }

        #endregion

        #region Empty Catch Blocks Detection

        private void DetectEmptyCatchBlocks(SyntaxNode root, string code, LegacyPatternResult result)
        {
            var catchClauses = root.DescendantNodes().OfType<CatchClauseSyntax>().ToList();

            foreach (var catchClause in catchClauses)
            {
                var catchBlock = catchClause.Block;
                if (catchBlock != null)
                {
                    var statements = catchBlock.Statements;
                    
                    // Check if catch block is empty or only has comments
                    var hasOnlyComments = statements.Count == 0 || 
                        statements.All(s => s is EmptyStatementSyntax || 
                                          (s.ToString().Trim().StartsWith("//") || s.ToString().Trim().StartsWith("/*")));

                    if (hasOnlyComments)
                    {
                        var location = GetLocation(catchClause);
                        result.Issues.Add(new LegacyIssue
                        {
                            PatternType = "EmptyCatchBlock",
                            Severity = "High",
                            Description = "Empty catch block detected. Exceptions are being silently swallowed, making debugging difficult.",
                            Location = location,
                            LineNumber = catchClause.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                            Recommendation = "Add logging, rethrow exception, or handle appropriately. At minimum, log the exception for debugging.",
                            CodeSnippet = catchClause.ToString()
                        });
                    }
                }
            }
        }

        private void DetectEmptyCatchBlocksRegex(string code, LegacyPatternResult result)
        {
            // Pattern for empty catch blocks: catch { } or catch (Exception) { }
            var emptyCatchPattern = new Regex(@"catch\s*(?:\([^)]*\))?\s*\{\s*\}", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var matches = emptyCatchPattern.Matches(code);

            foreach (Match match in matches)
            {
                var lineNumber = GetLineNumber(code, match.Index);
                result.Issues.Add(new LegacyIssue
                {
                    PatternType = "EmptyCatchBlock",
                    Severity = "High",
                    Description = "Empty catch block detected. Exceptions are being silently swallowed.",
                    Location = $"Line {lineNumber}",
                    LineNumber = lineNumber,
                    Recommendation = "Add logging or proper exception handling.",
                    CodeSnippet = match.Value
                });
            }
        }

        #endregion

        #region Global State Detection

        private void DetectGlobalState(SyntaxNode root, string code, LegacyPatternResult result)
        {
            // Check for static fields and properties
            var staticFields = root.DescendantNodes()
                .OfType<FieldDeclarationSyntax>()
                .Where(f => f.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)) &&
                           f.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
                .ToList();

            if (staticFields.Count > 3)
            {
                result.Issues.Add(new LegacyIssue
                {
                    PatternType = "GlobalState",
                    Severity = "Medium",
                    Description = $"Global state detected: {staticFields.Count} public static fields found. Global state makes testing difficult and can cause thread-safety issues.",
                    Location = "File level",
                    Recommendation = "Replace static fields with dependency injection. Use singleton pattern with proper lifecycle management if global state is necessary.",
                    CodeSnippet = string.Join(", ", staticFields.Select(f => f.Declaration.Variables.FirstOrDefault()?.Identifier.ToString() ?? ""))
                });
            }

            // Check for HttpContext.Current usage (ASP.NET legacy)
            if (code.Contains("HttpContext.Current"))
            {
                var lineNumber = GetLineNumber(code, code.IndexOf("HttpContext.Current"));
                result.Issues.Add(new LegacyIssue
                {
                    PatternType = "GlobalState",
                    Severity = "High",
                    Description = "HttpContext.Current usage detected. This is a legacy ASP.NET pattern that creates tight coupling and makes testing difficult.",
                    Location = $"Line {lineNumber}",
                    LineNumber = lineNumber,
                    Recommendation = "Use dependency injection to pass HttpContext or IHttpContextAccessor. This improves testability and follows modern ASP.NET Core patterns.",
                    CodeSnippet = "HttpContext.Current"
                });
            }
        }

        private void DetectGlobalStateRegex(string code, LegacyPatternResult result)
        {
            foreach (var pattern in GlobalStatePatterns)
            {
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                var matches = regex.Matches(code);

                foreach (Match match in matches)
                {
                    var lineNumber = GetLineNumber(code, match.Index);
                    result.Issues.Add(new LegacyIssue
                    {
                        PatternType = "GlobalState",
                        Severity = "Medium",
                        Description = $"Global state pattern detected: {match.Value}. This creates tight coupling and testing difficulties.",
                        Location = $"Line {lineNumber}",
                        LineNumber = lineNumber,
                        Recommendation = "Consider using dependency injection instead of global state.",
                        CodeSnippet = match.Value
                    });
                }
            }
        }

        #endregion

        #region Helper Methods

        private string GetLocation(SyntaxNode node)
        {
            var location = node.GetLocation().GetLineSpan();
            var className = node.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier.ToString();
            var methodName = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault()?.Identifier.ToString();

            if (!string.IsNullOrEmpty(className) && !string.IsNullOrEmpty(methodName))
                return $"{className}.{methodName}";
            if (!string.IsNullOrEmpty(className))
                return className;
            
            return $"Line {location.StartLinePosition.Line + 1}";
        }

        private int GetLineNumber(string code, int index)
        {
            if (index < 0 || index >= code.Length)
                return 1;

            var beforeIndex = code.Substring(0, index);
            return beforeIndex.Split(new[] { '\r', '\n' }, StringSplitOptions.None).Length;
        }

        #endregion
    }

    /// <summary>
    /// Registry of legacy patterns for detection.
    /// </summary>
    internal class LegacyPatternRegistry
    {
        // This can be extended with more patterns as needed
        public Dictionary<string, string> PatternDescriptions { get; } = new()
        {
            { "GodObject", "A class that knows too much or does too much, violating Single Responsibility Principle" },
            { "MagicNumber", "Numeric literals used directly in code without named constants" },
            { "CyclicDependency", "Circular dependencies between classes or modules" },
            { "DeadCode", "Unused code that should be removed" },
            { "ObsoleteApi", "Usage of deprecated or obsolete APIs" },
            { "EmptyCatchBlock", "Catch blocks that silently swallow exceptions" },
            { "GlobalState", "Global variables or static state that creates tight coupling" }
        };
    }
}


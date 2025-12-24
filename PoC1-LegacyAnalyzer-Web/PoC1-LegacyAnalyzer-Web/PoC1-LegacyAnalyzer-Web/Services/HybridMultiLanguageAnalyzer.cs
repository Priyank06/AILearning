using PoC1_LegacyAnalyzer_Web.Models;
using PoC1_LegacyAnalyzer_Web.Services.Analysis;
using System.Text.Json;
using System.Text.RegularExpressions;
using TreeSitter;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Hybrid analyzer combining Tree-sitter syntax analysis with AI semantic analysis.
    /// Provides comprehensive analysis for Python, JavaScript, TypeScript, Java, and Go.
    /// </summary>
    public class HybridMultiLanguageAnalyzer : IHybridMultiLanguageAnalyzer
    {
        private readonly ITreeSitterLanguageRegistry _treeSitterRegistry;
        private readonly IAIAnalysisService _aiAnalysisService;
        private readonly ILanguageDetector _languageDetector;
        private readonly ILogger<HybridMultiLanguageAnalyzer> _logger;
        private readonly IConfiguration _configuration;

        public HybridMultiLanguageAnalyzer(
            ITreeSitterLanguageRegistry treeSitterRegistry,
            IAIAnalysisService aiAnalysisService,
            ILanguageDetector languageDetector,
            ILogger<HybridMultiLanguageAnalyzer> logger,
            IConfiguration configuration)
        {
            _treeSitterRegistry = treeSitterRegistry ?? throw new ArgumentNullException(nameof(treeSitterRegistry));
            _aiAnalysisService = aiAnalysisService ?? throw new ArgumentNullException(nameof(aiAnalysisService));
            _languageDetector = languageDetector ?? throw new ArgumentNullException(nameof(languageDetector));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<SemanticAnalysisResult> AnalyzeAsync(
            string code,
            string fileName,
            LanguageKind language,
            string? analysisType = null,
            CancellationToken cancellationToken = default)
        {
            var result = new SemanticAnalysisResult();

            // Step 1: Tree-sitter syntax analysis
            try
            {
                using var parser = _treeSitterRegistry.GetParser(language);
                using var tree = parser.Parse(code);
                var root = tree.RootNode;

                // Build syntax structure
                var structure = BuildSyntaxStructure(code, root, language);
                result.SyntaxAnalysis = BuildSyntaxSummary(structure, language);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tree-sitter syntax analysis failed for {FileName}", fileName);
            }

            // Step 2: AI semantic analysis
            try
            {
                var semanticPrompt = BuildSemanticAnalysisPrompt(code, language, analysisType);
                var aiResponse = await _aiAnalysisService.GetAnalysisAsync(semanticPrompt, analysisType ?? "semantic", null);
                
                // Parse semantic issues from AI response
                result.SemanticIssues = ParseSemanticIssues(aiResponse, language);
                result.DetectedPatterns = await DetectLanguageSpecificPatternsAsync(code, language, cancellationToken);
                
                // Calculate semantic quality score
                result.SemanticQualityScore = CalculateSemanticQualityScore(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI semantic analysis failed for {FileName}", fileName);
            }

            return result;
        }

        public async Task<List<LanguageSpecificPattern>> DetectLanguageSpecificPatternsAsync(
            string code,
            LanguageKind language,
            CancellationToken cancellationToken = default)
        {
            var patterns = new List<LanguageSpecificPattern>();

            switch (language)
            {
                case LanguageKind.Python:
                    patterns.AddRange(DetectPythonPatterns(code));
                    break;
                case LanguageKind.JavaScript:
                case LanguageKind.TypeScript:
                    patterns.AddRange(DetectJavaScriptPatterns(code));
                    break;
                case LanguageKind.Java:
                    patterns.AddRange(DetectJavaPatterns(code));
                    break;
                case LanguageKind.Go:
                    patterns.AddRange(DetectGoPatterns(code));
                    break;
            }

            return await Task.FromResult(patterns);
        }

        private CodeStructure BuildSyntaxStructure(string code, Node root, LanguageKind language)
        {
            var lines = code.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var structure = new CodeStructure
            {
                LanguageKind = language,
                Language = language.ToString().ToLowerInvariant(),
                FileName = "",
                LineCount = lines.Length
            };

            // Extract classes, methods, imports using Tree-sitter
            ExtractSyntaxElements(root, structure);
            return structure;
        }

        private void ExtractSyntaxElements(Node node, CodeStructure structure)
        {
            var type = node.Type;

            // Classes/types
            if (type is "class_definition" or "class_declaration" or "type_declaration")
            {
                var nameNode = node.Children.FirstOrDefault(c => c.Type is "identifier" or "name");
                if (nameNode != null)
                {
                    structure.Classes.Add(new ClassDeclaration
                    {
                        Name = nameNode.Text,
                        LineNumber = node.StartPosition.Row + 1,
                        LineCount = node.EndPosition.Row - node.StartPosition.Row + 1
                    });
                }
            }

            // Functions/methods
            if (type is "function_definition" or "function_declaration" or "method_definition")
            {
                var nameNode = node.Children.FirstOrDefault(c => c.Type is "identifier" or "name");
                if (nameNode != null)
                {
                    structure.Functions.Add(new FunctionDeclaration
                    {
                        Name = nameNode.Text,
                        LineNumber = node.StartPosition.Row + 1,
                        LineCount = node.EndPosition.Row - node.StartPosition.Row + 1
                    });
                }
            }

            // Imports
            if (type.Contains("import") || type.Contains("using") || type.Contains("require"))
            {
                var moduleNode = node.Children.FirstOrDefault(c => c.Type is "string" or "identifier" or "dotted_name");
                if (moduleNode != null)
                {
                    structure.Imports.Add(new ImportDeclaration
                    {
                        ModuleName = moduleNode.Text.Trim('"', '\'', '`')
                    });
                }
            }

            foreach (var child in node.Children)
            {
                ExtractSyntaxElements(child, structure);
            }
        }

        private CodeAnalysisResult BuildSyntaxSummary(CodeStructure structure, LanguageKind language)
        {
            return new CodeAnalysisResult
            {
                LanguageKind = language,
                Language = language.ToString().ToLowerInvariant(),
                ClassCount = structure.Classes.Count,
                MethodCount = structure.Classes.Sum(c => c.Methods.Count) + structure.Functions.Count,
                PropertyCount = structure.Classes.Sum(c => c.Properties.Count),
                UsingCount = structure.Imports.Count,
                Classes = structure.Classes.Select(c => c.Name).ToList(),
                Methods = structure.Classes
                    .SelectMany(c => c.Methods.Select(m => $"{c.Name}.{m.Name}"))
                    .Concat(structure.Functions.Select(f => f.Name))
                    .ToList(),
                UsingStatements = structure.Imports.Select(i => i.ModuleName).ToList()
            };
        }

        private string BuildSemanticAnalysisPrompt(string code, LanguageKind language, string? analysisType)
        {
            var languageSpecificInstructions = GetLanguageSpecificSemanticInstructions(language);
            
            return $@"Perform semantic analysis on the following {language} code.

{languageSpecificInstructions}

Code to analyze:
{code}

Analyze for:
1. Type errors and type safety issues
2. Uninitialized variables
3. Control flow problems (unreachable code, infinite loops, etc.)
4. Language-specific anti-patterns and deprecated patterns
5. Potential runtime errors

Respond in JSON format:
{{
  ""semanticIssues"": [
    {{
      ""issueType"": ""TypeError"" | ""UninitializedVariable"" | ""ControlFlow"" | ""DeprecatedPattern"" | ""RuntimeError"",
      ""category"": ""Error"" | ""Warning"" | ""Info"",
      ""description"": ""Detailed description of the semantic issue"",
      ""location"": ""File, function, or line number"",
      ""lineNumber"": <number>,
      ""codeSnippet"": ""Relevant code snippet"",
      ""recommendation"": ""How to fix this issue"",
      ""severity"": <number 1-5>
    }}
  ]
}}";
        }

        private string GetLanguageSpecificSemanticInstructions(LanguageKind language)
        {
            return language switch
            {
                LanguageKind.Python => @"
PYTHON-SPECIFIC SEMANTIC ANALYSIS:
- Detect Python 2.x patterns (old-style classes, print statements, etc.)
- Identify uninitialized variables and NameError risks
- Check for type mismatches and implicit type conversions
- Detect missing imports and undefined names
- Identify deprecated patterns (old-style classes, string formatting, etc.)
- Check for control flow issues (unreachable code after return/raise)
- Detect potential AttributeError and KeyError risks",
                
                LanguageKind.JavaScript or LanguageKind.TypeScript => @"
JAVASCRIPT/TYPESCRIPT-SPECIFIC SEMANTIC ANALYSIS:
- Detect 'var' usage (prefer 'let'/'const')
- Identify callback hell patterns (deeply nested callbacks)
- Check for undefined/null access risks
- Detect hoisting issues with var declarations
- Identify potential ReferenceError and TypeError risks
- Check for missing await in async functions
- Detect uninitialized variables and temporal dead zone issues
- For TypeScript: detect type mismatches and any usage",
                
                LanguageKind.Java => @"
JAVA-SPECIFIC SEMANTIC ANALYSIS:
- Detect uninitialized variables and fields
- Check for null pointer exception risks
- Identify deprecated API usage
- Detect unchecked exceptions and missing exception handling
- Check for type casting issues and ClassCastException risks
- Identify missing @Override annotations
- Detect potential IndexOutOfBoundsException risks",
                
                LanguageKind.Go => @"
GO-SPECIFIC SEMANTIC ANALYSIS:
- Detect uninitialized variables (zero values)
- Check for nil pointer dereference risks
- Identify missing error handling
- Detect unused variables and imports
- Check for type assertion failures
- Identify potential panic scenarios
- Detect missing defer statements for resource cleanup",
                
                _ => "Perform general semantic analysis for type errors, uninitialized variables, and control flow issues."
            };
        }

        private List<SemanticIssue> ParseSemanticIssues(string aiResponse, LanguageKind language)
        {
            var issues = new List<SemanticIssue>();

            try
            {
                // Try to extract JSON from response
                var jsonMatch = Regex.Match(aiResponse, @"\{[\s\S]*""semanticIssues""[\s\S]*\}");
                if (jsonMatch.Success)
                {
                    var json = jsonMatch.Value;
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var parsed = JsonSerializer.Deserialize<JsonElement>(json, options);

                    if (parsed.TryGetProperty("semanticIssues", out var issuesArray))
                    {
                        foreach (var issueElement in issuesArray.EnumerateArray())
                        {
                            var issue = new SemanticIssue
                            {
                                Language = language.ToString().ToLowerInvariant()
                            };

                            if (issueElement.TryGetProperty("issueType", out var issueType))
                                issue.IssueType = issueType.GetString() ?? "";
                            if (issueElement.TryGetProperty("category", out var category))
                                issue.Category = category.GetString() ?? "";
                            if (issueElement.TryGetProperty("description", out var description))
                                issue.Description = description.GetString() ?? "";
                            if (issueElement.TryGetProperty("location", out var location))
                                issue.Location = location.GetString() ?? "";
                            if (issueElement.TryGetProperty("lineNumber", out var lineNumber))
                                issue.LineNumber = lineNumber.GetInt32();
                            if (issueElement.TryGetProperty("codeSnippet", out var codeSnippet))
                                issue.CodeSnippet = codeSnippet.GetString() ?? "";
                            if (issueElement.TryGetProperty("recommendation", out var recommendation))
                                issue.Recommendation = recommendation.GetString() ?? "";
                            if (issueElement.TryGetProperty("severity", out var severity))
                                issue.Severity = severity.GetInt32();

                            issues.Add(issue);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse semantic issues from AI response");
            }

            return issues;
        }

        private List<LanguageSpecificPattern> DetectPythonPatterns(string code)
        {
            var patterns = new List<LanguageSpecificPattern>();
            var lines = code.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            // Python 2.x patterns
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineNum = i + 1;

                // Old-style print statement (Python 2)
                if (Regex.IsMatch(line, @"\bprint\s+[^(]"))
                {
                    patterns.Add(new LanguageSpecificPattern
                    {
                        PatternType = "Python2Print",
                        Description = "Python 2.x print statement detected",
                        Location = $"Line {lineNum}",
                        LineNumber = lineNum,
                        CodeSnippet = line.Trim(),
                        ModernAlternative = "Use print() function (Python 3)",
                        Language = "python",
                        IsDeprecated = true,
                        MigrationGuidance = "Replace 'print x' with 'print(x)'"
                    });
                }

                // Old-style class definition
                if (Regex.IsMatch(line, @"^\s*class\s+\w+\s*[^(:]"))
                {
                    patterns.Add(new LanguageSpecificPattern
                    {
                        PatternType = "OldStyleClass",
                        Description = "Old-style class definition (Python 2)",
                        Location = $"Line {lineNum}",
                        LineNumber = lineNum,
                        CodeSnippet = line.Trim(),
                        ModernAlternative = "Use new-style class: class MyClass(object):",
                        Language = "python",
                        IsDeprecated = true,
                        MigrationGuidance = "Inherit from 'object' or use Python 3 (all classes are new-style)"
                    });
                }

                // String formatting with %
                if (Regex.IsMatch(line, @"""[^""]*%[^""]*""\s*%"))
                {
                    patterns.Add(new LanguageSpecificPattern
                    {
                        PatternType = "OldStringFormatting",
                        Description = "Old-style string formatting with % operator",
                        Location = $"Line {lineNum}",
                        LineNumber = lineNum,
                        CodeSnippet = line.Trim(),
                        ModernAlternative = "Use .format() or f-strings",
                        Language = "python",
                        IsDeprecated = false,
                        MigrationGuidance = "Use str.format() or f-strings for better readability"
                    });
                }
            }

            return patterns;
        }

        private List<LanguageSpecificPattern> DetectJavaScriptPatterns(string code)
        {
            var patterns = new List<LanguageSpecificPattern>();
            var lines = code.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineNum = i + 1;

                // var usage (prefer let/const)
                if (Regex.IsMatch(line, @"\bvar\s+\w+"))
                {
                    patterns.Add(new LanguageSpecificPattern
                    {
                        PatternType = "VarUsage",
                        Description = "Use of 'var' keyword (prefer 'let' or 'const')",
                        Location = $"Line {lineNum}",
                        LineNumber = lineNum,
                        CodeSnippet = line.Trim(),
                        ModernAlternative = "Use 'let' for variables that change, 'const' for constants",
                        Language = "javascript",
                        IsDeprecated = false,
                        MigrationGuidance = "Replace 'var' with 'let' or 'const' to avoid hoisting issues and improve scoping"
                    });
                }

                // Callback hell detection (multiple nested callbacks)
                var callbackDepth = CountNestedCallbacks(line);
                if (callbackDepth >= 3)
                {
                    patterns.Add(new LanguageSpecificPattern
                    {
                        PatternType = "CallbackHell",
                        Description = $"Deeply nested callbacks detected (depth: {callbackDepth})",
                        Location = $"Line {lineNum}",
                        LineNumber = lineNum,
                        CodeSnippet = line.Trim(),
                        ModernAlternative = "Use Promises or async/await",
                        Language = "javascript",
                        IsDeprecated = false,
                        MigrationGuidance = "Refactor to use async/await or Promise chains to reduce nesting"
                    });
                }
            }

            return patterns;
        }

        private int CountNestedCallbacks(string line)
        {
            // Simple heuristic: count function( and => patterns
            var functionMatches = Regex.Matches(line, @"\bfunction\s*\(");
            var arrowMatches = Regex.Matches(line, @"=>");
            return functionMatches.Count + arrowMatches.Count;
        }

        private List<LanguageSpecificPattern> DetectJavaPatterns(string code)
        {
            var patterns = new List<LanguageSpecificPattern>();
            var lines = code.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineNum = i + 1;

                // Raw types (pre-generics)
                if (Regex.IsMatch(line, @"\b(ArrayList|HashMap|Vector|Hashtable)\s+\w+\s*="))
                {
                    patterns.Add(new LanguageSpecificPattern
                    {
                        PatternType = "RawType",
                        Description = "Raw type usage (pre-generics Java)",
                        Location = $"Line {lineNum}",
                        LineNumber = lineNum,
                        CodeSnippet = line.Trim(),
                        ModernAlternative = "Use generic types: ArrayList<String>, HashMap<K,V>",
                        Language = "java",
                        IsDeprecated = false,
                        MigrationGuidance = "Add type parameters to collections for type safety"
                    });
                }
            }

            return patterns;
        }

        private List<LanguageSpecificPattern> DetectGoPatterns(string code)
        {
            var patterns = new List<LanguageSpecificPattern>();
            var lines = code.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineNum = i + 1;

                // Missing error handling
                if (Regex.IsMatch(line, @"\w+\([^)]*\)\s*$") && !line.Contains("if err") && !line.Contains("err :="))
                {
                    // This is a heuristic - might have false positives
                    if (line.Contains("err") || line.Contains("error"))
                    {
                        patterns.Add(new LanguageSpecificPattern
                        {
                            PatternType = "MissingErrorHandling",
                            Description = "Potential missing error handling",
                            Location = $"Line {lineNum}",
                            LineNumber = lineNum,
                            CodeSnippet = line.Trim(),
                            ModernAlternative = "Always check and handle errors explicitly",
                            Language = "go",
                            IsDeprecated = false,
                            MigrationGuidance = "Add explicit error checking: if err != nil { ... }"
                        });
                    }
                }
            }

            return patterns;
        }

        private int CalculateSemanticQualityScore(SemanticAnalysisResult result)
        {
            // Base score
            var score = 100;

            // Deduct points for semantic issues
            foreach (var issue in result.SemanticIssues)
            {
                score -= issue.Severity * 5; // Each severity point = -5 points
            }

            // Deduct points for deprecated patterns
            foreach (var pattern in result.DetectedPatterns.Where(p => p.IsDeprecated))
            {
                score -= 10;
            }

            return Math.Max(0, Math.Min(100, score));
        }
    }
}


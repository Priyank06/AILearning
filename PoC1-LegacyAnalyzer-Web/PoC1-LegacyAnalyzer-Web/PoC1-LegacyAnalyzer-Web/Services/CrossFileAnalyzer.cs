using Microsoft.AspNetCore.Components.Forms;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PoC1_LegacyAnalyzer_Web.Models;
using PoC1_LegacyAnalyzer_Web.Services.Analysis;
using System.Collections.Concurrent;
using TreeSitter;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Analyzes cross-file dependencies using Roslyn semantic model.
    /// Detects method calls, inheritance, and other relationships across files.
    /// </summary>
    public class CrossFileAnalyzer : ICrossFileAnalyzer
    {
        private readonly ILogger<CrossFileAnalyzer> _logger;
        private readonly ITreeSitterLanguageRegistry? _treeSitterRegistry;
        private readonly ILanguageDetector _languageDetector;

        public CrossFileAnalyzer(
            ILogger<CrossFileAnalyzer> logger,
            ILanguageDetector languageDetector,
            ITreeSitterLanguageRegistry? treeSitterRegistry = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _languageDetector = languageDetector ?? throw new ArgumentNullException(nameof(languageDetector));
            _treeSitterRegistry = treeSitterRegistry;
        }

        public async Task<DependencyGraph> BuildDependencyGraphAsync(List<IBrowserFile> files, CancellationToken cancellationToken = default)
        {
            var graph = new DependencyGraph();
            var nodes = new ConcurrentDictionary<string, DependencyNode>();
            var edges = new ConcurrentBag<DependencyEdge>();
            var fileContents = new Dictionary<string, string>();
            var syntaxTrees = new Dictionary<string, SyntaxTree>();
            var treeSitterTrees = new Dictionary<string, Tree>();

            // Step 1: Parse all files and build syntax trees
            _logger.LogInformation("Parsing {FileCount} files for dependency analysis", files.Count);

            // Group files by language
            var csFiles = new List<IBrowserFile>();
            var otherLanguageFiles = new List<IBrowserFile>();

            foreach (var file in files)
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();
                fileContents[file.Name] = content;

                var languageKind = _languageDetector.DetectLanguage(file.Name, content);
                
                if (languageKind == LanguageKind.CSharp)
                {
                    csFiles.Add(file);
                }
                else if (languageKind != LanguageKind.Unknown && _treeSitterRegistry != null)
                {
                    otherLanguageFiles.Add(file);
                }
            }

            // Step 1a: Process C# files with Roslyn
            foreach (var file in csFiles)
            {
                try
                {
                    using var stream = file.OpenReadStream();
                    using var reader = new StreamReader(stream);
                    var content = await reader.ReadToEndAsync();
                    fileContents[file.Name] = content;

                    var syntaxTree = CSharpSyntaxTree.ParseText(content, path: file.Name, cancellationToken: cancellationToken);
                    syntaxTrees[file.Name] = syntaxTree;

                    // Build nodes from syntax tree
                    var root = await syntaxTree.GetRootAsync(cancellationToken);
                    BuildNodesFromSyntaxTree(root, file.Name, nodes);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse file {FileName} for dependency analysis", file.Name);
                }
            }

            // Step 1b: Process other language files with Tree-sitter
            foreach (var file in otherLanguageFiles)
            {
                try
                {
                    var content = fileContents[file.Name];
                    var languageKind = _languageDetector.DetectLanguage(file.Name, content);
                    
                    if (_treeSitterRegistry != null)
                    {
                        using var parser = _treeSitterRegistry.GetParser(languageKind);
                        var tree = parser.Parse(content);
                        treeSitterTrees[file.Name] = tree;

                        // Build nodes from Tree-sitter AST
                        BuildNodesFromTreeSitter(tree.RootNode, file.Name, languageKind, nodes);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse file {FileName} with Tree-sitter for dependency analysis", file.Name);
                }
            }

            // Step 2: Create compilation to get semantic model (C# only)
            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location)
            };

            var compilation = CSharpCompilation.Create(
                "DependencyAnalysis",
                syntaxTrees.Values,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // Step 3a: Analyze C# dependencies using semantic model
            if (syntaxTrees.Any())
            {
                _logger.LogInformation("Analyzing C# dependencies using semantic model");

                foreach (var kvp in syntaxTrees)
            {
                var fileName = kvp.Key;
                var syntaxTree = kvp.Value;
                var semanticModel = compilation.GetSemanticModel(syntaxTree);

                try
                {
                    AnalyzeDependencies(syntaxTree, semanticModel, fileName, fileContents, nodes, edges, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to analyze dependencies for file {FileName}", fileName);
                }
            }
            }

            // Step 3b: Analyze other language dependencies using Tree-sitter
            if (treeSitterTrees.Any())
            {
                _logger.LogInformation("Analyzing dependencies for {LanguageCount} non-C# files using Tree-sitter", treeSitterTrees.Count);

                foreach (var kvp in treeSitterTrees)
                {
                    var fileName = kvp.Key;
                    var tree = kvp.Value;
                    var content = fileContents[fileName];
                    var languageKind = _languageDetector.DetectLanguage(fileName, content);

                    try
                    {
                        AnalyzeTreeSitterDependencies(tree.RootNode, fileName, languageKind, fileContents, nodes, edges, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to analyze dependencies for file {FileName}", fileName);
                    }
                    finally
                    {
                        tree.Dispose();
                    }
                }
            }

            graph.Nodes = nodes.Values.ToList();
            graph.Edges = edges.ToList();

            // Step 4: Build file-level dependency maps
            BuildFileDependencyMaps(graph);

            _logger.LogInformation("Built dependency graph with {NodeCount} nodes and {EdgeCount} edges", 
                graph.Nodes.Count, graph.Edges.Count);

            return graph;
        }

        private void BuildNodesFromSyntaxTree(SyntaxNode root, string fileName, ConcurrentDictionary<string, DependencyNode> nodes)
        {
            var namespaceName = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>()
                .FirstOrDefault()?.Name.ToString() ?? "";

            // Extract classes
            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var className = classDecl.Identifier.ValueText;
                var classId = $"{fileName}::{namespaceName}::{className}";
                var lineNumber = classDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

                nodes.TryAdd(classId, new DependencyNode
                {
                    Id = classId,
                    Name = className,
                    FileName = fileName,
                    Namespace = namespaceName,
                    Type = "Class",
                    FullName = string.IsNullOrEmpty(namespaceName) ? className : $"{namespaceName}.{className}",
                    LineNumber = lineNumber
                });

                // Extract methods
                foreach (var method in classDecl.Members.OfType<MethodDeclarationSyntax>())
                {
                    var methodName = method.Identifier.ValueText;
                    var methodId = $"{classId}::{methodName}";
                    var methodLineNumber = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

                    nodes.TryAdd(methodId, new DependencyNode
                    {
                        Id = methodId,
                        Name = methodName,
                        FileName = fileName,
                        Namespace = namespaceName,
                        Type = "Method",
                        FullName = $"{namespaceName}.{className}.{methodName}",
                        LineNumber = methodLineNumber
                    });
                }

                // Extract properties
                foreach (var property in classDecl.Members.OfType<PropertyDeclarationSyntax>())
                {
                    var propertyName = property.Identifier.ValueText;
                    var propertyId = $"{classId}::{propertyName}";
                    var propertyLineNumber = property.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

                    nodes.TryAdd(propertyId, new DependencyNode
                    {
                        Id = propertyId,
                        Name = propertyName,
                        FileName = fileName,
                        Namespace = namespaceName,
                        Type = "Property",
                        FullName = $"{namespaceName}.{className}.{propertyName}",
                        LineNumber = propertyLineNumber
                    });
                }
            }
        }

        private void AnalyzeDependencies(
            SyntaxTree syntaxTree,
            SemanticModel semanticModel,
            string fileName,
            Dictionary<string, string> fileContents,
            ConcurrentDictionary<string, DependencyNode> nodes,
            ConcurrentBag<DependencyEdge> edges,
            CancellationToken cancellationToken)
        {
            var root = syntaxTree.GetRoot(cancellationToken);

            // Analyze method invocations
            foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                try
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(invocation);
                    if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                    {
                        var targetMethod = methodSymbol;
                        var targetClass = targetMethod.ContainingType;
                        var targetFile = FindFileForSymbol(targetClass, fileContents.Keys.ToList());

                        if (!string.IsNullOrEmpty(targetFile) && targetFile != fileName)
                        {
                            var sourceNode = FindContainingNode(invocation, nodes, fileName);
                            var targetNodeId = BuildNodeId(targetFile, targetClass, targetMethod);

                            if (nodes.ContainsKey(targetNodeId))
                            {
                                var lineNumber = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                                edges.Add(new DependencyEdge
                                {
                                    SourceId = sourceNode?.Id ?? "",
                                    TargetId = targetNodeId,
                                    SourceFile = fileName,
                                    TargetFile = targetFile,
                                    Type = DependencyType.MethodCall,
                                    Description = $"{sourceNode?.Name ?? "Unknown"} calls {targetMethod.Name}",
                                    LineNumber = lineNumber
                                });

                                // Update connectivity
                                if (sourceNode != null)
                                {
                                    sourceNode.Connectivity++;
                                }
                                if (nodes.TryGetValue(targetNodeId, out var targetNode))
                                {
                                    targetNode.Connectivity++;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to analyze invocation at line {Line}", 
                        invocation.GetLocation().GetLineSpan().StartLinePosition.Line);
                }
            }

            // Analyze inheritance
            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (classDecl.BaseList != null)
                {
                    foreach (var baseType in classDecl.BaseList.Types)
                    {
                        try
                        {
                            var typeInfo = semanticModel.GetTypeInfo(baseType.Type);
                            if (typeInfo.Type is INamedTypeSymbol baseTypeSymbol)
                            {
                                var baseFile = FindFileForSymbol(baseTypeSymbol, fileContents.Keys.ToList());
                                if (!string.IsNullOrEmpty(baseFile) && baseFile != fileName)
                                {
                                    var namespaceName = classDecl.Ancestors().OfType<BaseNamespaceDeclarationSyntax>()
                                        .FirstOrDefault()?.Name.ToString() ?? "";
                                    var className = classDecl.Identifier.ValueText;
                                    var sourceNodeId = $"{fileName}::{namespaceName}::{className}";
                                    var targetClassId = BuildClassNodeId(baseFile, baseTypeSymbol);

                                    if (nodes.ContainsKey(sourceNodeId) && nodes.ContainsKey(targetClassId))
                                    {
                                        edges.Add(new DependencyEdge
                                        {
                                            SourceId = sourceNodeId,
                                            TargetId = targetClassId,
                                            SourceFile = fileName,
                                            TargetFile = baseFile,
                                            Type = DependencyType.Inheritance,
                                            Description = $"{className} inherits from {baseTypeSymbol.Name}",
                                            LineNumber = classDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                                        });
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Failed to analyze inheritance for class {ClassName}", 
                                classDecl.Identifier.ValueText);
                        }
                    }
                }
            }
        }

        private DependencyNode? FindContainingNode(SyntaxNode syntaxNode, ConcurrentDictionary<string, DependencyNode> nodes, string fileName)
        {
            // Find the containing method or class
            var method = syntaxNode.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (method != null)
            {
                var classDecl = method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if (classDecl != null)
                {
                    var namespaceName = classDecl.Ancestors().OfType<BaseNamespaceDeclarationSyntax>()
                        .FirstOrDefault()?.Name.ToString() ?? "";
                    var className = classDecl.Identifier.ValueText;
                    var methodName = method.Identifier.ValueText;
                    var nodeId = $"{fileName}::{namespaceName}::{className}::{methodName}";
                    nodes.TryGetValue(nodeId, out var node);
                    return node;
                }
            }
            return null;
        }

        private string FindFileForSymbol(INamedTypeSymbol symbol, List<string> fileNames)
        {
            // Try to find file by matching namespace and class name
            var symbolNamespace = symbol.ContainingNamespace?.ToDisplayString() ?? "";
            var symbolName = symbol.Name;

            foreach (var fileName in fileNames)
            {
                // This is a simplified approach - in production, you'd use symbol locations
                // For now, we'll use heuristics based on namespace/class name matching
                if (fileName.Contains(symbolName, StringComparison.OrdinalIgnoreCase))
                {
                    return fileName;
                }
            }

            return "";
        }

        private string BuildNodeId(string fileName, INamedTypeSymbol containingType, IMethodSymbol method)
        {
            var namespaceName = containingType.ContainingNamespace?.ToDisplayString() ?? "";
            var className = containingType.Name;
            var methodName = method.Name;
            return $"{fileName}::{namespaceName}::{className}::{methodName}";
        }

        private string BuildClassNodeId(string fileName, INamedTypeSymbol typeSymbol)
        {
            var namespaceName = typeSymbol.ContainingNamespace?.ToDisplayString() ?? "";
            var className = typeSymbol.Name;
            return $"{fileName}::{namespaceName}::{className}";
        }

        private void BuildFileDependencyMaps(DependencyGraph graph)
        {
            foreach (var edge in graph.Edges)
            {
                // Build file dependencies (what this file depends on)
                if (!graph.FileDependencies.ContainsKey(edge.SourceFile))
                {
                    graph.FileDependencies[edge.SourceFile] = new List<string>();
                }
                if (!graph.FileDependencies[edge.SourceFile].Contains(edge.TargetFile))
                {
                    graph.FileDependencies[edge.SourceFile].Add(edge.TargetFile);
                }

                // Build file dependents (what depends on this file)
                if (!graph.FileDependents.ContainsKey(edge.TargetFile))
                {
                    graph.FileDependents[edge.TargetFile] = new List<string>();
                }
                if (!graph.FileDependents[edge.TargetFile].Contains(edge.SourceFile))
                {
                    graph.FileDependents[edge.TargetFile].Add(edge.SourceFile);
                }
            }
        }

        public async Task<DependencyImpact> AnalyzeImpactAsync(DependencyGraph graph, string elementId)
        {
            return await Task.Run(() =>
            {
                var impact = new DependencyImpact
                {
                    ElementId = elementId
                };

                var node = graph.Nodes.FirstOrDefault(n => n.Id == elementId);
                if (node == null)
                {
                    return impact;
                }

                impact.ElementName = node.Name;
                impact.FileName = node.FileName;

                // Find all edges that target this element
                var incomingEdges = graph.Edges.Where(e => e.TargetId == elementId).ToList();
                var affectedFiles = incomingEdges.Select(e => e.SourceFile).Distinct().ToList();
                var affectedClasses = incomingEdges
                    .Select(e => graph.Nodes.FirstOrDefault(n => n.Id == e.SourceId))
                    .Where(n => n != null && n.Type == "Class")
                    .Select(n => n!.FullName)
                    .Distinct()
                    .ToList();
                var affectedMethods = incomingEdges
                    .Select(e => graph.Nodes.FirstOrDefault(n => n.Id == e.SourceId))
                    .Where(n => n != null && n.Type == "Method")
                    .Select(n => n!.FullName)
                    .Distinct()
                    .ToList();

                impact.AffectedFilesCount = affectedFiles.Count;
                impact.AffectedFiles = affectedFiles;
                impact.AffectedClassesCount = affectedClasses.Count;
                impact.AffectedClasses = affectedClasses;
                impact.AffectedMethodsCount = affectedMethods.Count;
                impact.AffectedMethods = affectedMethods;

                // Check if God Object
                impact.IsGodObject = node.Connectivity > 20;

                // Determine risk level
                if (impact.AffectedFilesCount > 10 || impact.IsGodObject)
                {
                    impact.RiskLevel = "Critical";
                }
                else if (impact.AffectedFilesCount > 5)
                {
                    impact.RiskLevel = "High";
                }
                else if (impact.AffectedFilesCount > 2)
                {
                    impact.RiskLevel = "Medium";
                }

                return impact;
            });
        }

        public async Task<List<CyclicDependency>> DetectCyclesAsync(DependencyGraph graph)
        {
            return await Task.Run(() =>
            {
                var cycles = new List<CyclicDependency>();
                var visited = new HashSet<string>();
                var recStack = new HashSet<string>();
                var path = new List<string>();

                // Build adjacency list (file-level cycles)
                var fileGraph = new Dictionary<string, List<string>>();
                foreach (var edge in graph.Edges)
                {
                    if (!fileGraph.ContainsKey(edge.SourceFile))
                    {
                        fileGraph[edge.SourceFile] = new List<string>();
                    }
                    if (!fileGraph[edge.SourceFile].Contains(edge.TargetFile))
                    {
                        fileGraph[edge.SourceFile].Add(edge.TargetFile);
                    }
                }

                // DFS to find cycles
                foreach (var file in fileGraph.Keys)
                {
                    if (!visited.Contains(file))
                    {
                        FindCyclesDFS(file, fileGraph, visited, recStack, path, cycles);
                    }
                }

                return cycles;
            });
        }

        private void FindCyclesDFS(
            string current,
            Dictionary<string, List<string>> graph,
            HashSet<string> visited,
            HashSet<string> recStack,
            List<string> path,
            List<CyclicDependency> cycles)
        {
            visited.Add(current);
            recStack.Add(current);
            path.Add(current);

            if (graph.ContainsKey(current))
            {
                foreach (var neighbor in graph[current])
                {
                    if (!visited.Contains(neighbor))
                    {
                        FindCyclesDFS(neighbor, graph, visited, recStack, path, cycles);
                    }
                    else if (recStack.Contains(neighbor))
                    {
                        // Found a cycle
                        var cycleStart = path.IndexOf(neighbor);
                        var cycle = path.Skip(cycleStart).Concat(new[] { neighbor }).ToList();
                        cycles.Add(new CyclicDependency
                        {
                            Cycle = cycle,
                            Type = DependencyType.TypeReference,
                            Description = $"Cyclic dependency: {string.Join(" -> ", cycle)}",
                            Severity = cycle.Count > 5 ? 5 : cycle.Count
                        });
                    }
                }
            }

            recStack.Remove(current);
            path.RemoveAt(path.Count - 1);
        }

        public async Task<List<DependencyNode>> DetectGodObjectsAsync(DependencyGraph graph, int threshold = 20)
        {
            return await Task.Run(() =>
            {
                return graph.Nodes
                    .Where(n => n.Type == "Class" && n.Connectivity > threshold)
                    .OrderByDescending(n => n.Connectivity)
                    .ToList();
            });
        }

        /// <summary>
        /// Builds nodes from Tree-sitter AST for non-C# languages.
        /// </summary>
        private void BuildNodesFromTreeSitter(Node root, string fileName, LanguageKind languageKind, ConcurrentDictionary<string, DependencyNode> nodes)
        {
            var namespaceName = ExtractNamespaceFromTreeSitter(root, languageKind);
            ExtractClassesFromTreeSitter(root, fileName, namespaceName, languageKind, nodes);
        }

        private string ExtractNamespaceFromTreeSitter(Node root, LanguageKind languageKind)
        {
            return languageKind switch
            {
                LanguageKind.Python => ExtractPythonModule(root),
                LanguageKind.Java => ExtractJavaPackage(root),
                LanguageKind.JavaScript or LanguageKind.TypeScript => ExtractJSModule(root),
                LanguageKind.Go => ExtractGoPackage(root),
                _ => ""
            };
        }

        private string ExtractPythonModule(Node root) => ""; // Python modules are file-based
        private string ExtractJavaPackage(Node root)
        {
            var packageNode = root.Children.FirstOrDefault(c => c.Type == "package_declaration");
            if (packageNode != null)
            {
                var identifier = packageNode.Children.FirstOrDefault(c => c.Type == "scoped_identifier" || c.Type == "identifier");
                return identifier?.Text ?? "";
            }
            return "";
        }
        private string ExtractJSModule(Node root) => ""; // JS modules are file-based
        private string ExtractGoPackage(Node root)
        {
            var packageNode = root.Children.FirstOrDefault(c => c.Type == "package_clause");
            if (packageNode != null)
            {
                var identifier = packageNode.Children.FirstOrDefault(c => c.Type == "package_identifier");
                return identifier?.Text ?? "";
            }
            return "";
        }

        private void ExtractClassesFromTreeSitter(Node node, string fileName, string namespaceName, LanguageKind languageKind, ConcurrentDictionary<string, DependencyNode> nodes)
        {
            var classNodeType = languageKind switch
            {
                LanguageKind.Python => "class_definition",
                LanguageKind.Java => "class_declaration",
                LanguageKind.JavaScript or LanguageKind.TypeScript => "class_declaration",
                LanguageKind.Go => "type_declaration",
                _ => null
            };

            if (classNodeType == null) return;

            foreach (var child in node.Children)
            {
                if (child.Type == classNodeType)
                {
                    var nameNode = child.Children.FirstOrDefault(c => 
                        c.Type == "identifier" || 
                        c.Type == "type_identifier" || 
                        c.Type == "name");
                    
                    if (nameNode != null)
                    {
                        var className = nameNode.Text;
                        var classId = $"{fileName}::{namespaceName}::{className}";
                        var lineNumber = child.StartPosition.Row + 1;

                        nodes.TryAdd(classId, new DependencyNode
                        {
                            Id = classId,
                            Name = className,
                            FileName = fileName,
                            Namespace = namespaceName,
                            Type = "Class",
                            FullName = string.IsNullOrEmpty(namespaceName) ? className : $"{namespaceName}.{className}",
                            LineNumber = lineNumber
                        });

                        ExtractMethodsFromTreeSitterClass(child, classId, fileName, namespaceName, languageKind, nodes);
                    }
                }
                ExtractClassesFromTreeSitter(child, fileName, namespaceName, languageKind, nodes);
            }
        }

        private void ExtractMethodsFromTreeSitterClass(Node classNode, string classId, string fileName, string namespaceName, LanguageKind languageKind, ConcurrentDictionary<string, DependencyNode> nodes)
        {
            var methodNodeType = languageKind switch
            {
                LanguageKind.Python => "function_definition",
                LanguageKind.Java => "method_declaration",
                LanguageKind.JavaScript or LanguageKind.TypeScript => "method_definition",
                LanguageKind.Go => "method_declaration",
                _ => null
            };

            if (methodNodeType == null) return;

            foreach (var child in classNode.Children)
            {
                if (child.Type == methodNodeType)
                {
                    var nameNode = child.Children.FirstOrDefault(c => 
                        c.Type == "identifier" || 
                        c.Type == "property_identifier" ||
                        c.Type == "name");
                    
                    if (nameNode != null)
                    {
                        var methodName = nameNode.Text;
                        var methodId = $"{classId}::{methodName}";
                        var lineNumber = child.StartPosition.Row + 1;

                        nodes.TryAdd(methodId, new DependencyNode
                        {
                            Id = methodId,
                            Name = methodName,
                            FileName = fileName,
                            Namespace = namespaceName,
                            Type = "Method",
                            FullName = $"{namespaceName}.{classId.Split("::").Last()}.{methodName}",
                            LineNumber = lineNumber
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes dependencies in Tree-sitter AST for non-C# languages.
        /// </summary>
        private void AnalyzeTreeSitterDependencies(
            Node root,
            string fileName,
            LanguageKind languageKind,
            Dictionary<string, string> fileContents,
            ConcurrentDictionary<string, DependencyNode> nodes,
            ConcurrentBag<DependencyEdge> edges,
            CancellationToken cancellationToken)
        {
            AnalyzeImports(root, fileName, languageKind, fileContents, nodes, edges);
            // Method call analysis requires semantic analysis - placeholder for future enhancement
            _logger.LogDebug("Method call analysis for {Language} requires semantic analysis", languageKind);
        }

        private void AnalyzeImports(Node root, string fileName, LanguageKind languageKind, Dictionary<string, string> fileContents, ConcurrentDictionary<string, DependencyNode> nodes, ConcurrentBag<DependencyEdge> edges)
        {
            var importNodeTypes = languageKind switch
            {
                LanguageKind.Python => new[] { "import_statement", "import_from_statement" },
                LanguageKind.Java => new[] { "import_declaration" },
                LanguageKind.JavaScript or LanguageKind.TypeScript => new[] { "import_statement", "require_call" },
                LanguageKind.Go => new[] { "import_declaration" },
                _ => Array.Empty<string>()
            };

            foreach (var importType in importNodeTypes)
            {
                var importNodes = GetAllNodesOfType(root, importType);
                foreach (var importNode in importNodes)
                {
                    try
                    {
                        var importedModule = ExtractImportedModule(importNode, languageKind);
                        if (!string.IsNullOrEmpty(importedModule))
                        {
                            var targetFile = FindFileByModuleName(importedModule, fileContents.Keys.ToList(), languageKind);
                            if (!string.IsNullOrEmpty(targetFile) && targetFile != fileName)
                            {
                                var sourceNode = nodes.Values.FirstOrDefault(n => n.FileName == fileName);
                                var targetNode = nodes.Values.FirstOrDefault(n => n.FileName == targetFile);

                                if (sourceNode != null && targetNode != null)
                                {
                                    edges.Add(new DependencyEdge
                                    {
                                        SourceId = sourceNode.Id,
                                        TargetId = targetNode.Id,
                                        SourceFile = fileName,
                                        TargetFile = targetFile,
                                        Type = DependencyType.TypeReference,
                                        Description = $"{fileName} imports {importedModule}",
                                        LineNumber = importNode.StartPosition.Row + 1
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to analyze import at line {Line}", importNode.StartPosition.Row);
                    }
                }
            }
        }

        private IEnumerable<Node> GetAllNodesOfType(Node root, string nodeType)
        {
            if (root.Type == nodeType)
                yield return root;

            foreach (var child in root.Children)
            {
                foreach (var node in GetAllNodesOfType(child, nodeType))
                    yield return node;
            }
        }

        private string ExtractImportedModule(Node importNode, LanguageKind languageKind)
        {
            return languageKind switch
            {
                LanguageKind.Python => importNode.Children.FirstOrDefault(c => c.Type == "dotted_name" || c.Type == "dotted_as_name")?.Text ?? "",
                LanguageKind.Java => importNode.Children.FirstOrDefault(c => c.Type == "scoped_identifier")?.Text ?? "",
                LanguageKind.JavaScript or LanguageKind.TypeScript => 
                    importNode.Children.FirstOrDefault(c => c.Type == "string")?.Text.Trim('"', '\'', '`') ?? "",
                LanguageKind.Go => 
                    importNode.Children.FirstOrDefault(c => c.Type == "import_spec")
                        ?.Children.FirstOrDefault(c => c.Type == "interpreted_string_literal")?.Text.Trim('"') ?? "",
                _ => ""
            };
        }

        private string FindFileByModuleName(string moduleName, List<string> fileNames, LanguageKind languageKind)
        {
            var normalizedModule = moduleName.Replace(".", "/").Replace("\\", "/");
            
            foreach (var fileName in fileNames)
            {
                var normalizedFileName = fileName.Replace("\\", "/");
                
                if (normalizedFileName.Contains(normalizedModule, StringComparison.OrdinalIgnoreCase) ||
                    normalizedModule.Contains(Path.GetFileNameWithoutExtension(fileName), StringComparison.OrdinalIgnoreCase))
                {
                    return fileName;
                }
            }

            return "";
        }
    }
}


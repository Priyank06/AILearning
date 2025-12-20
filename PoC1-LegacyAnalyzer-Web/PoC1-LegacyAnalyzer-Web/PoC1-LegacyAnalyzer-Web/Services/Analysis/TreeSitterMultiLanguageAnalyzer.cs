using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoC1_LegacyAnalyzer_Web.Models;
using TreeSitter;

namespace PoC1_LegacyAnalyzer_Web.Services.Analysis
{
    /// <summary>
    /// Tree-sitter based analyzer for non-C# languages (Python, JavaScript/TypeScript, Java, Go).
    /// Produces unified CodeStructure/CodeAnalysisResult models.
    /// </summary>
    public class TreeSitterMultiLanguageAnalyzer : ICodeAnalyzer, ILanguageSpecificAnalyzer
    {
        private readonly ITreeSitterLanguageRegistry _registry;

        public TreeSitterMultiLanguageAnalyzer(ITreeSitterLanguageRegistry registry)
        {
            _registry = registry;
        }

        public LanguageKind Language => LanguageKind.Unknown; // Not used directly; router maps by languageKind when registering.

        public Task<(CodeStructure structure, CodeAnalysisResult summary)> AnalyzeAsync(
            AnalyzableFile file,
            CancellationToken cancellationToken = default)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (file.Language == LanguageKind.CSharp)
            {
                throw new NotSupportedException("TreeSitterMultiLanguageAnalyzer is intended for non-C# languages.");
            }

            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var parser = _registry.GetParser(file.Language);
                using var tree = parser.Parse(file.Content);
                var root = tree.RootNode;

                var structure = BuildStructure(file, root);
                var summary = BuildSummary(structure);

                return (structure, summary);
            }, cancellationToken);
        }

        private static CodeStructure BuildStructure(AnalyzableFile file, Node root)
        {
            var lines = file.Content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var languageName = file.Language.ToString().ToLowerInvariant();

            var structure = new CodeStructure
            {
                LanguageKind = file.Language,
                Language = languageName,
                FileName = file.FileName,
                LineCount = lines.Length,
                RawSyntaxTree = root.ToString()
            };

            // Very lightweight, grammar-agnostic extraction using common node names.
            foreach (var child in root.Children)
            {
                ExtractFromNode(child, structure);
            }

            return structure;
        }

        private static void ExtractFromNode(Node node, CodeStructure structure, ClassDeclaration? currentClass = null)
        {
            var type = node.Type;

            // Heuristics for classes/types
            if (type is "class_definition" or "class_declaration" or "type_declaration")
            {
                var nameNode = node.Children.FirstOrDefault(c => c.Type is "identifier" or "name");
                var classDecl = new ClassDeclaration
                {
                    Name = nameNode?.Text ?? "Anonymous",
                    LineNumber = node.StartPosition.Row + 1,
                    LineCount = node.EndPosition.Row - node.StartPosition.Row + 1,
                    AccessModifier = AccessModifier.Unknown
                };

                structure.Classes.Add(classDecl);

                foreach (var child in node.Children)
                {
                    ExtractFromNode(child, structure, classDecl);
                }

                return;
            }

            // Functions / methods
            if (type is "function_definition" or "function_declaration" or "method_definition")
            {
                var nameNode = node.Children.FirstOrDefault(c => c.Type is "identifier" or "name");
                var func = new FunctionDeclaration
                {
                    Name = nameNode?.Text ?? "anonymous",
                    LineNumber = node.StartPosition.Row + 1,
                    LineCount = node.EndPosition.Row - node.StartPosition.Row + 1,
                    AccessModifier = AccessModifier.Unknown
                };

                if (currentClass != null)
                {
                    currentClass.Methods.Add(func);
                }
                else
                {
                    structure.Functions.Add(func);
                }
            }

            // Imports / requires
            if (type.Contains("import") || type.Contains("using"))
            {
                var moduleNode = node.Children.FirstOrDefault(c => c.Type is "string" or "identifier" or "dotted_name");
                var import = new ImportDeclaration
                {
                    ModuleName = moduleNode?.Text.Trim('\"') ?? string.Empty,
                    IsWildcard = node.Text.Contains("*")
                };
                structure.Imports.Add(import);
            }

            foreach (var child in node.Children)
            {
                ExtractFromNode(child, structure, currentClass);
            }
        }

        private static CodeAnalysisResult BuildSummary(CodeStructure structure)
        {
            return new CodeAnalysisResult
            {
                LanguageKind = structure.LanguageKind,
                Language = structure.Language,
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
    }
}



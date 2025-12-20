using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.Analysis
{
    /// <summary>
    /// C# analyzer implementation backed by Roslyn.
    /// Produces unified CodeStructure and CodeAnalysisResult models.
    /// </summary>
    public class RoslynCSharpAnalyzer : ICodeAnalyzer, ILanguageSpecificAnalyzer
    {
        public LanguageKind Language => LanguageKind.CSharp;

        public Task<(CodeStructure structure, CodeAnalysisResult summary)> AnalyzeAsync(
            AnalyzableFile file,
            CancellationToken cancellationToken = default)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var syntaxTree = CSharpSyntaxTree.ParseText(file.Content, cancellationToken: cancellationToken);
                var root = syntaxTree.GetRoot(cancellationToken);

                var structure = BuildCodeStructure(file, root);
                var summary = BuildSummary(root);

                return (structure, summary);
            }, cancellationToken);
        }

        private static CodeStructure BuildCodeStructure(AnalyzableFile file, SyntaxNode root)
        {
            var codeLines = file.Content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            var structure = new CodeStructure
            {
                Language = "csharp",
                FileName = file.FileName,
                ContainerName = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString() ?? string.Empty,
                LineCount = codeLines.Length
            };

            // Imports
            var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>();
            foreach (var u in usingDirectives)
            {
                var import = new ImportDeclaration
                {
                    ModuleName = u.Name?.ToString() ?? string.Empty,
                    IsWildcard = u.Alias == null && u.StaticKeyword != default
                };
                structure.Imports.Add(import);
            }

            // Classes and members
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var cls in classes)
            {
                var classDecl = new ClassDeclaration
                {
                    Name = cls.Identifier.ValueText,
                    LineNumber = cls.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    LineCount = cls.GetLocation().GetLineSpan().EndLinePosition.Line
                               - cls.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    AccessModifier = GetAccessModifier(cls.Modifiers),
                    BaseTypes = cls.BaseList?.Types.Select(t => t.ToString()).ToList() ?? []
                };

                // Methods
                foreach (var method in cls.Members.OfType<MethodDeclarationSyntax>())
                {
                    classDecl.Methods.Add(CreateFunctionDeclaration(method));
                }

                // Properties
                foreach (var prop in cls.Members.OfType<PropertyDeclarationSyntax>())
                {
                    classDecl.Properties.Add(new PropertyDeclaration
                    {
                        Name = prop.Identifier.ValueText,
                        Type = prop.Type.ToString(),
                        AccessModifier = GetAccessModifier(prop.Modifiers),
                        HasGetter = prop.AccessorList?.Accessors.Any(a => a.Kind() == SyntaxKind.GetAccessorDeclaration) ?? false,
                        HasSetter = prop.AccessorList?.Accessors.Any(a => a.Kind() == SyntaxKind.SetAccessorDeclaration) ?? false
                    });
                }

                structure.Classes.Add(classDecl);
            }

            // Top-level functions (C# 9+)
            var topLevelMethods = root.DescendantNodes()
                .OfType<GlobalStatementSyntax>()
                .SelectMany(gs => gs.DescendantNodes().OfType<MethodDeclarationSyntax>());

            foreach (var method in topLevelMethods)
            {
                structure.Functions.Add(CreateFunctionDeclaration(method));
            }

            return structure;
        }

        private static FunctionDeclaration CreateFunctionDeclaration(MethodDeclarationSyntax method)
        {
            var span = method.GetLocation().GetLineSpan();

            return new FunctionDeclaration
            {
                Name = method.Identifier.ValueText,
                LineNumber = span.StartLinePosition.Line + 1,
                LineCount = span.EndLinePosition.Line - span.StartLinePosition.Line + 1,
                Parameters = method.ParameterList.Parameters
                    .Select(p => new ParameterDeclaration
                    {
                        Name = p.Identifier.ValueText,
                        Type = p.Type?.ToString() ?? string.Empty,
                        IsOptional = p.Default != null
                    })
                    .ToList(),
                ReturnType = method.ReturnType.ToString(),
                AccessModifier = GetAccessModifier(method.Modifiers),
                IsAsync = method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)),
                IsStatic = method.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword))
            };
        }

        private static AccessModifier GetAccessModifier(SyntaxTokenList modifiers)
        {
            if (modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword))) return AccessModifier.Public;
            if (modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword))) return AccessModifier.Private;
            if (modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword)) &&
                modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword))) return AccessModifier.ProtectedInternal;
            if (modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword))) return AccessModifier.Protected;
            if (modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword))) return AccessModifier.Internal;

            return AccessModifier.Unknown;
        }

        private static CodeAnalysisResult BuildSummary(SyntaxNode root)
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
                UsingStatements = usingDirectives.Select(u => u.Name?.ToString() ?? string.Empty).ToList()
            };
        }

        private static string GetClassName(SyntaxNode method)
        {
            var classDeclaration = method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            return classDeclaration?.Identifier.ValueText ?? "Global";
        }
    }
}



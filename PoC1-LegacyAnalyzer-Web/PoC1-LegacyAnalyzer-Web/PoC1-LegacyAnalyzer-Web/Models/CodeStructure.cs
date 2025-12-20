using System.Collections.Generic;

namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Language-agnostic representation of parsed code structure.
    /// Designed to unify analysis across languages (C#, Python, JavaScript, etc.)
    /// and to be compatible with existing AI analysis models (e.g. CodeAnalysisResult).
    /// </summary>
    public class CodeStructure
    {
        /// <summary>
        /// Strongly-typed language indicator used by analyzers and routing logic.
        /// </summary>
        public LanguageKind LanguageKind { get; set; } = LanguageKind.Unknown;

        /// <summary>
        /// The programming language of the source file (e.g. "csharp", "python", "javascript").
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// The name of the file being analyzed (without being tied to any specific language concept like "namespace").
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Optional logical container for the code (e.g., Java package, Python module),
        /// kept generic to support multiple languages.
        /// </summary>
        public string ContainerName { get; set; }

        /// <summary>
        /// Total number of lines in the file.
        /// </summary>
        public int LineCount { get; set; }

        /// <summary>
        /// Classes or class-like constructs found in the file.
        /// </summary>
        public List<ClassDeclaration> Classes { get; set; } = new();

        /// <summary>
        /// Top-level functions or function-like constructs found in the file.
        /// </summary>
        public List<FunctionDeclaration> Functions { get; set; } = new();

        /// <summary>
        /// Imports / using / require / include statements for the file.
        /// </summary>
        public List<ImportDeclaration> Imports { get; set; } = new();

        /// <summary>
        /// Generic complexity metrics keyed by metric name
        /// (e.g. "CyclomaticComplexity", "NPathComplexity", "MaintainabilityIndex").
        /// </summary>
        public Dictionary<string, int> ComplexityMetrics { get; set; } = new();

        /// <summary>
        /// Detected language-agnostic code patterns (security, performance, architecture, etc.).
        /// </summary>
        public List<CodePattern> DetectedPatterns { get; set; } = new();

        /// <summary>
        /// Serialized representation of the underlying syntax tree or parse result,
        /// primarily for debugging or advanced prompt usage.
        /// </summary>
        public string RawSyntaxTree { get; set; }
    }

    /// <summary>
    /// Language-agnostic representation of a class or class-like type.
    /// </summary>
    public class ClassDeclaration
    {
        public string Name { get; set; }

        /// <summary>
        /// 1-based line number where the declaration starts.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Total number of lines occupied by the declaration.
        /// </summary>
        public int LineCount { get; set; }

        /// <summary>
        /// Base types, implemented interfaces, or prototype chains, expressed generically as strings.
        /// </summary>
        public List<string> BaseTypes { get; set; } = new();

        /// <summary>
        /// Methods or behavior members of this class.
        /// </summary>
        public List<FunctionDeclaration> Methods { get; set; } = new();

        /// <summary>
        /// Properties / fields / attributes represented generically.
        /// </summary>
        public List<PropertyDeclaration> Properties { get; set; } = new();

        public AccessModifier AccessModifier { get; set; }

        /// <summary>
        /// Cyclomatic complexity of the class body (aggregated or analyzer-specific).
        /// </summary>
        public int CyclomaticComplexity { get; set; }
    }

    /// <summary>
    /// Language-agnostic representation of a function, method, or callable symbol.
    /// </summary>
    public class FunctionDeclaration
    {
        public string Name { get; set; }

        /// <summary>
        /// 1-based line number where the declaration starts.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Total number of lines occupied by the declaration.
        /// </summary>
        public int LineCount { get; set; }

        /// <summary>
        /// Parameters of the function. For dynamic languages, Type may be null or empty.
        /// </summary>
        public List<ParameterDeclaration> Parameters { get; set; } = new();

        /// <summary>
        /// Return type name, if available. In dynamic languages this may be null or empty.
        /// </summary>
        public string ReturnType { get; set; }

        public AccessModifier AccessModifier { get; set; }

        /// <summary>
        /// Cyclomatic complexity of the function.
        /// </summary>
        public int CyclomaticComplexity { get; set; }

        /// <summary>
        /// Indicates whether this function is asynchronous, if the language supports async.
        /// </summary>
        public bool IsAsync { get; set; }

        /// <summary>
        /// Indicates whether this function is static (or top-level) where applicable.
        /// </summary>
        public bool IsStatic { get; set; }
    }

    /// <summary>
    /// Language-agnostic representation of a property, field, or attribute.
    /// </summary>
    public class PropertyDeclaration
    {
        public string Name { get; set; }

        /// <summary>
        /// Type name if available; for dynamic languages this may be null or empty.
        /// </summary>
        public string Type { get; set; }

        public AccessModifier AccessModifier { get; set; }

        public bool HasGetter { get; set; }

        public bool HasSetter { get; set; }
    }

    /// <summary>
    /// Language-agnostic representation of a parameter in a callable.
    /// </summary>
    public class ParameterDeclaration
    {
        public string Name { get; set; }

        /// <summary>
        /// Type name if available; for dynamic languages this may be null or empty.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Indicates if the parameter is optional (default value, varargs, etc.).
        /// </summary>
        public bool IsOptional { get; set; }

        /// <summary>
        /// The default value as a string representation, if any.
        /// </summary>
        public string DefaultValue { get; set; }
    }

    /// <summary>
    /// Language-agnostic representation of an import / using / require / include statement.
    /// </summary>
    public class ImportDeclaration
    {
        /// <summary>
        /// The module, package, namespace, or file being imported.
        /// </summary>
        public string ModuleName { get; set; }

        /// <summary>
        /// Symbols imported from the module. Empty list means the entire module is referenced.
        /// </summary>
        public List<string> ImportedSymbols { get; set; } = new();

        /// <summary>
        /// Indicates if this is a wildcard import (e.g., using *, import * as, etc.).
        /// </summary>
        public bool IsWildcard { get; set; }
    }

    /// <summary>
    /// Represents a detected pattern in the code for security, performance, architecture, or general quality.
    /// </summary>
    public class CodePattern
    {
        public PatternType Type { get; set; }

        /// <summary>
        /// Human-readable description that can be passed directly into AI prompts.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 1-based line number where the pattern was detected.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Representative snippet of the code around the pattern.
        /// </summary>
        public string CodeSnippet { get; set; }

        /// <summary>
        /// Relative risk associated with this pattern for prioritization.
        /// </summary>
        public RiskLevel Risk { get; set; }
    }

    public enum PatternType
    {
        SecurityVulnerability,
        PerformanceIssue,
        CodeSmell,
        BestPracticeViolation,
        ArchitecturalConcern
    }

    public enum AccessModifier
    {
        Public,
        Private,
        Protected,
        Internal,
        ProtectedInternal,
        PrivateProtected,
        Unknown
    }

    public enum RiskLevel
    {
        Low,
        Medium,
        High,
        Critical
    }
}



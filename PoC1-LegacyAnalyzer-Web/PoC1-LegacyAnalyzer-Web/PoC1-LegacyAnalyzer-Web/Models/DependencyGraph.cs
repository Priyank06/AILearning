namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Represents a dependency graph of code elements across files.
    /// </summary>
    public class DependencyGraph
    {
        public List<DependencyNode> Nodes { get; set; } = new();
        public List<DependencyEdge> Edges { get; set; } = new();
        public Dictionary<string, List<string>> FileDependencies { get; set; } = new(); // File -> List of files it depends on
        public Dictionary<string, List<string>> FileDependents { get; set; } = new(); // File -> List of files that depend on it
    }

    /// <summary>
    /// Represents a node in the dependency graph (class, method, property, etc.)
    /// </summary>
    public class DependencyNode
    {
        public string Id { get; set; } = string.Empty; // Unique identifier: "FileName::Namespace::ClassName::MethodName"
        public string Name { get; set; } = string.Empty; // Display name
        public string FileName { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Class", "Method", "Property", "Interface"
        public string FullName { get; set; } = string.Empty; // Fully qualified name
        public int LineNumber { get; set; }
        public int Connectivity { get; set; } // Number of connections (for God Object detection)
    }

    /// <summary>
    /// Represents an edge in the dependency graph (dependency relationship)
    /// </summary>
    public class DependencyEdge
    {
        public string SourceId { get; set; } = string.Empty; // Source node ID
        public string TargetId { get; set; } = string.Empty; // Target node ID
        public string SourceFile { get; set; } = string.Empty;
        public string TargetFile { get; set; } = string.Empty;
        public DependencyType Type { get; set; } // Type of dependency
        public string Description { get; set; } = string.Empty; // Human-readable description
        public int LineNumber { get; set; } // Line where dependency occurs
    }

    /// <summary>
    /// Types of dependencies between code elements
    /// </summary>
    public enum DependencyType
    {
        MethodCall,           // Method calls another method
        Inheritance,          // Class inherits from another class
        InterfaceImplementation, // Class implements interface
        PropertyAccess,       // Property access
        FieldAccess,          // Field access
        TypeReference,        // Type reference (parameter, return type, etc.)
        GenericConstraint,   // Generic type constraint
        Attribute            // Attribute usage
    }

    /// <summary>
    /// Impact analysis for a code element (what breaks if this changes)
    /// </summary>
    public class DependencyImpact
    {
        public string ElementId { get; set; } = string.Empty;
        public string ElementName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int AffectedFilesCount { get; set; }
        public List<string> AffectedFiles { get; set; } = new();
        public int AffectedClassesCount { get; set; }
        public List<string> AffectedClasses { get; set; } = new();
        public int AffectedMethodsCount { get; set; }
        public List<string> AffectedMethods { get; set; } = new();
        public bool IsGodObject { get; set; } // High connectivity (threshold: >20 connections)
        public bool IsInCycle { get; set; } // Part of cyclic dependency
        public List<List<string>> Cycles { get; set; } = new(); // List of cycles this element is part of
        public string RiskLevel { get; set; } = "Low"; // "Low", "Medium", "High", "Critical"
    }

    /// <summary>
    /// Cyclic dependency detection result
    /// </summary>
    public class CyclicDependency
    {
        public List<string> Cycle { get; set; } = new(); // List of file/class names in cycle
        public DependencyType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Severity { get; set; } // 1-5, higher is worse
    }
}


namespace PoC1_LegacyAnalyzer_Web.Models
{
    public class CodePatternAnalysis
    {
        // Legacy properties for backward compatibility
        public List<string> SecurityFindings { get; set; } = new();
        public List<string> PerformanceFindings { get; set; } = new();
        public List<string> ArchitectureFindings { get; set; } = new();
        
        // Security patterns
        public bool HasSqlInjectionRisk { get; set; }
        public bool HasHardcodedSecrets { get; set; }
        public bool HasWeakCryptography { get; set; }
        public bool HasPathTraversalRisk { get; set; }
        public bool HasCommandInjectionRisk { get; set; }

        // Legacy patterns
        public bool UsesDeprecatedApis { get; set; }
        public bool HasLegacyDataAccess { get; set; }
        public bool UsesOutdatedFrameworks { get; set; }

        // Code quality patterns
        public bool HasLongMethods { get; set; }
        public bool HasDeepNesting { get; set; }
        public bool HasMagicNumbers { get; set; }
        public bool HasEmptyCatchBlocks { get; set; }
        public bool HasTodoComments { get; set; }

        // Architecture patterns
        public bool UsesDesignPatterns { get; set; }
        public List<string> DetectedPatterns { get; set; } = new();
        public List<string> AntiPatterns { get; set; } = new();

        // Specific code smells with locations
        public List<CodeSmell> CodeSmells { get; set; } = new();
    }
    
    public class CodeSmell
    {
        public string SmellType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int LineNumber { get; set; }
    }
}


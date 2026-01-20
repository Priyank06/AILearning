# Code Analysis Services

This folder contains services for static code analysis, pattern detection, complexity calculation, and code structure analysis. These services do NOT use AI/LLM - they perform traditional static analysis.

## Purpose

Services in this folder handle:
- **Static Code Analysis** - Roslyn-based C# analysis, Tree-sitter for other languages
- **Pattern Detection** - Detecting code patterns and anti-patterns
- **Complexity Calculation** - Calculating code complexity metrics
- **Metadata Extraction** - Extracting code metadata (classes, methods, dependencies)
- **File Preprocessing** - Preparing files for analysis
- **Cross-File Analysis** - Analyzing dependencies across files
- **Dependency Graph** - Building and managing dependency graphs

## Services

- `CodeAnalysisService` - Core code analysis service
- `PatternDetectionService` - Pattern detection
- `LegacyPatternDetectionService` - Legacy code pattern detection
- `ComplexityCalculationService` - Complexity metrics
- `ComplexityCalculatorService` - Alternative complexity calculator
- `MetadataExtractionService` - Code metadata extraction
- `FilePreProcessingService` - File preprocessing facade
- `FileFilteringService` - File filtering logic
- `CrossFileAnalyzer` - Cross-file dependency analysis
- `DependencyGraphService` - Dependency graph management
- `HybridMultiLanguageAnalyzer` - Multi-language analysis
- `CodeAnalysisAgentService` - Agent wrapper for code analysis

## Dependencies

These services depend on:
- `Services/Analysis/` - Language-specific analyzers (Roslyn, Tree-sitter)
- `Services/Infrastructure/` - Caching, file operations

## Lifetime

Code analysis services are registered as `Scoped` for per-request isolation.

## Related Folders

- `Services/Analysis/` - Language-specific analyzers (RoslynCSharpAnalyzer, TreeSitter analyzers)
- `Services/AI/` - AI services that may use code analysis results


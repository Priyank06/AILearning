# Project Analysis Services

This folder contains services for project-level analysis, including metadata extraction, folder analysis, architecture assessment, and project insights.

## Purpose

Services in this folder handle:
- **Project Metadata** - Extracting and managing project-level metadata
- **Folder Analysis** - Analyzing folder structures and organization
- **Architecture Assessment** - Assessing project architecture
- **Project Insights** - Generating project-level insights
- **Enhanced Project Analysis** - Comprehensive project analysis

## Services

- `ProjectMetadataService` - Project metadata extraction and management
- `FolderAnalysisService` - Folder structure analysis
- `ArchitectureAssessmentService` - Architecture assessment
- `ProjectInsightsGenerator` - Project insights generation
- `EnhancedProjectAnalysisService` - Comprehensive project analysis

## Dependencies

These services depend on:
- `Services/CodeAnalysis/` - Code analysis for project-level insights
- `Services/Business/` - Business impact calculations
- `Services/Infrastructure/` - File operations

## Lifetime

Project analysis services are registered as `Scoped` for per-request isolation.

## Related Folders

- `Services/CodeAnalysis/` - Uses code analysis for project insights
- `Services/Business/` - Business impact assessment
- `Models/ProjectAnalysis/` - Project analysis models


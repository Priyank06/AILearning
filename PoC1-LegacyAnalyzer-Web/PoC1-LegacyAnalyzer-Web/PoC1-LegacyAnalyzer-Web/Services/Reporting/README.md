# Reporting Services

This folder contains services for generating reports, summaries, and documentation from analysis results.

## Purpose

Services in this folder handle:
- **Report Generation** - Creating markdown, HTML, or other format reports
- **Team Reports** - Generating multi-agent team analysis reports
- **Report Formatting** - Formatting analysis results for presentation

## Services

- `ReportService` - Core report generation service
- `TeamReportService` - Multi-agent team report generation

## Dependencies

These services depend on:
- `Services/Orchestration/` - Analysis results
- `Services/Business/` - Business metrics for reports
- `Services/Infrastructure/` - File operations for report output

## Lifetime

Reporting services are registered as `Scoped` for per-request isolation.

## Related Folders

- `Services/Orchestration/` - Source of analysis results
- `Services/Business/` - Business metrics included in reports


# AI Learning Environment

This folder contains your complete AI learning journey setup optimized for 256GB MacBook.

## Folder Structure

```
~/AILearning/
├── Core/                          # Shared libraries and services
├── PoC1-LegacyAnalyzer/          # Legacy Code Migration Assistant
├── PoC2-DatabaseAnalyzer/        # Smart Database Schema Analyzer  
├── PoC3-ResourceOptimizer/       # Intelligent Resource Allocation
├── Learning/                     # Daily/weekly experiments
├── Resources/                    # Notes, screenshots, documentation
├── Scripts/                      # Management scripts
└── Archive/                      # Compressed old projects
```

## Quick Commands

```bash
ai-cd          # Navigate to AI Learning folder
ai-clean       # Clean build outputs and cache
ai-space       # Check space usage and status
ai-new MyApp   # Create new project (console/maui/blazor)
ai-archive     # Archive old project to save space
ai-status      # Quick status check
```

## Scripts

- `cleanup.sh` - Clean build outputs, NuGet cache, temp files
- `space-check.sh` - Monitor disk usage and project sizes
- `archive.sh` - Compress and archive old projects
- `new-project.sh` - Quick project creation with common packages

## Space Management

- **Target Usage**: < 20GB total
- **Weekly Cleanup**: Run `ai-clean` every Sunday
- **Archive When**: Project completed or > 500MB
- **Monitor**: Run `ai-space` when needed

## Getting Started

1. `ai-cd` - Navigate to learning folder
2. `ai-new Day1Test console` - Create your first test project
3. `ai-space` - Check your space usage
4. `ai-clean` - Clean up when done

Happy learning! 🚀

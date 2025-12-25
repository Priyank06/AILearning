# Ground Truth Validation System

## Overview

The Ground Truth Validation System allows you to measure the quality and accuracy of AI-generated code analysis findings by comparing them against benchmark datasets with **known, verified issues**.

## Key Features

✅ **Precision & Recall Metrics** - Measure what percentage of AI findings are correct (precision) and what percentage of real issues were detected (recall)
✅ **F1 Score Calculation** - Get an overall quality indicator combining precision and recall
✅ **Per-Agent Metrics** - See how each specialist agent (Security, Performance, Architecture) performs individually
✅ **Per-Category Analysis** - Understand accuracy across different issue types (SQL Injection, Performance, etc.)
✅ **False Positive/Negative Tracking** - Identify what the AI got wrong
✅ **Sample Benchmark Dataset** - Ready-to-use dataset with 11 known legacy code issues

## How It Works

### 1. Ground Truth Dataset Structure

A ground truth dataset contains:
- **Benchmark Files**: Code files with known issues
- **Ground Truth Issues**: Verified, documented problems in those files
- **Expected Detectors**: Which agents should find each issue

### 2. Validation Process

```
┌─────────────────────┐
│ 1. Load Dataset     │  Load benchmark with known issues
└──────────┬──────────┘
           ▼
┌─────────────────────┐
│ 2. Run AI Analysis  │  Analyze benchmark files with your AI agents
└──────────┬──────────┘
           ▼
┌─────────────────────┐
│ 3. Match Findings   │  Compare AI findings to ground truth
└──────────┬──────────┘
           ▼
┌─────────────────────┐
│ 4. Calculate Metrics│  Compute precision, recall, F1 score
└─────────────────────┘
```

### 3. Metrics Explained

| Metric | Formula | Meaning | Target |
|--------|---------|---------|--------|
| **Precision** | TP / (TP + FP) | % of AI findings that are correct | ≥ 85% |
| **Recall** | TP / (TP + FN) | % of real issues AI detected | ≥ 80% |
| **F1 Score** | 2 × (P × R) / (P + R) | Overall quality (harmonic mean) | ≥ 75% |

**Where:**
- **TP (True Positives)** = AI correctly detected a real issue
- **FP (False Positives)** = AI found an issue that doesn't exist
- **FN (False Negatives)** = AI missed a known issue

## Usage Guide

### Using the UI (Recommended)

1. Navigate to `/groundtruth` page
2. Click **"Load Sample Dataset"** to use the built-in benchmark
3. Click **"Run Validation"**
4. Wait 1-2 minutes for analysis to complete
5. View precision, recall, and F1 score metrics

### Programmatic Usage

```csharp
// 1. Inject the service
@inject IGroundTruthValidationService ValidationService

// 2. Load or create a dataset
var dataset = LegacyCodeBenchmark.CreateSampleDataset();

// Or load from file
var dataset = await ValidationService.LoadDatasetAsync("path/to/dataset.json");

// 3. Run your AI analysis
var analysisResult = await AnalysisService.AnalyzeMultipleFilesAsync(files, objective, null, default);

// 4. Validate against ground truth
var validationResult = await ValidationService.ValidateAsync(
    analysisResult,
    dataset,
    new ValidationConfiguration
    {
        MinMatchConfidence = 70.0,
        AllowedSeverityDifference = 1,
        CountPartialMatchesAsTruePositives = true
    });

// 5. View metrics
Console.WriteLine($"Precision: {validationResult.OverallMetrics.Precision:F1}%");
Console.WriteLine($"Recall: {validationResult.OverallMetrics.Recall:F1}%");
Console.WriteLine($"F1 Score: {validationResult.OverallMetrics.F1Score:F1}%");
```

## Creating Custom Datasets

### Using the Builder API

```csharp
var dataset = new GroundTruthDatasetBuilder("My Dataset", "Description")
    .AddFile("UserService.cs", "CSharp", codeContent, "Services/UserService.cs")
    .AddSecurityIssue(
        "UserService.cs",
        "SQL Injection",
        "String concatenation used in SQL query",
        "CRITICAL",
        "UserService.GetUser",
        lineNumber: 42,
        codeSnippet: "var sql = \"SELECT * FROM Users WHERE Id=\" + userId;"
    )
    .AddPerformanceIssue(
        "UserService.cs",
        "N+1 Query",
        "Loop executes separate query for each iteration",
        "HIGH",
        "UserService.LoadUserOrders",
        lineNumber: 78
    )
    .WithTags("legacy", "asp.net", "security")
    .Build();

// Save to file
await ValidationService.SaveDatasetAsync(dataset, "my-dataset.json");
```

### Manual JSON Format

```json
{
  "id": "guid",
  "name": "My Custom Dataset",
  "version": "1.0",
  "description": "Dataset description",
  "issues": [
    {
      "fileName": "UserService.cs",
      "category": "SQL Injection",
      "description": "Detailed issue description",
      "severity": "CRITICAL",
      "location": "UserService.GetUser",
      "lineNumber": 42,
      "codeSnippet": "var sql = ...",
      "expectedDetectorAgents": ["Security"],
      "isMandatory": true
    }
  ],
  "files": [
    {
      "fileName": "UserService.cs",
      "language": "CSharp",
      "content": "... full file content ...",
      "relativePath": "Services/UserService.cs"
    }
  ]
}
```

## Sample Dataset Details

The built-in sample dataset (`LegacyCodeBenchmark`) includes:

### Files (5 total)
1. **UserRepository.cs** - Legacy data access with SQL injection
2. **UserProfile.aspx.cs** - ASP.NET WebForms with global state
3. **UserManager.cs** - God Object anti-pattern
4. **EmailService.cs** - Hardcoded credentials
5. **FileProcessor.cs** - Missing error handling and resource leaks

### Issues (11 total)

| Category | Severity | Count | Examples |
|----------|----------|-------|----------|
| Security | CRITICAL | 2 | SQL Injection, Hardcoded Credentials |
| Security | MEDIUM | 1 | Missing Error Handling |
| Performance | HIGH | 2 | Synchronous I/O, Resource Leaks |
| Performance | MEDIUM | 2 | DataSet/DataTable Usage, ViewState Overhead |
| Architecture | HIGH | 2 | Global State (HttpContext.Current), God Object |
| Architecture | MEDIUM | 1 | Session State Manipulation |

### Expected Detection Rates

**Target Metrics for Sample Dataset:**
- **Precision:** ≥ 85% (AI should not hallucinate many false issues)
- **Recall:** ≥ 80% (AI should catch at least 9 out of 11 issues)
- **F1 Score:** ≥ 80% (Overall quality threshold)

## Quality Benchmarks

| F1 Score Range | Quality Level | Status | Action |
|----------------|---------------|--------|--------|
| ≥ 85% | **Excellent** | ✅ Production Ready | Deploy with confidence |
| 75-84% | **Good** | ✅ Ready | Suitable for most use cases |
| 65-74% | **Moderate** | ⚠️ Needs Work | Improve prompts or validation |
| < 65% | **Low** | ❌ Not Ready | Significant improvements needed |

## Validation Configuration Options

```csharp
new ValidationConfiguration
{
    // Minimum confidence to count as a match (0-100%)
    MinMatchConfidence = 70.0,

    // Allow severity to differ by N levels (LOW→MEDIUM=1, LOW→HIGH=2)
    AllowedSeverityDifference = 1,

    // Allow line numbers to differ by ±N lines
    AllowedLineNumberDifference = 5,

    // Count partial matches (category matches but severity/location differ) as true positives
    CountPartialMatchesAsTruePositives = true,

    // Weights for match confidence calculation
    CategoryMatchWeight = 0.5,   // Category is most important
    SeverityMatchWeight = 0.3,   // Severity is moderately important
    LocationMatchWeight = 0.2    // Location is least important
}
```

## Best Practices

### 1. **Start with Sample Dataset**
   - Use the built-in benchmark to establish a baseline
   - Target: F1 ≥ 80% on sample dataset before production use

### 2. **Create Domain-Specific Datasets**
   - Build datasets matching your actual codebase patterns
   - Include issues you care about most (SQL injection, performance, etc.)

### 3. **Balance Difficulty**
   - Include easy issues (hardcoded "password"), medium (N+1 queries), and hard (subtle race conditions)
   - Track metrics by difficulty level

### 4. **Update Regularly**
   - Add new issues as you discover false positives/negatives
   - Re-validate after prompt changes or model updates

### 5. **Track Trends Over Time**
   - Run validation monthly to detect quality degradation
   - Compare metrics across different LLM models (GPT-3.5 vs GPT-4)

## Interpreting Results

### High Precision, Low Recall
```
Precision: 95%, Recall: 60%, F1: 74%
```
**Problem:** AI is very accurate but misses many issues
**Action:** Improve prompts to be more comprehensive; reduce confidence thresholds

### Low Precision, High Recall
```
Precision: 65%, Recall: 90%, F1: 75%
```
**Problem:** AI finds issues but hallucinates many false positives
**Action:** Strengthen validation rules; increase confidence thresholds; add hallucination guardrails

### Balanced Performance
```
Precision: 85%, Recall: 82%, F1: 83%
```
**Status:** ✅ Good quality, production-ready

## Troubleshooting

### Issue: All findings are false positives
**Cause:** Ground truth and AI findings use different category names
**Solution:** Check category matching logic; use `AreCategorySynonyms` to add aliases

### Issue: F1 score is unexpectedly low
**Causes:**
1. Ground truth issues are too subtle for AI to detect (increase difficulty metadata)
2. Prompts don't emphasize important issue types (update agent prompts)
3. Validation configuration is too strict (relax `MinMatchConfidence`)

### Issue: Metrics vary wildly between runs
**Cause:** LLM non-determinism
**Solution:** Run validation 5-10 times and average metrics; see [Determinism Measurement](./DeterminismMeasurement.md)

## API Reference

### IGroundTruthValidationService

```csharp
public interface IGroundTruthValidationService
{
    // Validate AI findings against ground truth
    Task<GroundTruthValidationResult> ValidateAsync(
        TeamAnalysisResult analysisResult,
        GroundTruthDataset groundTruthDataset,
        ValidationConfiguration? configuration = null);

    // Load/save datasets
    Task<GroundTruthDataset> LoadDatasetAsync(string datasetPath);
    Task SaveDatasetAsync(GroundTruthDataset dataset, string datasetPath);

    // Create new dataset
    GroundTruthDataset CreateDataset(string name, string description);

    // Generate reports
    string GenerateSummaryReport(GroundTruthValidationResult validationResult);
}
```

## Future Enhancements

- [ ] Automated dataset generation from git blame + bug tracking systems
- [ ] Multi-language benchmark datasets (Python, JavaScript, Java)
- [ ] Historical trend tracking (quality over time)
- [ ] Confidence calibration based on validation results
- [ ] Integration with CI/CD pipelines
- [ ] Community-contributed benchmark datasets

## Related Documentation

- [Determinism Measurement](./DeterminismMeasurement.md) - Measuring consistency across runs
- [Confidence Calibration](./ConfidenceCalibration.md) - Adjusting confidence scores based on accuracy
- [Hallucination Guardrails](./HallucinationGuardrails.md) - Reducing false positives

## Support

For issues or questions:
1. Check the [FAQ](./FAQ.md)
2. Review validation logs in Application Insights
3. Create an issue on GitHub with validation results attached

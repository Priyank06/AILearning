# Determinism Measurement System

## Overview

The Determinism Measurement System quantifies how **consistent** AI-generated code analysis findings are across multiple runs of the same input. Since Large Language Models (LLMs) are inherently non-deterministic, measuring consistency helps establish trust and reliability in AI-generated insights.

## Key Features

✅ **Consistency Scoring** - Measures finding reproducibility (0-100% determinism score)
✅ **Multi-Run Testing** - Runs analysis 2-50 times (default: 10) on identical inputs
✅ **Per-Agent Metrics** - See how each specialist (Security, Performance, Architecture) performs individually
✅ **Per-Category Analysis** - Understand consistency across different issue types
✅ **Finding Classification** - Identifies fully consistent, highly consistent, and inconsistent findings
✅ **Statistical Analysis** - Provides mean, standard deviation, and range of finding counts
✅ **Actionable Recommendations** - Suggests improvements based on consistency scores

## Why Determinism Matters

### 1. **Trust & Confidence**
Users need to trust that findings are reproducible. If the same file analyzed twice produces wildly different results, confidence erodes.

### 2. **Quality Indicator**
Low determinism often indicates:
- Prompts are too vague or open-ended
- Temperature setting is too high
- Model is hallucinating or guessing
- Analysis logic needs refinement

### 3. **Prompt Engineering Feedback**
Determinism metrics help you tune prompts. After prompt changes, re-run determinism tests to see if consistency improved.

### 4. **Model Comparison**
Compare determinism across different LLM models (GPT-3.5 vs GPT-4) or versions to select the most reliable option.

## How It Works

### 1. Test Process

```
┌─────────────────────┐
│ 1. Upload Files     │  Select code files for testing
└──────────┬──────────┘
           ▼
┌─────────────────────┐
│ 2. Configure Test   │  Set runs (default: 10), temperature, agents
└──────────┬──────────┘
           ▼
┌─────────────────────┐
│ 3. Run N Times      │  Execute identical analysis N times
└──────────┬──────────┘
           ▼
┌─────────────────────┐
│ 4. Match Findings   │  Compare findings across runs using normalized keys
└──────────┬──────────┘
           ▼
┌─────────────────────┐
│ 5. Calculate Metrics│  Compute determinism score and statistics
└─────────────────────┘
```

### 2. Finding Matching Logic

Findings are matched across runs using **normalized keys**:

```csharp
FindingKey = "{category}|{normalizedLocation}"
```

**Normalization:**
- Line numbers are wildcarded: `:42` → `:*`
- Method signatures are simplified
- Case-insensitive matching
- Whitespace normalized

**Example:**
```
Original: "SQL Injection in UserService.GetUser():42"
Key:      "sql injection|userservice.getuser:*"
```

This allows findings to match even if line numbers vary slightly between runs.

### 3. Consistency Classification

| Appearance Rate | Classification | Meaning |
|----------------|----------------|---------|
| **100%** | Fully Consistent | Found in every single run |
| **≥ 80%** | Highly Consistent | Found in ≥80% of runs |
| **50-79%** | Moderately Consistent | Found in 50-79% of runs |
| **< 50%** | Inconsistent / Poorly Consistent | Found in <50% of runs |

### 4. Score Calculation

```
Determinism Score = Σ (Finding Appearance Rate × Weight) / Σ Weights
```

Where:
- **Weight** = Severity multiplier (CRITICAL=3, HIGH=2, MEDIUM=1.5, LOW=1)
- **Appearance Rate** = % of runs where finding appeared

**Example:**
- Finding A (CRITICAL): appeared in 10/10 runs → 100% × 3 = 300 points
- Finding B (HIGH): appeared in 8/10 runs → 80% × 2 = 160 points
- Finding C (MEDIUM): appeared in 5/10 runs → 50% × 1.5 = 75 points

```
Score = (300 + 160 + 75) / (3 + 2 + 1.5) = 535 / 6.5 = 82.3%
```

## Consistency Levels

| Score Range | Level | Badge Color | Interpretation |
|------------|-------|-------------|----------------|
| **≥ 90%** | Excellent | Green | Highly reliable and reproducible findings. Production ready. |
| **80-89%** | Good | Blue | Generally reliable with minor variations. Suitable for most use cases. |
| **70-79%** | Moderate | Yellow | Some findings vary between runs. Consider reviewing prompts. |
| **60-69%** | Fair | Orange | Findings vary noticeably. Use with caution; improvements needed. |
| **< 60%** | Poor | Red | Unreliable; significant prompt tuning or temperature adjustment needed. |

## Usage Guide

### Using the UI (Recommended)

1. Navigate to `/determinism` page
2. Upload code files (C#, Python, Java, JavaScript, etc.)
3. Configure test parameters:
   - **Number of Runs:** 2-50 (default: 10)
   - **Temperature:** 0.0-1.0 (default: 0.3)
   - **Agents:** Select which specialists to test
4. Click **"Run Determinism Test"**
5. Wait for completion (≈15 seconds per run × number of runs)
6. Review consistency metrics and findings

### Programmatic Usage

```csharp
// 1. Inject the service
@inject IDeterminismMeasurementService DeterminismService

// 2. Configure the test
var configuration = new DeterminismConfiguration
{
    NumberOfRuns = 10,
    Temperature = 0.3,
    IncludeRawResults = true,
    GroupByAgent = true,
    GroupByCategory = true
};

// 3. Run the test with progress reporting
var progress = new Progress<string>(message =>
{
    Console.WriteLine(message);
});

var result = await DeterminismService.MeasureDeterminismAsync(
    files,
    "Comprehensive legacy code analysis",
    new List<string> { "security", "performance", "architecture" },
    configuration,
    progress,
    cancellationToken);

// 4. View metrics
Console.WriteLine($"Determinism Score: {result.DeterminismScore:F1}%");
Console.WriteLine($"Consistency Level: {result.ConsistencyLevel}");
Console.WriteLine($"Consistent Findings: {result.Statistics.FullyConsistentFindings}");
Console.WriteLine($"Inconsistent Findings: {result.Statistics.PoorlyConsistentFindings}");

// 5. Analyze per-agent metrics
foreach (var (agent, metrics) in result.MetricsByAgent)
{
    Console.WriteLine($"{agent}: {metrics.DeterminismScore:F1}%");
}
```

## Configuration Options

### DeterminismConfiguration

```csharp
new DeterminismConfiguration
{
    // Number of analysis runs to perform (2-50)
    NumberOfRuns = 10,

    // LLM temperature for testing (0.0-1.0)
    // Lower = more deterministic, Higher = more creative/varied
    Temperature = 0.3,

    // Consistency thresholds
    FullyConsistentThreshold = 100.0,      // 100% appearance rate
    HighlyConsistentThreshold = 80.0,      // ≥80% appearance rate
    ModeratelyConsistentThreshold = 50.0,  // 50-79% appearance rate

    // Include raw analysis results in output (for debugging)
    IncludeRawResults = true,

    // Calculate metrics grouped by agent
    GroupByAgent = true,

    // Calculate metrics grouped by category
    GroupByCategory = true,

    // Random seed for reproducibility (optional)
    RandomSeed = null
}
```

## Interpreting Results

### High Determinism (≥ 90%)

```
Score: 95%, Excellent Consistency
Consistent Findings: 28, Inconsistent: 2
```

**Interpretation:** AI is highly reliable. Findings are reproducible across runs.

**Action:** ✅ Production ready. No changes needed.

---

### Moderate Determinism (70-79%)

```
Score: 75%, Moderate Consistency
Consistent Findings: 15, Inconsistent: 12
```

**Interpretation:** Some findings vary. May indicate:
- Temperature too high (>0.3)
- Prompts need more structure/examples
- Model is less deterministic for certain issue types

**Action:** ⚠️ Improve prompts or lower temperature to 0.1-0.2.

---

### Low Determinism (< 60%)

```
Score: 52%, Poor Consistency
Consistent Findings: 5, Inconsistent: 22
```

**Interpretation:** Findings are unreliable. Likely causes:
- Vague or ambiguous prompts
- Temperature too high
- Model hallucinating
- Analysis logic flawed

**Action:** ❌ Significant improvements needed:
1. Lower temperature to 0.1
2. Add concrete examples to prompts
3. Use structured output formats (JSON)
4. Test with a different LLM model

## Improving Determinism

### 1. **Lower Temperature**

Temperature controls randomness:

| Temperature | Effect | Use Case |
|------------|--------|----------|
| **0.0-0.2** | Highly deterministic, focused | Production analysis |
| **0.3-0.5** | Balanced creativity/consistency | Development, testing |
| **0.6-1.0** | Creative, varied outputs | Brainstorming, exploration |

**Recommendation:** Start with 0.3, lower to 0.1-0.2 if determinism < 80%.

### 2. **Refine Prompts**

**Bad (Vague):**
```
"Analyze this code and find issues."
```

**Good (Specific with Examples):**
```
"Analyze this C# code for:
1. SQL Injection (string concatenation in queries)
   Example: var sql = "SELECT * FROM Users WHERE Id=" + userId;
2. Hardcoded credentials (passwords, API keys)
   Example: var password = "admin123";
3. Missing error handling (no try-catch)
   Example: file.ReadAllText() without try-catch

For each issue, provide:
- Category: [SQL Injection, Hardcoded Credential, etc.]
- Severity: [CRITICAL, HIGH, MEDIUM, LOW]
- Location: ClassName.MethodName:LineNumber
- Description: Brief explanation
"
```

### 3. **Use Structured Output**

Enforce JSON schema for findings:

```json
{
  "findings": [
    {
      "category": "SQL Injection",
      "severity": "CRITICAL",
      "location": "UserService.GetUser:42",
      "description": "String concatenation in SQL query"
    }
  ]
}
```

This reduces parsing ambiguity and improves consistency.

### 4. **Test Different Models**

| Model | Typical Determinism | Notes |
|-------|---------------------|-------|
| **GPT-4** | 85-95% | Higher quality, more consistent |
| **GPT-3.5-turbo** | 70-85% | Faster, less consistent |
| **Claude 3** | 80-90% | Good balance |
| **Llama 3** | 60-75% | Open source, more variation |

**Tip:** Run determinism tests on each model with same prompts to compare.

### 5. **Increase Sample Size**

Run more iterations for statistical confidence:

| Runs | Confidence Level | Use Case |
|------|------------------|----------|
| **2-5** | Low | Quick tests during development |
| **10** | Medium | Standard testing (default) |
| **20-50** | High | Production validation, benchmarking |

## Best Practices

### 1. **Baseline Testing**

Before making prompt changes:
1. Run determinism test with 10 runs
2. Record baseline score (e.g., 82%)
3. Make prompt changes
4. Re-run determinism test
5. Compare: Did score improve?

### 2. **Track Trends Over Time**

Run determinism tests monthly:
- Detect quality degradation
- Validate model updates (GPT-4 → GPT-4.5)
- Ensure prompt changes didn't harm consistency

### 3. **Per-Agent Testing**

Test each agent individually:
```csharp
// Test Security Agent only
var securityResult = await DeterminismService.MeasureDeterminismAsync(
    files, objective, new List<string> { "security" }, config, progress, ct);

// Test Performance Agent only
var perfResult = await DeterminismService.MeasureDreteminismAsync(
    files, objective, new List<string> { "performance" }, config, progress, ct);
```

This identifies which agents need improvement.

### 4. **Representative Files**

Use files that represent your production codebase:
- Typical size (500-1000 lines)
- Common patterns (legacy, modern)
- Mix of issue severities
- Multiple languages if applicable

### 5. **Budget for Testing**

Determinism tests consume API tokens:

```
Cost = Runs × Files × (AvgInputTokens + AvgOutputTokens) × TokenPrice
```

**Example:**
- 10 runs
- 3 files × 2000 tokens each = 6000 input tokens/run
- 1500 output tokens/run
- Total: 10 × (6000 + 1500) = 75,000 tokens
- GPT-4 cost: $0.75-$1.50

**Tip:** Start with 5 runs and 1 file for quick tests.

## Troubleshooting

### Issue: Determinism score is unexpectedly low (< 60%)

**Possible Causes:**
1. Temperature too high (>0.5)
2. Prompts are vague or ambiguous
3. Model is hallucinating
4. Finding matching logic too strict

**Solutions:**
- Lower temperature to 0.1-0.2
- Add specific examples to prompts
- Check logs for hallucinated categories
- Review finding normalization logic

---

### Issue: All findings are "inconsistent"

**Possible Causes:**
1. Finding keys don't match due to normalization issues
2. Model is generating completely different findings each run
3. Randomness is too high

**Solutions:**
- Review normalization logic in `GenerateFindingKey()`
- Check raw results (`IncludeRawResults = true`)
- Lower temperature dramatically (0.0-0.1)

---

### Issue: Some agents have high determinism, others low

**Interpretation:** This is normal. Some agents (Security) tend to be more consistent than others (Architecture).

**Example:**
- Security Agent: 92% (specific patterns: SQL injection, XSS)
- Performance Agent: 85% (measurable: N+1 queries, synchronous I/O)
- Architecture Agent: 68% (subjective: "code smells", SOLID violations)

**Action:** Focus improvements on low-scoring agents.

---

### Issue: Determinism varies wildly between files

**Interpretation:** Some files are inherently harder to analyze consistently.

**Examples:**
- **High Determinism:** Files with clear, objective issues (SQL injection)
- **Low Determinism:** Files with subjective issues (code smells, naming)

**Action:** Track per-file metrics if needed.

## API Reference

### IDeterminismMeasurementService

```csharp
public interface IDeterminismMeasurementService
{
    /// <summary>
    /// Measure determinism by running analysis multiple times
    /// </summary>
    Task<DeterminismResult> MeasureDeterminismAsync(
        List<IBrowserFile> files,
        string businessObjective,
        List<string> requiredSpecialties,
        DeterminismConfiguration? configuration = null,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
```

### DeterminismResult

```csharp
public class DeterminismResult
{
    public string TestId { get; set; }
    public DateTime TestedAt { get; set; }
    public int RunCount { get; set; }
    public double DeterminismScore { get; set; }        // 0-100%
    public ConsistencyLevel ConsistencyLevel { get; set; }

    public List<ConsistentFinding> ConsistentFindings { get; set; }
    public List<InconsistentFinding> InconsistentFindings { get; set; }

    public DeterminismStatistics Statistics { get; set; }

    public Dictionary<string, AgentDeterminismMetrics> MetricsByAgent { get; set; }
    public Dictionary<string, AgentDeterminismMetrics> MetricsByCategory { get; set; }

    public List<AnalysisRun> AllRuns { get; set; }
}

public enum ConsistencyLevel
{
    Excellent,  // ≥ 90%
    Good,       // 80-89%
    Moderate,   // 70-79%
    Fair,       // 60-69%
    Poor        // < 60%
}
```

## Integration with Other Systems

### 1. **Ground Truth Validation**

Combine determinism measurement with ground truth validation:

```csharp
// 1. Measure determinism
var determinismResult = await DeterminismService.MeasureDeterminismAsync(...);

// 2. If determinism is good, validate accuracy
if (determinismResult.DeterminismScore >= 80)
{
    var validationResult = await ValidationService.ValidateAsync(
        lastAnalysisResult, groundTruthDataset, ...);

    Console.WriteLine($"Determinism: {determinismResult.DeterminismScore:F1}%");
    Console.WriteLine($"F1 Score: {validationResult.OverallMetrics.F1Score:F1}%");
}
else
{
    Console.WriteLine("Improve determinism before validating accuracy.");
}
```

### 2. **Confidence Calibration**

Use determinism to calibrate confidence scores:

```csharp
// Findings that appear in 100% of runs get higher confidence
if (finding.AppearanceRate == 100)
{
    finding.ConfidenceScore *= 1.2;  // Boost by 20%
}
else if (finding.AppearanceRate < 50)
{
    finding.ConfidenceScore *= 0.7;  // Reduce by 30%
}
```

### 3. **CI/CD Integration**

Add determinism checks to your pipeline:

```yaml
# GitHub Actions example
- name: Run Determinism Test
  run: |
    dotnet run --project DeterminismTester -- \
      --files src/critical/*.cs \
      --runs 10 \
      --min-score 85
```

Fail the build if determinism drops below threshold.

## Metrics Dashboard (Future Enhancement)

Planned features:
- [ ] Historical trend tracking (determinism over time)
- [ ] Per-file determinism heatmap
- [ ] Correlation with accuracy (determinism vs F1 score)
- [ ] Model comparison charts
- [ ] Automated prompt tuning suggestions

## Related Documentation

- [Ground Truth Validation](./GroundTruthValidation.md) - Measuring accuracy with benchmark datasets
- [Confidence Calibration](./ConfidenceCalibration.md) - Adjusting confidence scores based on determinism
- [Hallucination Guardrails](./HallucinationGuardrails.md) - Reducing false positives

## Support

For issues or questions:
1. Check the [FAQ](./FAQ.md)
2. Review determinism logs in Application Insights
3. Create an issue on GitHub with test results attached

---

**Last Updated:** 2025-12-25
**Version:** 1.0

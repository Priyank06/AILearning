using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using PoC1_LegacyAnalyzer_Web.Models.Determinism;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using System.Diagnostics;
using System.Text;

namespace PoC1_LegacyAnalyzer_Web.Services.Determinism
{
    /// <summary>
    /// Measures determinism of AI analysis by running multiple analyses and comparing consistency
    /// </summary>
    public class DeterminismMeasurementService : IDeterminismMeasurementService
    {
        private readonly IAgentOrchestrationService _orchestrationService;
        private readonly ILogger<DeterminismMeasurementService> _logger;

        public DeterminismMeasurementService(
            IAgentOrchestrationService orchestrationService,
            ILogger<DeterminismMeasurementService> logger)
        {
            _orchestrationService = orchestrationService ?? throw new ArgumentNullException(nameof(orchestrationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DeterminismResult> MeasureDeterminismAsync(
            List<IBrowserFile> files,
            string businessObjective,
            List<string> requiredSpecialties,
            DeterminismConfiguration? configuration = null,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            configuration ??= new DeterminismConfiguration();

            _logger.LogInformation(
                "Starting determinism measurement with {RunCount} runs for {FileCount} files",
                configuration.RunCount, files.Count);

            var result = new DeterminismResult
            {
                RunCount = configuration.RunCount,
                Configuration = configuration,
                TestedAt = DateTime.UtcNow
            };

            var allFindings = new List<List<Finding>>();

            // Run analysis multiple times
            for (int i = 0; i < configuration.RunCount; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Determinism measurement cancelled at run {RunNumber}", i + 1);
                    break;
                }

                progress?.Report($"Running analysis {i + 1}/{configuration.RunCount}...");
                _logger.LogInformation("Starting run {RunNumber}/{TotalRuns}", i + 1, configuration.RunCount);

                var sw = Stopwatch.StartNew();
                try
                {
                    // Run analysis
                    // Note: We should temporarily disable caching for determinism testing
                    var analysisResult = await _orchestrationService.CoordinateTeamAnalysisAsync(
                        files,
                        businessObjective,
                        requiredSpecialties,
                        null,
                        null,
                        cancellationToken);

                    // Collect findings from all agents
                    var runFindings = new List<Finding>();
                    foreach (var agentAnalysis in analysisResult.AgentAnalyses)
                    {
                        if (agentAnalysis.KeyFindings != null)
                        {
                            runFindings.AddRange(agentAnalysis.KeyFindings);
                        }
                    }

                    allFindings.Add(runFindings);

                    sw.Stop();

                    // Record this run
                    result.Runs.Add(new AnalysisRun
                    {
                        RunNumber = i + 1,
                        StartedAt = DateTime.UtcNow.Subtract(sw.Elapsed),
                        Duration = sw.Elapsed,
                        Findings = runFindings,
                        FindingCount = runFindings.Count,
                        RunHash = ComputeRunHash(runFindings)
                    });

                    _logger.LogInformation(
                        "Run {RunNumber} completed in {Duration}ms with {FindingCount} findings",
                        i + 1, sw.ElapsedMilliseconds, runFindings.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Run {RunNumber} failed", i + 1);
                    progress?.Report($"Run {i + 1} failed: {ex.Message}");
                }

                // Delay between runs to avoid rate limiting
                if (i < configuration.RunCount - 1 && configuration.DelayBetweenRunsMs > 0)
                {
                    await Task.Delay(configuration.DelayBetweenRunsMs, cancellationToken);
                }
            }

            // Analyze consistency
            progress?.Report("Analyzing consistency across runs...");
            AnalyzeConsistency(result, allFindings, configuration);

            // Calculate overall determinism score
            result.DeterminismScore = CalculateOverallDeterminismScore(result);
            result.ConsistencyLevel = ClassifyConsistencyLevel(result.DeterminismScore);

            // Calculate per-agent metrics
            CalculatePerAgentMetrics(result);

            // Calculate per-category metrics
            CalculatePerCategoryMetrics(result);

            // Calculate statistics
            CalculateStatistics(result);

            _logger.LogInformation(
                "Determinism measurement complete. Score: {Score:F1}%, Level: {Level}",
                result.DeterminismScore, result.ConsistencyLevel);

            return result;
        }

        private void AnalyzeConsistency(
            DeterminismResult result,
            List<List<Finding>> allFindings,
            DeterminismConfiguration config)
        {
            // Build a map of all unique findings and track which runs they appeared in
            var findingAppearances = new Dictionary<string, FindingAppearanceInfo>();

            for (int runIndex = 0; runIndex < allFindings.Count; runIndex++)
            {
                var findings = allFindings[runIndex];
                var runNumber = runIndex + 1;

                foreach (var finding in findings)
                {
                    var key = GenerateFindingKey(finding);

                    if (!findingAppearances.ContainsKey(key))
                    {
                        findingAppearances[key] = new FindingAppearanceInfo
                        {
                            Finding = finding,
                            AppearedInRuns = new List<int>(),
                            Severities = new List<string>()
                        };
                    }

                    findingAppearances[key].AppearedInRuns.Add(runNumber);
                    findingAppearances[key].Severities.Add(finding.Severity);
                }
            }

            // Classify findings as consistent or inconsistent
            foreach (var (key, info) in findingAppearances)
            {
                var appearanceRate = (info.AppearedInRuns.Count / (double)result.RunCount) * 100;

                if (appearanceRate >= config.ConsistencyThreshold)
                {
                    // Consistent finding
                    result.ConsistentFindings.Add(new ConsistentFinding
                    {
                        Finding = info.Finding,
                        AppearanceCount = info.AppearedInRuns.Count,
                        RunCount = result.RunCount,
                        SeveritiesAcrossRuns = info.Severities.Distinct().ToList(),
                        DescriptionConsistency = 100.0 // Simplified - could calculate text similarity
                    });
                }
                else
                {
                    // Inconsistent finding
                    result.InconsistentFindings.Add(new InconsistentFinding
                    {
                        Finding = info.Finding,
                        AppearedInRuns = info.AppearedInRuns,
                        TotalRuns = result.RunCount,
                        InconsistencyReason = $"Appeared in {info.AppearedInRuns.Count}/{result.RunCount} runs ({appearanceRate:F1}%)"
                    });
                }
            }
        }

        private double CalculateOverallDeterminismScore(DeterminismResult result)
        {
            if (result.ConsistentFindings.Count + result.InconsistentFindings.Count == 0)
            {
                return 0;
            }

            // Weighted average of appearance rates
            var totalFindings = result.ConsistentFindings.Count + result.InconsistentFindings.Count;
            var weightedSum = 0.0;

            foreach (var finding in result.ConsistentFindings)
            {
                weightedSum += finding.AppearanceRate;
            }

            foreach (var finding in result.InconsistentFindings)
            {
                weightedSum += finding.AppearanceRate;
            }

            return weightedSum / totalFindings;
        }

        private ConsistencyLevel ClassifyConsistencyLevel(double score)
        {
            if (score >= 90) return ConsistencyLevel.Excellent;
            if (score >= 80) return ConsistencyLevel.Good;
            if (score >= 70) return ConsistencyLevel.Moderate;
            if (score >= 60) return ConsistencyLevel.Fair;
            return ConsistencyLevel.Poor;
        }

        private void CalculatePerAgentMetrics(DeterminismResult result)
        {
            // Group findings by agent and calculate metrics
            var agentGroups = result.ConsistentFindings
                .Concat(result.InconsistentFindings.Select(i => new ConsistentFinding
                {
                    Finding = i.Finding,
                    AppearanceCount = i.AppearedInRuns.Count,
                    RunCount = i.TotalRuns
                }))
                .GroupBy(f => "Unknown"); // Would need to track agent per finding

            // Simplified - would need agent tracking in Finding model
            // For now, calculate overall metrics
        }

        private void CalculatePerCategoryMetrics(DeterminismResult result)
        {
            var categoryGroups = result.ConsistentFindings
                .GroupBy(f => f.Finding.Category);

            foreach (var group in categoryGroups)
            {
                var category = group.Key;
                var consistentCount = group.Count();
                var inconsistentCount = result.InconsistentFindings.Count(i => i.Finding.Category == category);
                var total = consistentCount + inconsistentCount;

                result.MetricsByCategory[category] = new CategoryDeterminismMetrics
                {
                    Category = category,
                    DeterminismScore = total > 0 ? (consistentCount / (double)total) * 100 : 0,
                    ConsistentFindings = consistentCount,
                    InconsistentFindings = inconsistentCount
                };
            }
        }

        private void CalculateStatistics(DeterminismResult result)
        {
            var stats = new DeterminismStatistics
            {
                TotalUniqueFindings = result.ConsistentFindings.Count + result.InconsistentFindings.Count,
                FullyConsistentFindings = result.ConsistentFindings.Count(f => f.AppearanceRate == 100),
                HighlyConsistentFindings = result.ConsistentFindings.Count(f => f.AppearanceRate >= 80),
                ModeratelyConsistentFindings = result.ConsistentFindings.Count(f => f.AppearanceRate >= 50 && f.AppearanceRate < 80),
                PoorlyConsistentFindings = result.InconsistentFindings.Count(f => f.AppearanceRate < 50)
            };

            // Calculate finding count statistics
            if (result.Runs.Any())
            {
                var findingCounts = result.Runs.Select(r => r.FindingCount).ToList();
                stats.AverageFindingsPerRun = findingCounts.Average();
                stats.MinFindings = findingCounts.Min();
                stats.MaxFindings = findingCounts.Max();

                // Calculate standard deviation
                var mean = stats.AverageFindingsPerRun;
                var sumSquares = findingCounts.Sum(count => Math.Pow(count - mean, 2));
                stats.FindingCountStdDev = Math.Sqrt(sumSquares / findingCounts.Count);
            }

            result.Statistics = stats;
        }

        public double CalculateFindingSimilarity(List<Finding> findings1, List<Finding> findings2)
        {
            if (!findings1.Any() && !findings2.Any())
                return 100.0;

            if (!findings1.Any() || !findings2.Any())
                return 0.0;

            // Use Jaccard similarity based on finding keys
            var keys1 = findings1.Select(GenerateFindingKey).ToHashSet();
            var keys2 = findings2.Select(GenerateFindingKey).ToHashSet();

            var intersection = keys1.Intersect(keys2).Count();
            var union = keys1.Union(keys2).Count();

            return union > 0 ? (intersection / (double)union) * 100 : 0;
        }

        public string GenerateSummaryReport(DeterminismResult result)
        {
            var sb = new StringBuilder();

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"   DETERMINISM MEASUREMENT REPORT");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"Test ID: {result.TestId}");
            sb.AppendLine($"Tested: {result.TestedAt:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"Number of Runs: {result.RunCount}");
            sb.AppendLine();

            // Overall Score
            sb.AppendLine("─────────────────────────────────────────────────────────────");
            sb.AppendLine("OVERALL DETERMINISM");
            sb.AppendLine("─────────────────────────────────────────────────────────────");
            sb.AppendLine($"Determinism Score: {result.DeterminismScore:F1}%");
            sb.AppendLine($"Consistency Level: {result.ConsistencyLevel}");
            sb.AppendLine();

            sb.AppendLine(GetConsistencyAssessment(result));
            sb.AppendLine();

            // Statistics
            sb.AppendLine("─────────────────────────────────────────────────────────────");
            sb.AppendLine("FINDING CONSISTENCY");
            sb.AppendLine("─────────────────────────────────────────────────────────────");
            var stats = result.Statistics;
            sb.AppendLine($"Total Unique Findings: {stats.TotalUniqueFindings}");
            sb.AppendLine($"  - 100% Consistent:  {stats.FullyConsistentFindings} findings");
            sb.AppendLine($"  - ≥80% Consistent:  {stats.HighlyConsistentFindings} findings");
            sb.AppendLine($"  - 50-79% Consistent: {stats.ModeratelyConsistentFindings} findings");
            sb.AppendLine($"  - <50% Consistent:  {stats.PoorlyConsistentFindings} findings");
            sb.AppendLine();

            sb.AppendLine($"Average Findings per Run: {stats.AverageFindingsPerRun:F1}");
            sb.AppendLine($"Standard Deviation: {stats.FindingCountStdDev:F2}");
            sb.AppendLine($"Range: {stats.MinFindings} - {stats.MaxFindings} findings");
            sb.AppendLine();

            // Per-Category Metrics
            if (result.MetricsByCategory.Any())
            {
                sb.AppendLine("─────────────────────────────────────────────────────────────");
                sb.AppendLine("DETERMINISM BY CATEGORY");
                sb.AppendLine("─────────────────────────────────────────────────────────────");
                foreach (var (category, metrics) in result.MetricsByCategory.OrderByDescending(x => x.Value.DeterminismScore))
                {
                    sb.AppendLine($"{category}: {metrics.DeterminismScore:F1}%");
                    sb.AppendLine($"  Consistent: {metrics.ConsistentFindings}, Inconsistent: {metrics.InconsistentFindings}");
                }
                sb.AppendLine();
            }

            // Top Inconsistent Findings
            if (result.InconsistentFindings.Any())
            {
                sb.AppendLine("─────────────────────────────────────────────────────────────");
                sb.AppendLine("TOP INCONSISTENT FINDINGS");
                sb.AppendLine("─────────────────────────────────────────────────────────────");
                foreach (var finding in result.InconsistentFindings.OrderBy(f => f.AppearanceRate).Take(5))
                {
                    sb.AppendLine($"• {finding.Finding.Category} ({finding.Finding.Severity})");
                    sb.AppendLine($"  {finding.Finding.Description}");
                    sb.AppendLine($"  Appeared in: {finding.AppearedInRuns.Count}/{finding.TotalRuns} runs ({finding.AppearanceRate:F1}%)");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("═══════════════════════════════════════════════════════════════");

            return sb.ToString();
        }

        private string GetConsistencyAssessment(DeterminismResult result)
        {
            var score = result.DeterminismScore;
            var sb = new StringBuilder();

            sb.AppendLine("Assessment:");

            if (score >= 90)
                sb.AppendLine("  ✓ EXCELLENT consistency - Findings are highly reliable");
            else if (score >= 80)
                sb.AppendLine("  ✓ GOOD consistency - Findings are generally reliable");
            else if (score >= 70)
                sb.AppendLine("  ⚠ MODERATE consistency - Some findings may vary between runs");
            else if (score >= 60)
                sb.AppendLine("  ⚠ FAIR consistency - Findings vary noticeably between runs");
            else
                sb.AppendLine("  ✗ POOR consistency - Findings are unreliable, consider prompt tuning");

            // Provide recommendations
            if (score < 80)
            {
                sb.AppendLine();
                sb.AppendLine("Recommendations:");
                sb.AppendLine("  • Lower temperature in LLM configuration (current: 0.3)");
                sb.AppendLine("  • Add more specific examples to prompts");
                sb.AppendLine("  • Use structured output formats (already implemented)");
                sb.AppendLine("  • Consider fine-tuning the model on your use case");
            }

            return sb.ToString();
        }

        #region Private Helpers

        private string GenerateFindingKey(Finding finding)
        {
            // Create a unique key based on category + location (normalized)
            // This helps match similar findings across runs
            var category = finding.Category?.ToLowerInvariant() ?? "";
            var location = finding.Location?.ToLowerInvariant() ?? "";

            // Normalize location (remove line numbers which may vary slightly)
            location = System.Text.RegularExpressions.Regex.Replace(location, @":\d+", ":*");
            location = System.Text.RegularExpressions.Regex.Replace(location, @"line\s+\d+", "line *");

            return $"{category}|{location}";
        }

        private string ComputeRunHash(List<Finding> findings)
        {
            // Create a hash of all findings for this run
            var combined = string.Join("|", findings.Select(f => $"{f.Category}:{f.Severity}:{f.Location}"));
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(combined);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes).Substring(0, 16);
        }

        #endregion

        private class FindingAppearanceInfo
        {
            public Finding Finding { get; set; } = new();
            public List<int> AppearedInRuns { get; set; } = new();
            public List<string> Severities { get; set; } = new();
        }
    }
}

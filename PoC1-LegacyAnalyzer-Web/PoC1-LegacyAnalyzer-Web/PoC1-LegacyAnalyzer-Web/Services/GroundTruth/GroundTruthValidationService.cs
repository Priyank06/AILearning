using Microsoft.Extensions.Logging;
using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;
using PoC1_LegacyAnalyzer_Web.Models.GroundTruth;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using System.Text;
using System.Text.Json;

namespace PoC1_LegacyAnalyzer_Web.Services.GroundTruth
{
    /// <summary>
    /// Validates AI analysis results against ground truth datasets to measure quality
    /// </summary>
    public class GroundTruthValidationService : IGroundTruthValidationService
    {
        private readonly ILogger<GroundTruthValidationService> _logger;

        // Severity levels for comparison (higher index = higher severity)
        private static readonly List<string> SeverityLevels = new() { "LOW", "MEDIUM", "HIGH", "CRITICAL" };

        public GroundTruthValidationService(ILogger<GroundTruthValidationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<GroundTruthValidationResult> ValidateAsync(
            TeamAnalysisResult analysisResult,
            GroundTruthDataset groundTruthDataset,
            ValidationConfiguration? configuration = null,
            CancellationToken cancellationToken = default)
        {
            configuration ??= new ValidationConfiguration();

            _logger.LogInformation("Starting ground truth validation against dataset: {DatasetName}", groundTruthDataset.Name);

            var result = new GroundTruthValidationResult
            {
                DatasetId = groundTruthDataset.Id,
                DatasetName = groundTruthDataset.Name,
                Configuration = configuration
            };

            // Collect all AI findings from all agents
            var allAiFindings = CollectAllFindings(analysisResult);
            _logger.LogInformation("Collected {FindingCount} AI findings from analysis", allAiFindings.Count);

            // Match AI findings to ground truth issues
            var matches = await MatchFindingsToGroundTruthAsync(
                allAiFindings,
                groundTruthDataset.Issues,
                configuration,
                cancellationToken);

            result.FindingMatches = matches;

            // Classify matches
            ClassifyMatches(result, allAiFindings, groundTruthDataset.Issues, matches, configuration);

            // Calculate overall metrics
            result.OverallMetrics = CalculateMetricsFromCounts(
                result.TruePositives.Count,
                result.FalsePositives.Count,
                result.FalseNegatives.Count);

            // Calculate per-agent metrics
            result.MetricsByAgent = CalculateMetricsByAgent(analysisResult, groundTruthDataset.Issues, configuration);

            // Calculate per-category metrics
            result.MetricsByCategory = CalculateMetricsByCategory(result);

            // Calculate per-severity metrics
            result.MetricsBySeverity = CalculateMetricsBySeverity(result);

            // Generate summary
            result.Summary = GenerateSummaryReport(result);

            _logger.LogInformation(
                "Validation complete. Precision: {Precision:F1}%, Recall: {Recall:F1}%, F1: {F1:F1}%",
                result.OverallMetrics.Precision,
                result.OverallMetrics.Recall,
                result.OverallMetrics.F1Score);

            return result;
        }

        private List<Finding> CollectAllFindings(TeamAnalysisResult analysisResult)
        {
            var findings = new List<Finding>();

            foreach (var agentAnalysis in analysisResult.AgentAnalyses)
            {
                if (agentAnalysis.KeyFindings != null)
                {
                    findings.AddRange(agentAnalysis.KeyFindings);
                }
            }

            return findings;
        }

        private async Task<List<FindingMatch>> MatchFindingsToGroundTruthAsync(
            List<Finding> aiFindings,
            List<GroundTruthIssue> groundTruthIssues,
            ValidationConfiguration configuration,
            CancellationToken cancellationToken)
        {
            var matches = new List<FindingMatch>();

            // For each AI finding, find the best matching ground truth issue
            foreach (var aiFinding in aiFindings)
            {
                var bestMatch = FindBestMatch(aiFinding, groundTruthIssues, configuration);
                if (bestMatch != null && bestMatch.MatchConfidence >= configuration.MinMatchConfidence)
                {
                    matches.Add(bestMatch);
                }
            }

            return await Task.FromResult(matches);
        }

        private FindingMatch? FindBestMatch(
            Finding aiFinding,
            List<GroundTruthIssue> groundTruthIssues,
            ValidationConfiguration configuration)
        {
            FindingMatch? bestMatch = null;
            double bestConfidence = 0;

            foreach (var gtIssue in groundTruthIssues)
            {
                var match = CalculateMatch(aiFinding, gtIssue, configuration);
                if (match.MatchConfidence > bestConfidence)
                {
                    bestConfidence = match.MatchConfidence;
                    bestMatch = match;
                }
            }

            return bestMatch;
        }

        private FindingMatch CalculateMatch(
            Finding aiFinding,
            GroundTruthIssue gtIssue,
            ValidationConfiguration configuration)
        {
            var match = new FindingMatch
            {
                AiFinding = aiFinding,
                GroundTruthIssue = gtIssue
            };

            // Check category match
            match.CategoryMatches = IsCategoryMatch(aiFinding.Category, gtIssue.Category);

            // Check severity match
            match.SeverityMatches = IsSeverityMatch(
                aiFinding.Severity,
                gtIssue.Severity,
                configuration.AllowedSeverityDifference);

            // Check location match
            match.LocationMatches = IsLocationMatch(
                aiFinding.Location,
                gtIssue.Location,
                ExtractLineNumber(aiFinding.Location),
                gtIssue.LineNumber,
                configuration.AllowedLineNumberDifference);

            // Calculate overall match confidence
            double categoryScore = match.CategoryMatches ? configuration.CategoryMatchWeight : 0;
            double severityScore = match.SeverityMatches ? configuration.SeverityMatchWeight : 0;
            double locationScore = match.LocationMatches ? configuration.LocationMatchWeight : 0;

            match.MatchConfidence = (categoryScore + severityScore + locationScore) * 100;

            // Determine match type
            if (match.CategoryMatches && match.SeverityMatches && match.LocationMatches)
            {
                match.MatchType = PoC1_LegacyAnalyzer_Web.Models.GroundTruth.MatchType.Exact;
            }
            else if (match.CategoryMatches && (match.SeverityMatches || match.LocationMatches))
            {
                match.MatchType = PoC1_LegacyAnalyzer_Web.Models.GroundTruth.MatchType.Partial;
            }
            else if (match.CategoryMatches)
            {
                match.MatchType = PoC1_LegacyAnalyzer_Web.Models.GroundTruth.MatchType.Weak;
            }
            else
            {
                match.MatchType = PoC1_LegacyAnalyzer_Web.Models.GroundTruth.MatchType.None;
            }

            return match;
        }

        private bool IsCategoryMatch(string aiCategory, string gtCategory)
        {
            // Normalize categories for comparison
            var normalizedAi = NormalizeCategory(aiCategory);
            var normalizedGt = NormalizeCategory(gtCategory);

            // Exact match
            if (normalizedAi.Equals(normalizedGt, StringComparison.OrdinalIgnoreCase))
                return true;

            // Fuzzy match (e.g., "SQL Injection" matches "SQL Injection Vulnerability")
            if (normalizedAi.Contains(normalizedGt, StringComparison.OrdinalIgnoreCase) ||
                normalizedGt.Contains(normalizedAi, StringComparison.OrdinalIgnoreCase))
                return true;

            // Category aliases (e.g., "Authentication" and "Auth" are the same)
            return AreCategorySynonyms(normalizedAi, normalizedGt);
        }

        private string NormalizeCategory(string category)
        {
            return category.Trim().ToLowerInvariant()
                .Replace("_", " ")
                .Replace("-", " ");
        }

        private bool AreCategorySynonyms(string cat1, string cat2)
        {
            var synonyms = new Dictionary<string, List<string>>
            {
                { "authentication", new() { "auth", "authn" } },
                { "authorization", new() { "authz", "access control" } },
                { "sql injection", new() { "sqli", "sql" } },
                { "cross site scripting", new() { "xss", "scripting" } },
                { "performance", new() { "perf", "optimization" } },
                { "architecture", new() { "arch", "design" } }
            };

            foreach (var (key, values) in synonyms)
            {
                if ((cat1 == key && values.Contains(cat2)) ||
                    (cat2 == key && values.Contains(cat1)) ||
                    (values.Contains(cat1) && values.Contains(cat2)))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsSeverityMatch(string aiSeverity, string gtSeverity, int allowedDifference)
        {
            var aiIndex = SeverityLevels.IndexOf(aiSeverity.ToUpperInvariant());
            var gtIndex = SeverityLevels.IndexOf(gtSeverity.ToUpperInvariant());

            if (aiIndex == -1 || gtIndex == -1)
                return false;

            return Math.Abs(aiIndex - gtIndex) <= allowedDifference;
        }

        private bool IsLocationMatch(
            string aiLocation,
            string gtLocation,
            int? aiLineNumber,
            int? gtLineNumber,
            int allowedLineDifference)
        {
            // If both have line numbers, compare them
            if (aiLineNumber.HasValue && gtLineNumber.HasValue)
            {
                return Math.Abs(aiLineNumber.Value - gtLineNumber.Value) <= allowedLineDifference;
            }

            // Otherwise, compare location strings (class name, method name, etc.)
            if (!string.IsNullOrEmpty(aiLocation) && !string.IsNullOrEmpty(gtLocation))
            {
                return aiLocation.Contains(gtLocation, StringComparison.OrdinalIgnoreCase) ||
                       gtLocation.Contains(aiLocation, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private int? ExtractLineNumber(string location)
        {
            // Try to extract line number from location string (e.g., "File.cs:42" or "Method at line 42")
            if (string.IsNullOrEmpty(location))
                return null;

            var patterns = new[] { @":(\d+)", @"line\s+(\d+)", @"#(\d+)" };
            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(location, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int lineNumber))
                {
                    return lineNumber;
                }
            }

            return null;
        }

        private void ClassifyMatches(
            GroundTruthValidationResult result,
            List<Finding> allAiFindings,
            List<GroundTruthIssue> groundTruthIssues,
            List<FindingMatch> matches,
            ValidationConfiguration configuration)
        {
            var matchedGtIssues = new HashSet<string>();
            var matchedAiFindings = new HashSet<Finding>();

            // Classify true positives
            foreach (var match in matches)
            {
                if (match.MatchType == PoC1_LegacyAnalyzer_Web.Models.GroundTruth.MatchType.Exact ||
                    (match.MatchType == PoC1_LegacyAnalyzer_Web.Models.GroundTruth.MatchType.Partial && configuration.CountPartialMatchesAsTruePositives))
                {
                    result.TruePositives.Add(match);
                    matchedGtIssues.Add(match.GroundTruthIssue.Id);
                    matchedAiFindings.Add(match.AiFinding);
                }
            }

            // Classify false positives (AI findings with no ground truth match)
            result.FalsePositives = allAiFindings
                .Where(f => !matchedAiFindings.Contains(f))
                .ToList();

            // Classify false negatives (ground truth issues that weren't detected)
            result.FalseNegatives = groundTruthIssues
                .Where(gt => !matchedGtIssues.Contains(gt.Id) && gt.IsMandatory)
                .ToList();
        }

        private QualityMetrics CalculateMetricsFromCounts(int truePositives, int falsePositives, int falseNegatives)
        {
            var metrics = new QualityMetrics
            {
                TruePositiveCount = truePositives,
                FalsePositiveCount = falsePositives,
                FalseNegativeCount = falseNegatives
            };

            metrics.Calculate();
            return metrics;
        }

        private Dictionary<string, QualityMetrics> CalculateMetricsByAgent(
            TeamAnalysisResult analysisResult,
            List<GroundTruthIssue> groundTruthIssues,
            ValidationConfiguration configuration)
        {
            var metricsByAgent = new Dictionary<string, QualityMetrics>();

            foreach (var agentAnalysis in analysisResult.AgentAnalyses)
            {
                var agentFindings = agentAnalysis.KeyFindings ?? new List<Finding>();
                var agentMatches = new List<FindingMatch>();

                // Match this agent's findings to ground truth
                foreach (var finding in agentFindings)
                {
                    var match = FindBestMatch(finding, groundTruthIssues, configuration);
                    if (match != null && match.MatchConfidence >= configuration.MinMatchConfidence)
                    {
                        agentMatches.Add(match);
                    }
                }

                // Calculate metrics for this agent
                var matchedGtIssues = new HashSet<string>();
                var matchedFindings = new HashSet<Finding>();

                var truePositives = 0;
                foreach (var match in agentMatches)
                {
                    if (match.MatchType == PoC1_LegacyAnalyzer_Web.Models.GroundTruth.MatchType.Exact ||
                        (match.MatchType == PoC1_LegacyAnalyzer_Web.Models.GroundTruth.MatchType.Partial && configuration.CountPartialMatchesAsTruePositives))
                    {
                        truePositives++;
                        matchedGtIssues.Add(match.GroundTruthIssue.Id);
                        matchedFindings.Add(match.AiFinding);
                    }
                }

                var falsePositives = agentFindings.Count(f => !matchedFindings.Contains(f));

                // Only count ground truth issues this agent should detect
                var relevantGtIssues = groundTruthIssues
                    .Where(gt => gt.IsMandatory &&
                                (gt.ExpectedDetectorAgents.Count == 0 ||
                                 gt.ExpectedDetectorAgents.Contains(agentAnalysis.Specialty, StringComparer.OrdinalIgnoreCase)))
                    .ToList();

                var falseNegatives = relevantGtIssues.Count(gt => !matchedGtIssues.Contains(gt.Id));

                metricsByAgent[agentAnalysis.AgentName] = CalculateMetricsFromCounts(
                    truePositives,
                    falsePositives,
                    falseNegatives);
            }

            return metricsByAgent;
        }

        private Dictionary<string, QualityMetrics> CalculateMetricsByCategory(GroundTruthValidationResult result)
        {
            var metricsByCategory = new Dictionary<string, QualityMetrics>();

            // Group by category
            var categories = result.TruePositives
                .Select(tp => tp.GroundTruthIssue.Category)
                .Concat(result.FalseNegatives.Select(fn => fn.Category))
                .Concat(result.FalsePositives.Select(fp => fp.Category))
                .Distinct();

            foreach (var category in categories)
            {
                var tp = result.TruePositives.Count(m => m.GroundTruthIssue.Category == category);
                var fp = result.FalsePositives.Count(f => f.Category == category);
                var fn = result.FalseNegatives.Count(gt => gt.Category == category);

                metricsByCategory[category] = CalculateMetricsFromCounts(tp, fp, fn);
            }

            return metricsByCategory;
        }

        private Dictionary<string, QualityMetrics> CalculateMetricsBySeverity(GroundTruthValidationResult result)
        {
            var metricsBySeverity = new Dictionary<string, QualityMetrics>();

            foreach (var severity in SeverityLevels)
            {
                var tp = result.TruePositives.Count(m => m.GroundTruthIssue.Severity == severity);
                var fp = result.FalsePositives.Count(f => f.Severity == severity);
                var fn = result.FalseNegatives.Count(gt => gt.Severity == severity);

                metricsBySeverity[severity] = CalculateMetricsFromCounts(tp, fp, fn);
            }

            return metricsBySeverity;
        }

        public QualityMetrics CalculateMetrics(GroundTruthValidationResult validationResult)
        {
            return validationResult.OverallMetrics;
        }

        public string GenerateSummaryReport(GroundTruthValidationResult validationResult)
        {
            var sb = new StringBuilder();

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"   GROUND TRUTH VALIDATION REPORT");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"Dataset: {validationResult.DatasetName}");
            sb.AppendLine($"Validated: {validationResult.ValidatedAt:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine();

            // Overall metrics
            sb.AppendLine("─────────────────────────────────────────────────────────────");
            sb.AppendLine("OVERALL QUALITY METRICS");
            sb.AppendLine("─────────────────────────────────────────────────────────────");
            var m = validationResult.OverallMetrics;
            sb.AppendLine($"Precision:  {m.Precision:F1}%  (What % of AI findings are correct?)");
            sb.AppendLine($"Recall:     {m.Recall:F1}%  (What % of real issues did AI find?)");
            sb.AppendLine($"F1 Score:   {m.F1Score:F1}%  (Harmonic mean of precision & recall)");
            sb.AppendLine($"Accuracy:   {m.Accuracy:F1}%");
            sb.AppendLine();
            sb.AppendLine($"True Positives:  {m.TruePositiveCount} (Correctly detected issues)");
            sb.AppendLine($"False Positives: {m.FalsePositiveCount} (AI found issues that don't exist)");
            sb.AppendLine($"False Negatives: {m.FalseNegativeCount} (AI missed known issues)");
            sb.AppendLine();

            // Quality assessment
            sb.AppendLine(GetQualityAssessment(m));
            sb.AppendLine();

            // Per-agent metrics
            if (validationResult.MetricsByAgent.Any())
            {
                sb.AppendLine("─────────────────────────────────────────────────────────────");
                sb.AppendLine("METRICS BY AGENT");
                sb.AppendLine("─────────────────────────────────────────────────────────────");
                foreach (var (agent, metrics) in validationResult.MetricsByAgent.OrderByDescending(x => x.Value.F1Score))
                {
                    sb.AppendLine($"{agent}:");
                    sb.AppendLine($"  Precision: {metrics.Precision:F1}%, Recall: {metrics.Recall:F1}%, F1: {metrics.F1Score:F1}%");
                    sb.AppendLine($"  TP: {metrics.TruePositiveCount}, FP: {metrics.FalsePositiveCount}, FN: {metrics.FalseNegativeCount}");
                }
                sb.AppendLine();
            }

            // Per-category metrics
            if (validationResult.MetricsByCategory.Any())
            {
                sb.AppendLine("─────────────────────────────────────────────────────────────");
                sb.AppendLine("METRICS BY CATEGORY");
                sb.AppendLine("─────────────────────────────────────────────────────────────");
                foreach (var (category, metrics) in validationResult.MetricsByCategory.OrderByDescending(x => x.Value.F1Score))
                {
                    sb.AppendLine($"{category}:");
                    sb.AppendLine($"  Precision: {metrics.Precision:F1}%, Recall: {metrics.Recall:F1}%, F1: {metrics.F1Score:F1}%");
                }
                sb.AppendLine();
            }

            // False positives (top 5)
            if (validationResult.FalsePositives.Any())
            {
                sb.AppendLine("─────────────────────────────────────────────────────────────");
                sb.AppendLine("TOP FALSE POSITIVES (Issues AI found that don't exist)");
                sb.AppendLine("─────────────────────────────────────────────────────────────");
                foreach (var fp in validationResult.FalsePositives.Take(5))
                {
                    sb.AppendLine($"• {fp.Category} ({fp.Severity}): {fp.Description}");
                    sb.AppendLine($"  Location: {fp.Location}");
                }
                if (validationResult.FalsePositives.Count > 5)
                {
                    sb.AppendLine($"... and {validationResult.FalsePositives.Count - 5} more");
                }
                sb.AppendLine();
            }

            // False negatives (top 5)
            if (validationResult.FalseNegatives.Any())
            {
                sb.AppendLine("─────────────────────────────────────────────────────────────");
                sb.AppendLine("TOP FALSE NEGATIVES (Known issues AI missed)");
                sb.AppendLine("─────────────────────────────────────────────────────────────");
                foreach (var fn in validationResult.FalseNegatives.Take(5))
                {
                    sb.AppendLine($"• {fn.Category} ({fn.Severity}): {fn.Description}");
                    sb.AppendLine($"  Location: {fn.Location}");
                    sb.AppendLine($"  Expected Detectors: {string.Join(", ", fn.ExpectedDetectorAgents)}");
                }
                if (validationResult.FalseNegatives.Count > 5)
                {
                    sb.AppendLine($"... and {validationResult.FalseNegatives.Count - 5} more");
                }
                sb.AppendLine();
            }

            sb.AppendLine("═══════════════════════════════════════════════════════════════");

            return sb.ToString();
        }

        private string GetQualityAssessment(QualityMetrics metrics)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Quality Assessment:");

            // Precision assessment
            if (metrics.Precision >= 90)
                sb.AppendLine("  ✓ EXCELLENT precision - Very few false positives");
            else if (metrics.Precision >= 80)
                sb.AppendLine("  ✓ GOOD precision - Acceptable false positive rate");
            else if (metrics.Precision >= 70)
                sb.AppendLine("  ⚠ MODERATE precision - Noticeable false positives");
            else
                sb.AppendLine("  ✗ LOW precision - Too many false positives");

            // Recall assessment
            if (metrics.Recall >= 90)
                sb.AppendLine("  ✓ EXCELLENT recall - Catches nearly all issues");
            else if (metrics.Recall >= 80)
                sb.AppendLine("  ✓ GOOD recall - Catches most issues");
            else if (metrics.Recall >= 70)
                sb.AppendLine("  ⚠ MODERATE recall - Misses some issues");
            else
                sb.AppendLine("  ✗ LOW recall - Misses too many issues");

            // F1 assessment
            if (metrics.F1Score >= 85)
                sb.AppendLine("  ✓ EXCELLENT overall quality - Production ready");
            else if (metrics.F1Score >= 75)
                sb.AppendLine("  ✓ GOOD overall quality - Suitable for most use cases");
            else if (metrics.F1Score >= 65)
                sb.AppendLine("  ⚠ MODERATE overall quality - Needs improvement");
            else
                sb.AppendLine("  ✗ LOW overall quality - Not production ready");

            return sb.ToString();
        }

        public async Task<GroundTruthDataset> LoadDatasetAsync(string datasetPath, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Loading ground truth dataset from: {DatasetPath}", datasetPath);

            if (!File.Exists(datasetPath))
            {
                throw new FileNotFoundException($"Ground truth dataset not found: {datasetPath}");
            }

            var json = await File.ReadAllTextAsync(datasetPath, cancellationToken);
            var dataset = JsonSerializer.Deserialize<GroundTruthDataset>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (dataset == null)
            {
                throw new InvalidOperationException($"Failed to deserialize dataset from: {datasetPath}");
            }

            _logger.LogInformation("Loaded dataset '{Name}' with {IssueCount} issues", dataset.Name, dataset.Issues.Count);

            return dataset;
        }

        public async Task SaveDatasetAsync(GroundTruthDataset dataset, string datasetPath, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Saving ground truth dataset to: {DatasetPath}", datasetPath);

            // Update statistics before saving
            UpdateDatasetStatistics(dataset);

            var json = JsonSerializer.Serialize(dataset, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(datasetPath, json, cancellationToken);

            _logger.LogInformation("Dataset saved successfully");
        }

        public GroundTruthDataset CreateDataset(string name, string description)
        {
            return new GroundTruthDataset
            {
                Name = name,
                Description = description,
                Version = "1.0",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private void UpdateDatasetStatistics(GroundTruthDataset dataset)
        {
            var stats = new DatasetStatistics
            {
                TotalFiles = dataset.Files.Count,
                TotalIssues = dataset.Issues.Count
            };

            // Group by severity
            stats.IssuesBySeverity = dataset.Issues
                .GroupBy(i => i.Severity)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by category
            stats.IssuesByCategory = dataset.Issues
                .GroupBy(i => i.Category)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by language
            stats.IssuesByLanguage = dataset.Files
                .GroupBy(f => f.Language)
                .ToDictionary(g => g.Key, g => g.Sum(f => f.IssueCount));

            // Group by difficulty
            stats.IssuesByDifficulty = dataset.Issues
                .GroupBy(i => i.DifficultyLevel)
                .ToDictionary(g => g.Key, g => g.Count());

            dataset.Statistics = stats;
        }
    }
}

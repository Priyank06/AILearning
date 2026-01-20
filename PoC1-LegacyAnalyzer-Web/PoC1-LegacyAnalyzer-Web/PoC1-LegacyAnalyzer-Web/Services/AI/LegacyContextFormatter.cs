using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Options;

namespace PoC1_LegacyAnalyzer_Web.Services.AI
{
    /// <summary>
    /// Helper class to format legacy context information for agent prompts.
    /// </summary>
    public class LegacyContextFormatter
    {
        private readonly LegacyContextMessagesConfiguration _messages;

        public LegacyContextFormatter(IOptions<LegacyContextMessagesConfiguration> messages)
        {
            _messages = messages?.Value ?? new LegacyContextMessagesConfiguration();
        }

        /// <summary>
        /// Formats legacy context information into a readable string for agent prompts.
        /// </summary>
        public string FormatLegacyContext(LegacyPatternResult? legacyResult)
        {
            if (legacyResult == null || legacyResult.Indicators == null)
                return "";

            var contextParts = new List<string>();

            // File age information
            if (legacyResult.Indicators.IsVeryOldCode && legacyResult.Indicators.EstimatedFileAgeYears.HasValue)
            {
                contextParts.Add(_messages.VeryOldCode.Replace("{years}", legacyResult.Indicators.EstimatedFileAgeYears.Value.ToString()));
            }

            // Framework version
            if (legacyResult.Indicators.IsAncientFramework)
            {
                var frameworkInfo = !string.IsNullOrEmpty(legacyResult.Indicators.FrameworkVersion)
                    ? $" ({legacyResult.Indicators.FrameworkVersion})"
                    : "";
                contextParts.Add(_messages.AncientFramework.Replace("{frameworkInfo}", frameworkInfo));
            }

            // Global state
            if (legacyResult.Indicators.HasGlobalState)
            {
                contextParts.Add(_messages.GlobalStateDetected);
            }

            // Legacy data access
            if (legacyResult.Indicators.HasLegacyDataAccess)
            {
                contextParts.Add(_messages.LegacyDataAccess);
            }

            // Obsolete APIs
            if (legacyResult.Indicators.UsesObsoleteApis)
            {
                contextParts.Add(_messages.ObsoleteApis);
            }

            // Legacy issues summary
            if (legacyResult.Issues.Any())
            {
                var criticalCount = legacyResult.Issues.Count(i => i.Severity == "Critical");
                var highCount = legacyResult.Issues.Count(i => i.Severity == "High");
                var totalCount = legacyResult.Issues.Count;

                var issueSummary = $"Detected {totalCount} legacy issues: {criticalCount} Critical, {highCount} High severity. ";
                
                var issueTypes = legacyResult.Issues
                    .GroupBy(i => i.PatternType)
                    .Select(g => $"{g.Key} ({g.Count()})")
                    .Take(5);
                
                issueSummary += $"Pattern types: {string.Join(", ", issueTypes)}.";
                contextParts.Add(issueSummary);
            }

            // Change frequency (if available)
            if (legacyResult.Context?.ChangeFrequency.HasValue == true)
            {
                var changeFreq = legacyResult.Context.ChangeFrequency.Value;
                if (changeFreq == 0)
                {
                    contextParts.Add(_messages.ChangeFrequencyNone);
                }
                else if (changeFreq < 3)
                {
                    contextParts.Add(_messages.ChangeFrequencyLow.Replace("{count}", changeFreq.ToString()));
                }
                else if (changeFreq > 10)
                {
                    contextParts.Add(_messages.ChangeFrequencyHigh.Replace("{count}", changeFreq.ToString()));
                }
            }

            if (!contextParts.Any())
                return "";

            return _messages.LegacyContextHeader + string.Join("\n", contextParts) + _messages.LegacyContextFooter;
        }

        /// <summary>
        /// Extracts legacy context from a list of file metadata.
        /// </summary>
        public string ExtractLegacyContextFromMetadata(List<FileMetadata> metadataList)
        {
            if (metadataList == null || !metadataList.Any())
                return "";

            var legacyResults = metadataList
                .Where(m => m.LegacyPatternResult != null)
                .Select(m => m.LegacyPatternResult!)
                .ToList();

            if (!legacyResults.Any())
                return "";

            var aggregatedContext = new List<string>();

            // Aggregate indicators
            var veryOldFiles = legacyResults.Count(r => r.Indicators?.IsVeryOldCode == true);
            var ancientFrameworkFiles = legacyResults.Count(r => r.Indicators?.IsAncientFramework == true);
            var globalStateFiles = legacyResults.Count(r => r.Indicators?.HasGlobalState == true);
            var obsoleteApiFiles = legacyResults.Count(r => r.Indicators?.UsesObsoleteApis == true);

            if (veryOldFiles > 0)
                aggregatedContext.Add(_messages.VeryOldFilesSummary.Replace("{count}", veryOldFiles.ToString()));

            if (ancientFrameworkFiles > 0)
                aggregatedContext.Add(_messages.AncientFrameworkFilesSummary.Replace("{count}", ancientFrameworkFiles.ToString()));

            if (globalStateFiles > 0)
                aggregatedContext.Add(_messages.GlobalStateFilesSummary.Replace("{count}", globalStateFiles.ToString()));

            if (obsoleteApiFiles > 0)
                aggregatedContext.Add(_messages.ObsoleteApiFilesSummary.Replace("{count}", obsoleteApiFiles.ToString()));

            // Total legacy issues
            var totalIssues = legacyResults.Sum(r => r.Issues.Count);
            if (totalIssues > 0)
            {
                aggregatedContext.Add(_messages.TotalLegacyIssuesSummary
                    .Replace("{totalIssues}", totalIssues.ToString())
                    .Replace("{fileCount}", legacyResults.Count.ToString()));
            }

            if (!aggregatedContext.Any())
                return "";

            return _messages.LegacyCodebaseContextHeader + string.Join("\n", aggregatedContext) + _messages.LegacyCodebaseContextFooter;
        }
    }
}


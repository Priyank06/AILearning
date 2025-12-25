using PoC1_LegacyAnalyzer_Web.Models.GroundTruth;

namespace PoC1_LegacyAnalyzer_Web.Services.GroundTruth
{
    /// <summary>
    /// Helper service for building ground truth datasets programmatically
    /// </summary>
    public class GroundTruthDatasetBuilder
    {
        private readonly GroundTruthDataset _dataset;

        public GroundTruthDatasetBuilder(string name, string description)
        {
            _dataset = new GroundTruthDataset
            {
                Name = name,
                Description = description,
                Version = "1.0",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Add a file to the dataset
        /// </summary>
        public GroundTruthDatasetBuilder AddFile(string fileName, string language, string content, string relativePath = "")
        {
            _dataset.Files.Add(new BenchmarkFile
            {
                FileName = fileName,
                Language = language,
                Content = content,
                RelativePath = relativePath,
                LineCount = content.Split('\n').Length
            });

            return this;
        }

        /// <summary>
        /// Add a ground truth issue to the dataset
        /// </summary>
        public GroundTruthDatasetBuilder AddIssue(
            string fileName,
            string category,
            string description,
            string severity,
            string location,
            int? lineNumber = null,
            string codeSnippet = "",
            params string[] expectedDetectorAgents)
        {
            var issue = new GroundTruthIssue
            {
                FileName = fileName,
                Category = category,
                Description = description,
                Severity = severity,
                Location = location,
                LineNumber = lineNumber,
                CodeSnippet = codeSnippet,
                ExpectedDetectorAgents = expectedDetectorAgents.ToList(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Dataset Builder"
            };

            _dataset.Issues.Add(issue);

            // Update file issue count
            var file = _dataset.Files.FirstOrDefault(f => f.FileName == fileName);
            if (file != null)
            {
                file.IssueCount++;
            }

            return this;
        }

        /// <summary>
        /// Add a security issue (shorthand)
        /// </summary>
        public GroundTruthDatasetBuilder AddSecurityIssue(
            string fileName,
            string category,
            string description,
            string severity,
            string location,
            int? lineNumber = null,
            string codeSnippet = "")
        {
            return AddIssue(fileName, category, description, severity, location, lineNumber, codeSnippet, "Security");
        }

        /// <summary>
        /// Add a performance issue (shorthand)
        /// </summary>
        public GroundTruthDatasetBuilder AddPerformanceIssue(
            string fileName,
            string category,
            string description,
            string severity,
            string location,
            int? lineNumber = null,
            string codeSnippet = "")
        {
            return AddIssue(fileName, category, description, severity, location, lineNumber, codeSnippet, "Performance");
        }

        /// <summary>
        /// Add an architecture issue (shorthand)
        /// </summary>
        public GroundTruthDatasetBuilder AddArchitectureIssue(
            string fileName,
            string category,
            string description,
            string severity,
            string location,
            int? lineNumber = null,
            string codeSnippet = "")
        {
            return AddIssue(fileName, category, description, severity, location, lineNumber, codeSnippet, "Architecture");
        }

        /// <summary>
        /// Add tags to the dataset
        /// </summary>
        public GroundTruthDatasetBuilder WithTags(params string[] tags)
        {
            _dataset.Tags.AddRange(tags);
            return this;
        }

        /// <summary>
        /// Build the final dataset
        /// </summary>
        public GroundTruthDataset Build()
        {
            // Calculate statistics
            _dataset.Statistics = new DatasetStatistics
            {
                TotalFiles = _dataset.Files.Count,
                TotalIssues = _dataset.Issues.Count,
                IssuesBySeverity = _dataset.Issues.GroupBy(i => i.Severity).ToDictionary(g => g.Key, g => g.Count()),
                IssuesByCategory = _dataset.Issues.GroupBy(i => i.Category).ToDictionary(g => g.Key, g => g.Count()),
                IssuesByLanguage = _dataset.Files.GroupBy(f => f.Language).ToDictionary(g => g.Key, g => g.Sum(f => f.IssueCount)),
                IssuesByDifficulty = _dataset.Issues.GroupBy(i => i.DifficultyLevel).ToDictionary(g => g.Key, g => g.Count())
            };

            _dataset.UpdatedAt = DateTime.UtcNow;

            return _dataset;
        }
    }
}

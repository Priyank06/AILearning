using PoC1_LegacyAnalyzer_Web.Models.AI102;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class ContentModerationService : IContentModerationService
    {
        private readonly ILogger<ContentModerationService> _logger;
        private readonly IConfiguration _configuration;

        public ContentModerationService(IConfiguration configuration, ILogger<ContentModerationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ContentModerationResult> ModerateCodeCommentsAsync(string codeWithComments)
        {
            await Task.Delay(100); // Simulate processing time

            var result = new ContentModerationResult
            {
                IsAppropriate = true,
                ConfidenceScore = 95.0,
                Issues = new List<ModerationIssue>(),
                Suggestions = new List<string>()
            };

            // Extract comments from code
            var comments = ExtractComments(codeWithComments);

            foreach (var comment in comments)
            {
                await AnalyzeComment(comment, result);
            }

            // Generate suggestions based on findings
            if (result.Issues.Any(i => i.Severity == IssueSeverity.High))
            {
                result.Suggestions.Add("Review and revise comments with inappropriate content");
                result.IsAppropriate = false;
            }

            if (result.Issues.Any(i => i.Type == IssueType.ProfessionalismConcern))
            {
                result.Suggestions.Add("Ensure all comments maintain professional tone");
            }

            return result;
        }

        public async Task<DocumentationQualityResult> AnalyzeDocumentationQualityAsync(string documentation)
        {
            await Task.Delay(150);

            var result = new DocumentationQualityResult
            {
                OverallScore = 0,
                QualityMetrics = new Dictionary<string, double>(),
                Recommendations = new List<string>(),
                IssuesFound = new List<DocumentationIssue>()
            };

            // Analyze various aspects of documentation
            var clarityScore = AnalyzeClaritySy(documentation);
            var completenessScore = AnalyzeCompleteness(documentation);
            var consistencyScore = AnalyzeConsistency(documentation);
            var grammarScore = AnalyzeGrammar(documentation);

            result.QualityMetrics["Clarity"] = clarityScore;
            result.QualityMetrics["Completeness"] = completenessScore;
            result.QualityMetrics["Consistency"] = consistencyScore;
            result.QualityMetrics["Grammar"] = grammarScore;

            result.OverallScore = result.QualityMetrics.Values.Average();

            // Generate recommendations
            if (clarityScore < 70)
                result.Recommendations.Add("Improve clarity by using simpler language and better structure");
            if (completenessScore < 70)
                result.Recommendations.Add("Add missing sections and more detailed explanations");
            if (consistencyScore < 70)
                result.Recommendations.Add("Ensure consistent terminology and formatting throughout");
            if (grammarScore < 80)
                result.Recommendations.Add("Review and correct grammar and spelling errors");

            return result;
        }

        public async Task<bool> IsContentAppropriateAsync(string content)
        {
            await Task.Delay(50);

            var inappropriate = new[]
            {
                "inappropriate", "offensive", "profanity", "hack", "exploit",
                "password", "secret", "confidential", "private key"
            };

            var lowerContent = content.ToLower();
            return !inappropriate.Any(word => lowerContent.Contains(word));
        }

        private List<string> ExtractComments(string code)
        {
            var comments = new List<string>();
            var lines = code.Split('\n');

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Single line comments
                if (trimmedLine.StartsWith("//"))
                {
                    comments.Add(trimmedLine.Substring(2).Trim());
                }

                // Multi-line comment detection (simplified)
                if (trimmedLine.StartsWith("/*") && trimmedLine.EndsWith("*/"))
                {
                    var comment = trimmedLine.Substring(2, trimmedLine.Length - 4).Trim();
                    comments.Add(comment);
                }
            }

            return comments;
        }

        private async Task AnalyzeComment(string comment, ContentModerationResult result)
        {
            await Task.Delay(10);

            // Check for professionalism issues
            var unprofessionalWords = new[] { "stupid", "dumb", "idiot", "hate", "sucks", "terrible" };
            var lowerComment = comment.ToLower();

            foreach (var word in unprofessionalWords)
            {
                if (lowerComment.Contains(word))
                {
                    result.Issues.Add(new ModerationIssue
                    {
                        Type = IssueType.ProfessionalismConcern,
                        Severity = IssueSeverity.Medium,
                        Description = $"Potentially unprofessional language detected: '{word}'",
                        Location = comment,
                        Confidence = 0.8
                    });
                }
            }

            // Check for security-sensitive information
            var sensitivePatterns = new[] { "password", "key", "token", "secret", "credential" };
            foreach (var pattern in sensitivePatterns)
            {
                if (lowerComment.Contains(pattern))
                {
                    result.Issues.Add(new ModerationIssue
                    {
                        Type = IssueType.SecurityConcern,
                        Severity = IssueSeverity.High,
                        Description = $"Potential security-sensitive information in comment: '{pattern}'",
                        Location = comment,
                        Confidence = 0.9
                    });
                }
            }

            // Check comment quality
            if (comment.Length < 5)
            {
                result.Issues.Add(new ModerationIssue
                {
                    Type = IssueType.QualityIssue,
                    Severity = IssueSeverity.Low,
                    Description = "Comment is too short to be meaningful",
                    Location = comment,
                    Confidence = 0.7
                });
            }
        }

        private double AnalyzeClaritySy(string documentation)
        {
            var sentences = documentation.Split('.', '!', '?');
            var avgWordsPerSentence = sentences.Where(s => !string.IsNullOrWhiteSpace(s))
                                              .Average(s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);

            // Penalize very long or very short sentences
            if (avgWordsPerSentence < 5 || avgWordsPerSentence > 25)
                return 60;

            return Math.Min(100, 100 - Math.Abs(avgWordsPerSentence - 15) * 2);
        }

        private double AnalyzeCompleteness(string documentation)
        {
            var score = 50; // Base score

            // Check for essential sections
            var essentialSections = new[] { "overview", "usage", "example", "parameter", "return" };
            var lowerDoc = documentation.ToLower();

            foreach (var section in essentialSections)
            {
                if (lowerDoc.Contains(section))
                    score += 10;
            }

            // Bonus for having code examples
            if (lowerDoc.Contains("```") || lowerDoc.Contains("example"))
                score += 10;

            return Math.Min(100, score);
        }

        private double AnalyzeConsistency(string documentation)
        {
            var lines = documentation.Split('\n');
            var headingStyles = lines.Where(l => l.TrimStart().StartsWith("#")).Select(l => l.TrimStart().TakeWhile(c => c == '#').Count()).ToList();

            if (headingStyles.Any())
            {
                var consistentHeadings = headingStyles.GroupBy(h => h).Count() <= 4; // Allow up to 4 heading levels
                return consistentHeadings ? 85 : 60;
            }

            return 75; // Default score if no headings detected
        }

        private double AnalyzeGrammar(string documentation)
        {
            var issues = 0;
            var totalSentences = documentation.Split('.', '!', '?').Length;

            // Simple grammar checks
            if (!documentation.Any(char.IsUpper)) issues++; // No capital letters
            if (documentation.Count(c => c == '(') != documentation.Count(c => c == ')')) issues++; // Unmatched parentheses
            if (documentation.Contains("  ")) issues++; // Double spaces

            var errorRate = (double)issues / Math.Max(1, totalSentences / 3);
            return Math.Max(50, 100 - (errorRate * 20));
        }
    }
}
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.SemanticKernel;
using PoC1_LegacyAnalyzer_Web.Models;
using PoC1_LegacyAnalyzer_Web.Models.AI102;
using System.Data;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class CognitiveSearchService : ICognitiveSearchService
    {
        private readonly SearchClient _searchClient;
        private readonly ILogger<CognitiveSearchService> _logger;

        public CognitiveSearchService(IConfiguration configuration, ILogger<CognitiveSearchService> logger)
        {
            var endpoint = configuration["CognitiveServices:Search:Endpoint"];
            var apiKey = configuration["CognitiveServices:Search:ApiKey"];
            var indexName = configuration["CognitiveServices:Search:IndexName"] ?? "code-analysis-index";

            if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
            {
                _searchClient = new SearchClient(new Uri(endpoint), indexName, new AzureKeyCredential(apiKey));
            }
            _logger = logger;
        }

        public async Task<bool> IndexCodebaseAsync(MultiFileAnalysisResult analysisResult)
        {
            if (_searchClient == null)
            {
                _logger.LogWarning("Cognitive Search not configured, skipping indexing");
                return false;
            }

            try
            {
                var documents = analysisResult.FileResults.SelectMany(file =>
                                file.StaticAnalysis.Classes.Select(className => new
                                {
                                    ClassName = className,
                                    Methods = file.StaticAnalysis.Methods
                                })
                                .SelectMany(cls =>
                                    cls.Methods.Select(methodName => new CodeDocument
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        FileName = file.FileName,
                                        ClassName = cls.ClassName,
                                        MethodName = methodName,
                                        SourceCode = "", // You may need to adjust this if you have method source code elsewhere
                                        Complexity = 0,  // Adjust as needed
                                        SecurityRisk = "None",
                                        PerformanceRisk = "None",
                                        Keywords = ExtractKeywords(""),
                                        LastModified = DateTime.UtcNow
                                    }))
                            );

                var batch = IndexDocumentsBatch.Create(
                    documents.Select(d => IndexDocumentsAction.MergeOrUpload(d)).ToArray());

                await _searchClient.IndexDocumentsAsync(batch);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing codebase");
                return false;
            }
        }

        public async Task<List<CodeSearchResult>> SearchCodeAsync(string query, string[] filters = null)
        {
            if (_searchClient == null)
            {
                return new List<CodeSearchResult>();
            }

            try
            {
                var searchOptions = new SearchOptions
                {
                    Size = 20,
                    IncludeTotalCount = true
                };

                if (filters != null)
                {
                    searchOptions.Filter = string.Join(" and ", filters);
                }

                var results = await _searchClient.SearchAsync<CodeDocument>(query, searchOptions);

                var searchResults = new List<CodeSearchResult>();
                await foreach (var result in results.Value.GetResultsAsync())
                {
                    searchResults.Add(new CodeSearchResult
                    {
                        Document = result.Document,
                        Score = result.Score ?? 0,
                        Highlights = result.Highlights != null
                            ? result.Highlights.ToDictionary(
                                kvp => kvp.Key,
                                kvp => kvp.Value.ToList())
                            : new Dictionary<string, List<string>>()
                    });
                }

                return searchResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching code with query: {Query}", query);
                return new List<CodeSearchResult>();
            }
        }

        public async Task<List<CodeSearchResult>> SemanticSearchAsync(string naturalLanguageQuery)
        {
            // Fallback to regular search if semantic search is not available
            return await SearchCodeAsync(naturalLanguageQuery);
        }

        public async Task<CodeInsightsResult> GenerateCodeInsightsAsync(string searchQuery)
        {
            var searchResults = await SearchCodeAsync(searchQuery);

            return new CodeInsightsResult
            {
                Insights = GenerateInsights(searchResults),
                PatternFrequency = CalculatePatternFrequency(searchResults),
                Recommendations = GenerateRecommendations(searchResults),
                OverallQualityScore = CalculateQualityScore(searchResults)
            };
        }

        private List<string> ExtractKeywords(string sourceCode)
        {
            var keywords = new List<string>();
            var commonKeywords = new[] { "public", "private", "class", "method", "async", "await", "return", "if", "for", "foreach" };

            foreach (var keyword in commonKeywords)
            {
                if (sourceCode.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    keywords.Add(keyword);
                }
            }

            return keywords;
        }

        private List<CodeInsight> GenerateInsights(List<CodeSearchResult> searchResults)
        {
            var insights = new List<CodeInsight>();

            var highComplexityMethods = searchResults
                .Where(r => r.Document.Complexity > 10)
                .ToList();

            if (highComplexityMethods.Any())
            {
                insights.Add(new CodeInsight
                {
                    Title = "High Complexity Methods Detected",
                    Description = $"Found {highComplexityMethods.Count} methods with complexity > 10",
                    Type = InsightType.Maintainability,
                    Impact = 3,
                    AffectedFiles = highComplexityMethods.Select(m => m.Document.FileName).Distinct().ToList()
                });
            }

            return insights;
        }

        private Dictionary<string, int> CalculatePatternFrequency(List<CodeSearchResult> searchResults)
        {
            var patterns = new Dictionary<string, int>();

            foreach (var result in searchResults)
            {
                foreach (var keyword in result.Document.Keywords)
                {
                    patterns[keyword] = patterns.GetValueOrDefault(keyword, 0) + 1;
                }
            }

            return patterns;
        }

        private List<string> GenerateRecommendations(List<CodeSearchResult> searchResults)
        {
            var recommendations = new List<string>();

            var avgComplexity = searchResults.Average(r => r.Document.Complexity);
            if (avgComplexity > 8)
            {
                recommendations.Add("Consider refactoring methods with high cyclomatic complexity");
            }

            var securityIssues = searchResults.Count(r => r.Document.SecurityRisk != "None");
            if (securityIssues > 0)
            {
                recommendations.Add($"Address {securityIssues} potential security issues identified");
            }

            return recommendations;
        }

        private double CalculateQualityScore(List<CodeSearchResult> searchResults)
        {
            if (!searchResults.Any()) return 100;

            var avgComplexity = searchResults.Average(r => r.Document.Complexity);
            var securityIssueRatio = searchResults.Count(r => r.Document.SecurityRisk != "None") / (double)searchResults.Count;

            var complexityScore = Math.Max(0, 100 - (avgComplexity * 5));
            var securityScore = (1 - securityIssueRatio) * 100;

            return (complexityScore + securityScore) / 2;
        }
    }
}

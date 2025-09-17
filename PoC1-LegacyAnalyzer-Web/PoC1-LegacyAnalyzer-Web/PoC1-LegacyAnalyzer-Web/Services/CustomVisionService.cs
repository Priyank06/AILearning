using PoC1_LegacyAnalyzer_Web.Models.AI102;
using System.Data;
using System.Text.RegularExpressions;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class CustomVisionService : ICustomVisionService
    {
        private readonly ILogger<CustomVisionService> _logger;
        private readonly IConfiguration _configuration;

        public CustomVisionService(IConfiguration configuration, ILogger<CustomVisionService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ClassificationResult> ClassifyArchitecturalPatternAsync(Stream imageStream)
        {
            await Task.Delay(100); // Simulate API call

            var predictions = new List<PatternPrediction>
            {
                new PatternPrediction
                {
                    PatternName = "Layered Architecture",
                    Confidence = 85.5,
                    Description = "Traditional layered architecture with clear separation of concerns",
                    Examples = new List<string> { "Presentation Layer", "Business Layer", "Data Layer" }
                },
                new PatternPrediction
                {
                    PatternName = "Microservices",
                    Confidence = 72.3,
                    Description = "Distributed microservices architecture pattern",
                    Examples = new List<string> { "Service Discovery", "API Gateway", "Individual Services" }
                }
            };

            return new ClassificationResult
            {
                TopPrediction = predictions.OrderByDescending(p => p.Confidence).First(),
                AllPredictions = predictions.OrderByDescending(p => p.Confidence).ToList(),
                OverallConfidence = predictions.Max(p => p.Confidence)
            };
        }

        public async Task<List<CodePatternPrediction>> DetectCodePatternsAsync(Stream codeScreenshot)
        {
            await Task.Delay(150);

            return new List<CodePatternPrediction>
            {
                new CodePatternPrediction
                {
                    PatternType = "Singleton Pattern",
                    Location = new Models.AI102.BoundingBox { X = 10, Y = 50, Width = 200, Height = 100 },
                    Confidence = 88.7,
                    Properties = new Dictionary<string, object>
                    {
                        ["ImplementationType"] = "Thread-Safe Singleton",
                        ["CodeLines"] = "15-25"
                    }
                },
                new CodePatternPrediction
                {
                    PatternType = "Factory Pattern",
                    Location = new Models.AI102.BoundingBox { X = 10, Y = 200, Width = 180, Height = 80 },
                    Confidence = 76.2,
                    Properties = new Dictionary<string, object>
                    {
                        ["FactoryType"] = "Abstract Factory",
                        ["CodeLines"] = "40-55"
                    }
                }
            };
        }

        public async Task<DesignPatternAnalysis> AnalyzeDesignPatternsAsync(string sourceCode)
        {
            await Task.Delay(200);

            var patterns = new List<IdentifiedPattern>();
            var patternUsage = new Dictionary<string, int>();

            // Analyze for Singleton pattern
            if (ContainsSingletonPattern(sourceCode))
            {
                patterns.Add(new IdentifiedPattern
                {
                    PatternName = "Singleton",
                    Location = "Class definition around line " + FindPatternLine(sourceCode, "singleton"),
                    Confidence = 90.0,
                    Quality = EvaluatePatternQuality(sourceCode, "Singleton"),
                    Suggestions = GetSingletonSuggestions(sourceCode)
                });
                patternUsage["Singleton"] = CountPatternOccurrences(sourceCode, "singleton");
            }

            // Analyze for Factory pattern
            if (ContainsFactoryPattern(sourceCode))
            {
                patterns.Add(new IdentifiedPattern
                {
                    PatternName = "Factory",
                    Location = "Method implementation around line " + FindPatternLine(sourceCode, "Create"),
                    Confidence = 85.0,
                    Quality = EvaluatePatternQuality(sourceCode, "Factory"),
                    Suggestions = GetFactorySuggestions(sourceCode)
                });
                patternUsage["Factory"] = CountPatternOccurrences(sourceCode, "Create|Factory");
            }

            // Analyze for Observer pattern
            if (ContainsObserverPattern(sourceCode))
            {
                patterns.Add(new IdentifiedPattern
                {
                    PatternName = "Observer",
                    Location = "Event handling around line " + FindPatternLine(sourceCode, "event|delegate"),
                    Confidence = 80.0,
                    Quality = EvaluatePatternQuality(sourceCode, "Observer"),
                    Suggestions = GetObserverSuggestions(sourceCode)
                });
                patternUsage["Observer"] = CountPatternOccurrences(sourceCode, "event|delegate|EventHandler");
            }

            var recommendations = GeneratePatternRecommendations(patterns, sourceCode);
            var qualityScore = CalculateOverallQuality(patterns);

            return new DesignPatternAnalysis
            {
                Patterns = patterns,
                Recommendations = recommendations,
                OverallQualityScore = qualityScore,
                PatternUsage = patternUsage
            };
        }

        #region Pattern Analysis Helper Methods

        private bool ContainsSingletonPattern(string sourceCode)
        {
            var singletonPattern = @"private\s+static\s+\w+\s+_?instance|static.*getInstance\(\)|private.*constructor";
            return Regex.IsMatch(sourceCode, singletonPattern, RegexOptions.IgnoreCase);
        }

        private bool ContainsFactoryPattern(string sourceCode)
        {
            var factoryPattern = @"Create\w+\(|Factory|new\s+\w+\(.*\)\s*{|switch.*case.*new";
            return Regex.IsMatch(sourceCode, factoryPattern, RegexOptions.IgnoreCase);
        }

        private bool ContainsObserverPattern(string sourceCode)
        {
            var observerPattern = @"event\s+\w+|delegate\s+\w+|EventHandler|Subscribe|Unsubscribe|\+=|\-=";
            return Regex.IsMatch(sourceCode, observerPattern, RegexOptions.IgnoreCase);
        }

        private int FindPatternLine(string sourceCode, string pattern)
        {
            var lines = sourceCode.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (Regex.IsMatch(lines[i], pattern, RegexOptions.IgnoreCase))
                {
                    return i + 1;
                }
            }
            return 1;
        }

        private PatternQuality EvaluatePatternQuality(string sourceCode, string patternType)
        {
            var lines = sourceCode.Split('\n').Length;
            var complexity = sourceCode.Split('{').Length - 1;

            return patternType switch
            {
                "Singleton" when sourceCode.Contains("lock") => PatternQuality.Excellent,
                "Factory" when complexity > 5 => PatternQuality.Good,
                _ when lines < 50 => PatternQuality.Good,
                _ => PatternQuality.NeedsImprovement
            };
        }

        private List<string> GetSingletonSuggestions(string sourceCode)
        {
            var suggestions = new List<string>();

            if (!sourceCode.Contains("lock"))
                suggestions.Add("Consider thread-safety with lock statement or Lazy<T>");
            if (sourceCode.Contains("public") && sourceCode.Contains("constructor"))
                suggestions.Add("Make constructor private to prevent external instantiation");
            if (!sourceCode.Contains("sealed"))
                suggestions.Add("Consider making singleton class sealed");

            return suggestions;
        }

        private List<string> GetFactorySuggestions(string sourceCode)
        {
            var suggestions = new List<string>();

            if (sourceCode.Split("new ").Length > 5)
                suggestions.Add("Consider using Abstract Factory for complex object creation");
            if (!sourceCode.Contains("interface"))
                suggestions.Add("Use interfaces to make factory more flexible");

            return suggestions;
        }

        private List<string> GetObserverSuggestions(string sourceCode)
        {
            var suggestions = new List<string>();

            if (!sourceCode.Contains("null"))
                suggestions.Add("Add null checks before invoking events");
            if (sourceCode.Contains("+=") && !sourceCode.Contains("-="))
                suggestions.Add("Provide unsubscribe mechanism to prevent memory leaks");

            return suggestions;
        }

        private int CountPatternOccurrences(string sourceCode, string pattern)
        {
            return Regex.Matches(sourceCode, pattern, RegexOptions.IgnoreCase).Count;
        }

        private List<string> GeneratePatternRecommendations(List<IdentifiedPattern> patterns, string sourceCode)
        {
            var recommendations = new List<string>();

            if (!patterns.Any())
            {
                recommendations.Add("Consider implementing design patterns to improve code structure");
                recommendations.Add("Factory pattern could help with object creation");
                recommendations.Add("Observer pattern useful for event-driven architecture");
            }
            else
            {
                var poorQualityPatterns = patterns.Where(p => p.Quality == PatternQuality.Poor).ToList();
                if (poorQualityPatterns.Any())
                {
                    recommendations.Add($"Review implementation of {string.Join(", ", poorQualityPatterns.Select(p => p.PatternName))} patterns");
                }

                if (patterns.Count == 1)
                {
                    recommendations.Add("Consider combining with other patterns for better architecture");
                }
            }

            return recommendations;
        }

        private double CalculateOverallQuality(List<IdentifiedPattern> patterns)
        {
            if (!patterns.Any()) return 50.0;

            var qualityScores = patterns.Select(p => p.Quality switch
            {
                PatternQuality.Excellent => 95.0,
                PatternQuality.Good => 80.0,
                PatternQuality.NeedsImprovement => 60.0,
                PatternQuality.Poor => 30.0,
                _ => 50.0
            });

            return qualityScores.Average();
        }

        #endregion
    }
}
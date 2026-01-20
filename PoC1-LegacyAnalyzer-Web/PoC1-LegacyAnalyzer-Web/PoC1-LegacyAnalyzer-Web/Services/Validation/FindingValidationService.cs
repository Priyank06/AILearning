using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace PoC1_LegacyAnalyzer_Web.Services.Validation
{
    /// <summary>
    /// Service for validating AI-generated findings to detect hallucinations and invalid data.
    /// Validates: (1) line numbers exist, (2) evidence snippets present, (3) severity consistency, (4) contradictions.
    /// </summary>
    public class FindingValidationService : IFindingValidationService
    {
        private readonly ILogger<FindingValidationService> _logger;

        // Severity mapping for consistency checks
        private static readonly Dictionary<string, HashSet<string>> ExpectedSeveritiesByCategory = new()
        {
            { "Security", new HashSet<string> { "CRITICAL", "HIGH", "MEDIUM" } },
            { "SQL Injection", new HashSet<string> { "CRITICAL", "HIGH" } },
            { "Authentication", new HashSet<string> { "CRITICAL", "HIGH", "MEDIUM" } },
            { "Authorization", new HashSet<string> { "CRITICAL", "HIGH", "MEDIUM" } },
            { "Performance", new HashSet<string> { "HIGH", "MEDIUM", "LOW" } },
            { "Architecture", new HashSet<string> { "HIGH", "MEDIUM", "LOW" } },
            { "Design", new HashSet<string> { "MEDIUM", "LOW" } },
            { "Code Quality", new HashSet<string> { "MEDIUM", "LOW" } },
            { "Pattern", new HashSet<string> { "MEDIUM", "LOW" } }
        };

        public FindingValidationService(ILogger<FindingValidationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public FindingValidationResult ValidateFinding(Finding finding, string? fileContent, string fileName)
        {
            var result = new FindingValidationResult
            {
                Status = FindingValidationStatus.Validated,
                Checks = new Dictionary<string, bool>()
            };

            if (finding == null)
            {
                result.Status = FindingValidationStatus.Failed;
                result.Errors.Add("Finding is null");
                return result;
            }

            // Check 1: Validate line numbers exist in file
            var lineNumberCheck = ValidateLineNumbers(finding, fileContent, fileName, result);
            result.Checks["LineNumbersValid"] = lineNumberCheck;

            // Check 2: Validate evidence snippets are present in code
            var evidenceCheck = ValidateEvidenceSnippets(finding, fileContent, fileName, result);
            result.Checks["EvidencePresent"] = evidenceCheck;

            // Check 3: Validate severity consistency with category
            var severityCheck = ValidateSeverityConsistency(finding, result);
            result.Checks["SeverityConsistent"] = severityCheck;

            // Determine final status
            if (result.Errors.Any())
            {
                result.Status = FindingValidationStatus.Failed;
            }
            else if (result.Warnings.Any() || !lineNumberCheck || !evidenceCheck || !severityCheck)
            {
                result.Status = FindingValidationStatus.LowConfidence;
            }
            else
            {
                result.Status = FindingValidationStatus.Validated;
            }

            return result;
        }

        public List<FindingValidationResult> ValidateFindings(List<Finding> findings, Dictionary<string, string> fileContents)
        {
            var results = new List<FindingValidationResult>();

            // First, validate each finding individually
            foreach (var finding in findings)
            {
                var fileName = ExtractFileName(finding.Location);
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = "unknown";
                }

                fileContents.TryGetValue(fileName, out var fileContent);
                var result = ValidateFinding(finding, fileContent, fileName);
                results.Add(result);
            }

            // Then check for contradictions between findings
            CheckForContradictions(findings, results);

            return results;
        }

        private bool ValidateLineNumbers(Finding finding, string? fileContent, string fileName, FindingValidationResult result)
        {
            if (string.IsNullOrEmpty(fileContent))
            {
                result.Warnings.Add($"File content not available for validation: {fileName}");
                return false;
            }

            var lineNumbers = ExtractLineNumbers(finding.Location);
            if (lineNumbers.Count == 0)
            {
                // No line number specified - this is acceptable but worth noting
                result.Warnings.Add("No line number specified in location");
                return true; // Not an error, just a warning
            }

            var lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var totalLines = lines.Length;

            foreach (var lineNum in lineNumbers)
            {
                if (lineNum < 1 || lineNum > totalLines)
                {
                    result.Errors.Add($"Line number {lineNum} is out of range. File has {totalLines} lines.");
                    return false;
                }
            }

            return true;
        }

        private bool ValidateEvidenceSnippets(Finding finding, string? fileContent, string fileName, FindingValidationResult result)
        {
            if (finding.Evidence == null || !finding.Evidence.Any())
            {
                result.Warnings.Add("No evidence snippets provided");
                return false;
            }

            if (string.IsNullOrEmpty(fileContent))
            {
                result.Warnings.Add($"File content not available to validate evidence: {fileName}");
                return false;
            }

            // Normalize file content for comparison (remove whitespace differences)
            var normalizedFileContent = NormalizeCode(fileContent);
            var allEvidenceFound = true;

            foreach (var evidence in finding.Evidence)
            {
                if (string.IsNullOrWhiteSpace(evidence))
                    continue;

                var normalizedEvidence = NormalizeCode(evidence);
                
                // Check if evidence snippet exists in file content
                // Use fuzzy matching - evidence might have slight differences (whitespace, formatting)
                if (!normalizedFileContent.Contains(normalizedEvidence, StringComparison.OrdinalIgnoreCase))
                {
                    // Try to find partial matches (at least 50% of the evidence)
                    var evidenceWords = normalizedEvidence.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    if (evidenceWords.Length > 0)
                    {
                        var minWordsToMatch = Math.Max(1, evidenceWords.Length / 2);
                        var matchedWords = evidenceWords.Count(word => 
                            normalizedFileContent.Contains(word, StringComparison.OrdinalIgnoreCase));
                        
                        if (matchedWords < minWordsToMatch)
                        {
                            result.Warnings.Add($"Evidence snippet not found in file: {Truncate(evidence, 50)}");
                            allEvidenceFound = false;
                        }
                    }
                    else
                    {
                        result.Warnings.Add($"Evidence snippet not found in file: {Truncate(evidence, 50)}");
                        allEvidenceFound = false;
                    }
                }
            }

            return allEvidenceFound;
        }

        private bool ValidateSeverityConsistency(Finding finding, FindingValidationResult result)
        {
            if (string.IsNullOrEmpty(finding.Category) || string.IsNullOrEmpty(finding.Severity))
            {
                return true; // Can't validate without category/severity
            }

            var category = finding.Category.Trim();
            var severity = finding.Severity.Trim().ToUpper();

            // Check if category has expected severity ranges
            foreach (var kvp in ExpectedSeveritiesByCategory)
            {
                if (category.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    if (!kvp.Value.Contains(severity))
                    {
                        result.Warnings.Add(
                            $"Severity '{severity}' may be inconsistent with category '{category}'. " +
                            $"Expected: {string.Join(", ", kvp.Value)}");
                        return false;
                    }
                    return true;
                }
            }

            // For unknown categories, check if severity seems reasonable
            // CRITICAL should generally be for security issues
            if (severity == "CRITICAL" && !category.Contains("Security", StringComparison.OrdinalIgnoreCase) &&
                !category.Contains("SQL", StringComparison.OrdinalIgnoreCase) &&
                !category.Contains("Authentication", StringComparison.OrdinalIgnoreCase) &&
                !category.Contains("Authorization", StringComparison.OrdinalIgnoreCase))
            {
                result.Warnings.Add(
                    $"CRITICAL severity used for non-security category '{category}'. " +
                    "This may be inflated.");
                return false;
            }

            return true;
        }

        private void CheckForContradictions(List<Finding> findings, List<FindingValidationResult> results)
        {
            for (int i = 0; i < findings.Count; i++)
            {
                for (int j = i + 1; j < findings.Count; j++)
                {
                    var finding1 = findings[i];
                    var finding2 = findings[j];

                    // Check if findings contradict each other
                    if (AreContradictory(finding1, finding2))
                    {
                        results[i].Warnings.Add(
                            $"Contradicts finding: '{Truncate(finding2.Description, 50)}'");
                        results[j].Warnings.Add(
                            $"Contradicts finding: '{Truncate(finding1.Description, 50)}'");
                        
                        // Downgrade status if not already failed
                        if (results[i].Status == FindingValidationStatus.Validated)
                            results[i].Status = FindingValidationStatus.LowConfidence;
                        if (results[j].Status == FindingValidationStatus.Validated)
                            results[j].Status = FindingValidationStatus.LowConfidence;
                    }
                }
            }
        }

        private bool AreContradictory(Finding finding1, Finding finding2)
        {
            // Simple contradiction detection:
            // 1. Same location but opposite severity
            // 2. Same evidence but different categories
            // 3. Overlapping descriptions with opposite meanings

            if (finding1.Location == finding2.Location && finding1.Location != "")
            {
                var severity1 = finding1.Severity?.ToUpper() ?? "";
                var severity2 = finding2.Severity?.ToUpper() ?? "";

                // Check for opposite severity (e.g., CRITICAL vs LOW)
                if ((severity1 == "CRITICAL" && severity2 == "LOW") ||
                    (severity1 == "LOW" && severity2 == "CRITICAL"))
                {
                    return true;
                }
            }

            // Check for same evidence but different categories
            if (finding1.Evidence != null && finding2.Evidence != null)
            {
                var commonEvidence = finding1.Evidence.Intersect(finding2.Evidence, StringComparer.OrdinalIgnoreCase);
                if (commonEvidence.Any() && finding1.Category != finding2.Category)
                {
                    // Could be contradictory if categories are opposite
                    var cat1 = finding1.Category.ToUpper();
                    var cat2 = finding2.Category.ToUpper();
                    
                    if ((cat1.Contains("SECURITY") && cat2.Contains("SAFE")) ||
                        (cat1.Contains("SAFE") && cat2.Contains("SECURITY")))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private List<int> ExtractLineNumbers(string location)
        {
            var lineNumbers = new List<int>();
            if (string.IsNullOrEmpty(location))
                return lineNumbers;

            // Match patterns like "line 123", "line 45-67", ":123", "at line 123"
            var patterns = new[]
            {
                new Regex(@"line\s+(\d+)", RegexOptions.IgnoreCase),
                new Regex(@":(\d+)", RegexOptions.IgnoreCase),
                new Regex(@"line\s+(\d+)\s*-\s*(\d+)", RegexOptions.IgnoreCase),
                new Regex(@"at\s+line\s+(\d+)", RegexOptions.IgnoreCase)
            };

            foreach (var pattern in patterns)
            {
                var matches = pattern.Matches(location);
                foreach (Match match in matches)
                {
                    for (int i = 1; i < match.Groups.Count; i++)
                    {
                        if (int.TryParse(match.Groups[i].Value, out var lineNum))
                        {
                            lineNumbers.Add(lineNum);
                        }
                    }
                }
            }

            return lineNumbers.Distinct().ToList();
        }

        private string ExtractFileName(string location)
        {
            if (string.IsNullOrEmpty(location))
                return "";

            // Match file patterns like "File.cs", "File.cs line 123", "at File.cs:123"
            var filePattern = new Regex(@"([\w\-_]+\.(cs|js|ts|py|java|cpp|h|cshtml|html|css|json|xml))", RegexOptions.IgnoreCase);
            var match = filePattern.Match(location);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return "";
        }

        private string NormalizeCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                return "";

            // Remove extra whitespace, normalize line endings, remove comments for comparison
            var normalized = Regex.Replace(code, @"\s+", " ");
            normalized = normalized.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");
            normalized = Regex.Replace(normalized, @"//.*?$", "", RegexOptions.Multiline);
            normalized = Regex.Replace(normalized, @"/\*.*?\*/", "", RegexOptions.Singleline);
            
            return normalized.Trim();
        }

        private string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text ?? "";
            return text.Substring(0, maxLength) + "...";
        }
    }
}


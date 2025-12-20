using PoC1_LegacyAnalyzer_Web.Models;
using System.Text.RegularExpressions;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Generic multi-language pattern detection strategy for JavaScript, TypeScript, Java, Go, etc.
    /// Detects common security, performance, and architectural issues across multiple languages.
    /// </summary>
    public class MultiLanguagePatternDetectionStrategy : IPatternDetectionStrategy
    {
        public void DetectPatterns(string code, CodePatternAnalysis analysis)
        {
            DetectSecurityPatterns(code, analysis);
            DetectPerformancePatterns(code, analysis);
            DetectArchitecturePatterns(code, analysis);
        }

        private void DetectSecurityPatterns(string code, CodePatternAnalysis analysis)
        {
            // 1. SQL Injection - string concatenation in SQL
            if (Regex.IsMatch(code, @"(SELECT|INSERT|UPDATE|DELETE)\s+.*[\+\`].*\{|SELECT.*\$\{", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[Critical] SQL injection risk: SQL query built via string concatenation or template literals");
            }

            // 2. Hardcoded credentials and secrets
            if (Regex.IsMatch(code, @"(password|pwd|secret|token|api[_-]?key|apikey)\s*[:=]\s*[""'][^""']+[""']", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[High] Hardcoded credentials or secrets detected");
            }

            // 3. Plain text password storage
            if (Regex.IsMatch(code, @"password\s*[:=]\s*[^,}\n]+(?!.*hash|.*bcrypt|.*pbkdf2|.*scrypt|.*argon2|.*encrypt)", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[Critical] Plain text password storage: passwords stored without hashing");
            }

            // 4. eval() usage (JavaScript/TypeScript)
            if (Regex.IsMatch(code, @"\beval\s*\(|\bFunction\s*\([""']", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[Critical] Code injection risk: eval() usage with user input");
            }

            // 5. InnerHTML assignment (XSS)
            if (Regex.IsMatch(code, @"\.innerHTML\s*=\s*.*\+|\.innerHTML\s*=\s*.*\$\{", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[Critical] XSS vulnerability: innerHTML assignment without sanitization");
            }

            // 6. Weak password validation
            if (Regex.IsMatch(code, @"(password|pwd)\.(length|len)\s*[<>=]\s*[0-4]", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[Medium] Weak password validation: minimum length too short");
            }

            // 7. Hardcoded credentials in authentication
            if (Regex.IsMatch(code, @"if\s*\([^)]*==\s*[""'](admin|password|1234|admin123)", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[Critical] Hardcoded credentials in authentication logic");
            }

            // 8. Missing input validation
            if (Regex.IsMatch(code, @"function\s+\w+\s*\([^)]*\)|public\s+\w+\s+[a-zA-Z]+\s*\([^)]*\)", RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(code, @"(validate|check|verify|isValid|sanitize)", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[Medium] Missing input validation: function parameters used without validation");
            }
        }

        private void DetectPerformancePatterns(string code, CodePatternAnalysis analysis)
        {
            // 1. Nested loops
            if (Regex.IsMatch(code, @"for\s*\([^)]*\)\s*\{[^}]*for\s*\([^)]*\)\s*\{[^}]*for\s*\(", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(code, @"for\s+\w+\s+in\s+.*:\s*\n[^\n]*for\s+\w+\s+in\s+.*:\s*\n[^\n]*for", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[Critical] Nested loops: O(n²) or O(n³) complexity detected. Remediation: Optimize algorithm or use data structures for O(1) lookups.");
            }

            // 2. String concatenation in loops
            if (Regex.IsMatch(code, @"for\s*\([^)]*\)\s*\{[^}]*\w+\s*\+=\s*|for\s+.*:\s*\n[^\n]*\w+\s*\+=\s*", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[High] String concatenation in loop (O(n²) performance). Remediation: Use array.join() or StringBuilder.");
            }

            // 3. Loading all records without pagination
            if (Regex.IsMatch(code, @"\.(all|findAll|fetchAll|getAll)\s*\(\)|SELECT\s+\*\s+FROM\s+\w+\s*;", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[High] No pagination: loading all records at once (scalability issue). Remediation: Implement pagination or limit query results.");
            }

            // 4. Repeated database queries in loops
            if (Regex.IsMatch(code, @"for\s*\([^)]*\)\s*\{[^}]*\.(query|execute|find|get)\s*\(|for\s+.*:\s*\n[^\n]*\.(query|execute)", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[Critical] N+1 query problem: database queries inside loops. Remediation: Batch queries or use JOIN operations.");
            }

            // 5. Callback hell (JavaScript/TypeScript)
            if (Regex.IsMatch(code, @"\.then\s*\([^)]*=>\s*\{[^}]*\.then\s*\([^)]*=>\s*\{[^}]*\.then", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[Medium] Callback hell: deeply nested promises. Remediation: Use async/await for better readability and error handling.");
            }

            // 6. Missing caching
            if (Regex.IsMatch(code, @"function\s+\w+\s*\([^)]*\)|public\s+\w+\s+[a-zA-Z]+\s*\([^)]*\)", RegexOptions.IgnoreCase) &&
                Regex.IsMatch(code, @"\.(query|fetch|get|request)", RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(code, @"(cache|Cache|CACHE)", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[Medium] Missing caching: expensive operations called repeatedly. Remediation: Implement caching strategy.");
            }
        }

        private void DetectArchitecturePatterns(string code, CodePatternAnalysis analysis)
        {
            // 1. Global variables
            if (Regex.IsMatch(code, @"^(var|let|const)\s+\w+\s*=\s*[^=]|^[a-zA-Z_][a-zA-Z0-9_]*\s*:=\s*", RegexOptions.Multiline))
            {
                analysis.ArchitectureFindings.Add("[Medium] Global variables: shared mutable state can lead to bugs. Remediation: Use modules, classes, or dependency injection.");
            }

            // 2. Missing error handling
            if (Regex.IsMatch(code, @"(function|public|private|func)\s+\w+\s*\([^)]*\)", RegexOptions.IgnoreCase) &&
                Regex.IsMatch(code, @"\.(query|execute|fetch|open|read)", RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(code, @"(try|catch|except|defer|finally)", RegexOptions.IgnoreCase))
            {
                analysis.ArchitectureFindings.Add("[High] Missing error handling: operations without try/catch blocks. Remediation: Add proper exception handling.");
            }

            // 3. Tight coupling
            if (Regex.IsMatch(code, @"new\s+\w+Service\s*\(|new\s+\w+Manager\s*\(", RegexOptions.IgnoreCase))
            {
                analysis.ArchitectureFindings.Add("[Medium] Tight coupling: direct service instantiation. Remediation: Use dependency injection.");
            }

            // 4. Long functions
            var lines = code.Split('\n');
            var longFunctionCount = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (Regex.IsMatch(lines[i], @"^\s*(function|public|private|func|def)\s+\w+"))
                {
                    var funcStart = i;
                    var funcEnd = lines.Length;
                    for (int j = funcStart + 1; j < lines.Length; j++)
                    {
                        if (Regex.IsMatch(lines[j], @"^\s*(function|public|private|func|def|class|})\s*") && 
                            !Regex.IsMatch(lines[j], @"^\s*//|^\s*#"))
                        {
                            funcEnd = j;
                            break;
                        }
                    }
                    var funcLength = funcEnd - funcStart;
                    if (funcLength > 50)
                    {
                        longFunctionCount++;
                    }
                }
            }
            if (longFunctionCount > 0)
            {
                analysis.ArchitectureFindings.Add($"[Medium] Long functions: {longFunctionCount} function(s) exceed 50 lines (maintainability concern). Remediation: Break into smaller functions.");
            }
        }
    }
}


using PoC1_LegacyAnalyzer_Web.Models;
using System.Text.RegularExpressions;

namespace PoC1_LegacyAnalyzer_Web.Services.CodeAnalysis
{
    /// <summary>
    /// Python-specific pattern detection strategy.
    /// Detects common security, performance, and architectural issues in Python code.
    /// </summary>
    public class PythonPatternDetectionStrategy : IPatternDetectionStrategy
    {
        public void DetectPatterns(string code, CodePatternAnalysis analysis)
        {
            DetectSecurityPatterns(code, analysis);
            DetectPerformancePatterns(code, analysis);
            DetectArchitecturePatterns(code, analysis);
        }

        private void DetectSecurityPatterns(string code, CodePatternAnalysis analysis)
        {
            // 1. SQL Injection - f-strings or % formatting in SQL queries
            if (Regex.IsMatch(code, @"(SELECT|INSERT|UPDATE|DELETE)\s+.*f[""'].*\{.*\}.*[""']|%s|%d.*SELECT", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[Critical] SQL injection risk: SQL query built via string formatting or f-strings");
            }

            // 2. Hardcoded credentials and secrets
            if (Regex.IsMatch(code, @"(password|pwd|secret|token|api_key|apikey)\s*=\s*[""'][^""']+[""']", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[High] Hardcoded credentials or secrets detected");
            }

            // 3. Plain text password storage
            if (Regex.IsMatch(code, @"password\s*[:=]\s*[^,}\n]+(?!.*hash|.*bcrypt|.*pbkdf2|.*scrypt|.*argon2)", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[Critical] Plain text password storage: passwords stored without hashing");
            }

            // 4. Weak password validation
            if (Regex.IsMatch(code, @"len\(password\)\s*[<>=]\s*[0-4]", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[Medium] Weak password validation: minimum length too short (< 8 characters)");
            }

            // 5. eval() or exec() usage
            if (Regex.IsMatch(code, @"\beval\s*\(|\bexec\s*\(", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[Critical] Code injection risk: eval() or exec() usage with user input");
            }

            // 6. Insecure deserialization (pickle)
            if (Regex.IsMatch(code, @"pickle\.(load|loads)\s*\(", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[High] Insecure deserialization: pickle.load() can execute arbitrary code");
            }

            // 7. Shell injection (os.system, subprocess without shell=False)
            if (Regex.IsMatch(code, @"os\.system\s*\(|subprocess\.(call|Popen)\s*\([^)]*shell\s*=\s*True", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[Critical] Shell injection risk: os.system() or subprocess with shell=True");
            }

            // 8. Weak random number generation
            if (Regex.IsMatch(code, @"random\.(randint|choice|random)\s*\(|random\.seed\s*\(", RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(code, @"secrets\.", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[Medium] Weak random number generation: use secrets module for cryptographic randomness");
            }

            // 9. Missing input validation
            if (Regex.IsMatch(code, @"def\s+\w+\s*\([^)]*\)\s*:.*\n[^\n]*\w+\s*=\s*\w+\[", RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(code, @"(isinstance|validate|check|verify)", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[Medium] Missing input validation: function parameters used without type/range checks");
            }

            // 10. Hardcoded credentials in authentication
            if (Regex.IsMatch(code, @"if\s+.*==\s*[""'](admin|password|1234|admin123)", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[Critical] Hardcoded credentials in authentication logic");
            }
        }

        private void DetectPerformancePatterns(string code, CodePatternAnalysis analysis)
        {
            // 1. Loading all records without pagination
            if (Regex.IsMatch(code, @"\.(all|fetchall|find_all)\s*\(\)|for\s+\w+\s+in\s+range\s*\(\s*[0-9]{4,}", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[High] No pagination: loading all records at once (scalability issue). Remediation: Implement pagination or limit query results.");
            }

            // 2. Nested loops with large ranges
            if (Regex.IsMatch(code, @"for\s+\w+\s+in\s+.*:\s*\n[^\n]*for\s+\w+\s+in\s+.*:\s*\n[^\n]*for\s+\w+\s+in\s+range", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[Critical] Nested loops: O(n²) or O(n³) complexity detected. Remediation: Optimize algorithm or use data structures (sets, dicts) for O(1) lookups.");
            }

            // 3. String concatenation in loops
            if (Regex.IsMatch(code, @"for\s+.*:\s*\n[^\n]*\w+\s*\+=\s*", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[High] String concatenation in loop (O(n²) performance). Remediation: Use list.join() or list comprehension.");
            }

            // 4. Repeated database queries in loops
            if (Regex.IsMatch(code, @"for\s+.*:\s*\n[^\n]*\.(query|execute|fetchone|get)\s*\(", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[Critical] N+1 query problem: database queries inside loops. Remediation: Batch queries or use JOIN operations.");
            }

            // 5. Synchronous I/O in loops
            if (Regex.IsMatch(code, @"for\s+.*:\s*\n[^\n]*(requests\.(get|post)|urllib|open\s*\()", RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(code, @"async|await|asyncio", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[High] Synchronous I/O in loops: blocking network calls. Remediation: Use async/await with aiohttp or concurrent.futures.");
            }

            // 6. Large list comprehensions without generators
            if (Regex.IsMatch(code, @"\[.*for\s+.*in\s+range\s*\(\s*[0-9]{4,}", RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(code, @"\(.*for\s+.*in\s+.*\)", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[Medium] Large list comprehension: memory-intensive. Remediation: Use generator expressions for large datasets.");
            }

            // 7. Missing caching for expensive operations
            if (Regex.IsMatch(code, @"def\s+\w+\s*\([^)]*\)\s*:.*\n[^\n]*(\.(query|execute|fetch)|requests\.|urllib)", RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(code, @"@(lru_cache|cache|memoize)", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[Medium] Missing caching: expensive operations called repeatedly. Remediation: Add @lru_cache or implement caching strategy.");
            }
        }

        private void DetectArchitecturePatterns(string code, CodePatternAnalysis analysis)
        {
            // 1. Global variables
            if (Regex.IsMatch(code, @"^[a-zA-Z_][a-zA-Z0-9_]*\s*=\s*[^=]", RegexOptions.Multiline))
            {
                analysis.ArchitectureFindings.Add("[Medium] Global variables: shared mutable state can lead to bugs. Remediation: Use classes or dependency injection.");
            }

            // 2. Tight coupling (direct instantiation)
            if (Regex.IsMatch(code, @"class\s+\w+.*:\s*\n[^\n]*\w+Service\s*\([^)]*\)", RegexOptions.IgnoreCase))
            {
                analysis.ArchitectureFindings.Add("[Medium] Tight coupling: direct service instantiation in classes. Remediation: Use dependency injection.");
            }

            // 3. Missing error handling
            if (Regex.IsMatch(code, @"def\s+\w+\s*\([^)]*\)\s*:.*\n[^\n]*(\.(query|execute|open|read)|requests\.)", RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(code, @"try:|except|raise", RegexOptions.IgnoreCase))
            {
                analysis.ArchitectureFindings.Add("[High] Missing error handling: operations without try/except blocks. Remediation: Add proper exception handling.");
            }

            // 4. Long functions/methods
            var lines = code.Split('\n');
            foreach (var line in lines)
            {
                if (Regex.IsMatch(line, @"^\s*def\s+\w+"))
                {
                    // Count lines until next def or class
                    var funcStart = Array.IndexOf(lines, line);
                    var funcEnd = lines.Length;
                    for (int i = funcStart + 1; i < lines.Length; i++)
                    {
                        if (Regex.IsMatch(lines[i], @"^\s*(def|class)\s+") && !Regex.IsMatch(lines[i], @"^\s+#"))
                        {
                            funcEnd = i;
                            break;
                        }
                    }
                    var funcLength = funcEnd - funcStart;
                    if (funcLength > 50)
                    {
                        analysis.ArchitectureFindings.Add("[Medium] Long function: function exceeds 50 lines (maintainability concern). Remediation: Break into smaller functions.");
                        break; // Only report once
                    }
                }
            }

            // 5. No type hints
            if (Regex.IsMatch(code, @"def\s+\w+\s*\([^)]*\)\s*:", RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(code, @"def\s+\w+\s*\([^)]*:\s*(int|str|float|bool|List|Dict|Optional)", RegexOptions.IgnoreCase))
            {
                analysis.ArchitectureFindings.Add("[Low] Missing type hints: code lacks type annotations (Python 3.5+). Remediation: Add type hints for better IDE support and documentation.");
            }
        }
    }
}


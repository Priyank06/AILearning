using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

namespace PoC1_LegacyAnalyzer_Web.Services.CodeAnalysis
{
    /// <summary>
    /// C# specific pattern detection strategy.
    /// </summary>
    public class CSharpPatternDetectionStrategy : IPatternDetectionStrategy
    {
        public void DetectPatterns(string code, CodePatternAnalysis analysis)
        {
            DetectSecurityPatterns(code, analysis);
            DetectPerformancePatterns(code, analysis);
            DetectArchitecturePatterns(code, analysis);
        }

        private void DetectSecurityPatterns(string code, CodePatternAnalysis analysis)
        {
            // 1. SQL Injection
            if (Regex.IsMatch(code, @"SELECT\s+.*\+\s*", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[Critical] SQL injection risk: SQL query built via string concatenation");
            }
            // 2. Hardcoded credentials
            if (Regex.IsMatch(code, @"(Password|PWD|Secret|Token)\s*=\s*""[^\""""]*""", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[High] Hardcoded credentials or secrets detected");
            }
            // 3. Exception swallowing
            if (Regex.IsMatch(code, @"catch\s*\{\s*\}", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[High] Exception swallowing: catch block without logging or rethrowing");
            }
            // 4. Unvalidated redirects
            if (Regex.IsMatch(code, @"Response\.Redirect\s*\(\s*.*user(Input|Name|Param|Request)", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[High] Unvalidated redirect: Response.Redirect with user input");
            }
            // 5. XSS vulnerabilities
            if (Regex.IsMatch(code, @"\.innerHTML\s*=\s*.*user(Input|Name|Param|Request)", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[Critical] XSS vulnerability: innerHTML assignment without encoding");
            }
            // 6. Path traversal
            if (Regex.IsMatch(code, @"File\.(Open|ReadAllText|WriteAllText)\s*\(\s*.*\+\s*user(Input|Name|Param|Request)", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[High] Path traversal risk: File access with concatenated user input");
            }
            // 7. Weak crypto
            if (Regex.IsMatch(code, @"(MD5CryptoServiceProvider|SHA1CryptoServiceProvider|DESCryptoServiceProvider|new\s+MD5|new\s+SHA1|new\s+DES)", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[Critical] Weak cryptography: MD5, SHA1, or DES usage detected");
            }
            // 8. Insecure deserialization
            if (Regex.IsMatch(code, @"new\s+BinaryFormatter\s*\(", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[Critical] Insecure deserialization: BinaryFormatter usage");
            }
            // 9. CSRF vulnerability
            if (Regex.IsMatch(code, @"\[HttpPost\][^\[]*public\s+.*ActionResult", RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(code, @"ValidateAntiForgeryToken", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[High] CSRF vulnerability: missing anti-forgery token on POST action");
            }
            // 10. Open redirects
            if (Regex.IsMatch(code, @"Redirect\s*\(\s*user(Input|Name|Param|Request)", RegexOptions.IgnoreCase) &&
                Regex.IsMatch(code, @"IsAuthenticated", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[High] Open redirect: authentication flow allows user-controlled redirect");
            }
            // 11. Sensitive data in logs
            if (Regex.IsMatch(code, @"Log(Information|Debug|Error|Warning)\s*\(\s*.*(Password|Token|Secret)", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[High] Sensitive data in logs: logging passwords or tokens");
            }
            // 12. Missing input validation
            if (Regex.IsMatch(code, @"Request\[\s*""[a-zA-Z0-9_]+""\s*\]", RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(code, @"(int\.TryParse|Validate|Sanitize|IsValid)", RegexOptions.IgnoreCase))
            {
                analysis.SecurityFindings.Add("[Medium] Missing input validation: direct parameter usage without checks");
            }
        }

        private void DetectPerformancePatterns(string code, CodePatternAnalysis analysis)
        {
            // 1. Blocking delays
            if (Regex.IsMatch(code, @"Task\.Delay\(|Thread\.Sleep\(", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[High] Blocking or artificial delays present (can cause 10x slower response). Remediation: Remove or replace with non-blocking logic.");
            }
            // 2. Sync-over-async
            if (Regex.IsMatch(code, @"\.Result|\.Wait\(\)", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[High] Synchronous wait on async calls (thread starvation risk). Remediation: Use async/await throughout call chain.");
            }
            // 3. N+1 query problem
            if (Regex.IsMatch(code, @"foreach\s*\(.*\)\s*\{[^\}]*\.(ExecuteReader|ToList|Find|Get|Query|Select|Load|Fetch)\s*\(", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[Critical] N+1 query problem: foreach loop with DB calls (can be 100x slower). Remediation: Batch queries or use .Include/.Join.");
            }
            // 4. Large object allocation in loops
            if (Regex.IsMatch(code, @"for(each)?\s*\(.*\)\s*\{[^\}]*new\s+[A-Z][A-Za-z0-9_]*\s*\(", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[High] Large object allocation inside loop (GC pressure, memory spikes). Remediation: Move allocation outside loop or reuse objects.");
            }
            // 5. String concatenation in loops
            if (Regex.IsMatch(code, @"for(each)?\s*\(.*\)\s*\{[^\}]*\+\s*=\s*.*string", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[High] String concatenation in loop (O(n^2) performance). Remediation: Use StringBuilder for repeated string operations.");
            }
            // 6. LINQ queries with multiple enumerations
            if (Regex.IsMatch(code, @"\.Where\(.*\)\.Select\(.*\)[^;]*\.Where\(.*\)", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(code, @"foreach\s*\(var\s+item\s+in\s+[a-zA-Z0-9_]+\.Where\(.*\)\)", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[Medium] LINQ query with multiple enumerations (hidden double/triple iteration). Remediation: Use .ToList() to materialize results.");
            }
            // 7. Unnecessary boxing/unboxing
            if (Regex.IsMatch(code, @"object\s*=\s*\(int|double|float|bool|string\)\s*[a-zA-Z0-9_]+", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[Medium] Unnecessary boxing/unboxing (heap allocations, slower access). Remediation: Use value types directly or generics.");
            }
            // 8. Large ViewState
            if (Regex.IsMatch(code, @"ViewState\[.*\]\s*=\s*new\s+[A-Z][A-Za-z0-9_]*\s*\(", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[High] Large ViewState detected (slow page loads, high bandwidth). Remediation: Minimize ViewState usage and size.");
            }
            // 9. Missing async/await in I/O
            if (Regex.IsMatch(code, @"File\.(ReadAllText|WriteAllText|Open)\s*\(", RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(code, @"await\s+File\.(ReadAllTextAsync|WriteAllTextAsync|OpenAsync)", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[High] Missing async/await in I/O operations (blocks threads, poor scalability). Remediation: Use async I/O APIs.");
            }
            // 10. Excessive exception handling
            if (Regex.Matches(code, @"catch\s*\{", RegexOptions.IgnoreCase).Count > 3)
            {
                analysis.PerformanceFindings.Add("[Medium] Excessive exception handling in hot paths (try/catch in tight loops). Remediation: Refactor to avoid exceptions in performance-critical code.");
            }
            // 11. Reflection in hot paths
            if (Regex.IsMatch(code, @"Type\.Get(Type|Method|Property|Field)\s*\(", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(code, @"Assembly\.Load|Activator\.CreateInstance", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[High] Reflection in performance-critical code (100x slower than direct calls). Remediation: Cache reflection results or avoid reflection in hot paths.");
            }
            // 12. Missing connection pooling
            if (Regex.IsMatch(code, @"new\s+SqlConnection\s*\(", RegexOptions.IgnoreCase) &&
                Regex.IsMatch(code, @"ConnectionPooling\s*=\s*false", RegexOptions.IgnoreCase))
            {
                analysis.PerformanceFindings.Add("[Critical] Missing database connection pooling (connection overhead, scalability bottleneck). Remediation: Enable connection pooling in connection string.");
            }
        }

        private void DetectArchitecturePatterns(string code, CodePatternAnalysis analysis)
        {
            try
            {
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetRoot();
                DetectArchitecturePatternsInternal(root, code, analysis);
            }
            catch
            {
                // Fallback to regex-only detection if Roslyn parsing fails
                if (Regex.IsMatch(code, @"new\s+SqlConnection\s*\(", RegexOptions.IgnoreCase))
                {
                    analysis.ArchitectureFindings.Add("[High] Tight coupling to SQL connection inside business logic. Remediation: Use dependency injection with IDbConnection interface. Effort: 2-4 hours. Pattern: Dependency Injection, Repository Pattern.");
                }
            }
        }

        private void DetectArchitecturePatternsInternal(SyntaxNode root, string code, CodePatternAnalysis analysis)
        {
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
            var allMethods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
            var allInterfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().ToList();
            var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>().Select(u => u.Name?.ToString() ?? "").ToList();

            // 1. God classes
            foreach (var cls in classes)
            {
                var methods = cls.Members.OfType<MethodDeclarationSyntax>().Count();
                if (methods >= 20)
                {
                    analysis.ArchitectureFindings.Add($"[High] God class detected: '{cls.Identifier}' has {methods} methods (threshold: 20). Remediation: Apply Extract Class refactoring, split into focused classes with single responsibility. Effort: 1-3 days. Pattern: Single Responsibility Principle, Facade Pattern.");
                }
            }

            // 2. Long methods
            foreach (var method in allMethods)
            {
                var startLine = method.GetLocation().GetLineSpan().StartLinePosition.Line;
                var endLine = method.GetLocation().GetLineSpan().EndLinePosition.Line;
                var lineCount = endLine - startLine + 1;

                if (lineCount > 50)
                {
                    var className = method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier.ToString() ?? "Unknown";
                    analysis.ArchitectureFindings.Add($"[Medium] Long method detected: '{className}.{method.Identifier}' has {lineCount} lines (threshold: 50). Remediation: Extract Method refactoring, break into smaller focused methods. Effort: 2-6 hours. Pattern: Extract Method, Command Pattern.");
                }
            }

            // 3. Deep inheritance
            foreach (var cls in classes)
            {
                var depth = CalculateInheritanceDepth(cls, classes);
                if (depth > 4)
                {
                    analysis.ArchitectureFindings.Add($"[High] Deep inheritance detected: '{cls.Identifier}' has inheritance depth of {depth} levels (threshold: 4). Remediation: Favor composition over inheritance, use Strategy or Decorator patterns. Effort: 2-5 days. Pattern: Composition over Inheritance, Strategy Pattern, Decorator Pattern.");
                }
            }

            // 4. Circular dependencies (heuristic)
            var namespaceNames = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>()
                .Select(n => n.Name.ToString()).ToList();
            if (namespaceNames.Count > 5 && usingDirectives.Count > 10)
            {
                var uniqueUsings = usingDirectives.Distinct().Count();
                if (uniqueUsings > namespaceNames.Count * 2)
                {
                    analysis.ArchitectureFindings.Add($"[Medium] Potential circular dependencies: {uniqueUsings} using statements across {namespaceNames.Count} namespaces. Remediation: Introduce dependency inversion, use interfaces to break cycles. Effort: 3-7 days. Pattern: Dependency Inversion Principle, Mediator Pattern.");
                }
            }

            // 5. Missing interfaces
            var concreteInstantiations = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
                .Where(expr => expr.Type != null)
                .Select(expr => expr.Type.ToString())
                .Where(type => !type.StartsWith("I", StringComparison.OrdinalIgnoreCase) &&
                              !type.Contains("List") && !type.Contains("Dictionary") &&
                              !type.Contains("Array") && !type.Contains("StringBuilder"))
                .ToList();

            var interfaceCount = allInterfaces.Count;
            var concreteClassCount = classes.Count;

            if (concreteInstantiations.Count > 5 && interfaceCount < concreteClassCount / 3)
            {
                analysis.ArchitectureFindings.Add($"[High] Missing interfaces: {concreteInstantiations.Count} concrete instantiations with only {interfaceCount} interfaces for {concreteClassCount} classes. Remediation: Extract interfaces for dependencies, use dependency injection. Effort: 2-5 days. Pattern: Dependency Inversion Principle, Interface Segregation Principle.");
            }

            // 6. Static cling
            var staticMethods = allMethods.Count(m => m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));
            var totalMethods = allMethods.Count;

            if (totalMethods > 0 && staticMethods > totalMethods * 0.3)
            {
                analysis.ArchitectureFindings.Add($"[Medium] Static cling: {staticMethods} static methods out of {totalMethods} total ({staticMethods * 100 / totalMethods}%). Remediation: Replace static methods with instance methods, use dependency injection for testability. Effort: 1-3 days. Pattern: Dependency Injection, Service Locator Pattern.");
            }

            // 7. Feature envy
            foreach (var method in allMethods)
            {
                var className = method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if (className == null) continue;

                var methodBody = method.Body?.ToString() ?? method.ExpressionBody?.ToString() ?? "";
                var ownClassReferences = Regex.Matches(methodBody, $@"\b{className.Identifier}\b").Count;

                var otherClassReferences = classes
                    .Where(c => c != className)
                    .Sum(c => Regex.Matches(methodBody, $@"\b{c.Identifier}\b").Count);

                if (otherClassReferences > ownClassReferences * 2 && otherClassReferences > 3)
                {
                    analysis.ArchitectureFindings.Add($"[Medium] Feature envy detected: '{className.Identifier}.{method.Identifier}' accesses other classes {otherClassReferences} times vs own class {ownClassReferences} times. Remediation: Move method to the class it uses most, or extract to a shared service. Effort: 2-8 hours. Pattern: Move Method refactoring, Extract Class.");
                }
            }

            // 8. Shotgun surgery
            foreach (var cls in classes)
            {
                var memberTypes = new HashSet<string>();
                foreach (var member in cls.Members)
                {
                    if (member is MethodDeclarationSyntax) memberTypes.Add("Method");
                    else if (member is PropertyDeclarationSyntax) memberTypes.Add("Property");
                    else if (member is FieldDeclarationSyntax) memberTypes.Add("Field");
                    else if (member is EventDeclarationSyntax) memberTypes.Add("Event");
                    else if (member is ClassDeclarationSyntax) memberTypes.Add("NestedClass");
                }

                if (memberTypes.Count >= 4 && cls.Members.Count > 10)
                {
                    analysis.ArchitectureFindings.Add($"[Medium] Potential shotgun surgery: '{cls.Identifier}' has {memberTypes.Count} different member types ({cls.Members.Count} total members), suggesting multiple responsibilities. Remediation: Apply Single Responsibility Principle, split into focused classes. Effort: 1-2 days. Pattern: Single Responsibility Principle, Extract Class refactoring.");
                }
            }

            // 9. Primitive obsession
            var primitiveParams = allMethods
                .Where(m => m.ParameterList.Parameters.Count(p =>
                    p.Type?.ToString() == "string" ||
                    p.Type?.ToString() == "int" ||
                    p.Type?.ToString() == "double" ||
                    p.Type?.ToString() == "decimal" ||
                    p.Type?.ToString() == "bool") >= 3)
                .ToList();

            if (primitiveParams.Count > 0)
            {
                var className = primitiveParams.First().Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier.ToString() ?? "Unknown";
                analysis.ArchitectureFindings.Add($"[Low] Primitive obsession: {primitiveParams.Count} methods with 3+ primitive parameters detected. Remediation: Introduce value objects (e.g., Money, Email, Address) to encapsulate related primitives. Effort: 1-3 days. Pattern: Value Object Pattern, Introduce Parameter Object.");
            }

            // 10. Missing separation of concerns
            var uiIndicators = new[] { "Page", "Controller", "View", "Razor", "Component", "Form", "UserControl" };
            var businessLogicIndicators = new[] { "Calculate", "Process", "Validate", "Business", "Service", "Repository", "DataAccess" };

            foreach (var cls in classes)
            {
                var className = cls.Identifier.ToString();
                var isUI = uiIndicators.Any(ind => className.Contains(ind, StringComparison.OrdinalIgnoreCase));

                if (isUI)
                {
                    var hasBusinessLogic = cls.Members.OfType<MethodDeclarationSyntax>()
                        .Any(m => businessLogicIndicators.Any(ind =>
                            m.Identifier.ToString().Contains(ind, StringComparison.OrdinalIgnoreCase) ||
                            (m.Body?.ToString() ?? "").Contains(ind, StringComparison.OrdinalIgnoreCase)));

                    if (hasBusinessLogic)
                    {
                        analysis.ArchitectureFindings.Add($"[High] Missing separation of concerns: '{className}' appears to be UI layer but contains business logic. Remediation: Extract business logic to service layer, use MVC/MVP/MVVM patterns. Effort: 2-5 days. Pattern: Layered Architecture, MVC Pattern, Service Layer Pattern.");
                    }
                }
            }

            // 11. Tight coupling to SQL
            if (Regex.IsMatch(code, @"new\s+SqlConnection\s*\(", RegexOptions.IgnoreCase))
            {
                analysis.ArchitectureFindings.Add("[High] Tight coupling to SQL connection inside business logic. Remediation: Use dependency injection with IDbConnection interface. Effort: 2-4 hours. Pattern: Dependency Injection, Repository Pattern.");
            }
        }

        private int CalculateInheritanceDepth(ClassDeclarationSyntax classDecl, List<ClassDeclarationSyntax> allClasses)
        {
            if (classDecl.BaseList == null || !classDecl.BaseList.Types.Any())
                return 1;

            var baseType = classDecl.BaseList.Types.First().Type.ToString();
            var baseClass = allClasses.FirstOrDefault(c => c.Identifier.ToString() == baseType);

            if (baseClass == null)
                return 2; // Base class not in this file, assume depth of 2

            return 1 + CalculateInheritanceDepth(baseClass, allClasses);
        }
    }
}


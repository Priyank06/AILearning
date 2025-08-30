using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Azure.AI.OpenAI;
using Azure;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AICodeAnalyzer
{
    class Program
    {
        private static OpenAIClient _openAIClient;
        private static string _deploymentName;
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("🤖 AI-Enhanced Code Analyzer - Day 2 (Simplified)");
            Console.WriteLine("==================================================");
            
            if (!InitializeAzureOpenAI())
            {
                Console.WriteLine("❌ Failed to initialize Azure OpenAI. Exiting.");
                return;
            }
            
            // Parse simple arguments
            string analysisType = "general";
            string targetPath = "TestCode.cs";
            
            if (args.Length > 0 && args[0] == "--help")
            {
                ShowUsage();
                return;
            }
            
            if (args.Length > 0 && args[0] == "--type" && args.Length > 1)
            {
                analysisType = args[1];
                if (args.Length > 2)
                    targetPath = args[2];
            }
            else if (args.Length > 0)
            {
                targetPath = args[0];
            }
            
            Console.WriteLine($"🔍 Analysis Type: {analysisType}");
            Console.WriteLine($"📁 Target: {targetPath}");
            Console.WriteLine();
            
            if (Directory.Exists(targetPath))
            {
                await AnalyzeProject(targetPath, analysisType);
            }
            else if (File.Exists(targetPath))
            {
                await AnalyzeCodeFile(targetPath, analysisType);
            }
            else
            {
                Console.WriteLine($"❌ Path not found: {targetPath}");
                Console.WriteLine("Creating sample analysis...");
                await AnalyzeCodeFile("TestCode.cs", analysisType);
            }
        }
        
        static void ShowUsage()
        {
            Console.WriteLine("📖 USAGE:");
            Console.WriteLine("dotnet run [file-or-directory]");
            Console.WriteLine("dotnet run --type [analysis-type] [file-or-directory]");
            Console.WriteLine();
            Console.WriteLine("Analysis Types:");
            Console.WriteLine("  general      - General modernization");
            Console.WriteLine("  security     - Security audit");
            Console.WriteLine("  performance  - Performance optimization");
            Console.WriteLine("  migration    - Migration readiness");
        }
        
        static bool InitializeAzureOpenAI()
        {
            try 
            {
                string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
                string apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
                _deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? "gpt-35-turbo";
                
                if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
                {
                    Console.WriteLine("❌ Missing Azure OpenAI environment variables");
                    return false;
                }
                
                _openAIClient = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
                Console.WriteLine("✅ Azure OpenAI initialized successfully");
                Console.WriteLine($"🚀 Using deployment: {_deploymentName}\n");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to initialize Azure OpenAI: {ex.Message}");
                return false;
            }
        }
        
        static async Task AnalyzeCodeFile(string fileName, string analysisType)
        {
            if (!File.Exists(fileName))
            {
                Console.WriteLine($"❌ File {fileName} not found!");
                return;
            }
            
            string code = File.ReadAllText(fileName);
            Console.WriteLine($"📁 Analyzing: {fileName}");
            Console.WriteLine($"📊 File size: {code.Length} characters\n");
            
            // Roslyn Analysis
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var root = syntaxTree.GetRoot();
            var analysis = PerformBasicAnalysis(root);
            
            DisplayBasicResults(analysis);
            
            // AI Analysis
            Console.WriteLine($"\n🤖 AI-Powered {analysisType} Analysis:");
            Console.WriteLine(new string('=', 50));
            
            var aiAnalysis = await GetAIAnalysis(code, fileName, analysisType, analysis);
            Console.WriteLine(aiAnalysis);
            
            // Generate Report
            await GenerateReport(analysis, aiAnalysis, fileName, analysisType);
        }
        
        static async Task AnalyzeProject(string projectPath, string analysisType)
        {
            Console.WriteLine($"🔍 Project Analysis: {projectPath}");
            Console.WriteLine("===================================");
            
            var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("/bin/") && !f.Contains("/obj/") && 
                           !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
                .Take(5) // Limit to 5 files for Day 2
                .ToList();
            
            Console.WriteLine($"Found {csFiles.Count} C# files to analyze\n");
            
            var projectSummary = new StringBuilder();
            int totalClasses = 0, totalMethods = 0;
            
            foreach (var file in csFiles)
            {
                try
                {
                    Console.WriteLine($"📄 {Path.GetFileName(file)}:");
                    
                    var code = File.ReadAllText(file);
                    var tree = CSharpSyntaxTree.ParseText(code);
                    var root = tree.GetRoot();
                    var fileAnalysis = PerformBasicAnalysis(root);
                    
                    totalClasses += fileAnalysis.ClassCount;
                    totalMethods += fileAnalysis.MethodCount;
                    
                    Console.WriteLine($"   📊 Classes: {fileAnalysis.ClassCount}, Methods: {fileAnalysis.MethodCount}");
                    
                    if (fileAnalysis.ClassCount > 0)
                    {
                        var quickInsight = await GetQuickInsight(code, Path.GetFileName(file));
                        Console.WriteLine($"   🤖 {quickInsight}");
                        projectSummary.AppendLine($"- {Path.GetFileName(file)}: {quickInsight}");
                    }
                    
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ Error: {ex.Message}");
                }
            }
            
            // Project-level summary
            Console.WriteLine("🧠 Project Summary:");
            Console.WriteLine("===================");
            Console.WriteLine($"Total Classes: {totalClasses}");
            Console.WriteLine($"Total Methods: {totalMethods}");
            Console.WriteLine($"Files Analyzed: {csFiles.Count}");
            Console.WriteLine();
            Console.WriteLine("Key Insights:");
            Console.WriteLine(projectSummary.ToString());
        }
        
        static async Task<string> GetAIAnalysis(string code, string fileName, string analysisType, CodeAnalysisResult analysis)
        {
            try
            {
                var prompt = BuildAnalysisPrompt(code, fileName, analysisType, analysis);
                
                var chatCompletionsOptions = new ChatCompletionsOptions()
                {
                    DeploymentName = _deploymentName,
                    Messages =
                    {
                        new ChatRequestSystemMessage(GetSystemPrompt(analysisType)),
                        new ChatRequestUserMessage(prompt)
                    },
                    MaxTokens = 400,
                    Temperature = 0.3f
                };

                var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
                return response.Value.Choices[0].Message.Content;
            }
            catch (Exception ex)
            {
                return $"❌ AI Analysis failed: {ex.Message}\n💡 Check your Azure OpenAI configuration and quota.";
            }
        }
        
        static async Task<string> GetQuickInsight(string code, string fileName)
        {
            try
            {
                var codePreview = code.Length > 1000 ? code.Substring(0, 1000) + "..." : code;
                var prompt = $"Analyze this C# file '{fileName}' and provide ONE key modernization insight:\n\n{codePreview}\n\nRespond with one actionable sentence.";
                
                var chatCompletionsOptions = new ChatCompletionsOptions()
                {
                    DeploymentName = _deploymentName,
                    Messages =
                    {
                        new ChatRequestSystemMessage("You provide concise, actionable code modernization insights."),
                        new ChatRequestUserMessage(prompt)
                    },
                    MaxTokens = 60,
                    Temperature = 0.2f
                };

                var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
                return response.Value.Choices[0].Message.Content.Trim();
            }
            catch (Exception ex)
            {
                return $"Insight unavailable: {ex.Message}";
            }
        }
        
        static string GetSystemPrompt(string analysisType)
        {
            return analysisType switch
            {
                "security" => "You are a security expert reviewing C# code for vulnerabilities and security best practices.",
                "performance" => "You are a performance specialist identifying bottlenecks and optimization opportunities.",
                "migration" => "You are a migration expert assessing code readiness for modern .NET versions.",
                _ => "You are an expert C# architect providing modernization recommendations."
            };
        }
        
        static string BuildAnalysisPrompt(string code, string fileName, string analysisType, CodeAnalysisResult analysis)
        {
            var codePreview = code.Length > 1200 ? code.Substring(0, 1200) + "..." : code;
            
            var basePrompt = $@"Analyze this C# file for {analysisType}:

FILE: {fileName}
METRICS: {analysis.ClassCount} classes, {analysis.MethodCount} methods
KEY CLASSES: {string.Join(", ", analysis.Classes.Take(3))}

CODE:
{codePreview}

Provide:";
            
            return analysisType switch
            {
                "security" => basePrompt + @"
1. Security Risk Level (1-10): [score]
2. Top 3 Security Concerns: [specific issues]
3. Immediate Actions: [critical fixes needed]",
                
                "performance" => basePrompt + @"
1. Performance Score (1-10): [rating]
2. Top 3 Bottlenecks: [specific performance issues]
3. Optimization Opportunities: [concrete improvements]",
                
                "migration" => basePrompt + @"
1. Migration Readiness (1-10): [compatibility score]
2. Breaking Changes: [specific issues for modern .NET]
3. Migration Effort: [estimated time/complexity]",
                
                _ => basePrompt + @"
1. Code Quality Score (1-10): [score with reason]
2. Top 3 Modernization Priorities: [actionable improvements]
3. Implementation Effort: [realistic time estimate]
4. Business Risk: [Low/Medium/High with mitigation]"
            };
        }
        
        static CodeAnalysisResult PerformBasicAnalysis(SyntaxNode root)
        {
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
            var properties = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().ToList();
            var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToList();
            
            return new CodeAnalysisResult
            {
                ClassCount = classes.Count,
                MethodCount = methods.Count,
                PropertyCount = properties.Count,
                UsingCount = usingDirectives.Count,
                Classes = classes.Select(c => c.Identifier.ValueText).ToList(),
                Methods = methods.Select(m => $"{GetClassName(m)}.{m.Identifier.ValueText}").ToList(),
                UsingStatements = usingDirectives.Select(u => u.Name.ToString()).ToList()
            };
        }
        
        static string GetClassName(SyntaxNode method)
        {
            var classDeclaration = method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            return classDeclaration?.Identifier.ValueText ?? "Unknown";
        }
        
        static void DisplayBasicResults(CodeAnalysisResult result)
        {
            Console.WriteLine("📊 Roslyn Static Analysis:");
            Console.WriteLine("===========================");
            Console.WriteLine($"Classes: {result.ClassCount}");
            Console.WriteLine($"Methods: {result.MethodCount}");
            Console.WriteLine($"Properties: {result.PropertyCount}");
            Console.WriteLine($"Using Statements: {result.UsingCount}");
            
            if (result.Classes.Any())
            {
                Console.WriteLine($"\n📝 Key Classes:");
                foreach (var className in result.Classes.Take(3))
                {
                    Console.WriteLine($"   - {className}");
                }
                if (result.Classes.Count > 3)
                    Console.WriteLine($"   ... and {result.Classes.Count - 3} more");
            }
        }
        
        static async Task GenerateReport(CodeAnalysisResult analysis, string aiAnalysis, string fileName, string analysisType)
        {
            var report = new StringBuilder();
            
            report.AppendLine($"# {analysisType.ToUpper()} Analysis Report");
            report.AppendLine($"*File: {fileName} | Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}*");
            report.AppendLine();
            
            report.AppendLine("## Static Analysis Results");
            report.AppendLine($"- **Classes**: {analysis.ClassCount}");
            report.AppendLine($"- **Methods**: {analysis.MethodCount}");
            report.AppendLine($"- **Properties**: {analysis.PropertyCount}");
            report.AppendLine($"- **Dependencies**: {analysis.UsingCount} using statements");
            report.AppendLine();
            
            report.AppendLine("## AI Analysis");
            report.AppendLine(aiAnalysis);
            report.AppendLine();
            
            report.AppendLine("## Next Steps");
            report.AppendLine("1. Address high-priority items identified above");
            report.AppendLine("2. Plan implementation timeline");
            report.AppendLine("3. Set up monitoring for improvements");
            
            var reportFileName = $"{analysisType}-analysis-{Path.GetFileNameWithoutExtension(fileName)}-{DateTime.Now:yyyyMMdd-HHmmss}.md";
            await File.WriteAllTextAsync(reportFileName, report.ToString());
            Console.WriteLine($"\n📄 Report generated: {reportFileName}");
        }
    }
    
    public class CodeAnalysisResult
    {
        public int ClassCount { get; set; }
        public int MethodCount { get; set; }
        public int PropertyCount { get; set; }
        public int UsingCount { get; set; }
        public List<string> Classes { get; set; } = new();
        public List<string> Methods { get; set; } = new();
        public List<string> UsingStatements { get; set; } = new();
    }
}
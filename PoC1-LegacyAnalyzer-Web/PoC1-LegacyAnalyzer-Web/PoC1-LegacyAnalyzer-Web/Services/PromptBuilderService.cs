using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Options;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class PromptBuilderService : IPromptBuilderService
    {
        private readonly PromptConfiguration _promptConfig;
        private readonly FileAnalysisLimitsConfig _fileLimits;
        private readonly ILogger<PromptBuilderService> _logger;

        public PromptBuilderService(
            IOptions<PromptConfiguration> promptOptions,
            IOptions<FileAnalysisLimitsConfig> fileLimitOptions,
            ILogger<PromptBuilderService> logger)
        {
            _promptConfig = promptOptions.Value ?? new PromptConfiguration();
            _fileLimits = fileLimitOptions.Value ?? new FileAnalysisLimitsConfig();
            _logger = logger;

            if (_promptConfig.SystemPrompts == null || _promptConfig.SystemPrompts.Count == 0)
            {
                throw new InvalidOperationException("PromptConfiguration is not properly configured in appsettings.json");
            }
        }

        public string BuildAnalysisPrompt(string code, string analysisType, CodeAnalysisResult analysis)
        {
            // Use configuration-based code preview length
            int maxLength = _promptConfig.CodePreviewMaxLength > 0
                ? _promptConfig.CodePreviewMaxLength
                : _fileLimits.DefaultCodePreviewLength;
            var codePreview = code.Length > maxLength ? code.Substring(0, maxLength) + "..." : code;

            // Build base prompt from configuration template
            var basePrompt = _promptConfig.AnalysisPromptTemplates.BaseTemplate
                .Replace("{analysisType}", analysisType)
                .Replace("{classCount}", analysis.ClassCount.ToString())
                .Replace("{methodCount}", analysis.MethodCount.ToString())
                .Replace("{propertyCount}", analysis.PropertyCount.ToString())
                .Replace("{principalClasses}", string.Join(", ", analysis.Classes.Take(3)))
                .Replace("{codePreview}", codePreview);

            // Get analysis sections from configuration
            if (_promptConfig.AnalysisPromptTemplates.Templates.TryGetValue(analysisType, out var template))
            {
                var sections = string.Join("\n", template.Sections);
                return basePrompt + "\n" + sections;
            }
            else if (_promptConfig.AnalysisPromptTemplates.Templates.TryGetValue("general", out var generalTemplate))
            {
                var sections = string.Join("\n", generalTemplate.Sections);
                return basePrompt + "\n" + sections;
            }
            else
            {
                // Fallback if no template found
                return basePrompt;
            }
        }

        public string BuildBatchAnalysisPrompt(
            List<(string fileName, string code, CodeAnalysisResult staticAnalysis)> fileAnalyses,
            string analysisType)
        {
            // Optimize code preview length based on batch size to stay within token limits
            int baseMaxLength = _promptConfig.CodePreviewMaxLength > 0 
                ? _promptConfig.CodePreviewMaxLength 
                : _fileLimits.DefaultCodePreviewLength;
            // Reduce preview length for larger batches to fit more files
            int maxLength = fileAnalyses.Count > _fileLimits.BatchSizeThresholdForPreviewReduction 
                ? Math.Max(_fileLimits.MinCodePreviewLength, baseMaxLength / (fileAnalyses.Count / _fileLimits.BatchPreviewLengthDivisor)) 
                : baseMaxLength;
            
            var fileSections = new List<string>();
            for (int i = 0; i < fileAnalyses.Count; i++)
            {
                var (fileName, code, analysis) = fileAnalyses[i];
                var codePreview = code.Length > maxLength ? code.Substring(0, maxLength) + "..." : code;
                
                // Compact format to reduce tokens while maintaining information
                fileSections.Add($@"
FILE {i + 1}: {fileName}
Metrics: {analysis.ClassCount} classes, {analysis.MethodCount} methods, {analysis.PropertyCount} properties
Top Classes: {string.Join(", ", analysis.Classes.Take(_fileLimits.MaxTopClassesToDisplay))}
Code:
{codePreview}
");
            }

            var batchPrompt = $@"
Analyze {fileAnalyses.Count} source files for {analysisType} assessment. Provide comprehensive analysis for EACH file.

{string.Join("\n---\n", fileSections)}

Return ONLY valid JSON (no markdown, no explanations):
{{
  ""analyses"": [
    {{""fileName"": ""File1.cs"", ""assessment"": ""{_fileLimits.MinAnalysisWordCount}-{_fileLimits.MaxAnalysisWordCount} word analysis covering all {analysisType} aspects""}},
    {{""fileName"": ""File2.cs"", ""assessment"": ""{_fileLimits.MinAnalysisWordCount}-{_fileLimits.MaxAnalysisWordCount} word analysis covering all {analysisType} aspects""}}
  ]
}}

Requirements:
- Every file must have exactly one entry
- Assessments must be comprehensive ({_fileLimits.MinAnalysisWordCount}-{_fileLimits.MaxAnalysisWordCount} words)
- Focus on {analysisType}-specific insights
- Include actionable recommendations
";

            return batchPrompt;
        }

        public string GetSystemPrompt(string analysisType)
        {
            if (_promptConfig.SystemPrompts.TryGetValue(analysisType, out var prompt))
            {
                return prompt;
            }

            return _promptConfig.SystemPrompts.GetValueOrDefault(
                "general",
                "You are a senior software architect providing comprehensive code analysis."
            );
        }

        public string GetBatchSystemPrompt(string analysisType)
        {
            var basePrompt = GetSystemPrompt(analysisType);
            return basePrompt + 
                "\n\nCRITICAL: You MUST return ONLY valid JSON. No markdown, no explanations, just JSON.\n" +
                "Required JSON structure:\n" +
                "{\n" +
                "  \"analyses\": [\n" +
                "    {\"fileName\": \"File1.cs\", \"assessment\": \"Your detailed analysis here...\"},\n" +
                "    {\"fileName\": \"File2.cs\", \"assessment\": \"Your detailed analysis here...\"}\n" +
                "  ]\n" +
                "}\n" +
                "Every file in the input MUST have exactly one entry in the analyses array. " +
                "The assessment should be comprehensive (200-500 words) covering all aspects of the analysis type.";
        }
    }
}


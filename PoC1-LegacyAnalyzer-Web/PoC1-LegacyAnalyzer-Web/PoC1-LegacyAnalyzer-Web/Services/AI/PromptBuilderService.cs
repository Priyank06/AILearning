using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Options;

namespace PoC1_LegacyAnalyzer_Web.Services.AI
{
    public class PromptBuilderService : IPromptBuilderService
    {
        private readonly PromptConfiguration _promptConfig;
        private readonly FileAnalysisLimitsConfig _fileLimits;
        private readonly ILogger<PromptBuilderService> _logger;
        private readonly PromptTemplatesConfiguration _promptTemplates;

        public PromptBuilderService(
            IOptions<PromptConfiguration> promptOptions,
            IOptions<FileAnalysisLimitsConfig> fileLimitOptions,
            IOptions<PromptTemplatesConfiguration> promptTemplates,
            ILogger<PromptBuilderService> logger)
        {
            _promptConfig = promptOptions.Value ?? new PromptConfiguration();
            _fileLimits = fileLimitOptions.Value ?? new FileAnalysisLimitsConfig();
            _promptTemplates = promptTemplates?.Value ?? new PromptTemplatesConfiguration();
            _logger = logger;

            if (_promptConfig.SystemPrompts == null || _promptConfig.SystemPrompts.Count == 0)
            {
                throw new InvalidOperationException("PromptConfiguration is not properly configured in appsettings.json");
            }
        }

        public string BuildAnalysisPrompt(string code, string analysisType, CodeAnalysisResult analysis)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentNullException(nameof(code), "Code cannot be null or empty");
            }
            if (analysis == null)
            {
                throw new ArgumentNullException(nameof(analysis), "Analysis result cannot be null");
            }
            if (_promptConfig.AnalysisPromptTemplates?.BaseTemplate == null)
            {
                throw new InvalidOperationException("AnalysisPromptTemplates.BaseTemplate is not configured in appsettings.json");
            }

            // Use configuration-based code preview length
            int maxLength = _promptConfig.CodePreviewMaxLength > 0
                ? _promptConfig.CodePreviewMaxLength
                : _fileLimits.DefaultCodePreviewLength;
            var codePreview = code.Length > maxLength ? code.Substring(0, maxLength) + "..." : code;

            // Get principal classes safely
            var principalClasses = analysis.Classes != null && analysis.Classes.Any()
                ? string.Join(", ", analysis.Classes.Take(3))
                : "None detected";

            // Build base prompt from configuration template
            var basePrompt = _promptConfig.AnalysisPromptTemplates.BaseTemplate
                .Replace("{analysisType}", analysisType ?? "general")
                .Replace("{classCount}", analysis.ClassCount.ToString())
                .Replace("{methodCount}", analysis.MethodCount.ToString())
                .Replace("{propertyCount}", analysis.PropertyCount.ToString())
                .Replace("{principalClasses}", principalClasses)
                .Replace("{codePreview}", codePreview);

            // Get analysis sections from configuration
            if (_promptConfig.AnalysisPromptTemplates.Templates.TryGetValue(analysisType, out var template) && template?.Sections != null)
            {
                var sections = string.Join("\n", template.Sections);
                return basePrompt + "\n" + sections;
            }
            else if (_promptConfig.AnalysisPromptTemplates.Templates.TryGetValue("general", out var generalTemplate) && generalTemplate?.Sections != null)
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
            // Validate inputs
            if (fileAnalyses == null || !fileAnalyses.Any())
            {
                throw new ArgumentException("File analyses cannot be null or empty", nameof(fileAnalyses));
            }
            if (_promptTemplates?.BatchAnalysisPrompt == null)
            {
                throw new InvalidOperationException("PromptTemplates.BatchAnalysisPrompt is not configured in appsettings.json");
            }

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
                
                // Get top classes safely
                var topClasses = analysis?.Classes != null && analysis.Classes.Any()
                    ? string.Join(", ", analysis.Classes.Take(_fileLimits.MaxTopClassesToDisplay))
                    : "None detected";
                
                // Compact format to reduce tokens while maintaining information
                fileSections.Add($@"
FILE {i + 1}: {fileName ?? "Unknown"}
Metrics: {analysis?.ClassCount ?? 0} classes, {analysis?.MethodCount ?? 0} methods, {analysis?.PropertyCount ?? 0} properties
Top Classes: {topClasses}
Code:
{codePreview}
");
            }

            var batchPrompt = _promptTemplates.BatchAnalysisPrompt
                .Replace("{fileCount}", fileAnalyses.Count.ToString())
                .Replace("{analysisType}", analysisType ?? "general")
                .Replace("{fileSections}", string.Join("\n---\n", fileSections))
                .Replace("{minWords}", _fileLimits.MinAnalysisWordCount.ToString())
                .Replace("{maxWords}", _fileLimits.MaxAnalysisWordCount.ToString());

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
                _promptTemplates.DefaultSystemPrompt
            );
        }

        public string GetBatchSystemPrompt(string analysisType)
        {
            var basePrompt = GetSystemPrompt(analysisType);
            return basePrompt + _promptTemplates.BatchSystemPromptSuffix;
        }
    }
}


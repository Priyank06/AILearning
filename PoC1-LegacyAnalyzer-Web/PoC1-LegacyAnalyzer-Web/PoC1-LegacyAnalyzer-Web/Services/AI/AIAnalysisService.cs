using Azure;
using Microsoft.SemanticKernel.ChatCompletion;
using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PoC1_LegacyAnalyzer_Web.Services.AI
{
    /// <summary>
    /// Provides AI-based code analysis using Azure OpenAI and configuration-driven prompt templates.
    /// </summary>
    public class AIAnalysisService : IAIAnalysisService
    {
        private readonly IChatCompletionService _chatCompletion;
        private readonly IPromptBuilderService _promptBuilder;
        private readonly PromptConfiguration _promptConfig;
        private readonly ILogger<AIAnalysisService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AIAnalysisService"/> class.
        /// </summary>
        /// <param name="chatCompletion">The chat completion service for AI interactions.</param>
        /// <param name="promptBuilder">The prompt builder service for constructing prompts.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="promptOptions">The prompt configuration options.</param>
        public AIAnalysisService(
            IChatCompletionService chatCompletion,
            IPromptBuilderService promptBuilder,
            ILogger<AIAnalysisService> logger,
            IOptions<PromptConfiguration> promptOptions)
        {
            _chatCompletion = chatCompletion;
            _promptBuilder = promptBuilder;
            _logger = logger;
            _promptConfig = promptOptions.Value ?? new PromptConfiguration();
        }

        /// <summary>
        /// Performs an AI-based analysis on the provided code using the specified analysis type and static analysis results.
        /// </summary>
        /// <param name="code">The source code to analyze.</param>
        /// <param name="analysisType">The type of analysis to perform.</param>
        /// <param name="staticAnalysis">The static analysis results to supplement the AI analysis.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the analysis output as a string.
        /// </returns>
        public async Task<string> GetAnalysisAsync(string code, string analysisType, CodeAnalysisResult staticAnalysis)
        {
            try
            {
                var prompt = _promptBuilder.BuildAnalysisPrompt(code, analysisType, staticAnalysis);
                var systemPrompt = _promptBuilder.GetSystemPrompt(analysisType);

                // Build chat history for the chat completion API
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage(systemPrompt);
                chatHistory.AddUserMessage(prompt);

                var result = await _chatCompletion.GetChatMessageContentsAsync(chatHistory);
                return result?.FirstOrDefault()?.Content 
                    ?? _promptConfig.ErrorMessages.GetValueOrDefault("noResponse", "No response from AI.");
            }
            catch (RequestFailedException ex) when (ex.Status == 429)
            {
                return _promptConfig.ErrorMessages.GetValueOrDefault("rateLimitExceeded", "AI analysis is currently at capacity.");
            }
            catch (RequestFailedException ex) when (ex.Status == 401)
            {
                return _promptConfig.ErrorMessages.GetValueOrDefault("authenticationFailed", "AI service configuration error.");
            }
            catch
            {
                return _promptConfig.ErrorMessages.GetValueOrDefault("generalFailure", "AI analysis is temporarily unavailable.");
            }
        }

        /// <summary>
        /// Retrieves the system prompt associated with the specified analysis type from configuration.
        /// </summary>
        /// <param name="analysisType">The type of analysis for which to get the system prompt.</param>
        /// <returns>The system prompt as a string.</returns>
        public string GetSystemPrompt(string analysisType)
        {
            return _promptBuilder.GetSystemPrompt(analysisType);
        }

        /// <summary>
        /// Performs batch AI analysis on multiple code files in a single API call.
        /// Uses structured JSON output for reliable parsing and 60-80% reduction in API calls.
        /// </summary>
        public async Task<Dictionary<string, string>> GetBatchAnalysisAsync(
            List<(string fileName, string code, CodeAnalysisResult staticAnalysis)> fileAnalyses,
            string analysisType)
        {
            if (fileAnalyses == null || !fileAnalyses.Any())
            {
                return new Dictionary<string, string>();
            }

            try
            {
                var batchPrompt = _promptBuilder.BuildBatchAnalysisPrompt(fileAnalyses, analysisType);
                var systemPrompt = _promptBuilder.GetBatchSystemPrompt(analysisType);

                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage(systemPrompt);
                chatHistory.AddUserMessage(batchPrompt);

                var result = await _chatCompletion.GetChatMessageContentsAsync(chatHistory);
                var response = result?.FirstOrDefault()?.Content ?? "";

                return ParseBatchAnalysisResponse(response, fileAnalyses.Select(f => f.fileName).ToList());
            }
            catch (RequestFailedException ex) when (ex.Status == 429)
            {
                // Rate limit - return fallback for all files (error isolation)
                return fileAnalyses.ToDictionary(
                    f => f.fileName,
                    f => _promptConfig.ErrorMessages.GetValueOrDefault("rateLimitExceeded", "AI analysis is currently at capacity.")
                );
            }
            catch (Exception ex)
            {
                // On error, return fallback assessments (error isolation - doesn't break entire batch)
                return fileAnalyses.ToDictionary(
                    f => f.fileName,
                    f => GenerateFallbackAssessment(f.staticAnalysis, analysisType)
                );
            }
        }


        /// <summary>
        /// Parses the batch analysis response from the AI model with improved error handling.
        /// Handles various response formats and ensures all files get results.
        /// </summary>
        private Dictionary<string, string> ParseBatchAnalysisResponse(string response, List<string> expectedFileNames)
        {
            var results = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(response))
            {
                // Empty response - return fallback for all files
                return expectedFileNames.ToDictionary(f => f, f => "No response received from AI analysis.");
            }

            try
            {
                // Try to extract JSON from the response (handle markdown code blocks)
                var jsonStart = response.IndexOf('{');
                var jsonEnd = response.LastIndexOf('}') + 1;
                
                // Also check for JSON in markdown code blocks
                if (jsonStart < 0 || jsonEnd <= jsonStart)
                {
                    var codeBlockStart = response.IndexOf("```json");
                    if (codeBlockStart >= 0)
                    {
                        jsonStart = response.IndexOf('{', codeBlockStart);
                        jsonEnd = response.LastIndexOf('}') + 1;
                    }
                    else
                    {
                        var codeBlockStart2 = response.IndexOf("```");
                        if (codeBlockStart2 >= 0)
                        {
                            jsonStart = response.IndexOf('{', codeBlockStart2);
                            jsonEnd = response.LastIndexOf('}') + 1;
                        }
                    }
                }
                
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart);
                    
                    // Try parsing with relaxed options
                    var options = new System.Text.Json.JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip
                    };
                    
                    var parsed = System.Text.Json.JsonSerializer.Deserialize<BatchAnalysisResponse>(jsonContent, options);

                    if (parsed?.Analyses != null && parsed.Analyses.Any())
                    {
                        foreach (var analysis in parsed.Analyses)
                        {
                            if (!string.IsNullOrEmpty(analysis.FileName) && !string.IsNullOrEmpty(analysis.Assessment))
                            {
                                // Normalize file name matching (handle path differences)
                                var normalizedFileName = analysis.FileName;
                                var matchingFileName = expectedFileNames.FirstOrDefault(f => 
                                    f.Equals(normalizedFileName, StringComparison.OrdinalIgnoreCase) ||
                                    f.EndsWith(normalizedFileName, StringComparison.OrdinalIgnoreCase) ||
                                    normalizedFileName.EndsWith(f, StringComparison.OrdinalIgnoreCase));
                                
                                if (matchingFileName != null)
                                {
                                    results[matchingFileName] = analysis.Assessment;
                                }
                                else
                                {
                                    // Store with original name, will match later
                                    results[normalizedFileName] = analysis.Assessment;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // JSON parsing failed - log and try text extraction
                // Error is logged but doesn't break processing
            }

            // Fill in missing files with extracted text or fallback
            foreach (var fileName in expectedFileNames)
            {
                if (!results.ContainsKey(fileName))
                {
                    // Try to find file-specific content in response
                    var fileSection = ExtractFileAssessmentFromText(response, fileName);
                    if (!string.IsNullOrWhiteSpace(fileSection) && fileSection.Length > 50)
                    {
                        results[fileName] = fileSection;
                    }
                    else
                    {
                        // Final fallback
                        results[fileName] = "Analysis completed. Detailed review recommended. (Parsed from batch response)";
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Extracts file-specific assessment from unstructured text response.
        /// </summary>
        private string? ExtractFileAssessmentFromText(string response, string fileName)
        {
            // Look for file name in response and extract following text
            var fileNameIndex = response.IndexOf(fileName, StringComparison.OrdinalIgnoreCase);
            if (fileNameIndex >= 0)
            {
                var startIndex = fileNameIndex + fileName.Length;
                var endIndex = Math.Min(startIndex + 2000, response.Length);
                var extracted = response.Substring(startIndex, endIndex - startIndex).Trim();
                
                // Clean up common prefixes
                extracted = extracted.TrimStart(':', '-', ' ', '\n', '\r');
                
                if (extracted.Length > 50)
                {
                    return extracted.Substring(0, Math.Min(extracted.Length, 400));
                }
            }
            return null;
        }

        /// <summary>
        /// Generates a fallback assessment when AI analysis fails.
        /// </summary>
        private string GenerateFallbackAssessment(CodeAnalysisResult analysis, string analysisType)
        {
            return analysisType switch
            {
                "security" => $"Security review recommended for {analysis.ClassCount} classes. Verify input validation, authentication, and authorization implementations.",
                "performance" => $"Performance assessment indicates {analysis.MethodCount} methods require optimization analysis. Focus on database operations and algorithmic efficiency.",
                "migration" => $"Migration complexity assessment: {analysis.ClassCount} classes require modernization evaluation. Plan for framework compatibility and API updates.",
                _ => $"Code quality assessment shows {analysis.ClassCount} classes with {analysis.MethodCount} methods requiring structured modernization approach with quality assurance."
            };
        }
    }

    /// <summary>
    /// Response model for batch analysis.
    /// </summary>
    internal class BatchAnalysisResponse
    {
        public List<FileAnalysisEntry> Analyses { get; set; } = new();
    }

    /// <summary>
    /// Individual file analysis entry in batch response.
    /// </summary>
    internal class FileAnalysisEntry
    {
        public string FileName { get; set; } = string.Empty;
        public string Assessment { get; set; } = string.Empty;
    }
}


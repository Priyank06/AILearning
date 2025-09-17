using Azure;
using Azure.AI.Language.Conversations;
using PoC1_LegacyAnalyzer_Web.Models.AI102;
using System.Data;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class LanguageUnderstandingService : ILanguageUnderstandingService
    {
        private readonly ConversationAnalysisClient _client;
        private readonly string _projectName;
        private readonly string _deploymentName;
        private readonly ILogger<LanguageUnderstandingService> _logger;
        private readonly Dictionary<string, ConversationContext> _conversationContexts;

        public LanguageUnderstandingService(IConfiguration configuration, ILogger<LanguageUnderstandingService> logger)
        {
            var endpoint = configuration["CognitiveServices:Language:Endpoint"] ??
                          Environment.GetEnvironmentVariable("LANGUAGE_ENDPOINT");
            var apiKey = configuration["CognitiveServices:Language:ApiKey"] ??
                        Environment.GetEnvironmentVariable("LANGUAGE_KEY");

            _projectName = configuration["CognitiveServices:Language:ProjectName"] ?? "CodeAnalysisProject";
            _deploymentName = configuration["CognitiveServices:Language:DeploymentName"] ?? "production";

            if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
            {
                _client = new ConversationAnalysisClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
            }

            _logger = logger;
            _conversationContexts = new Dictionary<string, ConversationContext>();
        }

        public async Task<AnalysisIntent> ParseAnalysisRequestAsync(string userInput)
        {
            if (_client == null)
            {
                // Fallback to rule-based intent detection when service is not configured
                return ParseIntentWithRules(userInput);
            }

            try
            {
                var data = new
                {
                    analysisInput = new
                    {
                        conversationItem = new
                        {
                            id = Guid.NewGuid().ToString(),
                            participantId = "user",
                            text = userInput
                        }
                    },
                    parameters = new
                    {
                        projectName = _projectName,
                        deploymentName = _deploymentName
                    },
                    kind = "Conversation"
                };

                // Note: This is a simplified implementation. In production, you'd use the proper SDK methods
                var result = ParseIntentWithRules(userInput); // Fallback for now
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing analysis request: {UserInput}", userInput);
                return ParseIntentWithRules(userInput);
            }
        }

        public async Task<List<EntityExtraction>> ExtractEntitiesAsync(string text)
        {
            await Task.Delay(50); // Simulate processing time

            var entities = new List<EntityExtraction>();
            var lowerText = text.ToLower();

            // Extract file-related entities
            if (lowerText.Contains(".cs") || lowerText.Contains(".dll") || lowerText.Contains("file"))
            {
                entities.Add(new EntityExtraction
                {
                    EntityType = "FileType",
                    Value = ExtractFileType(text),
                    Confidence = 0.9,
                    Context = "File analysis context"
                });
            }

            // Extract analysis type entities
            var analysisTypes = new[] { "security", "performance", "architecture", "code quality", "complexity" };
            foreach (var analysisType in analysisTypes)
            {
                if (lowerText.Contains(analysisType))
                {
                    entities.Add(new EntityExtraction
                    {
                        EntityType = "AnalysisType",
                        Value = analysisType,
                        Confidence = 0.85,
                        Context = "Analysis scope"
                    });
                }
            }

            // Extract programming concepts
            var concepts = new[] { "class", "method", "interface", "namespace", "async", "database", "api" };
            foreach (var concept in concepts)
            {
                if (lowerText.Contains(concept))
                {
                    entities.Add(new EntityExtraction
                    {
                        EntityType = "ProgrammingConcept",
                        Value = concept,
                        Confidence = 0.8,
                        Context = "Technical terminology"
                    });
                }
            }

            return entities;
        }

        public async Task<ConversationContext> MaintainConversationContextAsync(string sessionId, string userInput)
        {
            await Task.Delay(25); // Simulate processing time

            if (!_conversationContexts.ContainsKey(sessionId))
            {
                _conversationContexts[sessionId] = new ConversationContext
                {
                    SessionId = sessionId,
                    History = new List<ConversationTurn>(),
                    State = new Dictionary<string, object>()
                };
            }

            var context = _conversationContexts[sessionId];
            var intent = await ParseAnalysisRequestAsync(userInput);

            var turn = new ConversationTurn
            {
                UserInput = userInput,
                Intent = intent,
                BotResponse = "", // Will be filled by the bot
                Timestamp = DateTime.UtcNow
            };

            context.History.Add(turn);
            context.LastActivity = DateTime.UtcNow;

            // Update conversation state based on intent
            UpdateConversationState(context, intent);

            // Clean up old history (keep last 10 turns)
            if (context.History.Count > 10)
            {
                context.History = context.History.TakeLast(10).ToList();
            }

            return context;
        }

        private AnalysisIntent ParseIntentWithRules(string userInput)
        {
            var lowerInput = userInput.ToLower();
            var intent = new AnalysisIntent
            {
                OriginalText = userInput,
                Entities = new List<EntityResult>()
            };

            // Intent classification rules
            if ((lowerInput.Contains("security") || lowerInput.Contains("vulnerability")) &&
                (lowerInput.Contains("analyze") || lowerInput.Contains("check") || lowerInput.Contains("scan")))
            {
                intent.IntentName = "SecurityAnalysis";
                intent.Confidence = 0.9;
            }
            else if ((lowerInput.Contains("performance") || lowerInput.Contains("optimization") || lowerInput.Contains("slow")) &&
                     (lowerInput.Contains("analyze") || lowerInput.Contains("check") || lowerInput.Contains("improve")))
            {
                intent.IntentName = "PerformanceAnalysis";
                intent.Confidence = 0.85;
            }
            else if ((lowerInput.Contains("architecture") || lowerInput.Contains("design") || lowerInput.Contains("pattern")) &&
                     (lowerInput.Contains("review") || lowerInput.Contains("analyze") || lowerInput.Contains("evaluate")))
            {
                intent.IntentName = "ArchitecturalReview";
                intent.Confidence = 0.8;
            }
            else if (lowerInput.Contains("report") || lowerInput.Contains("generate") || lowerInput.Contains("summary"))
            {
                intent.IntentName = "GenerateReport";
                intent.Confidence = 0.85;
            }
            else if (lowerInput.Contains("search") || lowerInput.Contains("find") || lowerInput.Contains("look for"))
            {
                intent.IntentName = "SearchCode";
                intent.Confidence = 0.8;
            }
            else if (lowerInput.Contains("help") || lowerInput.Contains("how") || lowerInput.Contains("what"))
            {
                intent.IntentName = "GeneralInquiry";
                intent.Confidence = 0.7;
            }
            else
            {
                intent.IntentName = "Unknown";
                intent.Confidence = 0.3;
            }

            // Extract entities
            intent.Entities = ExtractEntitiesFromInput(userInput);

            return intent;
        }

        private List<EntityResult> ExtractEntitiesFromInput(string input)
        {
            var entities = new List<EntityResult>();
            var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i].ToLower();

                // File extensions
                if (word.Contains(".cs") || word.Contains(".dll") || word.Contains(".exe"))
                {
                    entities.Add(new EntityResult
                    {
                        EntityName = "FileName",
                        Value = words[i],
                        Confidence = 0.9,
                        StartIndex = input.IndexOf(words[i]),
                        EndIndex = input.IndexOf(words[i]) + words[i].Length
                    });
                }

                // Numbers (possibly complexity scores, line numbers, etc.)
                if (int.TryParse(word, out _))
                {
                    entities.Add(new EntityResult
                    {
                        EntityName = "Number",
                        Value = word,
                        Confidence = 0.8,
                        StartIndex = input.IndexOf(word),
                        EndIndex = input.IndexOf(word) + word.Length
                    });
                }
            }

            return entities;
        }

        private string ExtractFileType(string text)
        {
            if (text.Contains(".cs")) return "C# Source File";
            if (text.Contains(".dll")) return "Dynamic Link Library";
            if (text.Contains(".exe")) return "Executable File";
            if (text.Contains(".json")) return "JSON Configuration";
            return "Unknown File Type";
        }

        private void UpdateConversationState(ConversationContext context, AnalysisIntent intent)
        {
            // Track the user's current focus
            context.State["LastIntentType"] = intent.IntentName;
            context.State["LastConfidence"] = intent.Confidence;

            // Track analysis preferences
            if (intent.IntentName == "SecurityAnalysis")
            {
                context.State["PreferredAnalysisType"] = "Security";
            }
            else if (intent.IntentName == "PerformanceAnalysis")
            {
                context.State["PreferredAnalysisType"] = "Performance";
            }

            // Count interaction types
            var interactionCount = context.State.GetValueOrDefault($"{intent.IntentName}Count", 0);
            context.State[$"{intent.IntentName}Count"] = (int)interactionCount + 1;
        }
    }
}
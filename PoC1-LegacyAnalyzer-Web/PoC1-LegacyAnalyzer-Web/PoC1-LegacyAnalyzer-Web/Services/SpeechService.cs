using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using PoC1_LegacyAnalyzer_Web.Models.AI102;
using System.Data;
using CommandType = PoC1_LegacyAnalyzer_Web.Models.AI102.CommandType;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class SpeechService : ISpeechService
    {
        private readonly SpeechConfig _speechConfig;
        private readonly ILogger<SpeechService> _logger;

        public SpeechService(IConfiguration configuration, ILogger<SpeechService> logger)
        {
            var subscriptionKey = configuration["CognitiveServices:Speech:SubscriptionKey"] ??
                                 Environment.GetEnvironmentVariable("SPEECH_KEY");
            var region = configuration["CognitiveServices:Speech:Region"] ??
                        Environment.GetEnvironmentVariable("SPEECH_REGION") ?? "eastus";

            if (!string.IsNullOrEmpty(subscriptionKey))
            {
                _speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);
                _speechConfig.SpeechRecognitionLanguage = "en-US";
            }

            _logger = logger;
        }

        public async Task<string> SpeechToTextAsync(Stream audioStream)
        {
            if (_speechConfig == null)
            {
                _logger.LogWarning("Speech service not configured");
                return "Speech service not configured";
            }

            try
            {
                using var audioConfig = AudioConfig.FromStreamInput(
                    AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1)));
                using var recognizer = new SpeechRecognizer(_speechConfig, audioConfig);

                var result = await recognizer.RecognizeOnceAsync();

                return result.Reason == ResultReason.RecognizedSpeech
                    ? result.Text
                    : $"Speech recognition failed: {result.Reason}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in speech to text conversion");
                return "Error in speech recognition";
            }
        }

        public async Task<Stream> TextToSpeechAsync(string text, VoiceType voiceType = VoiceType.Professional)
        {
            if (_speechConfig == null)
            {
                return new MemoryStream();
            }

            try
            {
                var voice = GetVoiceName(voiceType);
                _speechConfig.SpeechSynthesisVoiceName = voice;

                using var synthesizer = new SpeechSynthesizer(_speechConfig, null);
                var result = await synthesizer.SpeakTextAsync(text);

                if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                {
                    return new MemoryStream(result.AudioData);
                }
                else
                {
                    _logger.LogWarning("Speech synthesis failed: {Reason}", result.Reason);
                    return new MemoryStream();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in text to speech conversion");
                return new MemoryStream();
            }
        }

        public async Task<AnalysisCommand> ParseVoiceCommandAsync(string spokenText)
        {
            await Task.Delay(10);

            var command = new AnalysisCommand
            {
                OriginalText = spokenText,
                Confidence = 0.8
            };

            var lowerText = spokenText.ToLower();

            // Parse command type
            if (lowerText.Contains("security") && lowerText.Contains("analyz"))
            {
                command.Type = CommandType.SecurityAnalysis;
                command.Parameters["focus"] = "security";
            }
            else if (lowerText.Contains("performance") || lowerText.Contains("optimization"))
            {
                command.Type = CommandType.PerformanceAnalysis;
                command.Parameters["focus"] = "performance";
            }
            else if (lowerText.Contains("architecture") || lowerText.Contains("design"))
            {
                command.Type = CommandType.ArchitecturalReview;
                command.Parameters["focus"] = "architecture";
            }
            else if (lowerText.Contains("report") || lowerText.Contains("generate"))
            {
                command.Type = CommandType.GenerateReport;
                command.Parameters["type"] = "comprehensive";
            }
            else if (lowerText.Contains("search") || lowerText.Contains("find"))
            {
                command.Type = CommandType.SearchCode;
                command.Parameters["query"] = ExtractSearchQuery(lowerText);
            }
            else
            {
                command.Type = CommandType.Unknown;
                command.Confidence = 0.3;
            }

            // Extract additional parameters
            if (lowerText.Contains("file"))
            {
                command.Parameters["scope"] = "file";
            }
            else if (lowerText.Contains("project"))
            {
                command.Parameters["scope"] = "project";
            }

            return command;
        }

        private string GetVoiceName(VoiceType voiceType)
        {
            return voiceType switch
            {
                VoiceType.Professional => "en-US-JennyNeural",
                VoiceType.Friendly => "en-US-AriaNeural",
                VoiceType.Technical => "en-US-BrianNeural",
                VoiceType.Executive => "en-US-GuyNeural",
                _ => "en-US-JennyNeural"
            };
        }

        private string ExtractSearchQuery(string spokenText)
        {
            var patterns = new[] { "search for ", "find ", "look for " };

            foreach (var pattern in patterns)
            {
                var index = spokenText.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    var query = spokenText.Substring(index + pattern.Length).Trim();
                    var stopWords = new[] { " in the code", " in code", " please", " now" };
                    foreach (var stopWord in stopWords)
                    {
                        if (query.EndsWith(stopWord, StringComparison.OrdinalIgnoreCase))
                        {
                            query = query.Substring(0, query.Length - stopWord.Length);
                        }
                    }
                    return query;
                }
            }

            return spokenText;
        }
    }
}
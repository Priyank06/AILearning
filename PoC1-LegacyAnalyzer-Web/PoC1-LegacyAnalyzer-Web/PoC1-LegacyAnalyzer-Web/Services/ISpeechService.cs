using PoC1_LegacyAnalyzer_Web.Models.AI102;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface ISpeechService
    {
        Task<string> SpeechToTextAsync(Stream audioStream);
        Task<Stream> TextToSpeechAsync(string text, VoiceType voiceType = VoiceType.Professional);
        Task<AnalysisCommand> ParseVoiceCommandAsync(string spokenText);
    }
}
using PoC1_LegacyAnalyzer_Web.Models.AI102;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface ISpeechAnalysisService
    {
        Task<VoiceCommandResult> ProcessVoiceCommandAsync(byte[] audioData);
        Task<byte[]> GenerateAudioSummaryAsync(string analysisReport);
        Task<string> ConvertSpeechToTextAsync(Stream audioStream);
    }
}
using System;
using System.IO;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.Analysis
{
    public interface ILanguageDetector
    {
        LanguageKind DetectLanguage(string fileName, string? content = null);
    }

    /// <summary>
    /// Simple language detector based primarily on file extension, with optional heuristics.
    /// </summary>
    public class LanguageDetector : ILanguageDetector
    {
        public LanguageKind DetectLanguage(string fileName, string? content = null)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return LanguageKind.Unknown;
            }

            var ext = Path.GetExtension(fileName).ToLowerInvariant();

            return ext switch
            {
                ".cs" => LanguageKind.CSharp,
                ".py" => LanguageKind.Python,
                ".js" => LanguageKind.JavaScript,
                ".ts" => LanguageKind.TypeScript,
                ".java" => LanguageKind.Java,
                ".go" => LanguageKind.Go,
                _ => LanguageKind.Unknown
            };
        }
    }
}



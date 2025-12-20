using System;
using System.Collections.Concurrent;
using PoC1_LegacyAnalyzer_Web.Models;
using TreeSitter;

namespace PoC1_LegacyAnalyzer_Web.Services.Analysis
{
    /// <summary>
    /// Central registry for Tree-sitter languages and parsers.
    /// Responsible for creating and caching parser instances per language.
    /// </summary>
    public interface ITreeSitterLanguageRegistry
    {
        Parser GetParser(LanguageKind languageKind);
        Language GetLanguage(LanguageKind languageKind);
    }

    public class TreeSitterLanguageRegistry : ITreeSitterLanguageRegistry
    {
        private readonly ConcurrentDictionary<LanguageKind, Parser> _parsers = new();

        public Parser GetParser(LanguageKind languageKind)
        {
            if (languageKind == LanguageKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(languageKind), "LanguageKind.Unknown cannot be parsed with Tree-sitter.");
            }

            return _parsers.GetOrAdd(languageKind, kind =>
            {
                var parser = new Parser();
                parser.Language = GetLanguage(kind);
                return parser;
            });
        }

        public Language GetLanguage(LanguageKind languageKind)
        {
            return languageKind switch
            {
                LanguageKind.Python => new Language("Python"),
                LanguageKind.JavaScript => new Language("JavaScript"),
                LanguageKind.TypeScript => new Language("TypeScript"),
                LanguageKind.Java => new Language("Java"),
                LanguageKind.Go => new Language("Go"),
                _ => throw new NotSupportedException($"Tree-sitter language is not configured for {languageKind}.")
            };
        }
    }
}



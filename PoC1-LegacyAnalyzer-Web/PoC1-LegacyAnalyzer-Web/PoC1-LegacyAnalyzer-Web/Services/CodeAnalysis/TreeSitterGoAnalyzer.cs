using TreeSitter;
using PoC1_LegacyAnalyzer_Web.Models;
using PoC1_LegacyAnalyzer_Web.Services.Analysis;

namespace PoC1_LegacyAnalyzer_Web.Services.CodeAnalysis
{
    /// <summary>
    /// Tree-sitter analyzer for Go language.
    /// </summary>
    public class TreeSitterGoAnalyzer : TreeSitterMultiLanguageAnalyzer, ILanguageSpecificAnalyzer
    {
        public TreeSitterGoAnalyzer(ITreeSitterLanguageRegistry languageRegistry)
            : base(languageRegistry)
        {
        }
        
        public LanguageKind SupportedLanguage => LanguageKind.Go;
        
        public async Task<CodeStructure> AnalyzeAsync(string code, string fileName)
        {
            var language = _languageRegistry.GetLanguage(LanguageKind.Go);
            if (language == null)
            {
                return new CodeStructure { LanguageKind = LanguageKind.Go, FileName = fileName };
            }
            
            using var parser = new Parser();
            parser.Language = language;
            var tree = parser.Parse(code);
            var rootNode = tree.RootNode;
            
            return ParseTreeSitterOutput(code, rootNode, LanguageKind.Go);
        }
    }
}

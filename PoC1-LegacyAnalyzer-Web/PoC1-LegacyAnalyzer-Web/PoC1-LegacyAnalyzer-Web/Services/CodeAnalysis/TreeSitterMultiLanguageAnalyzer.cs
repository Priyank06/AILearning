using TreeSitter;
using PoC1_LegacyAnalyzer_Web.Models;
using PoC1_LegacyAnalyzer_Web.Services.Analysis;

namespace PoC1_LegacyAnalyzer_Web.Services.CodeAnalysis
{
    /// <summary>
    /// Base class for Tree-sitter based multi-language analyzers.
    /// </summary>
    public abstract class TreeSitterMultiLanguageAnalyzer
    {
        protected readonly ITreeSitterLanguageRegistry _languageRegistry;
        
        protected TreeSitterMultiLanguageAnalyzer(ITreeSitterLanguageRegistry languageRegistry)
        {
            _languageRegistry = languageRegistry;
        }
        
        protected virtual CodeStructure ParseTreeSitterOutput(string code, Node root, LanguageKind language)
        {
            // Default implementation - can be overridden by derived classes
            return new CodeStructure { LanguageKind = language };
        }
    }
}


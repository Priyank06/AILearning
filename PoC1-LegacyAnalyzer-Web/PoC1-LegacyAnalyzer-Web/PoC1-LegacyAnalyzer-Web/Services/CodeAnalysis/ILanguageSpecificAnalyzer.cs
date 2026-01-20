using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.CodeAnalysis
{
    /// <summary>
    /// Base interface for language-specific code analyzers.
    /// </summary>
    public interface ILanguageSpecificAnalyzer
    {
        /// <summary>
        /// Gets the language that this analyzer supports.
        /// </summary>
        LanguageKind SupportedLanguage { get; }
        
        /// <summary>
        /// Analyzes code and returns the code structure.
        /// </summary>
        Task<CodeStructure> AnalyzeAsync(string code, string fileName);
    }
}


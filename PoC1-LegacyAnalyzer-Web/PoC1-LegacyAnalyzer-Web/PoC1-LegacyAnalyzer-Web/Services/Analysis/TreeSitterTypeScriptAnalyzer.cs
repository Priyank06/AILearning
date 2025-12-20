using System;
using PoC1_LegacyAnalyzer_Web.Models;
using System.Threading;
using System.Threading.Tasks;

namespace PoC1_LegacyAnalyzer_Web.Services.Analysis
{
    /// <summary>
    /// TypeScript-specific analyzer wrapper around TreeSitterMultiLanguageAnalyzer.
    /// </summary>
    public class TreeSitterTypeScriptAnalyzer : ICodeAnalyzer, ILanguageSpecificAnalyzer
    {
        private readonly TreeSitterMultiLanguageAnalyzer _baseAnalyzer;

        public LanguageKind Language => LanguageKind.TypeScript;

        public TreeSitterTypeScriptAnalyzer(ITreeSitterLanguageRegistry registry)
        {
            _baseAnalyzer = new TreeSitterMultiLanguageAnalyzer(registry);
        }

        public Task<(CodeStructure structure, CodeAnalysisResult summary)> AnalyzeAsync(
            AnalyzableFile file,
            CancellationToken cancellationToken = default)
        {
            if (file.Language != LanguageKind.TypeScript)
            {
                throw new ArgumentException($"TreeSitterTypeScriptAnalyzer can only analyze TypeScript code, but received {file.Language}.", nameof(file));
            }

            return _baseAnalyzer.AnalyzeAsync(file, cancellationToken);
        }
    }
}


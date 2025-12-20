using System;
using PoC1_LegacyAnalyzer_Web.Models;
using System.Threading;
using System.Threading.Tasks;

namespace PoC1_LegacyAnalyzer_Web.Services.Analysis
{
    /// <summary>
    /// JavaScript-specific analyzer wrapper around TreeSitterMultiLanguageAnalyzer.
    /// </summary>
    public class TreeSitterJavaScriptAnalyzer : ICodeAnalyzer, ILanguageSpecificAnalyzer
    {
        private readonly TreeSitterMultiLanguageAnalyzer _baseAnalyzer;

        public LanguageKind Language => LanguageKind.JavaScript;

        public TreeSitterJavaScriptAnalyzer(ITreeSitterLanguageRegistry registry)
        {
            _baseAnalyzer = new TreeSitterMultiLanguageAnalyzer(registry);
        }

        public Task<(CodeStructure structure, CodeAnalysisResult summary)> AnalyzeAsync(
            AnalyzableFile file,
            CancellationToken cancellationToken = default)
        {
            if (file.Language != LanguageKind.JavaScript)
            {
                throw new ArgumentException($"TreeSitterJavaScriptAnalyzer can only analyze JavaScript code, but received {file.Language}.", nameof(file));
            }

            return _baseAnalyzer.AnalyzeAsync(file, cancellationToken);
        }
    }
}


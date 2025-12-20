using System;
using PoC1_LegacyAnalyzer_Web.Models;
using System.Threading;
using System.Threading.Tasks;

namespace PoC1_LegacyAnalyzer_Web.Services.Analysis
{
    /// <summary>
    /// Go-specific analyzer wrapper around TreeSitterMultiLanguageAnalyzer.
    /// </summary>
    public class TreeSitterGoAnalyzer : ICodeAnalyzer, ILanguageSpecificAnalyzer
    {
        private readonly TreeSitterMultiLanguageAnalyzer _baseAnalyzer;

        public LanguageKind Language => LanguageKind.Go;

        public TreeSitterGoAnalyzer(ITreeSitterLanguageRegistry registry)
        {
            _baseAnalyzer = new TreeSitterMultiLanguageAnalyzer(registry);
        }

        public Task<(CodeStructure structure, CodeAnalysisResult summary)> AnalyzeAsync(
            AnalyzableFile file,
            CancellationToken cancellationToken = default)
        {
            if (file.Language != LanguageKind.Go)
            {
                throw new ArgumentException($"TreeSitterGoAnalyzer can only analyze Go code, but received {file.Language}.", nameof(file));
            }

            return _baseAnalyzer.AnalyzeAsync(file, cancellationToken);
        }
    }
}


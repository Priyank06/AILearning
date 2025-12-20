using System;
using PoC1_LegacyAnalyzer_Web.Models;
using System.Threading;
using System.Threading.Tasks;

namespace PoC1_LegacyAnalyzer_Web.Services.Analysis
{
    /// <summary>
    /// Java-specific analyzer wrapper around TreeSitterMultiLanguageAnalyzer.
    /// </summary>
    public class TreeSitterJavaAnalyzer : ICodeAnalyzer, ILanguageSpecificAnalyzer
    {
        private readonly TreeSitterMultiLanguageAnalyzer _baseAnalyzer;

        public LanguageKind Language => LanguageKind.Java;

        public TreeSitterJavaAnalyzer(ITreeSitterLanguageRegistry registry)
        {
            _baseAnalyzer = new TreeSitterMultiLanguageAnalyzer(registry);
        }

        public Task<(CodeStructure structure, CodeAnalysisResult summary)> AnalyzeAsync(
            AnalyzableFile file,
            CancellationToken cancellationToken = default)
        {
            if (file.Language != LanguageKind.Java)
            {
                throw new ArgumentException($"TreeSitterJavaAnalyzer can only analyze Java code, but received {file.Language}.", nameof(file));
            }

            return _baseAnalyzer.AnalyzeAsync(file, cancellationToken);
        }
    }
}


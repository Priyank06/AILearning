using System;
using PoC1_LegacyAnalyzer_Web.Models;
using System.Threading;
using System.Threading.Tasks;

namespace PoC1_LegacyAnalyzer_Web.Services.Analysis
{
    /// <summary>
    /// Python-specific analyzer wrapper around TreeSitterMultiLanguageAnalyzer.
    /// </summary>
    public class TreeSitterPythonAnalyzer : ICodeAnalyzer, ILanguageSpecificAnalyzer
    {
        private readonly TreeSitterMultiLanguageAnalyzer _baseAnalyzer;

        public LanguageKind Language => LanguageKind.Python;

        public TreeSitterPythonAnalyzer(ITreeSitterLanguageRegistry registry)
        {
            _baseAnalyzer = new TreeSitterMultiLanguageAnalyzer(registry);
        }

        public Task<(CodeStructure structure, CodeAnalysisResult summary)> AnalyzeAsync(
            AnalyzableFile file,
            CancellationToken cancellationToken = default)
        {
            if (file.Language != LanguageKind.Python)
            {
                throw new ArgumentException($"TreeSitterPythonAnalyzer can only analyze Python code, but received {file.Language}.", nameof(file));
            }

            return _baseAnalyzer.AnalyzeAsync(file, cancellationToken);
        }
    }
}


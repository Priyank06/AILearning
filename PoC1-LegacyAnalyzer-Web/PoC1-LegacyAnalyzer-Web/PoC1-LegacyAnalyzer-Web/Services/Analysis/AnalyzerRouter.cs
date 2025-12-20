using PoC1_LegacyAnalyzer_Web.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.Analysis
{
    /// <summary>
    /// Routes analysis requests to the appropriate language-specific analyzer.
    /// </summary>
    public interface IAnalyzerRouter
    {
        Task<(CodeStructure structure, CodeAnalysisResult summary)> AnalyzeAsync(
            AnalyzableFile file,
            CancellationToken cancellationToken = default);
    }

    public class AnalyzerRouter : IAnalyzerRouter
    {
        private readonly IDictionary<LanguageKind, ICodeAnalyzer> _analyzers;

        public AnalyzerRouter(IEnumerable<ICodeAnalyzer> analyzers)
        {
            _analyzers = new Dictionary<LanguageKind, ICodeAnalyzer>();

            foreach (var analyzer in analyzers)
            {
                if (analyzer is ILanguageSpecificAnalyzer typed)
                {
                    if (typed.Language != LanguageKind.Unknown)
                    {
                        _analyzers[typed.Language] = analyzer;
                    }
                }
            }
        }

        public Task<(CodeStructure structure, CodeAnalysisResult summary)> AnalyzeAsync(
            AnalyzableFile file,
            CancellationToken cancellationToken = default)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            if (!_analyzers.TryGetValue(file.Language, out var analyzer))
            {
                throw new NotSupportedException($"No analyzer registered for language '{file.Language}'.");
            }

            return analyzer.AnalyzeAsync(file, cancellationToken);
        }
    }

    /// <summary>
    /// Small helper interface so each analyzer can declare the LanguageKind it supports.
    /// </summary>
    public interface ILanguageSpecificAnalyzer
    {
        LanguageKind Language { get; }
    }
}



using PoC1_LegacyAnalyzer_Web.Models.GroundTruth;

namespace PoC1_LegacyAnalyzer_Web.SampleData
{
    /// <summary>
    /// Provides sample benchmark datasets for ground truth validation.
    /// </summary>
    public static class LegacyCodeBenchmark
    {
        public static GroundTruthDataset CreateSampleDataset()
        {
            return new GroundTruthDataset
            {
                Name = "Sample Legacy C# Benchmark",
                Description = "Sample dataset for ground truth validation",
                Version = "1.0",
                Files = new List<BenchmarkFile>(),
                Issues = new List<GroundTruthIssue>(),
                Tags = new List<string> { "sample", "csharp" }
            };
        }
    }
}

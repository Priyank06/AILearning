using System.Collections.Generic;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;

namespace PoC1_LegacyAnalyzer_Web.Models
{
    public class LLMStructuredResponse
    {
        public List<Finding> Findings { get; set; } = new();
        public List<Recommendation> Recommendations { get; set; } = new();
        public string BusinessImpact { get; set; } = string.Empty;
        public int ConfidenceScore { get; set; }
    }
}
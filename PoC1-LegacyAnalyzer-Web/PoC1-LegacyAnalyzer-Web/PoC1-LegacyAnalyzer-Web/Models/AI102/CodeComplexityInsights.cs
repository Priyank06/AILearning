namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{   
public class CodeComplexityInsights
{
    public int EstimatedLinesOfCode { get; set; }
    public string ComplexityLevel { get; set; } = "";
    public List<string> IdentifiedPatterns { get; set; } = new();
    public List<string> PotentialIssues { get; set; } = new();
    public double AnalysisConfidence { get; set; }
}
    }
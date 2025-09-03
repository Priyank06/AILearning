namespace PoC1_LegacyAnalyzer_Web.Models
{
    public class CodeAnalysisResult
    {
        public int ClassCount { get; set; }
        public int MethodCount { get; set; }
        public int PropertyCount { get; set; }
        public int UsingCount { get; set; }
        public List<string> Classes { get; set; } = new List<string>();
        public List<string> Methods { get; set; } = new List<string>();
        public List<string> UsingStatements { get; set; } = new List<string>();
    }
}

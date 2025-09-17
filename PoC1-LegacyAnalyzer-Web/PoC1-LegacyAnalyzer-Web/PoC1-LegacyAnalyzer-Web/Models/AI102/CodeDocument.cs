namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class CodeDocument
    {
        public string Id { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
        public string SourceCode { get; set; } = string.Empty;
        public int Complexity { get; set; }
        public string SecurityRisk { get; set; } = string.Empty;
        public string PerformanceRisk { get; set; } = string.Empty;
        public List<string> Keywords { get; set; } = new();
        public DateTime LastModified { get; set; }
    }
}
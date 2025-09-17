namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class EntityResult
    {
        public string EntityName { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
    }
}
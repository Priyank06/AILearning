namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class TableData
    {
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
        public List<List<string>> Cells { get; set; } = new();
        public double Confidence { get; set; }
    }
}
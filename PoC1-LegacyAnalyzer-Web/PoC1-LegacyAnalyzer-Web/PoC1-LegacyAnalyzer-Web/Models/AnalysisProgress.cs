namespace PoC1_LegacyAnalyzer_Web.Models
{
    public class AnalysisProgress
    {
        public string CurrentFile { get; set; } = "";
        public int CompletedFiles { get; set; }
        public int TotalFiles { get; set; }
        public string Status { get; set; } = "";
        public double ProgressPercentage => TotalFiles > 0 ? (double)CompletedFiles / TotalFiles * 100 : 0;
        public string CurrentAnalysisType { get; set; } = "";
        public DateTime StartTime { get; set; } = DateTime.Now;
        public TimeSpan ElapsedTime => DateTime.Now - StartTime;
    }
}

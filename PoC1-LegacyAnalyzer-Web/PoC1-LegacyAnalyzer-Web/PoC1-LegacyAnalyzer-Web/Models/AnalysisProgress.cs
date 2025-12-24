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
        
        // Batch processing information
        public int CurrentBatch { get; set; }
        public int TotalBatches { get; set; }
        public int FilesInCurrentBatch { get; set; }
        public TimeSpan? EstimatedTimeRemaining { get; set; }
        public string BatchStatus { get; set; } = ""; // e.g., "Analyzing batch 2 of 5..."
        
        // Per-agent progress tracking (for parallel agent processing)
        public Dictionary<string, AgentProgress> AgentProgress { get; set; } = new();
    }
    
    /// <summary>
    /// Progress tracking for individual agents running in parallel.
    /// </summary>
    public class AgentProgress
    {
        public string AgentName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public int ProgressPercentage { get; set; }
        public string Status { get; set; } = "Initializing";
        public DateTime StartTime { get; set; } = DateTime.Now;
        public TimeSpan ElapsedTime => DateTime.Now - StartTime;
        public bool IsComplete { get; set; }
        public bool HasError { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

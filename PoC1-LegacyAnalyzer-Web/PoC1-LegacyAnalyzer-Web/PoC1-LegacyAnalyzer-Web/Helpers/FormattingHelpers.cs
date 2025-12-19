namespace PoC1_LegacyAnalyzer_Web.Helpers
{
    /// <summary>
    /// Helper methods for formatting data in the UI.
    /// </summary>
    public static class FormattingHelpers
    {
        /// <summary>
        /// Gets a human-readable description for a business objective.
        /// </summary>
        public static string GetObjectiveDescription(string objective)
        {
            return objective switch
            {
                "security-compliance" => "Comprehensive security assessment with focus on vulnerability identification and compliance requirements (SOX, GDPR, PCI-DSS)",
                "performance-optimization" => "Performance bottleneck analysis and scalability assessment for enterprise-grade optimization recommendations",
                "architecture-modernization" => "Architectural analysis and modernization planning with focus on SOLID principles, design patterns, and maintainability improvements",
                "comprehensive-audit" => "Complete code audit covering security, performance, and architectural aspects for holistic modernization planning",
                _ => "Custom business-focused code analysis and improvement recommendations"
            };
        }

        /// <summary>
        /// Gets CSS class for complexity level visualization.
        /// </summary>
        public static string GetComplexityClass(int complexityScore)
        {
            return complexityScore switch
            {
                <= 3 => "text-success", // Low complexity
                <= 6 => "text-warning", // Medium complexity
                _ => "text-danger" // High complexity
            };
        }

        /// <summary>
        /// Formats timestamp for display.
        /// </summary>
        public static string FormatTimestamp(DateTime timestamp)
        {
            return timestamp.ToString("HH:mm:ss");
        }

        /// <summary>
        /// Formats executive summary with HTML markup.
        /// </summary>
        public static string FormatExecutiveSummary(string summary)
        {
            if (string.IsNullOrWhiteSpace(summary)) return "";
            
            summary = summary.Replace("\n\n", "</p><p>");
            summary = summary.Replace("\n", "<br>");
            return $"<p>{summary}</p>";
        }

        /// <summary>
        /// Gets CSS class for priority badges.
        /// </summary>
        public static string GetPriorityBadgeClass(string priority)
        {
            if (string.IsNullOrWhiteSpace(priority))
                return "bg-secondary";

            return priority.ToUpper() switch
            {
                "CRITICAL" => "bg-danger",
                "HIGH" => "bg-danger",
                "MEDIUM" => "bg-warning text-dark",
                "LOW" => "bg-info",
                _ => "bg-secondary"
            };
        }

        /// <summary>
        /// Gets icon for priority levels.
        /// </summary>
        public static string GetPriorityIcon(string priority)
        {
            if (string.IsNullOrWhiteSpace(priority))
                return "";

            return priority.ToUpper() switch
            {
                "CRITICAL" => "<i class=\"bi bi-exclamation-triangle-fill\"></i>",
                "HIGH" => "<i class=\"bi bi-exclamation-circle-fill\"></i>",
                "MEDIUM" => "<i class=\"bi bi-info-circle-fill\"></i>",
                "LOW" => "<i class=\"bi bi-info-circle\"></i>",
                _ => ""
            };
        }

        /// <summary>
        /// Gets CSS class for confidence level badges.
        /// </summary>
        public static string GetConfidenceBadgeClass(int confidence)
        {
            return confidence switch
            {
                >= 90 => "bg-success",
                >= 75 => "bg-primary",
                >= 60 => "bg-warning text-dark",
                _ => "bg-danger"
            };
        }

        /// <summary>
        /// Gets CSS class for severity badges.
        /// </summary>
        public static string GetSeverityBadgeClass(string severity)
        {
            return severity?.ToUpper() switch
            {
                "CRITICAL" => "bg-danger",
                "HIGH" => "bg-warning text-dark",
                "MEDIUM" => "bg-info",
                "LOW" => "bg-secondary",
                _ => "bg-light text-dark"
            };
        }

        /// <summary>
        /// Formats file size in human-readable format.
        /// </summary>
        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Truncates message to specified length.
        /// </summary>
        public static string TruncateMessage(string message, int maxLength)
        {
            if (string.IsNullOrEmpty(message) || message.Length <= maxLength)
                return message;

            return message.Substring(0, maxLength) + "...";
        }

        /// <summary>
        /// Gets estimated timeline based on effort hours.
        /// </summary>
        public static string GetEstimatedTimeline(decimal hours)
        {
            return hours switch
            {
                <= 40 => "1 week",
                <= 80 => "2 weeks",
                <= 160 => "1 month",
                <= 320 => "2 months",
                _ => "3+ months"
            };
        }
    }
}
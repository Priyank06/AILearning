using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Helpers
{
    /// <summary>
    /// Helper methods for MultiFile analysis UI rendering
    /// Extracted from MultiFile.razor to improve maintainability
    /// </summary>
    public static class MultiFileHelpers
    {
        #region Badge and Color Classes

        public static string GetRiskBadgeClass(string riskLevel)
        {
            return riskLevel?.ToUpper() switch
            {
                "CRITICAL" => "bg-danger",
                "HIGH" => "bg-warning text-dark",
                "MEDIUM" => "bg-info text-white",
                "LOW" => "bg-success",
                _ => "bg-secondary"
            };
        }

        public static string GetRiskBadgeClass(MultiFileAnalysisResult? result)
        {
            return result?.OverallRiskLevel switch
            {
                "LOW" => "success",
                "MEDIUM" => "warning",
                "HIGH" => "danger",
                _ => "secondary"
            };
        }

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

        public static string GetComplexityBadgeClass(int score, int lowThreshold = 30, int mediumThreshold = 50)
        {
            if (score < lowThreshold) return "bg-success";
            if (score < mediumThreshold) return "bg-warning text-dark";
            return "bg-danger";
        }

        public static string GetQualityColorClass(int complexity, int lowThreshold = 30, int mediumThreshold = 50)
        {
            if (complexity > mediumThreshold) return "bg-danger";
            if (complexity > lowThreshold) return "bg-warning";
            return "bg-success";
        }

        public static string GetComplexityColorClass(int score, int lowThreshold = 30, int mediumThreshold = 50)
        {
            if (score < lowThreshold) return "bg-success";
            if (score < mediumThreshold) return "bg-warning";
            return "bg-danger";
        }

        public static string GetComplexityTextColor(int score, int mediumThreshold = 50, int highThreshold = 70)
        {
            if (score < mediumThreshold) return "text-success";
            if (score < highThreshold) return "text-warning";
            return "text-danger";
        }

        public static string GetSemanticQualityColor(int score)
        {
            return score >= 80 ? "bg-success" :
                   score >= 60 ? "bg-warning" :
                   score >= 40 ? "bg-info" :
                   "bg-danger";
        }

        public static string GetSemanticIssueTypeBadge(string issueType)
        {
            return issueType.ToUpper() switch
            {
                var t when t.Contains("TYPE") => "bg-danger",
                var t when t.Contains("UNINITIALIZED") => "bg-warning text-dark",
                var t when t.Contains("CONTROL") => "bg-info",
                var t when t.Contains("DEPRECATED") => "bg-danger",
                var t when t.Contains("RUNTIME") => "bg-danger",
                _ => "bg-secondary"
            };
        }

        public static string GetCategoryBadge(string category)
        {
            return category?.ToUpper() switch
            {
                "ERROR" => "bg-danger",
                "WARNING" => "bg-warning text-dark",
                "INFO" => "bg-info",
                _ => "bg-secondary"
            };
        }

        public static string GetRiskTextColorClass(string risk)
        {
            return risk switch
            {
                "LOW" => "text-success",
                "MEDIUM" => "text-warning",
                "HIGH" => "text-danger",
                _ => "text-secondary"
            };
        }

        #endregion

        #region Status and Label Methods

        public static string GetComplexityLabel(int score, int lowThreshold = 30, int mediumThreshold = 50)
        {
            if (score < lowThreshold) return "Low Risk";
            if (score < mediumThreshold) return "Medium Risk";
            return "High Risk";
        }

        public static string GetStatusBadgeClass(int complexity, int lowThreshold = 30, int mediumThreshold = 50)
        {
            if (complexity > mediumThreshold) return "bg-danger";
            if (complexity > lowThreshold) return "bg-warning";
            return "bg-success";
        }

        public static string GetStatusText(int complexity, int lowThreshold = 30, int mediumThreshold = 50)
        {
            if (complexity > mediumThreshold) return "Needs Review";
            if (complexity > lowThreshold) return "Monitor";
            return "Good";
        }

        public static string GetMaintenanceLevel(int complexity, int lowThreshold = 30, int mediumThreshold = 50)
        {
            if (complexity > mediumThreshold) return "Immediate attention";
            if (complexity > lowThreshold) return "Scheduled review";
            return "Standard maintenance";
        }

        public static string GetRiskDescription(string risk)
        {
            return risk switch
            {
                "LOW" => "Standard migration process applicable",
                "MEDIUM" => "Structured approach with experienced team required",
                "HIGH" => "Complex migration requiring specialist expertise",
                _ => "Assessment in progress"
            };
        }

        #endregion

        #region Assessment Methods

        public static string GetFileCountAssessment(int fileCount)
        {
            if (fileCount < 5) return "Small project scope";
            if (fileCount < 15) return "Medium project complexity";
            if (fileCount < 30) return "Large project - requires structured approach";
            return "Enterprise-scale project - dedicated team recommended";
        }

        public static string GetClassCountAssessment(int classCount)
        {
            if (classCount < 10) return "Low structural complexity";
            if (classCount < 25) return "Moderate architectural complexity";
            if (classCount < 50) return "Complex architecture - careful planning required";
            return "Highly complex system - expert analysis recommended";
        }

        public static string GetMethodCountAssessment(int methodCount)
        {
            if (methodCount < 50) return "Manageable codebase size";
            if (methodCount < 150) return "Medium codebase - standard development practices apply";
            if (methodCount < 300) return "Large codebase - requires systematic approach";
            return "Very large codebase - enterprise development practices essential";
        }

        public static string GetMigrationTimeline(int complexityScore, int lowThreshold = 30, int mediumThreshold = 50, int highThreshold = 70)
        {
            if (complexityScore < lowThreshold) return "2-4 weeks (Low complexity migration)";
            if (complexityScore < mediumThreshold) return "4-8 weeks (Standard migration process)";
            if (complexityScore < highThreshold) return "8-16 weeks (Complex migration - phased approach)";
            return "16+ weeks (High complexity - dedicated team required)";
        }

        public static string GetResourceRequirements(int complexityScore, int lowThreshold = 30, int mediumThreshold = 50, int highThreshold = 70)
        {
            if (complexityScore < lowThreshold) return "1-2 developers, standard skillset";
            if (complexityScore < mediumThreshold) return "2-3 developers, experienced team";
            if (complexityScore < highThreshold) return "3-5 developers, senior expertise required";
            return "5+ developers, specialist migration team with architect oversight";
        }

        public static string GetFinancialImpact(int complexityScore, int lowThreshold = 30, int mediumThreshold = 50, int highThreshold = 70)
        {
            if (complexityScore < lowThreshold) return "Low cost - standard development rates";
            if (complexityScore < mediumThreshold) return "Moderate cost - budget for quality assurance";
            if (complexityScore < highThreshold) return "High cost - include risk mitigation budget";
            return "Very high cost - executive approval recommended";
        }

        public static string GetExecutiveTimeline(int complexityScore, int lowThreshold = 30, int mediumThreshold = 50, int highThreshold = 70)
        {
            if (complexityScore < lowThreshold)
                return "2-4 weeks (Low complexity migration)";
            else if (complexityScore < mediumThreshold)
                return "4-8 weeks (Standard migration process)";
            else if (complexityScore < highThreshold)
                return "8-12 weeks (Complex migration - phased approach)";
            else
                return "12+ weeks (High complexity - dedicated team required)";
        }

        #endregion

        #region Formatting Methods

        public static string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }

        public static string FormatAIInsight(string insight)
        {
            if (string.IsNullOrEmpty(insight))
                return "<div class='alert alert-light'><em>Assessment pending - analysis in progress</em></div>";

            try
            {
                // Clean up the text aggressively
                var cleaned = insight
                    .Replace("###", "")
                    .Replace("##", "")
                    .Replace("#", "")
                    .Replace("**", "")
                    .Replace("*", "")
                    .Replace("```", "")
                    .Replace("`", "")
                    .Replace("---", "")
                    .Trim();

                // Split into sentences and clean them
                var sentences = cleaned.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 10 && !s.StartsWith("Priority") && !s.StartsWith("Implementation"))
                    .Take(3) // Limit to 3 key insights for executives
                    .ToList();

                if (!sentences.Any())
                {
                    return "<div class='alert alert-warning'><strong>Assessment Summary:</strong> Code analysis completed. Technical details require manual review.</div>";
                }

                var formatted = new System.Text.StringBuilder();
                formatted.Append("<div class='executive-summary'>");

                for (int i = 0; i < sentences.Count; i++)
                {
                    var sentence = sentences[i].Trim();
                    if (!sentence.EndsWith(".")) sentence += ".";

                    // Format as executive-friendly insight
                    formatted.Append($"<div class='insight-item mb-2'>");
                    formatted.Append($"<span class='badge bg-primary me-2'>{i + 1}</span>");
                    formatted.Append($"<span class='insight-text'>{sentence}</span>");
                    formatted.Append("</div>");
                }

                formatted.Append("</div>");
                return formatted.ToString();
            }
            catch (Exception)
            {
                return "<div class='alert alert-info'><strong>Technical Assessment:</strong> Code structure analyzed. Detailed recommendations available upon request.</div>";
            }
        }

        #endregion

        #region Statistics Methods

        public static (int low, int medium, int high, double lowPercent, double mediumPercent, double highPercent) GetRiskStatistics(
            MultiFileAnalysisResult? analysisResult,
            int mediumThreshold = 40,
            int highThreshold = 70)
        {
            if (analysisResult?.FileResults == null) return (0, 0, 0, 0, 0, 0);

            var total = analysisResult.FileResults.Count;
            var low = analysisResult.FileResults.Count(f => f.ComplexityScore < mediumThreshold);
            var medium = analysisResult.FileResults.Count(f => f.ComplexityScore >= mediumThreshold && f.ComplexityScore < highThreshold);
            var high = analysisResult.FileResults.Count(f => f.ComplexityScore >= highThreshold);

            return (low, medium, high,
                    low * 100.0 / total,
                    medium * 100.0 / total,
                    high * 100.0 / total);
        }

        #endregion
    }
}

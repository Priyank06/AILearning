using System.Text;
using PoC1_LegacyAnalyzer_Web.Helpers;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.Reporting
{
    /// <summary>
    /// Generates executive summary report content from analysis results.
    /// </summary>
    public class ExecutiveReportService : IExecutiveReportService
    {
        public int CalculateManualAnalysisHours(MultiFileAnalysisResult result)
        {
            var baseHours = result.TotalClasses * 2;
            var methodHours = result.TotalMethods * 0.25;
            var dependencyHours = result.TotalUsingStatements * 0.5;
            return (int)(baseHours + methodHours + dependencyHours);
        }

        public string GenerateExecutiveReport(MultiFileAnalysisResult result, string analysisType, int complexityLowThreshold, int complexityMediumThreshold, int complexityHighThreshold)
        {
            var report = new StringBuilder();

            report.AppendLine("# Executive Project Analysis Report");
            report.AppendLine($"**Analysis Type**: {analysisType.ToUpper()} ASSESSMENT");
            report.AppendLine($"**Report Generated**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"**Project Scope**: {result.TotalFiles} source files analyzed");
            report.AppendLine();

            report.AppendLine("## Executive Summary");
            report.AppendLine($"This comprehensive analysis evaluated {result.TotalFiles} source files containing {result.TotalClasses} classes and {result.TotalMethods} methods. ");
            report.AppendLine($"The overall project complexity is rated as **{result.OverallRiskLevel}** with a risk score of {result.OverallComplexityScore}/100.");
            report.AppendLine();

            var manualHours = CalculateManualAnalysisHours(result);
            var costSavings = manualHours * 125;

            report.AppendLine("## Strategic Business Context");
            report.AppendLine("**Market Timing**: Legacy modernization market projected 15% annual growth");
            report.AppendLine("**Competitive Advantage**: AI-powered analysis delivers results 20x faster than manual assessment");
            report.AppendLine($"**Risk Management**: {result.OverallRiskLevel} complexity level (score: {result.OverallComplexityScore}/100) requires immediate attention");
            report.AppendLine();

            report.AppendLine("## Financial Impact Analysis");
            report.AppendLine($"**Code Complexity**: {result.TotalClasses} classes with {result.TotalMethods} methods analyzed");
            report.AppendLine($"**Estimated Manual Analysis Time**: {manualHours} hours");
            report.AppendLine($"**AI Analysis Time**: 3 minutes");
            report.AppendLine($"**Time Savings**: {manualHours} hours manual vs. 3 minutes AI = {manualHours * 60 - 3} minutes saved");
            report.AppendLine($"**Cost Avoidance**: ${costSavings:N0} in developer time cost avoidance");
            report.AppendLine();

            var speedMultiplier = (manualHours * 60) / 3;
            report.AppendLine("## Competitive Advantage Analysis");
            report.AppendLine();
            report.AppendLine("### Speed Advantage");
            report.AppendLine($"- **Traditional Approach**: {manualHours} hours manual analysis");
            report.AppendLine($"- **AI-Enhanced Approach**: 3 minutes automated analysis");
            report.AppendLine($"- **Speed Multiplier**: {speedMultiplier:F0}x faster time-to-insight");
            report.AppendLine();
            report.AppendLine("### Cost Advantage");
            report.AppendLine($"- **Traditional Manual Review**: ${costSavings:N0} ({manualHours} hours @ $125/hour)");
            report.AppendLine($"- **AI-Enhanced Analysis**: $5 per comprehensive assessment");
            report.AppendLine($"- **Cost Reduction**: ${costSavings - 5:N0} savings per analysis");
            report.AppendLine();
            report.AppendLine("### Project Metrics");
            report.AppendLine($"- **Code Complexity**: {result.OverallComplexityScore}/100 complexity score");
            report.AppendLine($"- **Risk Assessment**: {result.OverallRiskLevel} risk level based on actual code structure");
            report.AppendLine($"- **Analysis Scope**: {result.TotalClasses} classes, {result.TotalMethods} methods, {result.TotalProperties} properties");
            report.AppendLine();

            report.AppendLine("## Key Performance Indicators");
            report.AppendLine("| Metric | Value | Assessment |");
            report.AppendLine("|--------|--------|------------|");
            report.AppendLine($"| Source Files | {result.TotalFiles} | {MultiFileHelpers.GetFileCountAssessment(result.TotalFiles)} |");
            report.AppendLine($"| Code Classes | {result.TotalClasses} | {MultiFileHelpers.GetClassCountAssessment(result.TotalClasses)} |");
            report.AppendLine($"| Methods | {result.TotalMethods} | {MultiFileHelpers.GetMethodCountAssessment(result.TotalMethods)} |");
            report.AppendLine($"| Complexity Score | {result.OverallComplexityScore}/100 | {result.OverallRiskLevel} Risk Level |");
            report.AppendLine();

            var riskStats = MultiFileHelpers.GetRiskStatistics(result);
            report.AppendLine("## Risk Assessment");
            report.AppendLine($"- **Low Risk Files**: {riskStats.low} files ({riskStats.lowPercent:F1}%)");
            report.AppendLine($"- **Medium Risk Files**: {riskStats.medium} files ({riskStats.mediumPercent:F1}%)");
            report.AppendLine($"- **High Risk Files**: {riskStats.high} files ({riskStats.highPercent:F1}%)");
            report.AppendLine();

            if (result.KeyRecommendations?.Any() == true)
            {
                report.AppendLine("## Strategic Recommendations");
                foreach (var recommendation in result.KeyRecommendations)
                    report.AppendLine($"- {recommendation}");
                report.AppendLine();
            }

            report.AppendLine("## Business Impact Assessment");
            report.AppendLine($"**Migration Timeline**: {MultiFileHelpers.GetMigrationTimeline(result.OverallComplexityScore, complexityLowThreshold, complexityMediumThreshold, complexityHighThreshold)}");
            report.AppendLine($"**Resource Requirements**: {MultiFileHelpers.GetResourceRequirements(result.OverallComplexityScore, complexityLowThreshold, complexityMediumThreshold, complexityHighThreshold)}");
            report.AppendLine($"**Financial Impact**: {MultiFileHelpers.GetFinancialImpact(result.OverallComplexityScore, complexityLowThreshold, complexityMediumThreshold, complexityHighThreshold)}");
            report.AppendLine();

            report.AppendLine("## Detailed File Analysis");
            foreach (var file in (result.FileResults ?? new List<FileAnalysisResult>()).OrderByDescending(f => f.ComplexityScore).Take(10))
            {
                report.AppendLine($"### {file.FileName}");
                report.AppendLine($"- **Complexity Score**: {file.ComplexityScore}/100");
                report.AppendLine($"- **Classes**: {file.StaticAnalysis.ClassCount}");
                report.AppendLine($"- **Methods**: {file.StaticAnalysis.MethodCount}");
                report.AppendLine($"- **Properties**: {file.StaticAnalysis.PropertyCount}");
                if (!string.IsNullOrEmpty(file.AIInsight))
                    report.AppendLine($"- **Assessment**: {file.AIInsight}");
                report.AppendLine();
            }

            report.AppendLine("## Recommended Next Steps");
            report.AppendLine("1. **Immediate Actions**: Address high-complexity files identified in this analysis");
            report.AppendLine("2. **Resource Planning**: Allocate development resources based on complexity assessment");
            report.AppendLine("3. **Timeline Development**: Create detailed project timeline using risk assessment data");
            report.AppendLine("4. **Stakeholder Communication**: Present findings to technical and business stakeholders");
            report.AppendLine("5. **Monitoring Strategy**: Establish metrics tracking for modernization progress");

            return report.ToString();
        }
    }
}

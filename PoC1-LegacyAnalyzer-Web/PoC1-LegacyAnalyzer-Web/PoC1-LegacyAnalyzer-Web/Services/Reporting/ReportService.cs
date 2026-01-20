using PoC1_LegacyAnalyzer_Web.Models;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace PoC1_LegacyAnalyzer_Web.Services.Reporting
{
    public class ReportService : IReportService
    {
        private readonly ILogger<ReportService> _logger;
        private readonly BusinessCalculationRules _businessRules;

        public ReportService(ILogger<ReportService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _businessRules = new BusinessCalculationRules();
            configuration.GetSection("BusinessCalculationRules").Bind(_businessRules);
        }

        public async Task<string> GenerateReportAsync(CodeAnalysisResult analysis, string aiAnalysis, string fileName, string analysisType)
        {
            return await Task.FromResult(GenerateReportContent(analysis, aiAnalysis, fileName, analysisType));
        }

        public async Task<byte[]> GenerateReportAsBytesAsync(CodeAnalysisResult analysis, string aiAnalysis, string fileName, string analysisType)
        {
            var reportContent = GenerateReportContent(analysis, aiAnalysis, fileName, analysisType);
            return await Task.FromResult(Encoding.UTF8.GetBytes(reportContent));
        }

        public string GenerateReportContent(CodeAnalysisResult analysis, string aiAnalysis, string fileName, string analysisType)
        {
            var report = new StringBuilder();

            // Professional Report Header
            report.AppendLine($"# Enterprise Code Analysis Report");
            report.AppendLine($"**Analysis Subject**: {fileName}");
            report.AppendLine($"**Assessment Type**: {analysisType.ToUpper()} ANALYSIS");
            report.AppendLine($"**Report Generated**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"**Analysis Methodology**: Automated static analysis with intelligent assessment");
            report.AppendLine();

            // Executive Summary
            report.AppendLine("## Executive Summary");
            var riskAssessment = GetExecutiveRiskAssessment(analysis, analysisType);
            var businessImpact = GetBusinessImpactAssessment(analysis);
            var timelineEstimate = GetExecutiveTimelineEstimate(analysis, analysisType);

            report.AppendLine($"**Risk Assessment**: {riskAssessment}");
            report.AppendLine($"**Business Impact**: {businessImpact}");
            report.AppendLine($"**Recommended Timeline**: {timelineEstimate}");
            report.AppendLine($"**Strategic Priority**: {GetStrategicPriority(analysis)}");
            report.AppendLine();

            // Technical Assessment Summary
            report.AppendLine("## Technical Assessment Summary");
            report.AppendLine($"- **Code Structure**: {analysis.ClassCount} classes with {analysis.MethodCount} methods");
            report.AppendLine($"- **Data Structures**: {analysis.PropertyCount} properties identified");
            report.AppendLine($"- **External Dependencies**: {analysis.UsingCount} framework and library references");
            report.AppendLine($"- **Architectural Complexity**: {GetArchitecturalComplexity(analysis)}");
            report.AppendLine();

            // Strategic Business Context
            var businessContext = CalculateRealBusinessContext(analysis, analysisType);
            report.AppendLine(GenerateExecutiveBusinessContext(businessContext));
            report.AppendLine();

            // Competitive Advantage Analysis  
            report.AppendLine(GenerateCompetitiveAdvantageSection(businessContext));
            report.AppendLine();

            // Key Components Analysis
            if (analysis.Classes.Any())
            {
                report.AppendLine("### Principal Components Identified");
                foreach (var className in analysis.Classes.Take(5))
                {
                    report.AppendLine($"- **{className}** class - Core business logic component");
                }
                if (analysis.Classes.Count > 5)
                {
                    report.AppendLine($"- Plus {analysis.Classes.Count - 5} additional supporting classes");
                }
                report.AppendLine();
            }

            // Intelligent Analysis Results
            report.AppendLine("## Intelligent Assessment Results");
            report.AppendLine(aiAnalysis);
            report.AppendLine();

            // Business Impact Assessment
            report.AppendLine("## Business Impact Assessment");
            report.AppendLine($"**Modernization ROI**: {GetModernizationROI(analysis)}");
            report.AppendLine($"**Technical Debt Level**: {GetTechnicalDebtAssessment(analysis)}");
            report.AppendLine($"**Migration Complexity**: {GetMigrationComplexityAssessment(analysis)}");
            report.AppendLine($"**Resource Requirements**: {GetResourceRequirementsAssessment(analysis)}");
            report.AppendLine();

            // Risk Analysis
            report.AppendLine("## Risk Analysis & Mitigation");
            report.AppendLine(GetRiskAnalysis(analysis, analysisType));
            report.AppendLine();

            // Strategic Recommendations
            report.AppendLine("## Strategic Recommendations");
            report.AppendLine("### Immediate Actions (0-2 weeks)");
            report.AppendLine("- Conduct stakeholder alignment meeting to review assessment findings");
            report.AppendLine("- Establish project team with appropriate skill mix and authority");
            report.AppendLine("- Define success criteria and key performance indicators");
            report.AppendLine();

            report.AppendLine("### Short-term Objectives (2-8 weeks)");
            report.AppendLine("- Implement recommended high-priority improvements identified in assessment");
            report.AppendLine("- Establish automated testing framework to support modernization activities");
            report.AppendLine("- Create detailed project timeline with milestone-based delivery approach");
            report.AppendLine();

            report.AppendLine("### Long-term Strategy (8+ weeks)");
            report.AppendLine("- Execute comprehensive modernization plan with continuous monitoring");
            report.AppendLine("- Establish code quality standards and governance processes");
            report.AppendLine("- Implement knowledge transfer and documentation practices");
            report.AppendLine();

            // Financial Analysis
            report.AppendLine("## Financial Impact Analysis");
            report.AppendLine($"**Estimated Project Cost**: {GetProjectCostEstimate(analysis, analysisType)}");
            report.AppendLine($"**Expected ROI Timeline**: {GetROITimeline(analysis)}");
            report.AppendLine($"**Cost of Inaction**: {GetCostOfInaction(analysis, analysisType)}");
            report.AppendLine();

            // Implementation Roadmap
            report.AppendLine("## Implementation Roadmap");
            report.AppendLine("### Phase 1: Foundation & Planning");
            report.AppendLine("- Architecture assessment and modernization strategy development");
            report.AppendLine("- Team formation and skill development planning");
            report.AppendLine("- Development environment and tooling setup");
            report.AppendLine();

            report.AppendLine("### Phase 2: Core Implementation");
            report.AppendLine("- Execute priority modernization activities identified in assessment");
            report.AppendLine("- Implement testing and quality assurance processes");
            report.AppendLine("- Continuous integration and deployment pipeline establishment");
            report.AppendLine();

            report.AppendLine("### Phase 3: Validation & Deployment");
            report.AppendLine("- Comprehensive testing and performance validation");
            report.AppendLine("- User acceptance testing and stakeholder sign-off");
            report.AppendLine("- Production deployment and monitoring implementation");
            report.AppendLine();

            // Quality Assurance
            report.AppendLine("## Quality Assurance Recommendations");
            report.AppendLine("- Implement automated code analysis tools with quality gates");
            report.AppendLine("- Establish peer review processes for all code modifications");
            report.AppendLine("- Create comprehensive test suite covering functional and performance requirements");
            report.AppendLine("- Implement continuous monitoring and alerting for production systems");
            report.AppendLine();

            // Conclusion
            report.AppendLine("## Executive Summary & Next Steps");
            report.AppendLine($"This {analysisType} assessment provides a comprehensive evaluation of the current codebase ");
            report.AppendLine($"and identifies specific opportunities for modernization and improvement. ");
            report.AppendLine($"The recommended approach balances business objectives with technical requirements ");
            report.AppendLine($"to deliver measurable value while minimizing implementation risk.");
            report.AppendLine();

            // Footer
            report.AppendLine("---");
            report.AppendLine("**Report Classification**: Internal Business Use");
            report.AppendLine("**Generated By**: Enterprise Code Analysis Platform");
            report.AppendLine($"**Analysis Timestamp**: {DateTime.Now:yyyy-MM-dd HH:mm:ss UTC}");
            report.AppendLine("**Recommended Review Cycle**: Quarterly assessment updates");

            return report.ToString();
        }

        private string GetExecutiveRiskAssessment(CodeAnalysisResult analysis, string analysisType)
        {
            var complexityScore = CalculateComplexityScore(analysis);
            return analysisType switch
            {
                "security" => complexityScore > 60 ? "HIGH RISK - Critical security review required immediately" :
                             complexityScore > 30 ? "MEDIUM RISK - Security improvements recommended within 30 days" :
                             "LOW RISK - Standard security practices appear adequate",
                "performance" => complexityScore > 80 ? "HIGH RISK - Performance bottlenecks likely impacting business operations" :
                                complexityScore > 50 ? "MEDIUM RISK - Performance optimization opportunities identified" :
                                "LOW RISK - Performance appears adequate for current operational requirements",
                "migration" => complexityScore > 70 ? "HIGH RISK - Complex migration requiring dedicated specialist team" :
                              complexityScore > 40 ? "MEDIUM RISK - Structured migration approach with experienced team required" :
                              "LOW RISK - Straightforward migration suitable for standard development practices",
                _ => complexityScore > 60 ? "MEDIUM RISK - Moderate modernization effort required" :
                     "LOW RISK - Minimal changes needed for modernization objectives"
            };
        }

        private string GetBusinessImpactAssessment(CodeAnalysisResult analysis)
        {
            return analysis.MethodCount switch
            {
                > 100 => "HIGH IMPACT - Core business system requiring executive oversight and structured approach",
                > 50 => "MEDIUM IMPACT - Important business component requiring experienced team and proper planning",
                > 20 => "MODERATE IMPACT - Standard business system suitable for normal development processes",
                _ => "LOW IMPACT - Supporting component appropriate for junior developer development"
            };
        }

        private string GetExecutiveTimelineEstimate(CodeAnalysisResult analysis, string analysisType)
        {
            var complexity = CalculateComplexityScore(analysis);
            var baseTimeline = complexity switch
            {
                > 70 => "12-20 weeks",
                > 50 => "8-12 weeks",
                > 30 => "4-8 weeks",
                _ => "2-4 weeks"
            };

            var riskFactor = analysisType == "security" ? " (expedited timeline recommended for security issues)" :
                           analysisType == "migration" ? " (includes testing and validation phases)" :
                           "";

            return baseTimeline + riskFactor;
        }

        private string GetStrategicPriority(CodeAnalysisResult analysis)
        {
            var priority = (analysis.ClassCount * 2 + analysis.MethodCount) switch
            {
                > 200 => "CRITICAL - Executive approval and dedicated team required",
                > 100 => "HIGH - Senior management oversight and experienced team necessary",
                > 50 => "MEDIUM - Standard project management and skilled developers appropriate",
                _ => "LOW - Can be included in regular development cycle"
            };
            return priority;
        }

        private string GetArchitecturalComplexity(CodeAnalysisResult analysis)
        {
            var methodsPerClass = analysis.ClassCount > 0 ? (double)analysis.MethodCount / analysis.ClassCount : 0;
            return methodsPerClass switch
            {
                > 15 => "High complexity - indicates potential architectural refactoring opportunities",
                > 8 => "Moderate complexity - standard enterprise application architecture",
                > 4 => "Low complexity - well-structured codebase with clear separation of concerns",
                _ => "Minimal complexity - simple architecture suitable for current requirements"
            };
        }

        private string GetModernizationROI(CodeAnalysisResult analysis)
        {
            return analysis.ClassCount switch
            {
                > 20 => "HIGH ROI - Significant operational and maintenance cost savings expected",
                > 10 => "MEDIUM ROI - Moderate benefits from improved maintainability and performance",
                > 5 => "LOW-MEDIUM ROI - Benefits primarily in code maintainability and developer productivity",
                _ => "LOW ROI - Consider as part of broader modernization initiative"
            };
        }

        private string GetTechnicalDebtAssessment(CodeAnalysisResult analysis)
        {
            var debtIndicator = analysis.UsingCount + (analysis.MethodCount / Math.Max(analysis.ClassCount, 1));
            return debtIndicator switch
            {
                > 25 => "SUBSTANTIAL - Significant refactoring required to address accumulated technical debt",
                > 15 => "MODERATE - Some architectural improvements needed to optimize maintainability",
                > 8 => "LOW - Well-maintained codebase with minimal technical debt accumulation",
                _ => "MINIMAL - Current code practices appear to manage technical debt effectively"
            };
        }

        private string GetMigrationComplexityAssessment(CodeAnalysisResult analysis)
        {
            var complexity = analysis.ClassCount * analysis.MethodCount;
            return complexity switch
            {
                > 1000 => "VERY HIGH - Enterprise-scale migration requiring specialized methodology and tools",
                > 500 => "HIGH - Complex migration requiring dedicated team with migration expertise",
                > 200 => "MEDIUM - Standard migration complexity suitable for experienced development team",
                _ => "LOW - Straightforward migration using standard development practices"
            };
        }

        private string GetResourceRequirementsAssessment(CodeAnalysisResult analysis)
        {
            var complexity = CalculateComplexityScore(analysis);
            return complexity switch
            {
                > 70 => "Senior architect + 3-5 experienced developers + dedicated QA resources",
                > 50 => "Technical lead + 2-3 experienced developers + standard QA support",
                > 30 => "Senior developer + 1-2 developers + integrated QA processes",
                _ => "Standard developer resources with senior developer oversight"
            };
        }

        private string GetRiskAnalysis(CodeAnalysisResult analysis, string analysisType)
        {
            var riskAnalysis = new StringBuilder();

            riskAnalysis.AppendLine("### Technical Risks");
            riskAnalysis.AppendLine("- **Code Complexity**: " + GetComplexityRisk(analysis));
            riskAnalysis.AppendLine("- **Dependency Management**: " + GetDependencyRisk(analysis));
            riskAnalysis.AppendLine("- **Architecture Scale**: " + GetScaleRisk(analysis));
            riskAnalysis.AppendLine();

            riskAnalysis.AppendLine("### Business Risks");
            riskAnalysis.AppendLine("- **Operational Continuity**: " + GetOperationalRisk(analysis));
            riskAnalysis.AppendLine("- **Resource Allocation**: " + GetResourceRisk(analysis));
            riskAnalysis.AppendLine("- **Timeline Adherence**: " + GetTimelineRisk(analysis));
            riskAnalysis.AppendLine();

            riskAnalysis.AppendLine("### Mitigation Strategies");
            riskAnalysis.AppendLine("- Implement comprehensive automated testing before modernization activities");
            riskAnalysis.AppendLine("- Establish rollback procedures and disaster recovery protocols");
            riskAnalysis.AppendLine("- Create detailed project timeline with milestone-based checkpoints");
            riskAnalysis.AppendLine("- Ensure adequate resource allocation with contingency planning");

            return riskAnalysis.ToString();
        }

        private string GetComplexityRisk(CodeAnalysisResult analysis) => CalculateComplexityScore(analysis) switch
        {
            > 70 => "High risk - Complex codebase requires specialist expertise and careful planning",
            > 40 => "Medium risk - Moderate complexity manageable with experienced team",
            _ => "Low risk - Standard complexity suitable for normal development practices"
        };

        private string GetDependencyRisk(CodeAnalysisResult analysis) => analysis.UsingCount switch
        {
            > 15 => "High risk - Extensive dependencies require careful compatibility analysis",
            > 8 => "Medium risk - Standard dependency management practices required",
            _ => "Low risk - Minimal external dependencies simplify modernization process"
        };

        private string GetScaleRisk(CodeAnalysisResult analysis) => analysis.ClassCount switch
        {
            > 25 => "High risk - Large codebase requires structured approach and dedicated team",
            > 10 => "Medium risk - Standard project management practices adequate",
            _ => "Low risk - Small codebase suitable for agile development approach"
        };

        private string GetOperationalRisk(CodeAnalysisResult analysis) => analysis.MethodCount switch
        {
            > 100 => "High risk - Core business functionality requires careful change management",
            > 50 => "Medium risk - Important business operations require structured testing approach",
            _ => "Low risk - Supporting functionality with minimal business impact"
        };

        private string GetResourceRisk(CodeAnalysisResult analysis) => CalculateComplexityScore(analysis) switch
        {
            > 60 => "High risk - Requires experienced team and may compete for limited senior resources",
            > 30 => "Medium risk - Standard resource planning and skill mix adequate",
            _ => "Low risk - Can be completed with existing team capabilities"
        };

        private string GetTimelineRisk(CodeAnalysisResult analysis) => CalculateComplexityScore(analysis) switch
        {
            > 70 => "High risk - Complex project with potential for scope creep and timeline extension",
            > 40 => "Medium risk - Standard project management practices should maintain timeline adherence",
            _ => "Low risk - Straightforward implementation with predictable timeline"
        };

        private string GetProjectCostEstimate(CodeAnalysisResult analysis, string analysisType)
        {
            var complexity = CalculateComplexityScore(analysis);
            var baseCost = complexity switch
            {
                > 70 => "$150K-300K",
                > 50 => "$75K-150K",
                > 30 => "$25K-75K",
                _ => "$10K-25K"
            };

            var analysisMultiplier = analysisType switch
            {
                "security" => " (includes security audit and remediation)",
                "migration" => " (includes testing and validation phases)",
                "performance" => " (includes performance testing and optimization)",
                _ => " (includes standard quality assurance)"
            };

            return baseCost + analysisMultiplier;
        }

        private string GetROITimeline(CodeAnalysisResult analysis) => CalculateComplexityScore(analysis) switch
        {
            > 60 => "12-18 months (long-term strategic investment)",
            > 30 => "6-12 months (medium-term operational improvements)",
            _ => "3-6 months (immediate productivity and maintenance benefits)"
        };

        private string GetCostOfInaction(CodeAnalysisResult analysis, string analysisType)
        {
            return analysisType switch
            {
                "security" => "Increasing security vulnerability exposure and potential compliance violations",
                "performance" => "Ongoing operational inefficiency and scalability limitations impacting business growth",
                "migration" => "Escalating technical debt and decreasing maintainability affecting long-term sustainability",
                _ => "Continued maintenance overhead and reduced development velocity impacting competitive position"
            };
        }

        private int CalculateComplexityScore(CodeAnalysisResult analysis)
        {
            var structuralComplexity = analysis.ClassCount * 3;
            var behavioralComplexity = analysis.MethodCount * 1;
            var dependencyComplexity = analysis.UsingCount * 2;

            var totalComplexity = structuralComplexity + behavioralComplexity + dependencyComplexity;
            return Math.Min(100, Math.Max(0, totalComplexity / 2));
        }

        private string GenerateExecutiveBusinessContext(BusinessAnalysisContext context)
        {
            var report = new StringBuilder();

            report.AppendLine("## Strategic Business Context");
            report.AppendLine("**Market Timing**: Legacy modernization market projected 15% annual growth");
            report.AppendLine("**Competitive Advantage**: AI-powered analysis delivers results 20x faster than manual assessment");
            report.AppendLine($"**Risk Management**: {context.ActualRiskLevel} complexity level (score: {context.ActualComplexityScore}/100) requires immediate attention");
            report.AppendLine();

            report.AppendLine("## Financial Impact Analysis");
            report.AppendLine($"**Code Complexity**: {context.ActualClassCount} classes with {context.ActualMethodCount} methods analyzed");
            report.AppendLine($"**Estimated Manual Analysis Time**: {CalculateManualAnalysisHours(context)} hours");
            report.AppendLine($"**AI Analysis Time**: 3 minutes");
            report.AppendLine($"**Time Savings**: {CalculateTimeSavings(context)}");
            report.AppendLine($"**Cost Avoidance**: {CalculateCostAvoidance(context)}");

            return report.ToString();
        }

        private int CalculateManualAnalysisHours(BusinessAnalysisContext context)
        {
            // Real calculation based on actual code metrics using configuration
            var manualConfig = _businessRules.ManualAnalysis;
            var baseHours = context.ActualClassCount * manualConfig.HoursPerClass;
            var methodHours = context.ActualMethodCount * manualConfig.HoursPerMethod;
            var dependencyHours = context.ActualUsingCount * manualConfig.HoursPerDependency;

            return (int)(baseHours + methodHours + dependencyHours);
        }

        private string CalculateTimeSavings(BusinessAnalysisContext context)
        {
            var manualHours = CalculateManualAnalysisHours(context);
            var aiTimeMinutes = _businessRules.ManualAnalysis.AIAnalysisTimeMinutes;
            return $"{manualHours} hours manual analysis vs. {aiTimeMinutes} minutes AI analysis = {manualHours * 60 - aiTimeMinutes} minutes saved";
        }

        private string CalculateCostAvoidance(BusinessAnalysisContext context)
        {
            var manualHours = CalculateManualAnalysisHours(context);
            var hourlyRate = _businessRules.CostCalculation.DefaultDeveloperHourlyRate;
            var costSavings = manualHours * hourlyRate;
            return $"${costSavings:N0} in developer time cost avoidance";
        }


        private string GenerateCompetitiveAdvantageSection(BusinessAnalysisContext context)
        {
            var manualHours = CalculateManualAnalysisHours(context);
            var hourlyRate = _businessRules.CostCalculation.DefaultDeveloperHourlyRate;
            var costSavings = manualHours * hourlyRate;
            var aiTimeMinutes = _businessRules.ManualAnalysis.AIAnalysisTimeMinutes;
            var aiCost = _businessRules.ManualAnalysis.AIAnalysisCostPerAssessment;
            var speedMultiplier = (manualHours * 60.0) / aiTimeMinutes; // Convert hours to minutes, divide by AI time

            return $@"## Competitive Advantage Analysis

### Speed Advantage
- **Traditional Approach**: {manualHours} hours manual analysis
- **AI-Enhanced Approach**: {aiTimeMinutes} minutes automated analysis  
- **Speed Multiplier**: {speedMultiplier:F0}x faster time-to-insight

### Cost Advantage  
- **Traditional Manual Review**: ${costSavings:N0} (${manualHours} hours @ ${hourlyRate}/hour)
- **AI-Enhanced Analysis**: ${aiCost} per comprehensive assessment (cloud costs)
- **Cost Reduction**: ${costSavings - aiCost:N0} savings per analysis

### Accuracy Advantage
- **Manual Analysis**: {context.ActualClassCount} classes requiring individual review
- **AI Analysis**: Consistent evaluation of {context.ActualClassCount} classes + {context.ActualMethodCount} methods
- **Coverage**: 100% code coverage vs. selective manual sampling

### Project Metrics
- **Code Complexity**: {context.ActualComplexityScore}/100 complexity score
- **Risk Assessment**: {context.ActualRiskLevel} risk level based on actual code structure
- **Analysis Scope**: {context.ActualClassCount} classes, {context.ActualMethodCount} methods, {context.ActualPropertyCount} properties";
        }

        private BusinessAnalysisContext CalculateRealBusinessContext(CodeAnalysisResult analysis, string analysisType)
        {
            var complexityScore = CalculateComplexityScore(analysis);
            var riskLevel = GetRiskLevel(complexityScore);

            return new BusinessAnalysisContext
            {
                ActualComplexityScore = complexityScore,
                ActualRiskLevel = riskLevel,
                ActualClassCount = analysis.ClassCount,
                ActualMethodCount = analysis.MethodCount,
                ActualPropertyCount = analysis.PropertyCount,
                ActualUsingCount = analysis.UsingCount,
                AnalysisType = analysisType
            };
        }

        private string GetRiskLevel(int complexityScore) => complexityScore switch
        {
            < 30 => "LOW",
            < 60 => "MEDIUM",
            _ => "HIGH"
        };
    }
}


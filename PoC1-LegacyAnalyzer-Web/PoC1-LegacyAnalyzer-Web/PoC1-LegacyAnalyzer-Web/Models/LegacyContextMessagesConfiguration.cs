namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Configuration for legacy context messages used in AI prompts.
    /// </summary>
    public class LegacyContextMessagesConfiguration
    {
        public string VeryOldCode { get; set; } = "⚠️ Very Old Code: This file is approximately {years} years old. Consider legacy patterns, outdated practices, and accumulated technical debt.";
        public string AncientFramework { get; set; } = "🏛️ Ancient .NET Framework{frameworkInfo}: This code uses an outdated framework version. Be aware of deprecated APIs, security vulnerabilities in older frameworks, and migration challenges.";
        public string GlobalStateDetected { get; set; } = "🌐 Global State Detected: This codebase uses global/static state patterns. This creates tight coupling, makes testing difficult, and can cause thread-safety issues. Consider dependency injection alternatives.";
        public string LegacyDataAccess { get; set; } = "📊 Legacy Data Access: This code uses older data access patterns (e.g., DataSet, DataTable, ADO.NET). Modern alternatives include Entity Framework Core, Dapper, or repository patterns.";
        public string ObsoleteApis { get; set; } = "🔴 Obsolete APIs: This code uses deprecated or obsolete APIs that may be removed in future framework versions. Prioritize migration to modern alternatives.";
        public string ChangeFrequencyNone { get; set; } = "📅 Change Frequency: This file has not been modified recently (0 changes in last year). It may be stable but could also indicate dead code or low maintenance priority.";
        public string ChangeFrequencyLow { get; set; } = "📅 Change Frequency: Low change frequency ({count} changes/year). This is relatively stable code, but may indicate low priority or technical debt accumulation.";
        public string ChangeFrequencyHigh { get; set; } = "📅 Change Frequency: High change frequency ({count} changes/year). This file is actively maintained but may indicate instability or frequent bug fixes.";
        public string LegacyContextHeader { get; set; } = "\n\nLEGACY CODE CONTEXT:\n";
        public string LegacyContextFooter { get; set; } = "\n\nWhen analyzing this code, pay special attention to:\n- Legacy anti-patterns and technical debt\n- Migration paths to modern frameworks and APIs\n- Security vulnerabilities common in older codebases\n- Performance issues from outdated patterns\n- Maintainability concerns from accumulated technical debt";
        public string LegacyCodebaseContextHeader { get; set; } = "\n\nLEGACY CODEBASE CONTEXT:\n";
        public string LegacyCodebaseContextFooter { get; set; } = "\n\nThis is a legacy codebase with accumulated technical debt. Prioritize modernization recommendations and migration paths.";
        public string VeryOldFilesSummary { get; set; } = "⚠️ {count} file(s) are very old (5+ years)";
        public string AncientFrameworkFilesSummary { get; set; } = "🏛️ {count} file(s) use ancient .NET Framework versions";
        public string GlobalStateFilesSummary { get; set; } = "🌐 {count} file(s) contain global state patterns";
        public string ObsoleteApiFilesSummary { get; set; } = "🔴 {count} file(s) use obsolete APIs";
        public string TotalLegacyIssuesSummary { get; set; } = "📋 Total legacy issues detected: {totalIssues} across {fileCount} files";
        public string LegacyContextHeaderSimple { get; set; } = "\n\nLEGACY CODE CONTEXT:\n";
        public string SecurityLegacyContextFooter { get; set; } = "\n\nPay special attention to security vulnerabilities common in legacy codebases.";
        public string PerformanceLegacyContextFooter { get; set; } = "\n\nPay special attention to performance bottlenecks from legacy patterns.";
        public string ArchitectureLegacyContextFooter { get; set; } = "\n\nPay special attention to architectural anti-patterns, technical debt, and modernization opportunities.";
    }

    public class AgentLegacyIndicatorsConfiguration
    {
        public SecurityLegacyIndicators Security { get; set; } = new();
        public PerformanceLegacyIndicators Performance { get; set; } = new();
        public ArchitectureLegacyIndicators Architecture { get; set; } = new();
    }

    public class SecurityLegacyIndicators
    {
        public string AncientFramework { get; set; } = "🏛️ Ancient .NET Framework detected";
        public string GlobalState { get; set; } = "🌐 Global state patterns detected";
        public string ObsoleteApis { get; set; } = "🔴 Obsolete APIs detected";
    }

    public class PerformanceLegacyIndicators
    {
        public string LegacyDataAccess { get; set; } = "📊 Legacy data access patterns detected (DataSet/DataTable) - these are memory-intensive and slow";
        public string SynchronousIO { get; set; } = "⚠️ Synchronous I/O patterns detected - these block threads and hurt scalability";
        public string LegacyWebForms { get; set; } = "🏛️ Legacy ASP.NET WebForms patterns detected - ViewState can cause performance issues";
    }

    public class ArchitectureLegacyIndicators
    {
        public string AncientFramework { get; set; } = "🏛️ Ancient .NET Framework detected";
        public string GlobalState { get; set; } = "🌐 Global state patterns detected";
        public string GodObjects { get; set; } = "⚠️ Large number of classes detected - potential God Objects";
    }

    public class PromptTemplatesConfiguration
    {
        public string BatchAnalysisPrompt { get; set; } = "Analyze {fileCount} source files for {analysisType} assessment. Provide comprehensive analysis for EACH file.\n\n{fileSections}\n\nReturn ONLY valid JSON (no markdown, no explanations):\n{{\n  \"analyses\": [\n    {{\"fileName\": \"File1.cs\", \"assessment\": \"{minWords}-{maxWords} word analysis covering all {analysisType} aspects\"}},\n    {{\"fileName\": \"File2.cs\", \"assessment\": \"{minWords}-{maxWords} word analysis covering all {analysisType} aspects\"}}\n  ]\n}}\n\nRequirements:\n- Every file must have exactly one entry\n- Assessments must be comprehensive ({minWords}-{maxWords} words)\n- Focus on {analysisType}-specific insights\n- Include actionable recommendations";
        public string BatchSystemPromptSuffix { get; set; } = "\n\nCRITICAL: You MUST return ONLY valid JSON. No markdown, no explanations, just JSON.\nRequired JSON structure:\n{{\n  \"analyses\": [\n    {{\"fileName\": \"File1.cs\", \"assessment\": \"Your detailed analysis here...\"}},\n    {{\"fileName\": \"File2.cs\", \"assessment\": \"Your detailed analysis here...\"}}\n  ]\n}}\nEvery file in the input MUST have exactly one entry in the analyses array. The assessment should be comprehensive (200-500 words) covering all aspects of the analysis type.";
        public string DefaultSystemPrompt { get; set; } = "You are a senior software architect providing comprehensive code analysis.";
        public string FileSectionTemplate { get; set; } = "FILE {index}: {fileName}\nMetrics: {classCount} classes, {methodCount} methods, {propertyCount} properties\nTop Classes: {topClasses}\nCode:\n{codePreview}";
    }
}


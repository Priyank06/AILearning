using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using PoC1_LegacyAnalyzer_Web.Models;
using System.ComponentModel;
using System.Text.Json;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class CodeAnalysisAgentService : ICodeAnalysisAgentService
    {
        private readonly Kernel _kernel;
        private readonly ICodeAnalysisService _codeAnalysisService;
        private readonly ILogger<CodeAnalysisAgentService> _logger;

        public CodeAnalysisAgentService(Kernel kernel, ICodeAnalysisService codeAnalysisService, ILogger<CodeAnalysisAgentService> logger)
        {
            _kernel = kernel;
            _codeAnalysisService = codeAnalysisService;
            _logger = logger;

            // Register this class as a plugin so the agent can call our functions
            _kernel.Plugins.AddFromObject(this, "CodeAnalysis");
        }

        [KernelFunction, Description("Analyze C# code structure, complexity, and quality metrics")]
        public async Task<string> AnalyzeCodeStructure(
            [Description("The C# source code to analyze")] string code)
        {
            try
            {
                var analysis = await _codeAnalysisService.AnalyzeCodeAsync(code);
                return JsonSerializer.Serialize(analysis, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in code structure analysis");
                return $"Error analyzing code: {ex.Message}";
            }
        }

        [KernelFunction, Description("Generate strategic business recommendations based on technical analysis")]
        public async Task<string> GenerateBusinessRecommendations(
            [Description("Technical analysis results in JSON format")] string analysisJson,
            [Description("Business goal or objective")] string businessGoal,
            [Description("Project context and constraints")] string projectContext)
        {
            var prompt = $@"
Based on this technical analysis: {analysisJson}

Business Goal: {businessGoal}
Project Context: {projectContext}

Generate strategic business recommendations that include:
1. Business impact assessment
2. Risk evaluation with mitigation strategies  
3. Resource requirements and timeline
4. ROI considerations
5. Implementation priorities

Provide actionable, executive-level recommendations.";

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? "Unable to generate recommendations";
        }

        [KernelFunction, Description("Create detailed implementation plan with phases and milestones")]
        public async Task<string> CreateImplementationPlan(
            [Description("Business recommendations")] string recommendations,
            [Description("Technical constraints and requirements")] string constraints,
            [Description("Available resources and timeline")] string resources)
        {
            var prompt = $@"
Based on these recommendations: {recommendations}

Technical Constraints: {constraints}
Available Resources: {resources}

Create a detailed implementation plan with:
1. Phase-based approach with clear milestones
2. Resource allocation for each phase
3. Risk mitigation strategies
4. Success criteria and validation checkpoints
5. Timeline with dependencies
6. Communication and stakeholder management plan

Format as an executive-ready implementation roadmap.";

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? "Unable to create implementation plan";
        }

        public async Task<AutonomousAnalysisResult> AnalyzeWithAgentAsync(
            string code,
            string businessGoal,
            string projectContext = "",
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting autonomous agent analysis for business goal: {BusinessGoal}", businessGoal);

                var result = new AutonomousAnalysisResult();

                // Step 1: Agent reasoning about the task
                result.AgentReasoning = await ReasonAboutTask(businessGoal, projectContext);

                // Step 2: Develop analysis strategy
                result.AnalysisStrategy = await DevelopAnalysisStrategy(businessGoal, projectContext);

                // Step 3: Execute technical analysis using our function
                var technicalAnalysis = await AnalyzeCodeStructure(code);
                result.TechnicalFindings = technicalAnalysis;

                // Step 4: Generate business recommendations
                result.BusinessImpact = await GenerateBusinessRecommendations(technicalAnalysis, businessGoal, projectContext);

                // Step 5: Create strategic recommendations
                result.StrategicRecommendations = await GenerateStrategicRecommendations(result.BusinessImpact, businessGoal);

                // Step 6: Develop implementation plan
                result.ImplementationPlan = await CreateImplementationPlan(
                    result.StrategicRecommendations,
                    projectContext,
                    "Standard enterprise development team");

                // Step 7: Determine next actions
                result.NextActions = await DetermineNextActions(result.ImplementationPlan, businessGoal);

                result.ConfidenceScore = CalculateConfidenceScore(result);

                _logger.LogInformation("Autonomous analysis completed with confidence score: {ConfidenceScore}", result.ConfidenceScore);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in autonomous agent analysis");
                return new AutonomousAnalysisResult
                {
                    AgentReasoning = $"Analysis encountered an error: {ex.Message}",
                    ConfidenceScore = 0
                };
            }
        }

        private async Task<string> ReasonAboutTask(string businessGoal, string projectContext)
        {
            var prompt = $@"
You are an AI architect agent tasked with analyzing legacy code for this business goal: {businessGoal}

Project Context: {projectContext}

First, reason about this task:
1. What are the key success criteria for this business goal?
2. What technical aspects should be prioritized?
3. What business risks need to be considered?
4. What stakeholders will be interested in the results?

Provide your reasoning in 2-3 clear paragraphs.";

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? "Unable to generate reasoning";
        }

        private async Task<string> DevelopAnalysisStrategy(string businessGoal, string projectContext)
        {
            var prompt = $@"
Based on this business goal: {businessGoal}
And project context: {projectContext}

Develop a comprehensive analysis strategy that outlines:
1. Technical analysis approach and focus areas
2. Business impact evaluation methodology  
3. Risk assessment framework
4. Success metrics and validation criteria
5. Reporting and communication strategy

Format as a structured analysis plan.";

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? "Unable to develop strategy";
        }

        private async Task<string> GenerateStrategicRecommendations(string businessImpact, string businessGoal)
        {
            var prompt = $@"
Based on this business impact analysis: {businessImpact}
And the original business goal: {businessGoal}

Generate strategic recommendations that include:
1. Priority actions for immediate implementation
2. Medium-term strategic initiatives  
3. Long-term transformation roadmap
4. Resource optimization opportunities
5. Competitive advantage considerations

Focus on actionable, business-value driven recommendations.";

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? "Unable to generate strategic recommendations";
        }

        private async Task<string> DetermineNextActions(string implementationPlan, string businessGoal)
        {
            var prompt = $@"
Based on this implementation plan: {implementationPlan}
And business goal: {businessGoal}

Determine the top 3 immediate next actions that should be taken in the next 2 weeks to move forward with this initiative.

Format as:
1. [Action] - [Justification] - [Timeline]
2. [Action] - [Justification] - [Timeline]  
3. [Action] - [Justification] - [Timeline]";

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? "Unable to determine next actions";
        }

        private int CalculateConfidenceScore(AutonomousAnalysisResult result)
        {
            int score = 100;

            // Deduct points for missing or short content
            if (string.IsNullOrWhiteSpace(result.TechnicalFindings) || result.TechnicalFindings.Length < 100) score -= 20;
            if (string.IsNullOrWhiteSpace(result.BusinessImpact) || result.BusinessImpact.Length < 100) score -= 20;
            if (string.IsNullOrWhiteSpace(result.StrategicRecommendations) || result.StrategicRecommendations.Length < 100) score -= 15;
            if (string.IsNullOrWhiteSpace(result.ImplementationPlan) || result.ImplementationPlan.Length < 100) score -= 15;
            if (string.IsNullOrWhiteSpace(result.NextActions) || result.NextActions.Length < 50) score -= 10;

            return Math.Max(0, score);
        }

        public async Task<string> GenerateStrategicPlanAsync(
            MultiFileAnalysisResult projectAnalysis,
            string businessObjective,
            CancellationToken cancellationToken = default)
        {
            var prompt = $@"
Based on this comprehensive project analysis:
- Total Files: {projectAnalysis.TotalFiles}
- Total Classes: {projectAnalysis.TotalClasses}  
- Total Methods: {projectAnalysis.TotalMethods}
- Overall Complexity: {projectAnalysis.OverallComplexityScore}/100
- Risk Level: {projectAnalysis.OverallRiskLevel}

Business Objective: {businessObjective}

As an AI architect agent, create a strategic modernization plan that includes:

1. EXECUTIVE SUMMARY
   - Project assessment and business impact
   - Strategic recommendations
   - Investment and timeline overview

2. TECHNICAL STRATEGY
   - Modernization approach and methodology
   - Risk mitigation and quality assurance
   - Technology stack and architecture decisions

3. IMPLEMENTATION ROADMAP
   - Phase-based delivery plan with milestones
   - Resource requirements and team structure
   - Success criteria and validation checkpoints

4. BUSINESS CASE
   - Cost-benefit analysis and ROI projections
   - Competitive advantages and strategic value
   - Risk assessment and mitigation strategies

Format as an executive-ready strategic plan suitable for board-level presentation.";

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? "Unable to generate strategic plan";
        }
    }
}

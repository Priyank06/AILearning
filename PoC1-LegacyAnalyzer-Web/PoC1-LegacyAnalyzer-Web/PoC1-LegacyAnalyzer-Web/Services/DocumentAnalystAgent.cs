using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using System.ComponentModel;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class DocumentAnalystAgent : ISpecialistAgentService
    {
        private readonly Kernel _kernel;
        private readonly IFormRecognizerService _formRecognizerService;
        private readonly ILogger<DocumentAnalystAgent> _logger;

        public string AgentName => "DocumentAnalyst-Delta";
        public string Specialty => "Technical Document Analysis & Requirements Extraction";
        public string AgentPersona => "Senior Technical Documentation Analyst with 12+ years experience in requirements analysis, technical specifications, and architecture documentation compliance";
        public int ConfidenceThreshold => 70;

        public DocumentAnalystAgent(
            Kernel kernel,
            IFormRecognizerService formRecognizerService,
            ILogger<DocumentAnalystAgent> logger)
        {
            _kernel = kernel;
            _formRecognizerService = formRecognizerService;
            _logger = logger;
            _kernel.Plugins.AddFromObject(this, "DocumentAnalyst");
        }

        public async Task<string> AnalyzeAsync(string input, string context, CancellationToken cancellationToken = default)
        {
            var prompt = $@"
You are {AgentPersona}.

ANALYSIS INPUT: {input}
CONTEXT: {context}

Conduct comprehensive technical document analysis:

1. DOCUMENT STRUCTURE ASSESSMENT
   - Information architecture evaluation
   - Content organization analysis
   - Completeness assessment

2. REQUIREMENTS EXTRACTION
   - Functional requirements identification
   - Non-functional requirements analysis
   - Business rules and constraints
   - Assumptions and dependencies

3. TECHNICAL SPECIFICATION REVIEW
   - Architecture requirements validation
   - Interface specifications analysis
   - Data requirements assessment
   - Quality attributes evaluation

4. COMPLIANCE AND STANDARDS
   - Industry standard adherence
   - Documentation best practices
   - Regulatory compliance considerations

Provide structured analysis with actionable recommendations.";

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt, cancellationToken: cancellationToken);
            return result.Content ?? "Unable to perform document analysis";
        }

        public async Task<string> ReviewPeerAnalysisAsync(string peerAnalysis, string originalInput, CancellationToken cancellationToken = default)
        {
            var prompt = $@"
As {AgentPersona}, review this peer analysis:

PEER ANALYSIS: {peerAnalysis}
ORIGINAL INPUT: {originalInput}

Provide constructive review focusing on:
1. Documentation completeness assessment
2. Requirements coverage evaluation
3. Specification accuracy validation
4. Additional document-specific insights
5. Recommendations for improvement

Focus on document analysis aspects that other agents might miss.";

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt, cancellationToken: cancellationToken);
            return result.Content ?? "Unable to review analysis";
        }

        [KernelFunction, Description("Analyze uploaded technical documents and extract structured information")]
        public async Task<string> AnalyzeTechnicalDocument(
            [Description("Document analysis results from Form Recognizer")] string documentAnalysis,
            [Description("Document type and context")] string documentContext)
        {
            var prompt = $@"
You are {AgentPersona}.

DOCUMENT ANALYSIS RESULTS:
{documentAnalysis}

DOCUMENT CONTEXT: {documentContext}

Perform comprehensive technical document analysis:

1. REQUIREMENTS EXTRACTION
   - Functional requirements with IDs and priorities
   - Non-functional requirements (performance, security, usability)
   - Business constraints and assumptions
   - Acceptance criteria identification

2. TECHNICAL SPECIFICATIONS REVIEW
   - Architecture requirements and patterns
   - Integration points and dependencies
   - Data structures and formats
   - Interface specifications

3. COMPLIANCE ASSESSMENT
   - Standards adherence (ISO, IEEE, industry-specific)
   - Regulatory compliance requirements
   - Documentation quality metrics

4. GAP ANALYSIS
   - Missing information identification
   - Inconsistencies and conflicts
   - Ambiguities requiring clarification

5. IMPLEMENTATION READINESS
   - Completeness score (1-100)
   - Risk assessment for implementation
   - Prioritized action items

Provide structured analysis suitable for technical stakeholders and project managers.";

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? "Unable to analyze document";
        }
    }
}
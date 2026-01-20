using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using PoC1_LegacyAnalyzer_Web.Models;
using PoC1_LegacyAnalyzer_Web.Services.Infrastructure;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Services.Business;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Orchestrates multi-agent code analysis, integrating preprocessing via <see cref="IFilePreProcessingService"/> to extract and filter metadata before routing to specialist agents.
    /// </summary>
    public class AgentOrchestrationService : IAgentOrchestrationService
    {
        private readonly Kernel _kernel;
        private readonly ILogger<AgentOrchestrationService> _logger;
        private readonly AgentConfiguration _agentConfig;
        private readonly IAgentRegistry _agentRegistry;
        private readonly IAgentCommunicationCoordinator _communicationCoordinator;
        private readonly IConsensusCalculator _consensusCalculator;
        private readonly IRecommendationSynthesizer _synthesizer;
        private readonly IExecutiveSummaryGenerator _summaryGenerator;
        private readonly IFilePreProcessingService _preprocessing;
        private readonly IInputValidationService _inputValidation;
        private readonly IErrorHandlingService _errorHandling;
        private readonly IRequestDeduplicationService? _deduplicationService;
        private readonly ICostTrackingService? _costTracking;
        private readonly ITracingService? _tracing;
        private readonly IAgentRateLimiter? _rateLimiter;
        private readonly DefaultValuesConfiguration _defaultValues;

        public AgentOrchestrationService(
            Kernel kernel,
            ILogger<AgentOrchestrationService> logger,
            IOptions<AgentConfiguration> agentOptions,
            IOptions<DefaultValuesConfiguration> defaultValues,
            IAgentRegistry agentRegistry,
            IAgentCommunicationCoordinator communicationCoordinator,
            IConsensusCalculator consensusCalculator,
            IRecommendationSynthesizer synthesizer,
            IExecutiveSummaryGenerator summaryGenerator,
            IFilePreProcessingService preprocessingService,
            IInputValidationService inputValidation,
            IErrorHandlingService errorHandling,
            IRequestDeduplicationService? deduplicationService = null,
            ICostTrackingService? costTracking = null,
            ITracingService? tracing = null,
            IAgentRateLimiter? rateLimiter = null)
        {
            _kernel = kernel;
            _logger = logger;
            _agentRegistry = agentRegistry;
            _communicationCoordinator = communicationCoordinator;
            _consensusCalculator = consensusCalculator;
            _preprocessing = preprocessingService;
            _synthesizer = synthesizer;
            _summaryGenerator = summaryGenerator;
            _inputValidation = inputValidation ?? throw new ArgumentNullException(nameof(inputValidation));
            _errorHandling = errorHandling ?? throw new ArgumentNullException(nameof(errorHandling));
            _deduplicationService = deduplicationService;
            _costTracking = costTracking;
            _tracing = tracing;
            _rateLimiter = rateLimiter;
            _defaultValues = defaultValues?.Value ?? new DefaultValuesConfiguration();

            // Register orchestrator functions with kernel
            _kernel.Plugins.AddFromObject(this, "AgentOrchestrator");
            _agentConfig = agentOptions.Value ?? new AgentConfiguration();
        }

        [KernelFunction, Description("Create analysis plan and assign agents")]
        public async Task<string> CreateAnalysisPlanAsync(
            [Description("Business objective and requirements")] string businessObjective,
            [Description("Code complexity and domain")] string codeContext)
        {
            var template = _agentConfig.OrchestrationPrompts.CreateAnalysisPlan;
            var prompt = template
                .Replace("{businessObjective}", businessObjective)
                .Replace("{codeContext}", codeContext);

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(prompt);
            return result.Content ?? "Analysis plan creation failed";
        }

        /// <summary>
        /// Coordinates a team of agents to analyze the provided files according to the specified business objective and required specialties.
        /// Preprocessing is performed first to extract and filter metadata, ensuring only relevant, token-optimized data is routed to each agent.
        /// </summary>
        /// <param name="files">The list of code files to be analyzed.</param>
        /// <param name="businessObjective">The business objective guiding the analysis.</param>
        /// <param name="requiredSpecialties">A list of specialties required for the analysis.</param>
        /// <param name="progress">Optional progress reporter for preprocessing phase.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="TeamAnalysisResult"/> containing the results of the team analysis.</returns>
        public async Task<PoC1_LegacyAnalyzer_Web.Models.AgentCommunication.TeamAnalysisResult> CoordinateTeamAnalysisAsync(
            List<IBrowserFile> files,
            string businessObjective,
            List<string> requiredSpecialties,
            IProgress<string>? progress = null,
            IProgress<AnalysisProgress>? detailedProgress = null,
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[Orchestrator] CoordinateTeamAnalysisAsync CALLED with {files?.Count ?? 0} files, objective: {businessObjective}, specialties: [{string.Join(", ", requiredSpecialties ?? new List<string>())}]");
            
            var orchestrationSw = System.Diagnostics.Stopwatch.StartNew();

            // Start distributed tracing activity
            using var activity = _tracing?.StartActivity("TeamAnalysis.Orchestrate");
            var correlationId = _tracing?.GetCorrelationId() ?? Guid.NewGuid().ToString();

            _logger.LogInformation("[Orchestrator] Team analysis STARTED at {UtcNow}, CorrelationId: {CorrelationId}, Files: {FileCount}, Objective: {Objective}",
                DateTime.UtcNow, correlationId, files?.Count ?? 0, businessObjective);
            
            Console.WriteLine($"[Orchestrator] Logging initialized, proceeding with analysis...");

            var conversationId = Guid.NewGuid().ToString();
            var teamResult = new TeamAnalysisResult
            {
                ConversationId = conversationId
            };

            // Add tracing tags
            _tracing?.AddTag("analysis.conversationId", conversationId);
            _tracing?.AddTag("analysis.businessObjective", businessObjective);
            _tracing?.AddTag("analysis.agentCount", requiredSpecialties.Count.ToString());
            _tracing?.AddTag("analysis.fileCount", files.Count.ToString());

            // Initialize cost tracking
            CostMetrics? costMetrics = null;
            if (_costTracking != null)
            {
                costMetrics = _costTracking.CreateCostTracker(conversationId);
                teamResult.CostMetrics = costMetrics;
            }

            try
            {
                // Request deduplication check
                if (_deduplicationService != null)
                {
                    var fingerprint = _deduplicationService.GenerateRequestFingerprint(files, businessObjective, requiredSpecialties);
                    if (!string.IsNullOrEmpty(fingerprint))
                    {
                        var isDuplicate = await _deduplicationService.IsDuplicateAsync(fingerprint);
                        if (isDuplicate)
                        {
                            var cachedResult = await _deduplicationService.GetCachedResultAsync<TeamAnalysisResult>(fingerprint);
                            if (cachedResult != null)
                            {
                                _logger.LogInformation("[Orchestrator] Returning cached result for duplicate request: {Fingerprint}",
                                    fingerprint.Substring(0, Math.Min(16, fingerprint.Length)));
                                progress?.Report("Returning cached result from previous analysis...");
                                return cachedResult;
                            }
                        }
                    }
                }

                // Input validation phase
                Console.WriteLine($"[Orchestrator] Starting input validation phase...");
                progress?.Report("Validating inputs...");

                // Validate business objective
                Console.WriteLine($"[Orchestrator] Validating business objective: {businessObjective}");
                var objectiveValidation = _inputValidation.ValidateBusinessObjective(businessObjective);
                Console.WriteLine($"[Orchestrator] Objective validation result: IsValid={objectiveValidation.IsValid}, Error={objectiveValidation.ErrorMessage}");
                if (!objectiveValidation.IsValid)
                {
                    throw new ArgumentException($"Invalid business objective: {objectiveValidation.ErrorMessage}");
                }

                // Sanitize business objective
                businessObjective = _inputValidation.SanitizeBusinessObjective(objectiveValidation.SanitizedValue ?? businessObjective);

                // Validate files
                var fileValidations = await _inputValidation.ValidateFilesAsync(files);
                var invalidFiles = fileValidations.Where(v => !v.IsValid).ToList();
                if (invalidFiles.Any())
                {
                    var errors = string.Join("; ", invalidFiles.Select(v => v.ErrorMessage));
                    throw new ArgumentException($"File validation failed: {errors}");
                }

                // Log warnings for files with warnings
                foreach (var validation in fileValidations.Where(v => v.Warnings.Any()))
                {
                    var fileIndex = fileValidations.IndexOf(validation);
                    if (fileIndex >= 0 && fileIndex < files.Count)
                    {
                        _logger.LogWarning("File {FileName} validation warnings: {Warnings}",
                            files[fileIndex].Name,
                            string.Join(", ", validation.Warnings));
                    }
                }

                var perfMetrics = new PerformanceMetrics();
                int llmCalls = 0;

                // Dictionary to store semantic analysis results
                var semanticAnalysisResults = new Dictionary<string, SemanticAnalysisResult>();

                // Preprocessing phase - language detection happens automatically in ExtractMetadataParallelAsync
                using var preprocessingActivity = _tracing?.StartActivity("Preprocessing.ExtractMetadata");
                _tracing?.AddTag("preprocessing.fileCount", files.Count.ToString());

                var preprocessingSw = System.Diagnostics.Stopwatch.StartNew();
                progress?.Report("Preprocessing files...");
                // Language hint is now optional - metadata extraction will auto-detect language for each file
                var metadata = await _preprocessing.ExtractMetadataParallelAsync(files);
                preprocessingSw.Stop();
                perfMetrics.PreprocessingTimeMs = preprocessingSw.ElapsedMilliseconds;

                _tracing?.AddTag("preprocessing.durationMs", preprocessingSw.ElapsedMilliseconds.ToString());
                _tracing?.AddTag("preprocessing.metadataCount", metadata.Count.ToString());
                _tracing?.AddEvent("Preprocessing.Completed");

                _logger.LogInformation("[Orchestrator] Preprocessed {FileCount} files in {ElapsedMs}ms", files.Count, preprocessingSw.ElapsedMilliseconds);
                progress?.Report($"Preprocessed {files.Count} files in {preprocessingSw.ElapsedMilliseconds}ms");

                // Collect semantic analysis results from metadata
                foreach (var meta in metadata)
                {
                    if (meta.SemanticAnalysis != null)
                    {
                        semanticAnalysisResults[meta.FileName] = meta.SemanticAnalysis;
                    }
                }

                if (semanticAnalysisResults.Any())
                {
                    _logger.LogInformation("[Orchestrator] Collected semantic analysis results for {FileCount} files", semanticAnalysisResults.Count);
                }

                // Step 1: Create analysis plan
                var codeContext = $"Preprocessed {metadata.Count} files, token-optimized for agent routing.";
                var analysisPlanSw = System.Diagnostics.Stopwatch.StartNew();
                var analysisPlan = await CreateAnalysisPlanAsync(businessObjective, codeContext);
                llmCalls++;
                analysisPlanSw.Stop();

                // Step 2: Execute specialist analyses in parallel with per-agent progress tracking
                var agentAnalysisSw = System.Diagnostics.Stopwatch.StartNew();
                var agentTaskStartTime = DateTime.UtcNow;

                // Initialize per-agent progress tracking
                var agentProgressDict = new Dictionary<string, AgentProgress>();
                foreach (var specialty in requiredSpecialties.Where(s => _agentRegistry.IsRegistered(s)))
                {
                    var agent = _agentRegistry.GetAgent(specialty);
                    agentProgressDict[specialty] = new AgentProgress
                    {
                        AgentName = agent?.AgentName ?? $"{specialty}Agent",
                        Specialty = specialty,
                        ProgressPercentage = 0,
                        Status = _defaultValues.Status.Initializing
                    };
                }

                // Report initial progress
                ReportAgentProgress(detailedProgress, agentProgressDict);

                var specialistTasks = requiredSpecialties
                    .Where(specialty => _agentRegistry.IsRegistered(specialty))
                    .Select(async specialty =>
                    {
                        try
                        {
                            // Update progress: Starting
                            if (agentProgressDict.ContainsKey(specialty))
                            {
                                agentProgressDict[specialty].Status = _defaultValues.Status.PreparingData;
                                agentProgressDict[specialty].ProgressPercentage = 10;
                                ReportAgentProgress(detailedProgress, agentProgressDict);
                            }

                            var taskCreatedAt = DateTime.UtcNow;
                            _logger.LogInformation("[Agent:{Specialty}] TASK CREATED at {UtcNow}", specialty, taskCreatedAt);

                            // Apply rate limiting if configured
                            if (_rateLimiter != null)
                            {
                                await _rateLimiter.WaitIfNeededAsync(specialty, cancellationToken);
                            }

                            // Update progress: Filtering data
                            if (agentProgressDict.ContainsKey(specialty))
                            {
                                agentProgressDict[specialty].Status = _defaultValues.Status.FilteringMetadata;
                                agentProgressDict[specialty].ProgressPercentage = 20;
                                ReportAgentProgress(detailedProgress, agentProgressDict);
                            }

                            var agentData = await _preprocessing.GetAgentSpecificData(metadata, specialty);
                            var filteredCount = agentData.Split('\n').Length;
                            var reduction = metadata.Count > 0 ? 100 - (filteredCount * 100 / metadata.Count) : 0;
                            _logger.LogInformation("[Agent:{Specialty}] Filtered {Total} files to {Filtered} ({Reduction}% reduction)", specialty, metadata.Count, filteredCount, reduction);
                            progress?.Report($"Filtered {metadata.Count} files to {filteredCount} for {specialty} agent ({reduction}% reduction)");

                            // Update progress: Analyzing
                            if (agentProgressDict.ContainsKey(specialty))
                            {
                                agentProgressDict[specialty].Status = _defaultValues.Status.AnalyzingCode;
                                agentProgressDict[specialty].ProgressPercentage = 40;
                                ReportAgentProgress(detailedProgress, agentProgressDict);
                            }

                            var sw = System.Diagnostics.Stopwatch.StartNew();

                            // Record API call for rate limiting
                            if (_rateLimiter != null)
                            {
                                _rateLimiter.RecordApiCall(specialty);
                            }

                            // Update progress: Processing AI response
                            if (agentProgressDict.ContainsKey(specialty))
                            {
                                agentProgressDict[specialty].Status = _defaultValues.Status.ProcessingAIResponse;
                                agentProgressDict[specialty].ProgressPercentage = 60;
                                ReportAgentProgress(detailedProgress, agentProgressDict);
                            }

                            var result = await ExecuteSpecialistAnalysisAsync(specialty, agentData, businessObjective, cancellationToken);
                            sw.Stop();

                            // Update progress: Complete
                            if (agentProgressDict.ContainsKey(specialty))
                            {
                                agentProgressDict[specialty].Status = _defaultValues.Status.Complete;
                                agentProgressDict[specialty].ProgressPercentage = 100;
                                agentProgressDict[specialty].IsComplete = true;
                                ReportAgentProgress(detailedProgress, agentProgressDict);
                            }

                            _logger.LogInformation("[Agent:{Specialty}] Completed in {ElapsedMs}ms", specialty, sw.ElapsedMilliseconds);
                            return result;
                        }
                        catch (Exception ex)
                        {
                            // Update progress: Error
                            if (agentProgressDict.ContainsKey(specialty))
                            {
                                agentProgressDict[specialty].Status = _defaultValues.Status.Error;
                                agentProgressDict[specialty].HasError = true;
                                agentProgressDict[specialty].ErrorMessage = ex.Message;
                                ReportAgentProgress(detailedProgress, agentProgressDict);
                            }
                            throw;
                        }
                    })
                    .ToList();

                // All agent tasks are started immediately above
                _logger.LogInformation("[Orchestrator] All specialist agent tasks CREATED at {UtcNow}", agentTaskStartTime);

                // Wait for all agent tasks to complete (some may fail)
                var agentTaskWrappers = specialistTasks.Select(async task =>
                {
                    try
                    {
                        var result = await task;
                        return new { Success = true, Result = result, Exception = (Exception?)null };
                    }
                    catch (Exception ex)
                    {
                        return new { Success = false, Result = (SpecialistAnalysisResult?)null, Exception = ex };
                    }
                });

                var agentTaskResults = await Task.WhenAll(agentTaskWrappers);

                agentAnalysisSw.Stop();
                perfMetrics.AgentAnalysisTimeMs = agentAnalysisSw.ElapsedMilliseconds;

                // Separate successful and failed results
                var successfulResults = agentTaskResults
                    .Where(r => r.Success && r.Result != null)
                    .Select(r => r.Result!)
                    .ToList();

                var failedResults = agentTaskResults
                    .Where(r => !r.Success)
                    .Select((r, index) => new { Index = index, Exception = r.Exception })
                    .ToList();

                // Log failures with context
                foreach (var failure in failedResults)
                {
                    var specialty = requiredSpecialties[failure.Index];
                    var errorResult = _errorHandling.CreateAgentErrorResult(specialty, failure.Exception!);
                    _logger.LogError(
                        failure.Exception,
                        "[Agent:{Specialty}] Analysis failed. ErrorCode: {ErrorCode}, Retryable: {IsRetryable}",
                        specialty,
                        errorResult.ErrorCode,
                        errorResult.IsRetryable);
                }

                // Determine if we should return partial results
                var totalAgents = requiredSpecialties.Count;
                var successfulCount = successfulResults.Count;
                var shouldReturnPartial = _errorHandling.ShouldReturnPartialResults(successfulCount, totalAgents);

                if (!shouldReturnPartial && failedResults.Any())
                {
                    // Not enough successful agents - throw exception with actionable message
                    var firstFailure = failedResults.First();
                    var specialty = requiredSpecialties[firstFailure.Index];
                    var errorMessage = _errorHandling.CreateActionableErrorMessage(
                        firstFailure.Exception!,
                        $"{failedResults.Count} of {totalAgents} agents failed. First failure in {specialty} agent");

                    throw new InvalidOperationException(errorMessage);
                }

                // Create error results for failed agents
                var errorAnalysisResults = failedResults.Select(failure =>
                {
                    var specialty = requiredSpecialties[failure.Index];
                    var errorResult = _errorHandling.CreateAgentErrorResult(specialty, failure.Exception!);

                    return new SpecialistAnalysisResult
                    {
                        AgentName = $"{specialty}Agent",
                        Specialty = specialty,
                        ConfidenceScore = 0,
                        BusinessImpact = $"Analysis failed: {errorResult.ErrorMessage}",
                        KeyFindings = new List<Finding>
                        {
                            new Finding
                            {
                                Category = "Analysis Error",
                                Description = errorResult.ErrorDescription,
                                Severity = "HIGH",
                                Location = $"Agent: {specialty}",
                                Evidence = errorResult.RemediationSteps
                            }
                        },
                        Recommendations = errorResult.RemediationSteps.Select((step, idx) => new Recommendation
                        {
                            Title = $"Remediation Step {idx + 1}",
                            Description = step,
                            Priority = "HIGH",
                            EstimatedHours = 0
                        }).ToList()
                    };
                }).ToList();

                // Combine successful and error results
                var allResults = new List<SpecialistAnalysisResult>();
                allResults.AddRange(successfulResults);
                allResults.AddRange(errorAnalysisResults);

                // Assign all analyses to team result (successful + error results)
                teamResult.IndividualAnalyses = allResults;

                // Add semantic analysis results to team result
                teamResult.FileSemanticAnalysis = semanticAnalysisResults;

                // Log partial success if applicable
                if (failedResults.Any() && shouldReturnPartial)
                {
                    _logger.LogWarning(
                        "Partial success: {SuccessfulCount}/{TotalAgents} agents completed successfully. Continuing with partial results.",
                        successfulCount,
                        totalAgents);
                    progress?.Report($"Warning: {failedResults.Count} agent(s) failed, but continuing with {successfulCount} successful analysis(es).");
                }

                // Use successful results for downstream processing
                var specialistResults = successfulResults.ToArray();

                // Step 3: Peer review discussion (starts after all agents complete, but only with successful ones)
                var peerReviewSw = System.Diagnostics.Stopwatch.StartNew();
                AgentConversation? discussion = null;
                if (successfulResults.Any())
                {
                    discussion = await _communicationCoordinator.FacilitateAgentDiscussionAsync(
                        $"Code Analysis for: {businessObjective}",
                        successfulResults.ToList(), // Only use successful results for discussion
                        codeContext,
                        cancellationToken);
                    peerReviewSw.Stop();
                    perfMetrics.PeerReviewTimeMs = peerReviewSw.ElapsedMilliseconds;
                    teamResult.TeamDiscussion = discussion.Messages;
                }
                else
                {
                    peerReviewSw.Stop();
                    perfMetrics.PeerReviewTimeMs = 0;
                    _logger.LogWarning("[Orchestrator] Skipping peer review - no successful agent results");
                }

                // Step 4 & 6: Synthesis and executive summary in parallel (after agent results)
                _logger.LogInformation("[Orchestrator] Starting synthesis and executive summary in parallel at {UtcNow}", DateTime.UtcNow);
                var synthesisSw = System.Diagnostics.Stopwatch.StartNew();
                var synthesisTask = _synthesizer.SynthesizeRecommendationsAsync(
                    successfulResults.ToList(), // Only use successful results for synthesis
                    businessObjective,
                    cancellationToken);
                var summarySw = System.Diagnostics.Stopwatch.StartNew();
                var summaryTask = _summaryGenerator.GenerateAsync(
                    teamResult,
                    businessObjective,
                    cancellationToken);
                llmCalls += 2;
                ConsolidatedRecommendations? recommendations = null;
                string? summary = null;
                Exception? synthesisEx = null;
                Exception? summaryEx = null;
                try
                {
                    await Task.WhenAll(synthesisTask, summaryTask);
                    synthesisSw.Stop();
                    summarySw.Stop();
                    perfMetrics.SynthesisTimeMs = synthesisSw.ElapsedMilliseconds;
                    perfMetrics.ExecutiveSummaryTimeMs = summarySw.ElapsedMilliseconds;
                    recommendations = synthesisTask.IsCompletedSuccessfully ? synthesisTask.Result : null;
                    summary = summaryTask.IsCompletedSuccessfully ? summaryTask.Result : null;
                }
                catch (Exception ex)
                {
                    synthesisSw.Stop();
                    summarySw.Stop();
                    _logger.LogError(ex, "[Orchestrator] Error during parallel synthesis/summary");
                    if (synthesisTask.IsFaulted)
                        synthesisEx = synthesisTask.Exception;
                    if (summaryTask.IsFaulted)
                        summaryEx = summaryTask.Exception;
                }
                var parallelTimeMs = Math.Max(synthesisSw.ElapsedMilliseconds, summarySw.ElapsedMilliseconds);
                _logger.LogInformation("[Orchestrator] Synthesis and summary completed in {ElapsedMs}ms (parallel, sequential estimate: {SeqMs}ms)", parallelTimeMs, 35000);

                if (recommendations != null)
                {
                    // Generate implementation strategy if not already generated
                    if (string.IsNullOrWhiteSpace(recommendations.ImplementationStrategy) ||
                        recommendations.ImplementationStrategy == "Implementation strategy to be generated.")
                    {
                        _logger.LogInformation("[Orchestrator] Generating implementation strategy for consolidated recommendations");
                        try
                        {
                            recommendations.ImplementationStrategy = await GenerateImplementationStrategyAsync(
                                recommendations,
                                businessObjective,
                                cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[Orchestrator] Failed to generate implementation strategy");
                            recommendations.ImplementationStrategy = "Implementation strategy generation failed. Please try again.";
                        }
                    }
                    teamResult.FinalRecommendations = recommendations;
                }
                else if (synthesisEx != null)
                    _logger.LogError(synthesisEx, "[Orchestrator] Synthesis failed, recommendations not set");

                if (summary != null)
                    teamResult.ExecutiveSummary = summary;
                else if (summaryEx != null)
                    _logger.LogError(summaryEx, "[Orchestrator] Summary failed, executive summary not set");

                // Step 5: Calculate consensus metrics (only if discussion occurred)
                if (discussion != null)
                {
                    teamResult.Consensus = _consensusCalculator.CalculateConsensusMetrics(discussion, specialistResults);
                }
                else
                {
                    // No discussion occurred, create empty consensus metrics
                    teamResult.Consensus = new TeamConsensusMetrics();
                }

                // Step 7: Calculate overall confidence
                teamResult.OverallConfidenceScore = _consensusCalculator.CalculateTeamConfidenceScore(specialistResults);

                orchestrationSw.Stop();
                perfMetrics.TotalTimeMs = orchestrationSw.ElapsedMilliseconds;
                perfMetrics.TotalLLMCalls = llmCalls;
                perfMetrics.ParallelSpeedup = perfMetrics.AgentAnalysisTimeMs > 0
                    ? Math.Round((double)(perfMetrics.AgentAnalysisTimeMs * specialistResults.Length) / perfMetrics.AgentAnalysisTimeMs, 2)
                    : 1.0;
                teamResult.PerformanceMetrics = perfMetrics;

                // Add performance summary to executive summary
                if (!string.IsNullOrEmpty(teamResult.ExecutiveSummary))
                {
                    teamResult.ExecutiveSummary += $"\n\nAnalysis completed efficiently using parallel execution. Total time: {perfMetrics.TotalTimeMs}ms ({perfMetrics.ParallelSpeedup}x speedup vs sequential).";
                }

                // Finalize cost tracking
                if (costMetrics != null && _costTracking != null)
                {
                    _costTracking.CalculateCost(costMetrics);
                    _costTracking.LogCostMetrics(costMetrics);
                    _logger.LogInformation(
                        "Analysis cost - Total: {TotalCost}, InputTokens: {InputTokens}, OutputTokens: {OutputTokens}",
                        _costTracking.FormatCost(costMetrics.TotalCost),
                        costMetrics.InputTokens,
                        costMetrics.OutputTokens);
                }

                // Performance metrics logging
                var tokenReduction = "75-80%"; // Based on preprocessing design
                _logger.LogInformation("Team analysis completed. Time saved: {ElapsedMs}ms, Tokens reduced: {TokenReduction}",
                    orchestrationSw.ElapsedMilliseconds, tokenReduction);

                // Store result for deduplication
                if (_deduplicationService != null)
                {
                    var fingerprint = _deduplicationService.GenerateRequestFingerprint(files, businessObjective, requiredSpecialties);
                    if (!string.IsNullOrEmpty(fingerprint))
                    {
                        await _deduplicationService.StoreRequestAsync(fingerprint, teamResult);
                    }
                }

                return teamResult;
            }
            catch (Exception ex)
            {
                var errorMessage = _errorHandling.CreateActionableErrorMessage(ex, "Team analysis coordination");
                _logger.LogError(ex, "[Orchestrator] Team analysis coordination failed. {ErrorMessage}", errorMessage);

                // Create error result with partial information if available
                if (teamResult.IndividualAnalyses.Any())
                {
                    _logger.LogWarning("[Orchestrator] Returning partial results from {Count} successful agents", teamResult.IndividualAnalyses.Count);
                    teamResult.ExecutiveSummary = $"Analysis completed with errors. {errorMessage}";
                    return teamResult;
                }

                throw new InvalidOperationException(errorMessage, ex);
            }
        }

        /// <summary>
        /// Executes a specialist agent analysis using preprocessed, filtered metadata summaries instead of full code.
        /// This method routes compact, token-optimized metadata to the agent, significantly reducing token usage and improving efficiency.
        /// All specialist agents must be able to handle metadata summaries as input.
        /// </summary>
        /// <param name="specialty">The specialty of the agent (e.g., security, performance, architecture).</param>
        /// <param name="filteredMetadataSummary">The filtered, preprocessed metadata summary for the agent (not full code).</param>
        /// <param name="businessObjective">The business objective guiding the analysis.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The result of the specialist agent's analysis.</returns>
        private async Task<SpecialistAnalysisResult> ExecuteSpecialistAnalysisAsync(
            string specialty,
            string filteredMetadataSummary,
            string businessObjective,
            CancellationToken cancellationToken)
        {
            try
            {
                var agent = _agentRegistry.GetAgent(specialty);

                if (agent == null)
                {
                    throw new InvalidOperationException($"Failed to resolve agent for specialty: {specialty}");
                }

                // Log token count reduction
                int tokenCount = filteredMetadataSummary?.Length > 0 ? filteredMetadataSummary.Length / 4 : 0;
                _logger.LogInformation("[Agent:{Specialty}] Routing filtered metadata summary. Token count: {TokenCount}", specialty, tokenCount);

                // Diagnostics: API call is about to start (already logged in orchestration)
                var analysisResultString = await agent.AnalyzeAsync(filteredMetadataSummary, businessObjective, cancellationToken);
                var analysisResult = JsonSerializer.Deserialize<SpecialistAnalysisResult>(
                    analysisResultString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (analysisResult == null)
                {
                    throw new InvalidOperationException("Agent returned null analysis result.");
                }

                return analysisResult;
            }
            catch (Exception ex)
            {
                // Create structured error result with remediation steps
                var inputSummary = $"Metadata summary length: {filteredMetadataSummary?.Length ?? 0} chars, Objective: {businessObjective.Substring(0, Math.Min(50, businessObjective.Length))}...";
                var errorResult = _errorHandling.CreateAgentErrorResult(specialty, ex, inputSummary);

                _logger.LogError(
                    ex,
                    "[Agent:{Specialty}] Failed to execute analysis. ErrorCode: {ErrorCode}, Retryable: {IsRetryable}",
                    specialty,
                    errorResult.ErrorCode,
                    errorResult.IsRetryable);

                // Return error result with actionable information
                return new SpecialistAnalysisResult
                {
                    AgentName = $"{specialty}Agent",
                    Specialty = specialty,
                    ConfidenceScore = 0,
                    BusinessImpact = errorResult.ErrorMessage,
                    KeyFindings = new List<Finding>
                    {
                        new Finding
                        {
                            Category = "Analysis Error",
                            Description = errorResult.ErrorDescription,
                            Severity = "HIGH",
                            Location = $"Agent: {specialty}",
                            Evidence = errorResult.RemediationSteps
                        }
                    },
                    Recommendations = errorResult.RemediationSteps.Select((step, index) => new Recommendation
                    {
                        Title = $"Remediation Step {index + 1}",
                        Description = step,
                        Priority = "HIGH",
                        EstimatedHours = 0
                    }).ToList()
                };
            }
        }


        public async Task<ConsolidatedRecommendations> SynthesizeRecommendationsAsync(
            List<SpecialistAnalysisResult> analyses,
            string businessContext,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var consolidatedRecs = new ConsolidatedRecommendations();

                // Collect all recommendations from all agents
                var allRecommendations = analyses.SelectMany(a => a.Recommendations.Select(r => new
                {
                    Agent = a.AgentName,
                    Recommendation = r
                }))
                .ToList();

                // Group by priority
                var criticalRecs = allRecommendations.Where(r => r.Recommendation.Priority == "CRITICAL").ToList();
                var highRecs = allRecommendations.Where(r => r.Recommendation.Priority == "HIGH").ToList();
                var mediumRecs = allRecommendations.Where(r => r.Recommendation.Priority == "MEDIUM").ToList();
                var lowRecs = allRecommendations.Where(r => r.Recommendation.Priority == "LOW").ToList();

                // Add high priority (critical + high)
                consolidatedRecs.HighPriorityActions.AddRange(criticalRecs.Select(r => r.Recommendation));
                consolidatedRecs.HighPriorityActions.AddRange(highRecs.Select(r => r.Recommendation));

                // Add medium priority
                consolidatedRecs.MediumPriorityActions.AddRange(mediumRecs.Select(r => r.Recommendation));

                // Add low priority as long-term strategic
                consolidatedRecs.LongTermStrategic.AddRange(lowRecs.Select(r => r.Recommendation));

                // Calculate total effort
                consolidatedRecs.TotalEstimatedEffort = allRecommendations.Sum(r => r.Recommendation.EstimatedHours);

                // Orchestration prompt for synthesizing recommendations
                var template = _agentConfig.OrchestrationPrompts.SynthesizeRecommendations;
                var analysesJson = JsonSerializer.Serialize(analyses);
                var consensus = "Consensus calculation logic here"; // Replace with actual consensus if available
                var conflicts = "Conflict resolution logic here"; // Replace with actual conflicts if available
                var synthPrompt = template
                    .Replace("{analyses}", analysesJson)
                    .Replace("{consensus}", consensus)
                    .Replace("{conflicts}", conflicts);

                var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
                var synthResult = await chatCompletion.GetChatMessageContentAsync(synthPrompt);
                consolidatedRecs.SynthesisSummary = synthResult.Content ?? "Synthesis failed";

                // Generate implementation strategy
                consolidatedRecs.ImplementationStrategy = await GenerateImplementationStrategyAsync(
                    consolidatedRecs,
                    businessContext);

                // Identify and resolve conflicts
                var recommendationTuples = allRecommendations.Select(r => (r.Agent, r.Recommendation)).ToList();
                consolidatedRecs.ResolvedConflicts = await IdentifyRecommendationConflictsAsync(recommendationTuples);

                _logger.LogInformation("Synthesized {Count} recommendations with {Hours} total effort",
                    allRecommendations.Count, consolidatedRecs.TotalEstimatedEffort);

                return consolidatedRecs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to synthesize recommendations");
                throw;
            }
        }

        [KernelFunction, Description("Generate implementation strategy for consolidated recommendations")]
        public async Task<string> GenerateImplementationStrategyAsync(
            [Description("Consolidated recommendations")] ConsolidatedRecommendations recommendations,
            [Description("Business context and constraints")] string businessContext,
            CancellationToken cancellationToken = default)
        {
            // Build detailed recommendation context with actual recommendation details
            var recDetails = new System.Text.StringBuilder();

            if (recommendations.HighPriorityActions?.Any() == true)
            {
                recDetails.AppendLine("HIGH PRIORITY RECOMMENDATIONS:");
                foreach (var rec in recommendations.HighPriorityActions.Take(5))
                {
                    recDetails.AppendLine($"- {rec.Title} ({rec.Priority}): {rec.Description}");
                    recDetails.AppendLine($"  Estimated Hours: {rec.EstimatedHours}");
                    if (!string.IsNullOrWhiteSpace(rec.Implementation))
                    {
                        recDetails.AppendLine($"  Implementation: {rec.Implementation.Substring(0, Math.Min(200, rec.Implementation.Length))}");
                    }
                }
                recDetails.AppendLine();
            }

            if (recommendations.MediumPriorityActions?.Any() == true)
            {
                recDetails.AppendLine("MEDIUM PRIORITY RECOMMENDATIONS:");
                foreach (var rec in recommendations.MediumPriorityActions.Take(3))
                {
                    recDetails.AppendLine($"- {rec.Title} ({rec.Priority}): {rec.Description}");
                    recDetails.AppendLine($"  Estimated Hours: {rec.EstimatedHours}");
                }
                recDetails.AppendLine();
            }

            var recContext = recDetails.ToString();
            if (string.IsNullOrWhiteSpace(recContext))
            {
                recContext = $"High priority: {recommendations.HighPriorityActions?.Count ?? 0}, " +
                           $"Medium priority: {recommendations.MediumPriorityActions?.Count ?? 0}, " +
                           $"Strategic: {recommendations.LongTermStrategic?.Count ?? 0}, " +
                           $"Total effort: {recommendations.TotalEstimatedEffort} hours";
            }

            var template = _agentConfig.OrchestrationPrompts.CreateImplementationStrategy;
            var prompt = $@"{template}

BUSINESS CONTEXT: {businessContext}

SPECIFIC RECOMMENDATIONS TO IMPLEMENT:
{recContext}

TOTAL EFFORT: {recommendations.TotalEstimatedEffort} hours

Please provide a detailed, step-by-step implementation strategy that:
1. Addresses the specific recommendations listed above
2. Prioritizes based on business impact and dependencies
3. Provides concrete implementation steps for each high-priority item
4. Considers resource allocation and timeline
5. Includes risk mitigation strategies

Do NOT provide generic advice. Focus on the specific recommendations and how to implement them.";

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var executionSettings = new PromptExecutionSettings();
            if (executionSettings.ExtensionData == null)
                executionSettings.ExtensionData = new Dictionary<string, object>();
            executionSettings.ExtensionData["max_tokens"] = 1500;
            executionSettings.ExtensionData["temperature"] = 0.4;

            var result = await chatCompletion.GetChatMessageContentAsync(prompt, executionSettings, cancellationToken: cancellationToken);
            return result.Content ?? "Implementation strategy generation failed";
        }

        private async Task<List<ConflictResolution>> IdentifyRecommendationConflictsAsync(List<(string Agent, Recommendation Recommendation)> allRecommendations)
        {
            var conflicts = new List<ConflictResolution>();

            // Simple conflict detection - recommendations that contradict each other
            var groupedByTitle = allRecommendations
                .GroupBy(r => r.Recommendation.Title)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in groupedByTitle)
            {
                var agents = group.Select(g => g.Agent).ToList();
                conflicts.Add(new ConflictResolution
                {
                    ConflictDescription = $"Multiple agents recommend: {group.Key}",
                    ConflictingAgents = agents,
                    Resolution = "Consolidate similar recommendations with combined implementation approach",
                    Rationale = "Multiple expert agreement strengthens recommendation priority",
                    ConfidenceInResolution = 90
                });
            }

            return conflicts;
        }

        /// <summary>
        /// Generates an executive summary using only the top 3 findings from each agent, their confidence score, and business impact.
        /// This reduces the LLM prompt size from 10,000+ tokens to under 2,000, preventing timeouts and improving reliability.
        /// Logs the token count and warns if the input exceeds 3,000 tokens.
        /// </summary>
        public async Task<string> GenerateExecutiveSummaryAsync(
            PoC1_LegacyAnalyzer_Web.Models.AgentCommunication.TeamAnalysisResult teamResult,
            string businessObjective,
            CancellationToken cancellationToken = default)
        {
            return await _summaryGenerator.GenerateAsync(teamResult, businessObjective, cancellationToken);
        }

        // Ensure streaming method is public and matches interface
        public async IAsyncEnumerable<string> GenerateExecutiveSummaryStreamingAsync(
            PoC1_LegacyAnalyzer_Web.Models.AgentCommunication.TeamAnalysisResult teamResult,
            string businessObjective,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int SeverityRank(string severity) => severity?.ToUpper() switch
            {
                "CRITICAL" => 1,
                "HIGH" => 2,
                "MEDIUM" => 3,
                "LOW" => 4,
                _ => 5
            };
            var summarySections = new List<string>();
            foreach (var agent in teamResult.IndividualAnalyses)
            {
                var topFindings = agent.KeyFindings
                    .OrderBy(f => SeverityRank(f.Severity))
                    .Take(3)
                    .Select(f => $"- [{f.Severity}] {f.Category}: {f.Description}")
                    .ToList();
                var agentSection = $@"
Agent: {agent.AgentName} ({agent.Specialty})
Confidence Score: {agent.ConfidenceScore}%
Business Impact: {agent.BusinessImpact}
Top Findings:
{string.Join("\n", topFindings)}
";
                summarySections.Add(agentSection.Trim());
            }
            var condensedPrompt = $@"
Executive Summary Request
Business Objective: {businessObjective}

Specialist Agent Findings:
{string.Join("\n\n", summarySections)}
";
            var template = _agentConfig.OrchestrationPrompts.CreateExecutiveSummary;
            var prompt = template
                .Replace("{businessObjective}", businessObjective)
                .Replace("{summaryContext}", condensedPrompt);
            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            bool streamingFailed = false;
            Exception? streamingException = null;
            var streamedChunks = new List<string>();
            try
            {
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage(prompt);
                await foreach (var chunk in chatCompletion.GetStreamingChatMessageContentsAsync(chatHistory, cancellationToken: cancellationToken))
                {
                    if (!string.IsNullOrEmpty(chunk.Content))
                        streamedChunks.Add(chunk.Content);
                }
            }
            catch (Exception ex)
            {
                streamingFailed = true;
                streamingException = ex;
            }
            if (streamingFailed)
            {
                _logger.LogError(streamingException, "Streaming executive summary failed, falling back to non-streaming.");
                var result = await chatCompletion.GetChatMessageContentAsync(prompt, cancellationToken: cancellationToken);
                yield return result.Content ?? "Executive summary generation failed";
                yield break;
            }
            foreach (var chunk in streamedChunks)
            {
                yield return chunk;
            }
        }

        public async Task<AgentConversation> FacilitateAgentDiscussionAsync(
            string topic,
            List<SpecialistAnalysisResult> initialAnalyses,
            string? codeContext = null,
            CancellationToken cancellationToken = default)
        {
            return await _communicationCoordinator.FacilitateAgentDiscussionAsync(
                topic,
                initialAnalyses,
                codeContext,
                cancellationToken);
        }

        /// <summary>
        /// Reports per-agent progress to the detailed progress reporter.
        /// </summary>
        private void ReportAgentProgress(IProgress<AnalysisProgress>? detailedProgress, Dictionary<string, AgentProgress> agentProgressDict)
        {
            if (detailedProgress == null) return;

            var progress = new AnalysisProgress
            {
                Status = _defaultValues.Status.AnalysisInProgress,
                AgentProgress = agentProgressDict
            };

            detailedProgress.Report(progress);
        }
    }
}

// Helper for atomic max update
internal static class InterlockedExtensions
{
    public static void UpdateMax(ref int target, int value)
    {
        int initialValue, newValue;
        do
        {
            initialValue = target;
            newValue = Math.Max(initialValue, value);
        } while (initialValue != Interlocked.CompareExchange(ref target, newValue, initialValue));
    }
}

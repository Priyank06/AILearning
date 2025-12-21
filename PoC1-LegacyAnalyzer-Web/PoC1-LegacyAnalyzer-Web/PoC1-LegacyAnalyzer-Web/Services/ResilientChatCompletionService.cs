using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PoC1_LegacyAnalyzer_Web.Models;
using Polly;
using Polly.CircuitBreaker;
using Azure;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Resilient wrapper for IChatCompletionService that adds retry policies and circuit breaker.
    /// </summary>
    public class ResilientChatCompletionService : IChatCompletionService
    {
        private readonly IChatCompletionService _innerService;
        private readonly ILogger<ResilientChatCompletionService> _logger;
        private readonly RetryPolicyConfiguration _config;
        private readonly IAsyncPolicy<IReadOnlyList<ChatMessageContent>> _retryPolicy;
        private readonly IAsyncPolicy<IReadOnlyList<ChatMessageContent>> _circuitBreakerPolicy;
        private readonly IAsyncPolicy<IReadOnlyList<ChatMessageContent>> _combinedPolicy;

        public ResilientChatCompletionService(
            IChatCompletionService innerService,
            ILogger<ResilientChatCompletionService> logger,
            IOptions<RetryPolicyConfiguration> retryConfig)
        {
            _innerService = innerService ?? throw new ArgumentNullException(nameof(innerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = retryConfig?.Value ?? new RetryPolicyConfiguration();

            // Build retry policy with exponential backoff
            _retryPolicy = Policy<IReadOnlyList<ChatMessageContent>>
                .Handle<RequestFailedException>(ex => ShouldRetry(ex))
                .Or<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    retryCount: _config.MaxRetryAttempts,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(
                        Math.Min(
                            _config.InitialDelaySeconds * Math.Pow(2, retryAttempt - 1),
                            _config.MaxDelaySeconds
                        )
                    ),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            "Retrying Azure OpenAI call. Attempt {RetryCount}/{MaxRetries} after {DelaySeconds}s. Exception: {ExceptionType}",
                            retryCount,
                            _config.MaxRetryAttempts,
                            timespan.TotalSeconds,
                            outcome.Exception?.GetType().Name ?? "Unknown");
                    });

            // Build circuit breaker policy
            _circuitBreakerPolicy = Policy<IReadOnlyList<ChatMessageContent>>
                .Handle<RequestFailedException>(ex => ShouldRetry(ex))
                .Or<HttpRequestException>()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: _config.CircuitBreakerFailureThreshold,
                    durationOfBreak: TimeSpan.FromSeconds(_config.CircuitBreakerDurationSeconds),
                    onBreak: (exception, duration) =>
                    {
                        _logger.LogError(
                            "Circuit breaker opened for {DurationSeconds}s due to {ExceptionType}. Azure OpenAI service may be experiencing issues.",
                            duration.TotalSeconds,
                            exception?.GetType().Name ?? "Unknown");
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Circuit breaker reset. Azure OpenAI service appears to be recovering.");
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogInformation("Circuit breaker half-open. Testing Azure OpenAI service availability.");
                    });

            // Combine policies: circuit breaker wraps retry policy
            _combinedPolicy = Policy.WrapAsync(_circuitBreakerPolicy, _retryPolicy);
        }

        /// <summary>
        /// Determines if an exception should trigger a retry.
        /// </summary>
        private bool ShouldRetry(RequestFailedException ex)
        {
            // RequestFailedException.Status is int (non-nullable)
            var status = ex.Status;

            // Don't retry on non-retryable status codes
            if (_config.NonRetryableStatusCodes.Contains(status))
                return false;

            // Retry on retryable status codes
            if (_config.RetryableStatusCodes.Contains(status))
                return true;

            // Default: retry on 5xx errors, don't retry on 4xx (except 429)
            return status >= 500;
        }

        public IReadOnlyDictionary<string, object?> Attributes => _innerService.Attributes;

        public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _combinedPolicy.ExecuteAsync(async () =>
                {
                    return await _innerService.GetChatMessageContentsAsync(
                        chatHistory,
                        executionSettings,
                        kernel,
                        cancellationToken);
                });
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "Circuit breaker is open. Azure OpenAI service is unavailable.");
                throw new InvalidOperationException(
                    "AI service is temporarily unavailable due to repeated failures. Please try again in a few moments.",
                    ex);
            }
            catch (RequestFailedException ex) when (!ShouldRetry(ex))
            {
                // Non-retryable errors (e.g., 401, 403, 404) - don't wrap, let them bubble up
                _logger.LogError(ex, "Non-retryable error from Azure OpenAI. Status: {StatusCode}", ex.Status);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get chat message contents after all retries.");
                throw;
            }
        }

        public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
            string prompt,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _combinedPolicy.ExecuteAsync(async () =>
                {
                    return await _innerService.GetChatMessageContentsAsync(
                        prompt,
                        executionSettings,
                        kernel,
                        cancellationToken);
                });
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "Circuit breaker is open. Azure OpenAI service is unavailable.");
                throw new InvalidOperationException(
                    "AI service is temporarily unavailable due to repeated failures. Please try again in a few moments.",
                    ex);
            }
            catch (RequestFailedException ex) when (!ShouldRetry(ex))
            {
                _logger.LogError(ex, "Non-retryable error from Azure OpenAI. Status: {StatusCode}", ex.Status);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get chat message contents after all retries.");
                throw;
            }
        }

        public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            // For streaming, we can't easily wrap with Polly retry since it's an async enumerable.
            // We'll pass through to inner service and let it handle errors.
            // The circuit breaker will still protect against repeated failures.
            try
            {
                return _innerService.GetStreamingChatMessageContentsAsync(
                    chatHistory,
                    executionSettings,
                    kernel,
                    cancellationToken);
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "Circuit breaker is open. Azure OpenAI service is unavailable.");
                throw new InvalidOperationException(
                    "AI service is temporarily unavailable due to repeated failures. Please try again in a few moments.",
                    ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start streaming chat message contents.");
                throw;
            }
        }

        public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
            string prompt,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return _innerService.GetStreamingChatMessageContentsAsync(
                    prompt,
                    executionSettings,
                    kernel,
                    cancellationToken);
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "Circuit breaker is open. Azure OpenAI service is unavailable.");
                throw new InvalidOperationException(
                    "AI service is temporarily unavailable due to repeated failures. Please try again in a few moments.",
                    ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start streaming chat message contents.");
                throw;
            }
        }
    }
}


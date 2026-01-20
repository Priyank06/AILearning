using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PoC1_LegacyAnalyzer_Web.Models;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;

namespace PoC1_LegacyAnalyzer_Web.Services.Infrastructure
{
    /// <summary>
    /// Service for handling errors and creating actionable error messages.
    /// </summary>
    public interface IErrorHandlingService
    {
        /// <summary>
        /// Creates an error result for a failed agent with remediation steps.
        /// </summary>
        AgentErrorResult CreateAgentErrorResult(string specialty, Exception exception, string? inputSummary = null);

        /// <summary>
        /// Determines if partial results should be returned based on successful agents.
        /// </summary>
        bool ShouldReturnPartialResults(int successfulAgents, int totalAgents);

        /// <summary>
        /// Creates an actionable error message with remediation steps.
        /// </summary>
        string CreateActionableErrorMessage(Exception exception, string context);

        /// <summary>
        /// Gets remediation steps for a specific error type.
        /// </summary>
        List<string> GetRemediationSteps(Exception exception);
    }

    /// <summary>
    /// Implementation of error handling service.
    /// </summary>
    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ErrorHandlingConfiguration _config;
        private readonly ILogger<ErrorHandlingService> _logger;
        private readonly ILogSanitizationService _logSanitization;

        public ErrorHandlingService(
            IOptions<ErrorHandlingConfiguration> config,
            ILogger<ErrorHandlingService> logger,
            ILogSanitizationService logSanitization)
        {
            _config = config?.Value ?? new ErrorHandlingConfiguration();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logSanitization = logSanitization ?? throw new ArgumentNullException(nameof(logSanitization));
        }

        public AgentErrorResult CreateAgentErrorResult(string specialty, Exception exception, string? inputSummary = null)
        {
            var errorCode = GenerateErrorCode(exception);
            var remediationSteps = GetRemediationSteps(exception);
            var isRetryable = IsRetryableError(exception);

            var result = new AgentErrorResult
            {
                AgentSpecialty = specialty,
                ErrorCode = errorCode,
                ErrorMessage = CreateActionableErrorMessage(exception, $"Agent: {specialty}"),
                ErrorDescription = _logSanitization.SanitizeException(exception),
                RemediationSteps = remediationSteps,
                IsRetryable = isRetryable,
                RetryDelaySeconds = isRetryable ? CalculateRetryDelay(exception) : 0,
                ExceptionType = exception.GetType().Name,
                ErrorTimestamp = DateTime.UtcNow,
                InputSummary = inputSummary != null ? _logSanitization.SanitizeMessage(inputSummary) : null
            };

            return result;
        }

        public bool ShouldReturnPartialResults(int successfulAgents, int totalAgents)
        {
            if (!_config.ReturnPartialResults)
                return false;

            return successfulAgents >= _config.MinSuccessfulAgentsForPartialSuccess;
        }

        public string CreateActionableErrorMessage(Exception exception, string context)
        {
            var errorType = exception.GetType().Name;
            var remediationSteps = GetRemediationSteps(exception);

            var message = $"{context} failed: {exception.Message}";

            if (_config.IncludeErrorCodes)
            {
                var errorCode = GenerateErrorCode(exception);
                message += $" (Error Code: {errorCode})";
            }

            if (_config.IncludeRemediationSteps && remediationSteps.Any())
            {
                message += "\n\nSuggested actions:";
                foreach (var step in remediationSteps)
                {
                    message += $"\n• {step}";
                }
            }

            return message;
        }

        public List<string> GetRemediationSteps(Exception exception)
        {
            var steps = new List<string>();

            switch (exception)
            {
                case ArgumentException argEx:
                    steps.Add("Verify the input parameters are correct");
                    steps.Add("Check that all required fields are provided");
                    steps.Add("Ensure input values are within allowed ranges");
                    break;

                case InvalidOperationException invOpEx:
                    steps.Add("Check system configuration and dependencies");
                    steps.Add("Verify required services are available");
                    steps.Add("Review system logs for additional context");
                    break;

                case TimeoutException timeoutEx:
                    steps.Add("The operation timed out - this may be temporary");
                    steps.Add("Try again in a few moments");
                    steps.Add("If the problem persists, reduce the number of files or file sizes");
                    break;

                case HttpRequestException httpEx:
                    steps.Add("Check your internet connection");
                    steps.Add("Verify the AI service is available");
                    steps.Add("Wait a moment and try again");
                    break;

                case UnauthorizedAccessException authEx:
                    steps.Add("Verify your API credentials are correct");
                    steps.Add("Check that your API key has not expired");
                    steps.Add("Contact support if the problem persists");
                    break;

                default:
                    if (exception.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
                        exception.Message.Contains("429", StringComparison.OrdinalIgnoreCase))
                    {
                        steps.Add("Rate limit exceeded - please wait before retrying");
                        steps.Add("Reduce the number of concurrent requests");
                        steps.Add("Try again in a few minutes");
                    }
                    else if (exception.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                    {
                        steps.Add("Operation timed out - try again");
                        steps.Add("Reduce file size or number of files");
                        steps.Add("Check your network connection");
                    }
                    else
                    {
                        steps.Add("An unexpected error occurred");
                        steps.Add("Try again in a few moments");
                        steps.Add("If the problem persists, contact support with the error code");
                    }
                    break;
            }

            return steps;
        }

        /// <summary>
        /// Generates an error code for support reference.
        /// </summary>
        private string GenerateErrorCode(Exception exception)
        {
            var errorType = exception.GetType().Name;
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
            var hash = Math.Abs(errorType.GetHashCode()).ToString("X4");
            return $"ERR-{errorType.Substring(0, Math.Min(3, errorType.Length)).ToUpper()}-{hash}";
        }

        /// <summary>
        /// Determines if an error is retryable.
        /// </summary>
        private bool IsRetryableError(Exception exception)
        {
            return exception switch
            {
                TimeoutException => true,
                HttpRequestException => true,
                TaskCanceledException => true,
                InvalidOperationException invOp when invOp.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) => true,
                InvalidOperationException invOp when invOp.Message.Contains("429", StringComparison.OrdinalIgnoreCase) => true,
                ArgumentException => false, // Don't retry invalid arguments
                UnauthorizedAccessException => false, // Don't retry auth failures
                _ => false
            };
        }

        /// <summary>
        /// Calculates retry delay in seconds based on error type.
        /// </summary>
        private int CalculateRetryDelay(Exception exception)
        {
            return exception switch
            {
                TimeoutException => 5,
                HttpRequestException => 10,
                InvalidOperationException invOp when invOp.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) => 60,
                _ => 5
            };
        }
    }
}


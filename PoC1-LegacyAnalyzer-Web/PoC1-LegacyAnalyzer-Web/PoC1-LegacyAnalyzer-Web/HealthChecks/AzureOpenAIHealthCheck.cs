using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;

namespace PoC1_LegacyAnalyzer_Web.HealthChecks
{
    /// <summary>
    /// Health check for Azure OpenAI service availability.
    /// </summary>
    public class AzureOpenAIHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureOpenAIHealthCheck> _logger;

        public AzureOpenAIHealthCheck(
            IConfiguration configuration,
            ILogger<AzureOpenAIHealthCheck> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if configuration is present
                var endpoint = _configuration["AzureOpenAI:Endpoint"];
                var apiKey = _configuration["AzureOpenAI:ApiKey"];
                var deployment = _configuration["AzureOpenAI:Deployment"];
                var model = _configuration["AzureOpenAI:Model"];

                if (string.IsNullOrWhiteSpace(endpoint) || 
                    string.IsNullOrWhiteSpace(apiKey) ||
                    string.IsNullOrWhiteSpace(deployment))
                {
                    return HealthCheckResult.Unhealthy(
                        "Azure OpenAI configuration is missing",
                        data: new Dictionary<string, object>
                        {
                            { "Endpoint", endpoint ?? "missing" },
                            { "Deployment", deployment ?? "missing" },
                            { "ApiKey", string.IsNullOrEmpty(apiKey) ? "missing" : "present" }
                        });
                }

                // Basic validation - in production, you might want to make a lightweight API call
                // For now, we just validate configuration
                return HealthCheckResult.Healthy(
                    "Azure OpenAI configuration is valid",
                    data: new Dictionary<string, object>
                    {
                        { "Endpoint", endpoint },
                        { "Deployment", deployment },
                        { "Model", model ?? "default" }
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed for Azure OpenAI");
                return HealthCheckResult.Unhealthy(
                    "Azure OpenAI health check failed",
                    ex,
                    data: new Dictionary<string, object>
                    {
                        { "Error", ex.Message }
                    });
            }
        }
    }
}


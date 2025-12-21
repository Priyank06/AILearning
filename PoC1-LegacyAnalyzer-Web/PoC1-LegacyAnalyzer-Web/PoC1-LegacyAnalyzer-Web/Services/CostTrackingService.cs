using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Service for tracking and calculating costs of AI operations.
    /// </summary>
    public interface ICostTrackingService
    {
        /// <summary>
        /// Creates a new cost metrics tracker for an analysis.
        /// </summary>
        CostMetrics CreateCostTracker(string analysisId, string model = "gpt-4");

        /// <summary>
        /// Records token usage for an operation.
        /// </summary>
        void RecordTokenUsage(CostMetrics metrics, int inputTokens, int outputTokens, string? operation = null);

        /// <summary>
        /// Calculates cost based on token usage.
        /// </summary>
        void CalculateCost(CostMetrics metrics);

        /// <summary>
        /// Logs cost metrics to Application Insights.
        /// </summary>
        void LogCostMetrics(CostMetrics metrics);

        /// <summary>
        /// Gets formatted cost string.
        /// </summary>
        string FormatCost(decimal cost);
    }

    /// <summary>
    /// Implementation of cost tracking service.
    /// </summary>
    public class CostTrackingService : ICostTrackingService
    {
        private readonly CostTrackingConfiguration _config;
        private readonly ILogger<CostTrackingService> _logger;
        private readonly TelemetryClient? _telemetryClient;

        public CostTrackingService(
            IOptions<CostTrackingConfiguration> config,
            ILogger<CostTrackingService> logger,
            TelemetryClient? telemetryClient = null)
        {
            _config = config?.Value ?? new CostTrackingConfiguration();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _telemetryClient = telemetryClient;
        }

        public CostMetrics CreateCostTracker(string analysisId, string model = "gpt-4")
        {
            return new CostMetrics
            {
                AnalysisId = analysisId,
                Model = model,
                Timestamp = DateTime.UtcNow
            };
        }

        public void RecordTokenUsage(CostMetrics metrics, int inputTokens, int outputTokens, string? operation = null)
        {
            if (!_config.Enabled)
                return;

            metrics.InputTokens += inputTokens;
            metrics.OutputTokens += outputTokens;

            // If operation specified, track in breakdown
            if (!string.IsNullOrEmpty(operation))
            {
                if (!metrics.Breakdown.ContainsKey(operation))
                {
                    metrics.Breakdown[operation] = new CostMetrics
                    {
                        AnalysisId = metrics.AnalysisId,
                        Model = metrics.Model,
                        Timestamp = DateTime.UtcNow
                    };
                }

                metrics.Breakdown[operation].InputTokens += inputTokens;
                metrics.Breakdown[operation].OutputTokens += outputTokens;
            }

            // Recalculate cost
            CalculateCost(metrics);
        }

        public void CalculateCost(CostMetrics metrics)
        {
            if (!_config.Enabled)
                return;

            var model = metrics.Model.ToLowerInvariant();
            decimal inputCostPer1K;
            decimal outputCostPer1K;

            // Determine pricing based on model
            if (model.Contains("gpt-3.5") || model.Contains("gpt-35"))
            {
                inputCostPer1K = _config.InputTokenCostPer1K_GPT35;
                outputCostPer1K = _config.OutputTokenCostPer1K_GPT35;
            }
            else
            {
                // Default to GPT-4 pricing
                inputCostPer1K = _config.InputTokenCostPer1K;
                outputCostPer1K = _config.OutputTokenCostPer1K;
            }

            // Calculate costs
            metrics.InputCost = (metrics.InputTokens / 1000.0m) * inputCostPer1K;
            metrics.OutputCost = (metrics.OutputTokens / 1000.0m) * outputCostPer1K;

            // Calculate costs for breakdown
            foreach (var breakdown in metrics.Breakdown.Values)
            {
                breakdown.InputCost = (breakdown.InputTokens / 1000.0m) * inputCostPer1K;
                breakdown.OutputCost = (breakdown.OutputTokens / 1000.0m) * outputCostPer1K;
            }
        }

        public void LogCostMetrics(CostMetrics metrics)
        {
            if (!_config.Enabled || !_config.LogCostMetrics)
                return;

            // Log to Application Insights
            if (_telemetryClient != null)
            {
                _telemetryClient.TrackMetric("AI.Cost.Total", (double)metrics.TotalCost, new Dictionary<string, string>
                {
                    { "AnalysisId", metrics.AnalysisId },
                    { "Model", metrics.Model },
                    { "InputTokens", metrics.InputTokens.ToString() },
                    { "OutputTokens", metrics.OutputTokens.ToString() },
                    { "TotalTokens", metrics.TotalTokens.ToString() }
                });

                _telemetryClient.TrackMetric("AI.Tokens.Input", metrics.InputTokens, new Dictionary<string, string>
                {
                    { "AnalysisId", metrics.AnalysisId },
                    { "Model", metrics.Model }
                });

                _telemetryClient.TrackMetric("AI.Tokens.Output", metrics.OutputTokens, new Dictionary<string, string>
                {
                    { "AnalysisId", metrics.AnalysisId },
                    { "Model", metrics.Model }
                });
            }

            // Log to standard logger
            _logger.LogInformation(
                "Cost metrics - AnalysisId: {AnalysisId}, Model: {Model}, InputTokens: {InputTokens}, OutputTokens: {OutputTokens}, TotalCost: ${TotalCost}",
                metrics.AnalysisId,
                metrics.Model,
                metrics.InputTokens,
                metrics.OutputTokens,
                metrics.TotalCost.ToString("F4"));
        }

        public string FormatCost(decimal cost)
        {
            if (cost < 0.01m)
                return $"${cost:F4}";
            else if (cost < 1.0m)
                return $"${cost:F3}";
            else
                return $"${cost:F2}";
        }
    }
}


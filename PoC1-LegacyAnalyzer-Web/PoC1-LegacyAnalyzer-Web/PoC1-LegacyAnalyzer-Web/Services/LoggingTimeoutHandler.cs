using System.Diagnostics;
using Microsoft.Extensions.Logging;

/// <summary>
/// Logs a warning if any LLM HttpClient call exceeds the specified threshold (default: 200 seconds).
/// </summary>
public class LoggingTimeoutHandler : DelegatingHandler
{
    private readonly int _thresholdSeconds;
    private readonly ILogger<LoggingTimeoutHandler> _logger;

    public LoggingTimeoutHandler(ILogger<LoggingTimeoutHandler> logger)
        : this(200, logger) // Default threshold 200 seconds
    {
    }

    public LoggingTimeoutHandler(int thresholdSeconds, ILogger<LoggingTimeoutHandler> logger)
    {
        _thresholdSeconds = thresholdSeconds;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var response = await base.SendAsync(request, cancellationToken);
        sw.Stop();
        if (sw.Elapsed.TotalSeconds > _thresholdSeconds)
        {
            _logger.LogWarning("LLM call to {Url} took {ElapsedSeconds} seconds (threshold: {Threshold}s)", request.RequestUri, sw.Elapsed.TotalSeconds, _thresholdSeconds);
        }
        return response;
    }
}
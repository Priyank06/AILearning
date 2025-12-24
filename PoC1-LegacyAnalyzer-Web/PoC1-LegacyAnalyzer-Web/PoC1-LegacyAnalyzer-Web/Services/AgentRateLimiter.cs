using System.Collections.Concurrent;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Implements per-agent rate limiting to prevent API overload.
    /// Uses sliding window algorithm to track calls per minute per agent.
    /// </summary>
    public class AgentRateLimiter : IAgentRateLimiter
    {
        private readonly ILogger<AgentRateLimiter> _logger;
        private readonly ConcurrentDictionary<string, Queue<DateTime>> _agentCallHistory = new();
        private readonly int _maxCallsPerMinute;
        private readonly TimeSpan _windowSize = TimeSpan.FromMinutes(1);

        public AgentRateLimiter(ILogger<AgentRateLimiter> logger, int maxCallsPerMinute = 20)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxCallsPerMinute = maxCallsPerMinute;
        }

        public async Task WaitIfNeededAsync(string agentName, CancellationToken cancellationToken = default)
        {
            var status = GetRateLimitStatus(agentName);
            
            if (status.IsThrottled && status.WaitTime.HasValue)
            {
                var waitTime = status.WaitTime.Value;
                _logger.LogDebug("Rate limiting agent {AgentName}: waiting {WaitTime}ms", agentName, waitTime.TotalMilliseconds);
                await Task.Delay(waitTime, cancellationToken);
            }
        }

        public void RecordApiCall(string agentName)
        {
            var now = DateTime.UtcNow;
            var queue = _agentCallHistory.GetOrAdd(agentName, _ => new Queue<DateTime>());

            lock (queue)
            {
                // Remove calls outside the time window
                while (queue.Count > 0 && now - queue.Peek() > _windowSize)
                {
                    queue.Dequeue();
                }

                // Add current call
                queue.Enqueue(now);
            }
        }

        public RateLimitStatus GetRateLimitStatus(string agentName)
        {
            var queue = _agentCallHistory.GetOrAdd(agentName, _ => new Queue<DateTime>());
            var now = DateTime.UtcNow;
            int callsInWindow;

            lock (queue)
            {
                // Remove calls outside the time window
                while (queue.Count > 0 && now - queue.Peek() > _windowSize)
                {
                    queue.Dequeue();
                }

                callsInWindow = queue.Count;
            }

            var isThrottled = callsInWindow >= _maxCallsPerMinute;
            TimeSpan? waitTime = null;

            if (isThrottled && queue.Count > 0)
            {
                lock (queue)
                {
                    var oldestCall = queue.Peek();
                    waitTime = _windowSize - (now - oldestCall);
                    if (waitTime.Value.TotalMilliseconds < 0)
                    {
                        waitTime = TimeSpan.Zero;
                    }
                }
            }

            return new RateLimitStatus
            {
                AgentName = agentName,
                CallsInLastMinute = callsInWindow,
                MaxCallsPerMinute = _maxCallsPerMinute,
                IsThrottled = isThrottled,
                WaitTime = waitTime
            };
        }
    }
}


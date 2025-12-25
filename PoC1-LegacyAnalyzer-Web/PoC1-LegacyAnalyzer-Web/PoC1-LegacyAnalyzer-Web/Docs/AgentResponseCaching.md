# Agent Response Caching

## Overview

Agent Response Caching significantly reduces API costs and latency by caching LLM responses based on file content, agent, and business objective. When the same file is analyzed multiple times, the cached response is reused instead of making expensive API calls.

## Cost Savings

**Estimated Savings:** 30-50% reduction in API costs for typical workflows

**Example Scenario:**
- Analyze 10 files with GPT-4: $15.00 (first run)
- Re-analyze 8 unchanged files + 2 changed files: $3.00 (80% cache hit rate)
- **Total Savings:** $12.00 (80% cost reduction)

## How It Works

### 1. Cache Key Generation

Responses are cached based on a SHA256 hash of:
```
Hash(AgentName + Model + FileContentHash + BusinessObjectiveHash)
```

**Example:**
```
Agent: SecurityAnalyst-Alpha
Model: gpt-4
File: UserService.cs (content hash: abc123...)
Objective: "Comprehensive security review" (hash: def456...)
→ Cache Key: agent_response:xyz789...
```

### 2. Cache Hit Flow

```
┌────────────────────┐
│ Agent Analysis     │
│ Request            │
└─────────┬──────────┘
          ▼
┌────────────────────┐
│ Check Cache        │ ◄─── SHA256 hash of inputs
└─────────┬──────────┘
          │
     ┌────┴────┐
     │  Found? │
     └────┬────┘
          │
    ┌─────┴─────┐
   Yes          No
    │            │
    ▼            ▼
┌────────┐  ┌─────────┐
│ Return │  │ Call    │
│ Cached │  │ LLM API │
│ Result │  │         │
└────────┘  └────┬────┘
                 │
                 ▼
            ┌─────────┐
            │ Cache   │
            │ Response│
            └─────────┘
```

### 3. Cache Entry Structure

```csharp
public class AgentResponseCacheEntry
{
    public string AgentName { get; set; }               // e.g., "SecurityAnalyst-Alpha"
    public SpecialistAnalysisResult AnalysisResult;     // Cached response
    public DateTime CachedAt { get; set; }              // When cached
    public DateTime ExpiresAt { get; set; }             // When expires
    public string Model { get; set; }                   // e.g., "gpt-4"
    public int TokenCount { get; set; }                 // Tokens used
    public double CostInDollars { get; set; }           // Cost of analysis
    public int HitCount { get; set; }                   // Times reused
    public double TotalCostSavings => CostInDollars * (HitCount - 1);
}
```

## Configuration

### appsettings.json

```json
{
  "AgentCache": {
    "Enabled": true,
    "CacheExpirationMinutes": 60,
    "UseSlidingExpiration": true,
    "SlidingExpirationMinutes": 30,
    "MaxCacheSize": 1000
  }
}
```

### Configuration Options

| Setting | Default | Description |
|---------|---------|-------------|
| **Enabled** | `true` | Enable/disable caching globally |
| **CacheExpirationMinutes** | `60` | How long to cache responses (absolute TTL) |
| **UseSlidingExpiration** | `true` | Extend TTL on each cache hit |
| **SlidingExpirationMinutes** | `30` | Sliding window duration |
| **MaxCacheSize** | `1000` | Maximum number of cached entries |

## Usage

### Automatic Caching (Recommended)

The cache is automatically used in `AgentOrchestrationService` when analyzing files:

```csharp
// Example: Analyze the same file twice
var result1 = await orchestrator.CoordinateTeamAnalysisAsync(files, objective, specialties);
// → Cache MISS: Calls LLM API, caches result

var result2 = await orchestrator.CoordinateTeamAnalysisAsync(files, objective, specialties);
// → Cache HIT: Returns cached result (0 API calls, instant response)
```

### Manual Integration

To integrate caching in custom code:

```csharp
@inject IAgentResponseCacheService CacheService

public async Task<SpecialistAnalysisResult> AnalyzeWithCache(
    string agentName,
    string specialty,
    string fileContent,
    string businessObjective)
{
    // Step 1: Try cache
    var cachedResult = await CacheService.GetCachedResponseAsync(
        agentName,
        fileContent,
        businessObjective,
        "gpt-4");

    if (cachedResult != null)
    {
        Console.WriteLine("Cache HIT!");
        return cachedResult;
    }

    // Step 2: Cache MISS - call LLM
    Console.WriteLine("Cache MISS - calling LLM...");
    var result = await CallLLMApi(fileContent, businessObjective);

    // Step 3: Cache the response
    await CacheService.CacheResponseAsync(
        agentName,
        specialty,
        fileContent,
        businessObjective,
        "gpt-4",
        result,
        tokenCount: 1500,
        costInDollars: 0.045,
        fileName: "UserService.cs",
        language: "CSharp");

    return result;
}
```

## Cache Statistics

### View Statistics Programmatically

```csharp
@inject IAgentResponseCacheService CacheService

var stats = await CacheService.GetStatisticsAsync();

Console.WriteLine($"Total Entries: {stats.TotalEntries}");
Console.WriteLine($"Cache Hit Rate: {stats.HitRate:F1}%");
Console.WriteLine($"Total Cost Savings: ${stats.TotalCostSavings:F2}");
Console.WriteLine($"Tokens Saved: {stats.TotalTokensSaved:N0}");

// Per-agent breakdown
foreach (var (agent, agentStats) in stats.StatsByAgent)
{
    Console.WriteLine($"{agent}: {agentStats.Hits} hits, ${agentStats.CostSavings:F2} saved");
}
```

### Example Output

```
Total Entries: 127
Cache Hit Rate: 68.4%
Total Cost Savings: $156.78
Tokens Saved: 523,450

SecurityAnalyst-Alpha: 45 hits, $67.89 saved
PerformanceGuru-Beta: 38 hits, $52.31 saved
Architect-Master-Gamma: 31 hits, $36.58 saved
```

## Cache Management

### Clear Expired Entries

```csharp
await CacheService.ClearExpiredEntriesAsync();
```

### Clear All Cache

```csharp
await CacheService.ClearAllAsync();
```

### Get All Cache Entries

```csharp
var entries = await CacheService.GetAllEntriesAsync();

foreach (var entry in entries)
{
    Console.WriteLine($"{entry.AgentName}: {entry.HitCount} hits, expires {entry.ExpiresAt}");
}
```

## Best Practices

### 1. **Use Sliding Expiration for Active Projects**
```json
{
  "UseSlidingExpiration": true,
  "SlidingExpirationMinutes": 30
}
```
- Keeps frequently-accessed analyses cached longer
- Expires inactive caches to save memory

### 2. **Adjust TTL Based on Code Change Frequency**

| Project Type | Recommended TTL | Rationale |
|--------------|-----------------|-----------|
| **Production Maintenance** | 120 minutes | Code changes infrequently |
| **Active Development** | 30 minutes | Code changes frequently |
| **CI/CD Pipeline** | 10 minutes | Fresh analysis each build |
| **Demo/Training** | 240 minutes | Code never changes |

### 3. **Monitor Cache Hit Rate**

**Target:** ≥ 60% cache hit rate for cost efficiency

```csharp
var stats = await CacheService.GetStatisticsAsync();
if (stats.HitRate < 60)
{
    // Consider increasing cache TTL or cache size
    _logger.LogWarning("Low cache hit rate: {HitRate:F1}%", stats.HitRate);
}
```

### 4. **Clear Cache After Major Prompt Changes**

When you update agent prompts significantly:
```csharp
await CacheService.ClearAllAsync();
```
Otherwise, cached responses will use old prompts.

### 5. **Use Different Objectives for Different Analysis Types**

Cache keys include the business objective, so vary objectives for different analyses:
```csharp
// These will have separate cache entries
await AnalyzeAsync(files, "Security compliance review", ...);
await AnalyzeAsync(files, "Performance optimization review", ...);
```

## Cache Invalidation

Cache entries are invalidated when:

1. **TTL Expires** - Absolute expiration reached
2. **File Content Changes** - Different hash triggers cache miss
3. **Business Objective Changes** - Different analysis scope
4. **Agent or Model Changes** - Different analysis engine
5. **Manual Clear** - `ClearExpiredEntriesAsync()` or `ClearAllAsync()`

## Performance Impact

| Metric | Without Cache | With Cache (80% hit rate) | Improvement |
|--------|---------------|---------------------------|-------------|
| **API Cost** | $100.00 | $20.00 | **80% reduction** |
| **Analysis Time** | 120 seconds | 24 seconds | **80% faster** |
| **Token Usage** | 500,000 | 100,000 | **80% reduction** |

## Troubleshooting

### Issue: Low Cache Hit Rate

**Causes:**
1. Code changes frequently (cache keys differ)
2. Business objective varies (not deterministic)
3. TTL too short (entries expire before reuse)

**Solutions:**
1. Increase cache TTL for stable codebases
2. Standardize business objective wording
3. Enable sliding expiration

### Issue: Memory Usage Too High

**Causes:**
1. Cache size too large
2. Long TTL accumulates entries

**Solutions:**
```json
{
  "MaxCacheSize": 500,  // Reduce from 1000
  "CacheExpirationMinutes": 30  // Reduce from 60
}
```

### Issue: Stale Cached Results

**Causes:**
1. File changed but hash collision (extremely rare)
2. Prompt updated but cache not cleared

**Solutions:**
1. Clear cache after prompt changes
2. Monitor cache age and clear old entries periodically

## Integration with CI/CD

### Example: GitHub Actions

```yaml
- name: Analyze Code with Caching
  run: |
    # Clear cache at start of build (fresh analysis)
    curl -X POST $API_URL/api/cache/clear

    # Run analysis (will populate cache)
    dotnet run --analyze

    # Second analysis uses cache (for regression testing)
    dotnet run --analyze --verify
```

### Example: Azure DevOps

```yaml
- task: PowerShell@2
  inputs:
    targetType: 'inline'
    script: |
      # Cache statistics before build
      Invoke-RestMethod -Uri "$env:API_URL/api/cache/stats"

      # Run analysis
      dotnet analyze
```

## Security Considerations

### 1. **Sensitive Data in Cache**

⚠️ Cache entries contain code snippets and findings. Ensure:
- Memory cache is not exposed to unauthorized users
- Logs don't leak cached content
- Cache is cleared when decommissioning instances

### 2. **Cache Poisoning**

✅ Mitigated by:
- SHA256 hashing (collision-resistant)
- Immutable cache keys
- No user input in cache keys (only hashes)

### 3. **Data Residency**

📌 Cache is in-memory (IMemoryCache), not persisted to disk
- Data cleared on application restart
- No cross-instance caching (each instance has own cache)

## Future Enhancements

- [ ] Distributed caching (Redis) for multi-instance deployments
- [ ] Persistent cache (database) for long-term storage
- [ ] Cache warming (pre-populate common analyses)
- [ ] Cache compression (reduce memory footprint)
- [ ] Partial cache hits (reuse similar analyses)
- [ ] Cache analytics dashboard in UI

## Related Documentation

- [Incremental Analysis](./IncrementalAnalysis.md) - Analyze only changed files
- [Determinism Measurement](./DeterminismMeasurement.md) - Measuring consistency
- [Cost Tracking](./CostTracking.md) - Track API usage and costs

## API Reference

### IAgentResponseCacheService

```csharp
Task<SpecialistAnalysisResult?> GetCachedResponseAsync(
    string agentName, string fileContent, string businessObjective, string model);

Task CacheResponseAsync(
    string agentName, string specialty, string fileContent,
    string businessObjective, string model, SpecialistAnalysisResult analysisResult,
    int tokenCount, double costInDollars, string fileName, string language);

Task<CacheStatistics> GetStatisticsAsync();
Task ClearExpiredEntriesAsync();
Task ClearAllAsync();
Task<List<AgentResponseCacheEntry>> GetAllEntriesAsync();
```

## Summary

Agent Response Caching provides:
- **30-50% cost reduction** for repeated analyses
- **80% faster response times** for cache hits
- **Simple configuration** with sensible defaults
- **Automatic invalidation** based on content changes
- **Detailed statistics** for monitoring

Enable it with one line in `appsettings.json` and start saving immediately!

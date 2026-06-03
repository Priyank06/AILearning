# ADR-005: Polly Retry and Circuit Breaker for Azure OpenAI Calls

## Status
Retrospective

## Context
Azure OpenAI endpoints are subject to transient rate-limiting (429), throttling, and occasional service hiccups. A single AI analysis involves multiple sequential agent calls; a failure midway through wastes previous work and degrades user experience. Without resilience policies, any transient error surfaces directly to the user.

## Decision
Wrap the Semantic Kernel `IChatCompletionService` in a **`ResilientChatCompletionService`** that applies **Polly** policies:
- **Retry policy** — exponential backoff on transient HTTP errors and 429 responses.
- **Circuit breaker** — opens after a threshold of consecutive failures to prevent thundering-herd against a degraded endpoint.

The resilience wrapper is transparent to all callers; agents use `IChatCompletionService` without awareness of the retry logic.

## Consequences
### Positive
- Transient Azure OpenAI errors are automatically recovered without user impact.
- Circuit breaker prevents cascading overload during sustained outages.
- Decorator pattern keeps agent code clean — no retry logic mixed with business logic.
- Configurable thresholds (`RetryPolicyConfiguration`) allow tuning without code changes.
### Negative
- Exponential backoff can extend total analysis time significantly during degraded periods.
- Circuit breaker open state returns errors immediately — the UI must handle this gracefully.
- Polly v8 (used with .NET 8) has a different API surface from v7; the team must be aware of the version in use.

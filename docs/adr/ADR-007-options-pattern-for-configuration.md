# ADR-007: Strongly-Typed Configuration via the Options Pattern

## Status
Retrospective

## Context
The application has an unusually large configuration surface (614 lines in `appsettings.json`): agent profiles, prompt templates, business calculation rules, file limits, complexity thresholds, rate limiting, log sanitization, and more. Accessing raw `IConfiguration` strings throughout the codebase would make validation, testing, and refactoring difficult.

## Decision
All configuration sections are mapped to strongly-typed POCO classes (e.g., `AgentConfiguration`, `BusinessCalculationRules`, `FileAnalysisLimitsConfig`, `RateLimitConfiguration`) registered via `services.Configure<T>()` and injected as `IOptions<T>`. Startup validation in `Program.cs` validates required sections at application start, failing fast if mandatory keys are missing or logically inconsistent (e.g., complexity thresholds not in ascending order).

## Consequences
### Positive
- Configuration is fully discoverable via IntelliSense and type-safe at compile time.
- Startup validation catches misconfiguration before any request is processed.
- Services receive only the configuration they need, reducing coupling to the full `IConfiguration` tree.
- Unit tests can construct configuration objects directly without mocking `IConfiguration`.
### Negative
- Adding a new configuration key requires changes in three places: `appsettings.json`, the POCO class, and registration in `Program.cs`.
- Strongly-typed options do not automatically validate complex cross-section constraints without custom validators.
- The 614-line `appsettings.json` is difficult to maintain; secrets and environment-specific values must be overridden carefully.

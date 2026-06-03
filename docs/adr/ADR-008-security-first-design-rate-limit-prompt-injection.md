# ADR-008: Security-First Design — Rate Limiting, Prompt Injection Detection, and Log Sanitization

## Status
Retrospective

## Context
The application accepts arbitrary user-supplied source files and sends their content to Azure OpenAI. This creates two categories of risk: (1) abuse — users could flood the app with requests, exhausting Azure OpenAI quota; (2) prompt injection — malicious content inside uploaded files could attempt to hijack agent instructions. Additionally, AI API keys and connection strings must not appear in log output.

## Decision
Three complementary security controls were implemented at the infrastructure layer:

1. **Rate limiting** (`RateLimitService`, custom middleware) — sliding-window limit of 10 requests/minute per IP address, configurable via `RateLimitConfiguration`.
2. **Input validation** (`InputValidationService`) — file signature verification, extension whitelist, size caps (512 KB / 10 files), and regex-based prompt injection detection against a list of known attack patterns.
3. **Log sanitization** (`LogSanitizationService`) — redacts API keys, passwords, tokens, and connection strings from all log output before it reaches Application Insights.

## Consequences
### Positive
- Prompt injection attempts in uploaded files are blocked before reaching agent prompts.
- Rate limiting prevents quota exhaustion and protects against denial-of-service from single users.
- Log sanitization ensures secrets are never accidentally captured in telemetry pipelines.
- All three controls are configurable via `appsettings.json` without code changes.
### Negative
- Regex-based prompt injection detection generates false positives on code files that legitimately contain shell commands or eval-style constructs.
- In-memory per-IP rate limiting state is not shared across multiple instances; horizontal scaling requires a distributed cache (e.g., Redis).
- Log sanitization applies broad patterns; overly aggressive redaction can obscure diagnostic information needed for debugging.

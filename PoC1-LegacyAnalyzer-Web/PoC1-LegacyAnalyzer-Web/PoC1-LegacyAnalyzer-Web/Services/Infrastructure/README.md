# Infrastructure Services

This folder contains infrastructure and cross-cutting concern services that support the application but are not specific to AI, code analysis, or business logic.

## Purpose

Services in this folder handle:
- **Rate limiting** - Request throttling and rate limit enforcement
- **Tracing** - Distributed tracing and correlation ID management
- **Caching** - File and response caching (see also `Services/Caching/`)
- **File operations** - File download and file system operations
- **Error handling** - Centralized error handling and exception management
- **Input validation** - Request and input validation
- **Logging** - Log sanitization and logging utilities
- **Request deduplication** - Preventing duplicate requests
- **Key Vault** - Azure Key Vault integration (located in root namespace)

## Services

- `RateLimitService` - Sliding window rate limiting
- `TracingService` - Correlation ID and distributed tracing
- `FileDownloadService` - Browser file download operations
- `FileCacheManager` - File content caching
- `ErrorHandlingService` - Centralized error handling
- `InputValidationService` - Input validation
- `LogSanitizationService` - Log content sanitization
- `RequestDeduplicationService` - Request deduplication
- `LoggingTimeoutHandler` - HTTP timeout logging

## Dependencies

These services typically have minimal dependencies and are used by other service layers. They should not depend on AI-specific or business logic services.

## Lifetime

Most infrastructure services are registered as `Singleton` or `Scoped` depending on whether they need per-request isolation.


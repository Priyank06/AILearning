# AI Services

This folder contains services directly related to AI/LLM operations, including agent implementations, AI analysis, and AI infrastructure.

## Purpose

Services in this folder handle:
- **AI Analysis** - Direct AI-powered code analysis
- **Agent Implementations** - Specialist agent classes (Security, Performance, Architecture)
- **AI Infrastructure** - Resilient chat completion, AI service wrappers
- **Agent Registry** - Agent registration and discovery

## Services

- `AIAnalysisService` - Core AI analysis service
- `SecurityAnalystAgent` - Security-focused AI agent
- `PerformanceAnalystAgent` - Performance-focused AI agent
- `ArchitecturalAnalystAgent` - Architecture-focused AI agent
- `AgentRegistryService` - Agent registration and lookup
- `ResilientChatCompletionService` - Resilient wrapper for chat completion with retry/circuit breaker

## Dependencies

These services depend on:
- Semantic Kernel (`Kernel`, `IChatCompletionService`)
- Configuration services
- Infrastructure services (tracing, error handling)

## Lifetime

AI services are typically registered as `Scoped` to ensure per-request isolation in Blazor Server.

## Related Folders

- `Services/Orchestration/` - Multi-agent orchestration (uses agents from this folder)
- `Services/Prompting/` - Prompt building and transformation


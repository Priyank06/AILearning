# ADR-002: Microsoft Semantic Kernel for Multi-Agent Orchestration

## Status
Retrospective

## Context
The system requires three specialist AI agents (Security, Performance, Architecture) to run concurrently, cross-review each other's outputs, reach a weighted consensus, and resolve conflicting findings — all against Azure OpenAI endpoints. A framework was needed to structure agent definitions, prompt templates, kernel functions, and service injection without hand-rolling plumbing.

## Decision
Use **Microsoft Semantic Kernel 1.66** as the agent orchestration layer. Each agent is implemented as a kernel plugin with `[KernelFunction]`-decorated methods. The `AgentOrchestrationService` invokes agents via the kernel and coordinates peer review and synthesis through additional service classes.

## Consequences
### Positive
- Standardised pattern for defining agent capabilities as strongly-typed C# methods.
- Native integration with Azure OpenAI chat completion, including token management and model selection.
- Plugin model allows agents to be added or swapped without changing orchestration logic.
- Semantic Kernel's IKernel is injectable via ASP.NET Core DI, simplifying scoped lifetime management.
### Negative
- Semantic Kernel is evolving rapidly; the v1.x API surface changed significantly from v0.x and may change again.
- Peer-review and consensus logic had to be implemented manually — Semantic Kernel does not provide these out of the box.
- Agent-to-agent communication is modelled through the service layer rather than a native multi-agent runtime.

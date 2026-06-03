# ADR-003: Hybrid Static Analysis Before AI Calls

## Status
Retrospective

## Context
Sending raw source files directly to GPT-4 is expensive and token-inefficient. Large files exceed context windows, and the LLM must infer structural information (class names, method signatures, dependencies) that compilers already know. Early prototyping showed that pre-processing code with static analysis improved recommendation quality and reduced token usage.

## Decision
Implement a **hybrid two-phase pipeline**:
1. **Static analysis first** — Roslyn (for C#) and TreeSitter (for Python, JavaScript, TypeScript, Java, Go) extract structured metadata: classes, methods, complexity scores, dependency graphs, and legacy pattern flags.
2. **AI analysis second** — Only the condensed, structured metadata and a bounded code preview (400–800 characters, adaptive by batch size) are passed to the Azure OpenAI agents.

`AnalyzerRouter` selects the appropriate static analyzer via `TreeSitterLanguageRegistry`; `FilePreProcessingService` orchestrates both phases.

## Consequences
### Positive
- Dramatically reduces token consumption per analysis request.
- Enables analysis of files that would otherwise exceed context limits.
- Static findings (complexity, pattern flags) are deterministic and fast — no API latency.
- Agents receive structured context, producing more focused and accurate recommendations.
### Negative
- TreeSitter bindings for .NET require native library management across platforms.
- Two-phase pipeline increases code complexity and introduces an additional failure point.
- Static analyzers must be kept up to date as language syntax evolves.
- Code previews are truncated — very long methods may lose context not captured by metadata.

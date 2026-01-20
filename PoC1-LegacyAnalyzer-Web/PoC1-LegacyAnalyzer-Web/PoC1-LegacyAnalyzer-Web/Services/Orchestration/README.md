# Orchestration Services

This folder contains services that orchestrate multi-agent workflows, coordinate agent communication, and synthesize results from multiple agents.

## Purpose

Services in this folder handle:
- **Multi-Agent Orchestration** - Coordinating multiple AI agents to work together
- **Agent Communication** - Facilitating communication between agents
- **Consensus Building** - Calculating consensus from multiple agent opinions
- **Recommendation Synthesis** - Combining recommendations from multiple agents
- **Executive Summary Generation** - Creating high-level summaries from agent analyses
- **Peer Review** - Coordinating peer review between agents

## Services

- `AgentOrchestrationService` - Main orchestration service for multi-agent workflows
- `AgentCommunicationCoordinator` - Coordinates agent-to-agent communication
- `PeerReviewCoordinator` - Manages peer review processes
- `RecommendationSynthesizer` - Synthesizes recommendations from multiple agents
- `ExecutiveSummaryGenerator` - Generates executive summaries
- `ConflictResolverService` - Resolves conflicts between agent opinions
- `ConsensusCalculatorService` - Calculates consensus metrics

## Dependencies

These services depend on:
- `Services/AI/` - Agent implementations
- `Services/CodeAnalysis/` - Code analysis services
- `Services/Infrastructure/` - Infrastructure services
- `Services/Validation/` - Validation services

## Lifetime

Orchestration services are registered as `Scoped` for per-request isolation.

## Related Folders

- `Services/AI/` - Agent implementations used by orchestration
- `Services/Business/` - Business metrics and impact calculations


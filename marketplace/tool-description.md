# Legacy Code Analyzer - MS Marketplace Tool Description

> This document provides the content for the Microsoft Marketplace listing.
> Sections map to the Marketplace submission fields: Summary, Detailed Description, and Getting Started.
> Diagrams referenced are located in `marketplace/diagrams/`.

---

## Summary

**Legacy Code Analyzer** is an AI-powered multi-agent code analysis tool built on .NET 8 and Blazor Server that helps development teams assess, understand, and modernize legacy codebases. It uses three specialized AI agents -- Security, Performance, and Architecture -- orchestrated through Microsoft Semantic Kernel and Azure OpenAI to deliver comprehensive, consensus-driven analysis reports across six programming languages (C#, Python, JavaScript, TypeScript, Java, and Go).

Upload your project files, and within minutes receive an executive summary covering security vulnerabilities, performance bottlenecks, architectural anti-patterns, legacy debt indicators, business impact metrics, and prioritized modernization recommendations -- all backed by multi-agent consensus and conflict resolution.

---

## Detailed Description

### Overview

Legacy Code Analyzer addresses the challenge of evaluating and modernizing aging codebases at scale. Instead of relying on a single analysis pass, it orchestrates three specialist AI agents that independently analyze your code, cross-review each other's findings, and reach consensus -- producing results that are more accurate and comprehensive than any single-pass tool.

The tool runs as a Blazor Server web application deployed on Azure, with all AI inference handled by Azure OpenAI. No code leaves your Azure tenant.

### Key Capabilities

#### Multi-Agent AI Analysis
- **Security Analyst Agent** -- Identifies vulnerabilities (SQL injection, XSS, hardcoded credentials), rates severity, and suggests remediation steps.
- **Performance Analyst Agent** -- Detects bottlenecks (nested loops, missing pagination, blocking calls), measures complexity, and recommends optimizations.
- **Architectural Analyst Agent** -- Evaluates design patterns, coupling, cohesion, and proposes modernization paths.

All three agents operate in parallel, then engage in peer review, consensus building, and conflict resolution to produce a unified, high-confidence result.

> See: [Multi-Agent Orchestration Diagram](diagrams/multi-agent-orchestration.md)

#### Multi-Language Support
The tool supports six programming languages through a hybrid analysis engine:
| Language   | Parser Engine              |
|------------|----------------------------|
| C#         | Microsoft Roslyn            |
| Python     | TreeSitter                  |
| JavaScript | TreeSitter                  |
| TypeScript | TreeSitter                  |
| Java       | TreeSitter                  |
| Go         | TreeSitter                  |

Language detection is automatic -- upload files and the Analyzer Router directs each file to the correct parser.

> See: [Architecture Overview Diagram](diagrams/architecture-overview.md)

#### Static + Semantic Analysis Pipeline
The pre-processing pipeline extracts structural metadata before AI analysis begins:
- **Metadata extraction** -- classes, methods, imports, dependencies, lines of code
- **Complexity calculation** -- cyclomatic complexity, cognitive complexity, nesting depth
- **Pattern detection** -- common code patterns and anti-patterns
- **Legacy pattern detection** -- deprecated APIs, outdated idioms, technical debt markers
- **Cross-file dependency analysis** -- inter-module coupling, dependency graphs
- **Hybrid analysis** -- combines static parser results with AI semantic understanding

> See: [Data Flow Diagram](diagrams/data-flow.md)

#### Orchestration & Consensus
The multi-agent orchestration pipeline ensures analysis quality:
1. **Parallel agent execution** -- all three agents analyze simultaneously
2. **Peer review** -- each agent reviews the others' findings
3. **Consensus calculation** -- confidence-weighted score aggregation
4. **Conflict resolution** -- automatic resolution of contradictory findings using priority rules
5. **Recommendation synthesis** -- merged, deduplicated, impact-prioritized action items
6. **Executive summary generation** -- AI-generated overview for stakeholders

> See: [System Interaction Diagrams](diagrams/system-interaction.md)

#### Business Impact & Reporting
Analysis results include business-focused metrics:
- **Risk assessment** -- severity classification with configurable thresholds
- **Cost estimation** -- API usage costs and estimated developer effort
- **Business impact scoring** -- ROI estimation for modernization initiatives
- **Complexity thresholds** -- configurable low/medium/high/critical bands

Generated reports include:
- Executive summary report (business stakeholders)
- Team technical report (development team)
- Detailed findings report (per-file analysis)
- Downloadable Markdown and HTML formats

#### Enterprise-Grade Infrastructure
- **Resilience** -- Polly-based retry policies with exponential backoff and circuit breaker patterns for Azure OpenAI calls
- **Rate limiting** -- sliding window rate limiting per agent (20 calls/min default) and global request rate limiting
- **Request deduplication** -- prevents duplicate API calls for identical analysis requests
- **Distributed tracing** -- correlation IDs propagated across all service calls
- **Log sanitization** -- automatic PII and secret redaction in logs
- **Health monitoring** -- `/health`, `/health/ready`, and `/health/live` endpoints with Azure OpenAI connectivity and memory usage checks
- **Azure Application Insights** -- full telemetry for requests, dependencies, exceptions, and performance counters

> See: [Deployment Architecture Diagram](diagrams/deployment-architecture.md)

#### Secure Client-Side Persistence
Analysis results are cached in the browser using encrypted storage:
- **Browser SQLite** -- encrypted analysis results and agent session state
- **IndexedDB** -- encrypted key material and user preferences
- **AES-GCM encryption** -- 256-bit keys derived via KDF, stored per-browser
- **Graceful fallback** -- application works without persistence when storage is unavailable

### Architecture Summary

| Layer                    | Technology                                             |
|--------------------------|--------------------------------------------------------|
| **Presentation**         | Blazor Server, Razor Components, SignalR               |
| **Middleware**            | Correlation ID, Rate Limiting, Error Handling          |
| **Application Services** | 15 service categories, DI with IOptions pattern        |
| **AI Engine**            | Microsoft Semantic Kernel, Azure OpenAI                |
| **Code Analysis**        | Roslyn (C#), TreeSitter (Python/JS/TS/Java/Go)        |
| **Persistence**          | Browser SQLite + IndexedDB (AES-GCM encrypted)         |
| **Infrastructure**       | Polly (resilience), Application Insights (monitoring)  |
| **Cloud Platform**       | Azure App Service, Azure Key Vault, Azure OpenAI       |

### Diagrams Reference

All architecture and system interaction diagrams are maintained in the `marketplace/diagrams/` directory:

| Diagram | Description |
|---------|-------------|
| [architecture-overview.md](diagrams/architecture-overview.md) | High-level system architecture and layered architecture view |
| [system-interaction.md](diagrams/system-interaction.md) | Sequence diagrams for all major workflows (project analysis, file upload, agent orchestration, resilient communication, persistence, startup) |
| [multi-agent-orchestration.md](diagrams/multi-agent-orchestration.md) | Agent ecosystem, workflow state machine, communication model, and consensus/conflict resolution detail |
| [deployment-architecture.md](diagrams/deployment-architecture.md) | Azure deployment topology, network/security architecture, infrastructure components, and health check architecture |
| [data-flow.md](diagrams/data-flow.md) | End-to-end data flow, token/cost tracking, configuration flow, and client-side data flow |

---

## Getting Started

### Prerequisites

| Requirement | Details |
|-------------|---------|
| **.NET 8 SDK** | [Download .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **Azure Subscription** | Required for Azure OpenAI and Key Vault |
| **Azure OpenAI Resource** | With a GPT model deployment (e.g., gpt-35-turbo) |
| **IDE** | Visual Studio 2022 17.8+ or VS Code with C# Dev Kit |

### Step 1: Clone the Repository

```bash
git clone <repository-url>
cd AILearning
```

### Step 2: Configure Azure OpenAI

The application requires three secrets for Azure OpenAI connectivity. Configure them using .NET User Secrets for local development:

```bash
cd PoC1-LegacyAnalyzer-Web/PoC1-LegacyAnalyzer-Web/PoC1-LegacyAnalyzer-Web

dotnet user-secrets init
dotnet user-secrets set "AzureOpenAI:ApiKey" "<your-azure-openai-api-key>"
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<your-resource>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:Deployment" "<your-deployment-name>"
```

For production, these secrets should be stored in Azure Key Vault. The application automatically reads secrets with the `App--` prefix from Key Vault when `KeyVault:Enabled` is set to `true` in `appsettings.json`.

### Step 3: (Optional) Configure Azure Key Vault

To enable Key Vault integration for production deployments, update `appsettings.json`:

```json
{
  "KeyVault": {
    "Enabled": true,
    "VaultUri": "https://<your-vault>.vault.azure.net/",
    "SecretPrefix": "App--",
    "ReloadIntervalSeconds": 300
  }
}
```

Store your secrets in Key Vault with the `App--` prefix:
- `App--AzureOpenAI--ApiKey`
- `App--AzureOpenAI--Endpoint`
- `App--AzureOpenAI--Deployment`

### Step 4: (Optional) Configure Application Insights

Set the connection string via environment variable or `appsettings.json`:

```bash
export APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=<key>;IngestionEndpoint=<endpoint>"
```

### Step 5: Build and Run

```bash
cd PoC1-LegacyAnalyzer-Web/PoC1-LegacyAnalyzer-Web/PoC1-LegacyAnalyzer-Web
dotnet restore
dotnet build
dotnet run
```

The application will start on `https://localhost:5001` (or the port configured in `Properties/launchSettings.json`).

### Step 6: Analyze Your First Project

1. Open the application in your browser
2. Navigate to **Multi-File Analysis** from the sidebar
3. Upload your project files (supports C#, Python, JavaScript, TypeScript, Java, Go)
4. Select your analysis parameters
5. Click **Analyze** to start the multi-agent orchestration
6. View the interactive results dashboard with:
   - Executive summary
   - Risk assessment cards
   - Legacy issues breakdown
   - Per-file findings with severity and confidence scores
7. Download reports in Markdown or HTML format

### Step 7: Explore Additional Features

| Feature | Navigation | Description |
|---------|-----------|-------------|
| **Multi-Agent Orchestration** | Sidebar > Multi-Agent | Run full 3-agent analysis with consensus |
| **Agent Analysis** | Sidebar > Agent Analysis | Single-agent focused analysis |
| **Determinism Measurement** | Sidebar > Determinism | Measure AI consistency across runs |
| **Ground Truth Validation** | Sidebar > Ground Truth | Validate analysis accuracy against known baselines |

### Configuration Reference

The main configuration file is `appsettings.json` (37.8 KB) with 30+ configurable sections. Key sections:

| Section | Purpose |
|---------|---------|
| `AzureOpenAI` | Model deployment, token limits, batch processing |
| `AgentConfiguration` | Agent profiles, prompt templates, orchestration prompts |
| `PromptConfiguration` | System prompts, analysis prompt templates |
| `BusinessCalculationRules` | Cost calculation, complexity thresholds, risk thresholds |
| `RetryPolicy` | Retry count, backoff configuration |
| `RateLimit` | Request rate limiting parameters |
| `FilePreProcessing` | Concurrent files, cache TTL, max file size |
| `ClientPersistence` | Browser storage enable/disable |
| `KeyVault` | Vault URI, managed identity, reload interval |

### Health Check Endpoints

| Endpoint | Purpose |
|----------|---------|
| `GET /health` | Full health status with JSON details |
| `GET /health/ready` | Readiness probe (checks external dependencies) |
| `GET /health/live` | Liveness probe (returns OK if app is running) |

### Troubleshooting

| Issue | Resolution |
|-------|-----------|
| **Startup fails with configuration error** | Check that `AgentConfiguration` has `security`, `performance`, and `architecture` profiles in `appsettings.json` |
| **Azure OpenAI timeout** | The default HTTP timeout is 300 seconds. Increase `AgentConfiguration:HttpTimeoutSeconds` for large files |
| **Rate limit errors** | Adjust `RateLimit` settings in `appsettings.json` or the per-agent limit (default: 20 calls/min) |
| **Circuit breaker open** | Wait for the recovery timeout, then retry. Check Azure OpenAI service health |
| **Browser storage errors** | Ensure browser supports IndexedDB and WebAssembly. Clear browser data if encrypted storage is corrupted |

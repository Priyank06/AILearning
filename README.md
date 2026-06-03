# LegacyLens — AI-Powered Legacy Code Analysis Portal

> **Recommended project name:** `LegacyLens`  
> See [`docs/adr/`](docs/adr/) for Architecture Decision Records.

---

## Overview

**LegacyLens** is a **Blazor Server** web application (ASP.NET Core 8) that uses a **multi-agent AI orchestration** system to evaluate legacy codebases for security vulnerabilities, performance bottlenecks, and architectural debt. Three specialist AI agents — SecurityAnalyst, PerformanceGuru, and ArchitectMaster — run concurrently via Microsoft Semantic Kernel against Azure OpenAI (GPT-3.5 / GPT-4), cross-review each other's findings, and synthesize a consolidated report with prioritized, business-contextualized recommendations.

The application was developed as **PoC 1** in the AI Learning initiative to validate multi-agent code intelligence at enterprise scale.

---

## Features

- **Multi-Agent Analysis** — Security, Performance, and Architecture agents run concurrently with configurable confidence thresholds (75–85%) per agent
- **Agent Peer Review** — Agents critique each other's findings to reduce false positives and surface overlooked issues
- **Consensus & Conflict Resolution** — Weighted consensus scoring and automated conflict resolution across divergent agent opinions
- **Hybrid Static + AI Pipeline** — Roslyn / TreeSitter extract structured metadata first; only condensed context is sent to the LLM, reducing token spend
- **Multi-Language Support** — C#, Python, JavaScript, TypeScript, Java, Go
- **Multi-File & Batch Processing** — Analyse entire project folders; batch pipeline manages token budgets automatically (max 5 files / 12 K tokens per batch)
- **Business Impact Scoring** — Cost-to-remediate estimates, risk scoring, and ROI framing driven by configurable business rules
- **Legacy Pattern Detection** — Detects ancient .NET Framework usage, global state, obsolete APIs, legacy ADO.NET data access, and more
- **Ground Truth Validation** — Compare AI findings against known ground-truth datasets to measure accuracy
- **Determinism Measurement** — Track result consistency across repeated analysis runs
- **Browser Persistence** — Analysis sessions stored client-side in an AES-GCM-encrypted SQLite database (sql.js, no server database required)
- **Executive & Team Reports** — Downloadable reports tailored for stakeholders and development teams
- **Observability** — Azure Application Insights telemetry, PII/secret-redacted structured logging, correlation IDs, distributed tracing
- **Security Controls** — Rate limiting (10 req/min per IP), prompt-injection detection, file signature validation, log sanitization
- **Health Endpoints** — `/health`, `/health/ready`, `/health/live` with Azure OpenAI connectivity checks

---

## Tech Stack

| Layer | Technology |
|---|---|
| **Web Framework** | ASP.NET Core 8, Blazor Server |
| **AI Orchestration** | Microsoft Semantic Kernel 1.66 |
| **LLM Provider** | Azure OpenAI (GPT-3.5-turbo / GPT-4) |
| **Static Code Analysis** | Microsoft.CodeAnalysis.CSharp (Roslyn), TreeSitter.DotNet |
| **Token Counting** | SharpToken (tiktoken) |
| **Resilience** | Polly (retry + circuit breaker) |
| **Secrets Management** | Azure Key Vault (`DefaultAzureCredential`, optional) |
| **Observability** | Azure Application Insights |
| **Client Storage** | sql.js (browser SQLite) + Web Crypto API (AES-GCM) |
| **Real-time UI** | SignalR (Blazor Server circuit) |

---

## Architecture

```
┌──────────────────────────────────────────────────────┐
│                  Blazor Server Pages                   │
│  Index · MultiFile · AgentAnalysis · Orchestration    │
│  GroundTruthValidation · DeterminismMeasurement       │
└─────────────────────┬────────────────────────────────┘
                      │  SignalR circuit
┌─────────────────────▼────────────────────────────────┐
│                    Service Layer                        │
│                                                        │
│  ┌──────────────────┐  ┌─────────────┐  ┌──────────┐  │
│  │  Orchestration   │  │    Code     │  │Reporting │  │
│  │  AgentRegistry   │  │  Analysis   │  │Executive │  │
│  │  PeerReview      │  │  Metadata   │  │  Team    │  │
│  │  Consensus       │  │  Patterns   │  │          │  │
│  └────────┬─────────┘  └─────────────┘  └──────────┘  │
│           │                                            │
│  ┌────────▼───────────────────────────────────────┐   │
│  │  Agent Team  (Semantic Kernel plugins)          │   │
│  │  SecurityAnalyst · PerformanceGuru              │   │
│  │  ArchitectMaster → PeerReview → Synthesis       │   │
│  └────────────────────────────────────────────────┘   │
│                                                        │
│  Infrastructure: RateLimit · InputValidation           │
│                  Resilience · LogSanitization          │
│                  Tracing · RequestDedup                │
└───────────────────────┬──────────────────────────────┘
                        │
        ┌───────────────▼───────────────┐
        │     Azure OpenAI / GPT-4      │
        └───────────────────────────────┘
```

**Key layers:**

- **Pages** — Blazor pages for each analysis mode; real-time agent progress via SignalR
- **Orchestration** — `AgentOrchestrationService` coordinates parallel agent execution, peer review, conflict resolution, and consensus scoring
- **Code Analysis** — Two-phase pipeline: static metadata extraction (Roslyn / TreeSitter) followed by targeted AI analysis on condensed context
- **Infrastructure** — Rate limiting, request deduplication, exponential-backoff retry, prompt-injection detection, PII-redacted logging
- **Persistence** — Browser-side encrypted SQLite (analysis history, agent sessions, preferences) plus in-memory agent response cache (60-min TTL)

---

## Run Locally

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- An **Azure OpenAI** resource with a deployed model (`gpt-35-turbo` or `gpt-4`)
- *(Optional)* An **Azure Key Vault** for production secret management

### 1. Configure Secrets

Set the three required values via .NET User Secrets (recommended for local development):

```bash
cd PoC1-LegacyAnalyzer-Web/PoC1-LegacyAnalyzer-Web/PoC1-LegacyAnalyzer-Web

dotnet user-secrets set "AzureOpenAI:Endpoint"   "https://<your-resource>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey"     "<your-api-key>"
dotnet user-secrets set "AzureOpenAI:Deployment" "<your-deployment-name>"
```

Alternatively, set them as environment variables or populate `appsettings.json` directly (not recommended for secrets).

To use Azure Key Vault, update `appsettings.json`:

```json
"KeyVault": {
  "Enabled": true,
  "VaultUri": "https://<your-vault>.vault.azure.net/"
}
```

### 2. Run

```bash
cd PoC1-LegacyAnalyzer-Web/PoC1-LegacyAnalyzer-Web/PoC1-LegacyAnalyzer-Web
dotnet run
```

The application starts on `https://localhost:7001` (or as configured in `launchSettings.json`).

### 3. Analyse Code

1. Navigate to the home page and upload one or more source files (`.cs`, `.py`, `.js`, `.ts`, `.java`, `.go`)
2. Select a **business objective** — Security Audit, Performance, Migration, or General
3. Choose an **agent team** configuration
4. Click **Analyse** — real-time progress updates appear as each agent completes
5. Review the consolidated results: executive summary, per-agent findings, prioritised recommendations, business impact scores
6. Download a team or executive report if needed

---

## Project Structure

```
AILearning/
├── README.md
├── AILearning.sln
├── docs/
│   └── adr/                          # Architecture Decision Records
│       ├── ADR-001-blazor-server-ui-framework.md
│       ├── ADR-002-semantic-kernel-agent-orchestration.md
│       ├── ADR-003-hybrid-static-and-ai-analysis.md
│       ├── ADR-004-browser-side-encrypted-persistence.md
│       ├── ADR-005-polly-resilience-for-ai-calls.md
│       ├── ADR-006-azure-openai-over-openai-direct.md
│       ├── ADR-007-options-pattern-for-configuration.md
│       └── ADR-008-security-first-design-rate-limit-prompt-injection.md
└── PoC1-LegacyAnalyzer-Web/
    └── PoC1-LegacyAnalyzer-Web/
        └── PoC1-LegacyAnalyzer-Web/
            ├── Program.cs                    # Startup, DI registration, config validation
            ├── ServiceCollectionExtensions.cs
            ├── appsettings.json              # Full configuration (614 lines)
            ├── Pages/                        # 7 Blazor pages
            ├── Components/                   # 58 Razor components
            ├── Services/
            │   ├── AI/                       # Agent implementations, token estimation
            │   ├── Analysis/                 # Roslyn + TreeSitter language analyzers
            │   ├── Orchestration/            # Multi-agent coordination, peer review, consensus
            │   ├── CodeAnalysis/             # Pattern detection, complexity, dependency graphs
            │   ├── Business/                 # Cost, risk, ROI calculations
            │   ├── Infrastructure/           # Rate limit, validation, resilience, tracing
            │   ├── Caching/                  # Agent response cache
            │   ├── Persistence/              # Browser SQLite interop, encrypted repositories
            │   ├── GroundTruth/              # Accuracy validation against known datasets
            │   ├── Determinism/              # Consistency measurement across runs
            │   ├── Reporting/                # Executive and team report generation
            │   ├── Validation/               # Finding and confidence validation
            │   ├── ProjectAnalysis/          # Multi-file project insights
            │   └── Prompting/                # Prompt template building
            ├── Models/                       # 40+ domain and configuration models
            ├── Middleware/                   # Rate limiting, correlation ID middleware
            ├── HealthChecks/                 # /health, /health/ready, /health/live
            ├── Extensions/                   # Extension methods
            ├── SampleData/                   # LegacyCodeBenchmark for demo/testing
            └── wwwroot/                      # CSS, JS, static assets
```

---

## Health Endpoints

| Endpoint | Description |
|---|---|
| `GET /health` | Full health report (JSON) including Azure OpenAI connectivity |
| `GET /health/ready` | Readiness — external dependencies only |
| `GET /health/live` | Liveness — always 200 if the process is running |

---

## Architecture Decision Records

All major architectural decisions are documented as ADRs in [`docs/adr/`](docs/adr/):

| ADR | Decision |
|---|---|
| [ADR-001](docs/adr/ADR-001-blazor-server-ui-framework.md) | Blazor Server as the UI framework |
| [ADR-002](docs/adr/ADR-002-semantic-kernel-agent-orchestration.md) | Semantic Kernel for multi-agent orchestration |
| [ADR-003](docs/adr/ADR-003-hybrid-static-and-ai-analysis.md) | Hybrid static + AI two-phase analysis pipeline |
| [ADR-004](docs/adr/ADR-004-browser-side-encrypted-persistence.md) | Browser-side encrypted SQLite for session persistence |
| [ADR-005](docs/adr/ADR-005-polly-resilience-for-ai-calls.md) | Polly retry and circuit breaker for Azure OpenAI calls |
| [ADR-006](docs/adr/ADR-006-azure-openai-over-openai-direct.md) | Azure OpenAI as the LLM provider |
| [ADR-007](docs/adr/ADR-007-options-pattern-for-configuration.md) | Strongly-typed configuration via the Options pattern |
| [ADR-008](docs/adr/ADR-008-security-first-design-rate-limit-prompt-injection.md) | Security-first design: rate limiting, prompt injection, log sanitization |

> These ADRs were generated retrospectively based on the existing codebase.

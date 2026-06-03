# Legacy Analyzer Web — PoC1

An AI-powered legacy code analysis tool built with **Blazor Server** (.NET 8). It uses a multi-agent orchestration system backed by Azure OpenAI / Semantic Kernel to evaluate legacy codebases for security vulnerabilities, performance bottlenecks, and architectural debt — then produces actionable, business-contextualized recommendations.

---

## Project Overview

PoC1-LegacyAnalyzer-Web is a proof-of-concept web application that enables engineering and architecture teams to upload source files (C#, Python, Java, JavaScript, TypeScript, Go, and more) and receive a structured, AI-driven analysis report. Three specialized agents — **SecurityAnalyst**, **PerformanceGuru**, and **Architect-Master** — run concurrently via an orchestration layer, cross-review each other's findings, and synthesize a consolidated executive summary with prioritized remediation steps.

---

## Features

- **Multi-Agent Analysis** — Independent Security, Performance, and Architecture agents analyze code in parallel with configurable confidence thresholds
- **Agent Peer Review** — Agents critique each other's findings to reduce false positives and surface overlooked issues
- **Multi-File & Batch Processing** — Analyze entire project folders; batch pipeline manages token budgets automatically
- **Business Impact Scoring** — Cost-to-remediate estimates, risk scoring, and ROI framing tied to configurable business rules
- **Legacy Pattern Detection** — Detects ancient .NET Framework usage, global state, obsolete APIs, and legacy data-access patterns
- **Ground Truth Validation** — Compares AI findings against known ground-truth datasets to measure accuracy
- **Determinism Measurement** — Tracks result consistency across repeated analysis runs
- **Browser Persistence** — Analysis sessions stored client-side in an encrypted browser SQLite database (sql.js + AES-GCM)
- **Executive & Team Reports** — Downloadable reports tailored for executive stakeholders and development teams
- **Health Checks** — `/health`, `/health/ready`, and `/health/live` endpoints with Azure OpenAI connectivity checks
- **Rate Limiting & Input Validation** — Server-side rate limiting, prompt-injection detection, and file signature validation
- **Observability** — Application Insights telemetry, structured logging with PII/secret redaction, correlation IDs, and distributed tracing

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8, Blazor Server |
| AI Orchestration | Microsoft Semantic Kernel 1.66 |
| AI Provider | Azure OpenAI (GPT-3.5/GPT-4) |
| Secrets Management | Azure Key Vault (optional, `DefaultAzureCredential`) |
| Observability | Azure Application Insights |
| Resilience | Polly (retry + circuit breaker) |
| Code Analysis | Microsoft.CodeAnalysis.CSharp, TreeSitter.DotNet |
| Token Counting | SharpToken (tiktoken) |
| Client Storage | sql.js (browser SQLite) + AES-GCM encryption via JS interop |
| Real-time UI | SignalR (Blazor Server circuit) |

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                  Blazor Server Pages                  │
│  Index · MultiFile · AgentAnalysis · Orchestration   │
│  GroundTruthValidation · DeterminismMeasurement      │
└────────────────────┬────────────────────────────────┘
                     │ SignalR circuit
┌────────────────────▼────────────────────────────────┐
│               Service Layer                          │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────┐  │
│  │ MultiAgent   │  │ CodeAnalysis │  │ Reporting │  │
│  │ Orchestration│  │ + Metadata   │  │ Services  │  │
│  └──────┬───────┘  └──────────────┘  └───────────┘  │
│         │                                            │
│  ┌──────▼──────────────────────────────────────┐    │
│  │  Agent Team (Semantic Kernel)               │    │
│  │  SecurityAnalyst · PerformanceGuru          │    │
│  │  ArchitectMaster  → Peer Review → Synthesis │    │
│  └──────────────────────────────────────────────┘   │
└─────────────────────────┬───────────────────────────┘
                          │
          ┌───────────────▼───────────────┐
          │     Azure OpenAI / GPT-4      │
          └───────────────────────────────┘
```

**Key layers:**

- **Pages** — Blazor pages for each analysis mode; progress tracked in real time via SignalR
- **Orchestration** — `AgentOrchestrationService` coordinates agent execution, peer review, conflict resolution, and consensus scoring
- **Code Analysis** — Static metadata extraction (classes, methods, dependencies) using Roslyn and TreeSitter before any API calls
- **Infrastructure** — Rate limiting, request deduplication, retry policies, input validation, and log sanitization
- **Persistence** — Optional browser-side encrypted SQLite (analysis history, agent sessions, preferences) plus in-memory agent response cache

---

## Run Locally

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- An **Azure OpenAI** resource with a deployed model (e.g. `gpt-35-turbo` or `gpt-4`)
- (Optional) An **Azure Key Vault** for secret management

### 1. Configure secrets

The application requires three values. Set them via **User Secrets** (recommended for local dev):

```bash
cd PoC1-LegacyAnalyzer-Web/PoC1-LegacyAnalyzer-Web/PoC1-LegacyAnalyzer-Web

dotnet user-secrets set "AzureOpenAI:Endpoint"   "https://<your-resource>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey"     "<your-api-key>"
dotnet user-secrets set "AzureOpenAI:Deployment" "<your-deployment-name>"
```

Alternatively, set environment variables or populate `appsettings.json` directly (not recommended for secrets).

If you have an Azure Key Vault, update `appsettings.json`:

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

### 3. Use the app

1. Navigate to the home page and upload one or more source files (`.cs`, `.py`, `.js`, `.ts`, `.java`, `.go`, etc.)
2. Select a **business objective** (Security, Performance, Migration, General)
3. Choose an **agent team** configuration
4. Click **Analyse** — real-time progress is shown as each agent completes
5. Review the consolidated results: executive summary, per-agent findings, prioritized recommendations, and business impact scores
6. Download a team or executive report if needed

---

## Health Endpoints

| Endpoint | Description |
|---|---|
| `GET /health` | Full health check (JSON) including Azure OpenAI connectivity |
| `GET /health/ready` | Readiness — external dependencies only |
| `GET /health/live` | Liveness — always 200 if process is running |

# Deployment Architecture - Legacy Code Analyzer

## Azure Cloud Deployment Topology

```mermaid
graph TB
    subgraph "Client Tier"
        Browser[Web Browser<br/>Chrome / Edge / Firefox]
        subgraph "Client-Side Storage"
            SQLite[Browser SQLite<br/>Encrypted Analysis Cache]
            IDB[IndexedDB<br/>Encrypted Sessions & Prefs]
        end
    end

    subgraph "Azure App Service"
        subgraph "Web Application"
            Blazor[Blazor Server<br/>.NET 8]
            SignalR[SignalR Hub<br/>WebSocket]
            Health[Health Endpoints<br/>/health<br/>/health/ready<br/>/health/live]
        end
    end

    subgraph "Azure Platform Services"
        subgraph "AI Services"
            AOAI[Azure OpenAI Service<br/>GPT-3.5-turbo Deployment]
        end

        subgraph "Security"
            KV[Azure Key Vault<br/>Secrets Management<br/>API Keys / Endpoints / Certs]
            MI[Managed Identity<br/>DefaultAzureCredential]
        end

        subgraph "Monitoring"
            AI[Application Insights<br/>Request Tracking<br/>Dependency Tracking<br/>Exception Telemetry<br/>Performance Counters]
        end
    end

    Browser <-->|WebSocket<br/>SignalR| SignalR
    Browser <-->|HTTPS| Blazor
    Browser --> SQLite
    Browser --> IDB

    Blazor --> SignalR
    Blazor -->|HTTPS| AOAI
    Blazor -->|HTTPS| KV
    MI -.->|Authentication| KV
    MI -.->|Authentication| AOAI
    Blazor -.->|Telemetry| AI

    Health -.->|Probes| AOAI

    style AOAI fill:#8e44ad,color:#fff
    style KV fill:#2c3e50,color:#fff
    style AI fill:#2980b9,color:#fff
    style Blazor fill:#4a90d9,color:#fff
```

## Network & Security Architecture

```mermaid
graph TB
    subgraph "Internet"
        Client[Client Browser]
    end

    subgraph "Azure Front Door / App Gateway"
        HTTPS[HTTPS Termination<br/>TLS 1.2+]
        HSTS[HSTS Enforcement]
    end

    subgraph "Azure App Service Environment"
        subgraph "Middleware Pipeline"
            CorrID[Correlation ID Middleware<br/>Request Tracing]
            RateLimit[Rate Limit Middleware<br/>Sliding Window]
        end

        subgraph "Application"
            BlazorApp[Blazor Server App<br/>.NET 8]
            MemCache[In-Memory Cache<br/>Size-Limited]
        end
    end

    subgraph "Azure Services - Private Endpoints"
        AOAI[Azure OpenAI<br/>API Key Auth]
        KV[Azure Key Vault<br/>Managed Identity Auth]
        AI[Application Insights<br/>Connection String Auth]
    end

    subgraph "Secrets Flow"
        direction LR
        S1[API Keys] --> KV
        S2[Connection Strings] --> KV
        S3[Deployment Names] --> KV
        KV -->|IConfiguration| BlazorApp
    end

    Client -->|HTTPS| HTTPS
    HTTPS --> HSTS
    HSTS --> CorrID
    CorrID --> RateLimit
    RateLimit --> BlazorApp
    BlazorApp --> MemCache
    BlazorApp -->|HTTPS + API Key| AOAI
    BlazorApp -->|HTTPS + MI| KV
    BlazorApp -.->|Telemetry| AI

    style Client fill:#ecf0f1,color:#2c3e50
    style AOAI fill:#8e44ad,color:#fff
    style KV fill:#2c3e50,color:#fff
    style RateLimit fill:#e74c3c,color:#fff
```

## Infrastructure Component Responsibilities

```mermaid
graph LR
    subgraph "Resilience Infrastructure"
        RP[Retry Policy<br/>Exponential backoff<br/>Max retries configurable]
        CB[Circuit Breaker<br/>Failure threshold<br/>Recovery timeout]
        RL[Rate Limiter<br/>Sliding window<br/>Per-agent limits 20/min]
        RD[Request Deduplication<br/>Prevent duplicate calls<br/>Memory-cache backed]
    end

    subgraph "Observability Infrastructure"
        CID[Correlation ID<br/>End-to-end request tracing]
        TS[Tracing Service<br/>Distributed trace context]
        LS[Log Sanitization<br/>PII/secret redaction]
        AI[Application Insights<br/>Metrics + Traces + Exceptions]
    end

    subgraph "Security Infrastructure"
        IV[Input Validation<br/>File size / type / content]
        KV[Key Vault Integration<br/>Secret rotation support]
        ENC[Client Encryption<br/>AES-GCM + KDF]
        FF[File Filtering<br/>Allowed extensions<br/>Max size enforcement]
    end

    subgraph "Caching Infrastructure"
        MC[Memory Cache<br/>Size-limited<br/>TTL-based eviction]
        AC[Agent Response Cache<br/>Singleton lifetime<br/>Cross-circuit sharing]
        FC[File Cache Manager<br/>Per-circuit scoping]
    end

    RP --> CB
    CID --> TS
    TS --> LS --> AI
    IV --> FF
    MC --> AC & FC

    style RP fill:#3498db,color:#fff
    style CB fill:#3498db,color:#fff
    style AI fill:#2980b9,color:#fff
    style ENC fill:#2c3e50,color:#fff
```

## Health Check Architecture

```mermaid
graph TB
    subgraph "Health Endpoints"
        H1["/health"<br/>Full status with JSON details<br/>All checks]
        H2["/health/ready"<br/>Readiness probe<br/>External dependencies only]
        H3["/health/live"<br/>Liveness probe<br/>Always OK if running]
    end

    subgraph "Health Checks"
        HC1[Azure OpenAI Health Check<br/>Tags: external, ai<br/>Validates API connectivity]
        HC2[Memory Health Check<br/>Tags: internal<br/>Degraded if >90% usage]
    end

    H1 --> HC1 & HC2
    H2 --> HC1
    H3 -.->|No checks| H3

    subgraph "Response Format"
        RF["JSON Response:<br/>{<br/>  status: Healthy/Degraded/Unhealthy,<br/>  checks: [{name, status, duration}],<br/>  totalDuration: ms<br/>}"]
    end

    HC1 & HC2 --> RF

    style H1 fill:#27ae60,color:#fff
    style H2 fill:#f39c12,color:#fff
    style H3 fill:#3498db,color:#fff
```

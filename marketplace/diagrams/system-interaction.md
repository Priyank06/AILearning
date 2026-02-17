# System Interaction Diagrams - Legacy Code Analyzer

## 1. Multi-File Project Analysis - End-to-End Sequence

```mermaid
sequenceDiagram
    actor User
    participant UI as Blazor UI
    participant Hub as SignalR Hub
    participant MW as Middleware<br/>(CorrID + RateLimit)
    participant EPA as Enhanced Project<br/>Analysis Service
    participant FPP as File Pre-Processing<br/>Service
    participant MA as Metadata Extraction
    participant AR as Analyzer Router
    participant LA as Language Analyzers<br/>(Roslyn / TreeSitter)
    participant AO as Agent Orchestration<br/>Service
    participant Agents as Specialist Agents<br/>(Sec / Perf / Arch)
    participant SK as Semantic Kernel
    participant AOAI as Azure OpenAI
    participant BM as Business Metrics<br/>Calculator
    participant RS as Report Service

    User->>UI: Upload project files
    UI->>Hub: SignalR connection
    Hub->>MW: HTTP request
    MW->>MW: Assign Correlation ID
    MW->>MW: Check Rate Limit

    MW->>EPA: Analyze project
    activate EPA

    EPA->>FPP: Pre-process files
    activate FPP
    loop For each file
        FPP->>MA: Extract metadata
        MA->>AR: Route to analyzer
        AR->>LA: Parse & analyze code
        LA-->>AR: Code structure + metrics
        AR-->>MA: Analysis result
        MA-->>FPP: File metadata
    end
    FPP-->>EPA: Pre-processed files<br/>with metadata
    deactivate FPP

    EPA->>AO: Orchestrate multi-agent analysis
    activate AO

    par Parallel Agent Execution
        AO->>Agents: Security analysis
        Agents->>SK: Build prompt + send
        SK->>AOAI: Chat completion request
        AOAI-->>SK: LLM response
        SK-->>Agents: Parsed findings
    and
        AO->>Agents: Performance analysis
        Agents->>SK: Build prompt + send
        SK->>AOAI: Chat completion request
        AOAI-->>SK: LLM response
        SK-->>Agents: Parsed findings
    and
        AO->>Agents: Architecture analysis
        Agents->>SK: Build prompt + send
        SK->>AOAI: Chat completion request
        AOAI-->>SK: LLM response
        SK-->>Agents: Parsed findings
    end

    AO->>AO: Calculate consensus
    AO->>AO: Resolve conflicts
    AO->>AO: Synthesize recommendations
    AO->>AO: Generate executive summary

    AO-->>EPA: Orchestrated results
    deactivate AO

    EPA->>BM: Calculate business metrics
    BM-->>EPA: Risk + Cost + Impact

    EPA->>RS: Generate reports
    RS-->>EPA: Markdown/HTML reports

    EPA-->>MW: Complete analysis result
    deactivate EPA
    MW-->>Hub: Response
    Hub-->>UI: Real-time updates
    UI-->>User: Display results<br/>+ downloadable reports
```

## 2. Single File Upload & Analysis Sequence

```mermaid
sequenceDiagram
    actor User
    participant UI as Blazor UI
    participant FU as FileUpload Component
    participant IV as Input Validation
    participant CA as Code Analysis Service
    participant FPP as File Pre-Processing
    participant LD as Language Detector
    participant AR as Analyzer Router
    participant Analyzer as Language Analyzer
    participant AI as AI Analysis Service
    participant PB as Prompt Builder
    participant SK as Semantic Kernel
    participant AOAI as Azure OpenAI

    User->>UI: Select & upload file
    UI->>FU: Handle file input
    FU->>IV: Validate file<br/>(size, type, content)

    alt Validation fails
        IV-->>FU: Validation error
        FU-->>UI: Show error alert
    else Validation passes
        IV-->>FU: File accepted

        FU->>CA: Analyze single file
        activate CA

        CA->>FPP: Pre-process file
        FPP->>LD: Detect language
        LD-->>FPP: Language identified
        FPP->>AR: Route to analyzer
        AR->>Analyzer: Parse code structure
        Analyzer-->>AR: AST + metrics
        AR-->>FPP: Structured result
        FPP-->>CA: Pre-processed file

        CA->>PB: Build analysis prompt
        PB-->>CA: Formatted prompt

        CA->>AI: Request AI analysis
        AI->>SK: Chat completion
        SK->>AOAI: API call
        AOAI-->>SK: LLM response
        SK-->>AI: Raw response
        AI->>AI: Transform & validate result
        AI-->>CA: Analysis findings

        CA-->>FU: Complete analysis
        deactivate CA
        FU-->>UI: Render results
        UI-->>User: Display findings<br/>with severity + confidence
    end
```

## 3. Multi-Agent Orchestration Interaction

```mermaid
sequenceDiagram
    participant AO as Agent Orchestration
    participant AR as Agent Registry
    participant ACC as Agent Communication<br/>Coordinator
    participant SA as Security Agent
    participant PA as Performance Agent
    participant AA as Architecture Agent
    participant CC as Consensus Calculator
    participant CR as Conflict Resolver
    participant PR as Peer Review<br/>Coordinator
    participant RS as Recommendation<br/>Synthesizer
    participant ES as Executive Summary<br/>Generator
    participant AOAI as Azure OpenAI

    AO->>AR: Get registered agents
    AR-->>AO: [Security, Performance, Architecture]

    AO->>ACC: Coordinate analysis round

    par Agent Analysis Phase
        ACC->>SA: Analyze(code, context)
        SA->>AOAI: Security-focused prompt
        AOAI-->>SA: Security findings
    and
        ACC->>PA: Analyze(code, context)
        PA->>AOAI: Performance-focused prompt
        AOAI-->>PA: Performance findings
    and
        ACC->>AA: Analyze(code, context)
        AA->>AOAI: Architecture-focused prompt
        AOAI-->>AA: Architecture findings
    end

    ACC-->>AO: All agent results

    AO->>PR: Coordinate peer reviews
    PR->>SA: Review Performance findings
    PR->>PA: Review Security findings
    PR->>AA: Review both findings
    PR-->>AO: Peer review feedback

    AO->>CC: Calculate consensus
    CC->>CC: Aggregate scores
    CC->>CC: Weight by confidence
    CC-->>AO: Consensus result

    AO->>CR: Resolve conflicts
    CR->>CR: Identify contradictions
    CR->>CR: Apply resolution rules
    CR-->>AO: Resolved findings

    AO->>RS: Synthesize recommendations
    RS->>RS: Merge agent recommendations
    RS->>RS: Prioritize by impact
    RS-->>AO: Unified recommendations

    AO->>ES: Generate executive summary
    ES->>AOAI: Summary prompt
    AOAI-->>ES: Executive summary text
    ES-->>AO: Formatted summary

    AO-->>AO: Compile final report
```

## 4. Resilient Azure OpenAI Communication

```mermaid
sequenceDiagram
    participant Caller as Calling Service
    participant RCS as Resilient Chat<br/>Completion Service
    participant RP as Retry Policy<br/>(Polly)
    participant CB as Circuit Breaker<br/>(Polly)
    participant AOAI as Azure OpenAI<br/>Chat Completion
    participant AI as Application Insights

    Caller->>RCS: GetChatMessageContentAsync()
    activate RCS
    RCS->>RP: Execute with retry

    loop Retry attempts (max 3)
        RP->>CB: Check circuit state

        alt Circuit OPEN
            CB-->>RP: Circuit open - fail fast
            RP-->>RCS: CircuitBrokenException
            RCS->>AI: Log circuit open event
        else Circuit CLOSED / HALF-OPEN
            CB->>AOAI: API request
            alt Success (200)
                AOAI-->>CB: Chat completion response
                CB-->>RP: Success
                RP-->>RCS: Response
            else Rate Limited (429)
                AOAI-->>CB: 429 Too Many Requests
                CB-->>RP: Transient failure
                RP->>RP: Wait (exponential backoff)
                RCS->>AI: Log rate limit hit
            else Server Error (5xx)
                AOAI-->>CB: 500/503 error
                CB->>CB: Record failure
                CB-->>RP: Transient failure
                RP->>RP: Wait (exponential backoff)
                RCS->>AI: Log server error
            else Timeout
                AOAI-->>CB: Request timeout (300s)
                CB->>CB: Record failure
                CB-->>RP: Timeout exception
                RP->>RP: Wait (exponential backoff)
                RCS->>AI: Log timeout
            end
        end
    end

    alt All retries exhausted
        RCS-->>Caller: Throw exception
        RCS->>AI: Log final failure
    else Success within retries
        RCS-->>Caller: Return result
    end
    deactivate RCS
```

## 5. Client-Side Persistence & Encryption Flow

```mermaid
sequenceDiagram
    participant UI as Blazor Component
    participant BSS as Browser Storage Service
    participant SCI as Secure Client Interop
    participant EKS as Encryption Key Strategy<br/>(AES-GCM + KDF)
    participant BAR as Browser Analysis<br/>Repository
    participant BASR as Browser Agent<br/>Session Repository
    participant JS as JavaScript Interop
    participant IDB as IndexedDB
    participant SQLite as Browser SQLite

    UI->>BSS: Save analysis result
    BSS->>SCI: Encrypt & store

    alt First access (no key)
        SCI->>EKS: Generate encryption key
        EKS->>EKS: Derive key via KDF
        EKS->>JS: Store key material
        JS->>IDB: Persist encrypted key
    end

    SCI->>EKS: Get encryption key
    EKS-->>SCI: AES-GCM key

    SCI->>SCI: Encrypt data (AES-GCM)
    SCI->>BAR: Store encrypted analysis
    BAR->>JS: JSInterop call
    JS->>SQLite: INSERT encrypted blob

    Note over UI,SQLite: Retrieval flow

    UI->>BSS: Load analysis result
    BSS->>SCI: Retrieve & decrypt
    SCI->>BAR: Fetch encrypted data
    BAR->>JS: JSInterop call
    JS->>SQLite: SELECT encrypted blob
    SQLite-->>JS: Encrypted data
    JS-->>BAR: Raw bytes
    BAR-->>SCI: Encrypted result
    SCI->>EKS: Get decryption key
    SCI->>SCI: Decrypt (AES-GCM)
    SCI-->>BSS: Decrypted result
    BSS-->>UI: Analysis result
```

## 6. Startup & Configuration Sequence

```mermaid
sequenceDiagram
    participant Host as WebApplication Builder
    participant Cfg as Configuration System
    participant KV as Azure Key Vault
    participant DI as Dependency Injection
    participant SK as Semantic Kernel
    participant HC as Health Checks
    participant MW as Middleware Pipeline
    participant App as Running Application

    Host->>Cfg: Load appsettings.json
    Cfg->>Cfg: Bind IOptions<T> models<br/>(30+ configuration sections)
    Cfg->>Cfg: Validate configurations

    Host->>KV: Configure Key Vault (if enabled)
    KV-->>Cfg: Inject secrets<br/>(API keys, endpoints)

    Host->>Cfg: Validate required secrets<br/>(AzureOpenAI: ApiKey, Endpoint, Deployment)

    Host->>DI: Register services
    DI->>DI: AddCodeAnalysisServices()
    DI->>DI: AddMultiAgentOrchestration()
    DI->>DI: AddSemanticKernel()
    DI->>DI: AddKeyVaultService()
    DI->>DI: AddSanitizedLogging()
    DI->>DI: Register persistence services
    DI->>DI: Register infrastructure services

    Host->>SK: Configure Semantic Kernel
    SK->>SK: Register IChatCompletionService<br/>(with resilient wrapper)
    SK->>SK: Build Kernel

    Host->>HC: Register health checks
    HC->>HC: AzureOpenAI health check
    HC->>HC: Memory usage check

    Host->>MW: Configure middleware pipeline
    MW->>MW: ExceptionHandler
    MW->>MW: HSTS / HTTPS redirect
    MW->>MW: Static files
    MW->>MW: Correlation ID middleware
    MW->>MW: Rate limit middleware
    MW->>MW: Routing
    MW->>MW: Health check endpoints<br/>(/health, /health/ready, /health/live)
    MW->>MW: Blazor Hub + Razor Pages

    MW->>App: app.Run()
    App-->>Host: Listening on configured port
```

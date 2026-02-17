# mtSmartBuild: System Interaction Diagrams

## 1. End-to-End Code Analysis Flow (Sequence Diagram)

```mermaid
sequenceDiagram
    actor User as Business User / Developer
    participant UI as Blazor Server UI
    participant ORC as Agent Orchestration Service
    participant FP as File Pre-Processing
    participant LD as Language Detector
    participant AR as Analyzer Router
    participant AA as Architectural Analyst
    participant PA as Performance Analyst
    participant SA as Security Analyst
    participant AI as Azure OpenAI (GPT)
    participant RT as Result Transformer
    participant BM as Business Metrics Calculator
    participant RP as Report Service

    User->>UI: Upload code files & configure analysis
    UI->>FP: Pre-process uploaded files
    FP->>LD: Detect languages
    LD-->>FP: Language classifications
    FP->>AR: Route to appropriate analyzers
    AR->>AR: Roslyn (C#) / Tree-Sitter (Python, JS, TS, Java, Go)
    AR-->>FP: Static analysis metadata
    FP-->>UI: File metadata & complexity scores

    User->>UI: Start multi-agent analysis
    UI->>ORC: Initiate team analysis

    par Parallel Specialist Analysis
        ORC->>AA: Analyze architecture patterns
        AA->>AI: Submit architectural prompts
        AI-->>AA: Architectural findings
    and
        ORC->>PA: Analyze performance patterns
        PA->>AI: Submit performance prompts
        AI-->>PA: Performance findings
    and
        ORC->>SA: Analyze security patterns
        SA->>AI: Submit security prompts
        AI-->>SA: Security findings
    end

    AA-->>ORC: Architectural analysis results
    PA-->>ORC: Performance analysis results
    SA-->>ORC: Security analysis results

    ORC->>ORC: Consensus calculation & conflict resolution
    ORC->>RT: Transform & synthesize results
    RT-->>ORC: Unified analysis output

    ORC->>BM: Calculate business impact & ROI
    BM-->>ORC: Business metrics & risk scores

    ORC->>RP: Generate reports
    RP-->>UI: Executive summary + detailed reports
    UI-->>User: Display interactive analysis dashboard
```

## 2. Multi-Agent Orchestration Flow

```mermaid
sequenceDiagram
    participant UI as Blazor UI
    participant ORC as Agent Orchestration
    participant REG as Agent Registry
    participant RL as Rate Limiter
    participant CC as Communication Coordinator
    participant CACHE as Response Cache
    participant AA as Architectural Agent
    participant PA as Performance Agent
    participant SA as Security Agent
    participant CR as Conflict Resolver
    participant CON as Consensus Calculator
    participant PR as Peer Review Coordinator
    participant RS as Recommendation Synthesizer

    UI->>ORC: Start team analysis (files, config)
    ORC->>REG: Get registered agents
    REG-->>ORC: Available specialist agents

    loop For each code file/batch
        ORC->>RL: Check rate limits
        RL-->>ORC: Approved

        ORC->>CACHE: Check cached results
        alt Cache hit
            CACHE-->>ORC: Return cached analysis
        else Cache miss
            ORC->>CC: Coordinate agent communication
            par Dispatch to specialists
                CC->>AA: Assign architectural review
                CC->>PA: Assign performance review
                CC->>SA: Assign security review
            end
            AA-->>CC: Architectural findings
            PA-->>CC: Performance findings
            SA-->>CC: Security findings
            CC-->>ORC: Collected specialist results
            ORC->>CACHE: Store results
        end
    end

    ORC->>CR: Resolve conflicting findings
    CR-->>ORC: Resolved findings

    ORC->>CON: Calculate consensus scores
    CON-->>ORC: Confidence & agreement metrics

    ORC->>PR: Coordinate peer reviews
    PR-->>ORC: Cross-validated findings

    ORC->>RS: Synthesize recommendations
    RS-->>ORC: Prioritized recommendation list

    ORC-->>UI: Complete team analysis results
```

## 3. Data Flow: File Upload to Report Generation

```mermaid
flowchart LR
    subgraph "Input"
        A["Code Files<br/>(C#, Python, JS,<br/>TS, Java, Go)"]
    end

    subgraph "Pre-Processing"
        B["File Filtering<br/>& Validation"]
        C["Language<br/>Detection"]
        D["Metadata<br/>Extraction"]
        E["Complexity<br/>Calculation"]
        F["Pattern<br/>Detection"]
        G["Legacy Pattern<br/>Detection"]
    end

    subgraph "Static Analysis"
        H["Roslyn<br/>(C#)"]
        I["Tree-Sitter<br/>(Multi-Language)"]
        J["Cross-File<br/>Dependency Analysis"]
        K["Architecture<br/>Assessment"]
    end

    subgraph "AI Analysis"
        L["Prompt<br/>Construction"]
        M["Token<br/>Estimation"]
        N["Azure OpenAI<br/>GPT Analysis"]
        O["JSON<br/>Extraction"]
        P["Result<br/>Transformation"]
    end

    subgraph "Business Analysis"
        Q["Business Impact<br/>Calculation"]
        R["Risk<br/>Assessment"]
        S["Cost<br/>Tracking"]
        T["Recommendation<br/>Generation"]
    end

    subgraph "Output"
        U["Executive<br/>Summary Report"]
        V["Team Analysis<br/>Report"]
        W["Detailed<br/>Technical Report"]
        X["Interactive<br/>Dashboard"]
    end

    A --> B --> C --> D --> E --> F --> G
    G --> H & I
    H & I --> J --> K
    K --> L --> M --> N --> O --> P
    P --> Q & R & S & T
    Q & R & S & T --> U & V & W & X

    style A fill:#E8F5E9,stroke:#388E3C
    style N fill:#E3F2FD,stroke:#1976D2
    style U fill:#FFF3E0,stroke:#F57C00
    style V fill:#FFF3E0,stroke:#F57C00
    style W fill:#FFF3E0,stroke:#F57C00
    style X fill:#FFF3E0,stroke:#F57C00
```

## 4. Microsoft Ecosystem Integration Map

```mermaid
graph TB
    subgraph "mtSmartBuild Platform"
        MS["mtSmartBuild<br/>Framework Core"]
    end

    subgraph "Microsoft Power Platform"
        PA["Power Apps<br/>(Custom Business Apps)"]
        PP["Power Pages<br/>(External Portals)"]
        PAU["Power Automate<br/>(Workflow Automation)"]
        PBI["Power BI<br/>(Analytics & Dashboards)"]
        CS["Copilot Studio<br/>(AI Assistants)"]
    end

    subgraph "Microsoft 365"
        Teams["Microsoft Teams"]
        SP["SharePoint"]
        OL["Outlook"]
        Excel["Excel"]
    end

    subgraph "Microsoft Azure"
        AOAI["Azure OpenAI Service"]
        AKV["Azure Key Vault"]
        AAI["App Insights"]
        AAD["Microsoft Entra ID"]
        ABS["Azure Blob Storage"]
        AAIF["Azure AI Foundry"]
        AIB["AI Builder"]
    end

    subgraph "Dynamics 365"
        D365S["Sales"]
        D365CS["Customer Service"]
        D365F["Finance"]
    end

    subgraph "Data Connectors"
        DC["400+ Pre-built<br/>Connectors"]
        CC["Custom Connectors"]
        DV["Dataverse"]
    end

    MS --> PA & PP
    MS --> AOAI & AKV & AAI
    PA --> PAU & PBI & CS
    PA --> Teams & SP & OL & Excel
    PA --> D365S & D365CS & D365F
    PA --> DC & CC & DV
    PP --> AAD
    PP --> DV
    CS --> AOAI & AAIF & AIB
    PAU --> DC

    style MS fill:#FFEBEE,stroke:#D32F2F,stroke-width:3px
    style PA fill:#E3F2FD,stroke:#1976D2
    style PP fill:#E3F2FD,stroke:#1976D2
    style AOAI fill:#E8F5E9,stroke:#388E3C
    style Teams fill:#F3E5F5,stroke:#7B1FA2
```

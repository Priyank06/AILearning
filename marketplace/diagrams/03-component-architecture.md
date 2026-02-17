# mtSmartBuild: Component Architecture Diagrams

## 1. Service Layer Component Diagram

```mermaid
graph TB
    subgraph "Presentation Layer"
        direction LR
        CFG["Configuration<br/>Components"]
        ANA["Analysis<br/>Components"]
        MFA["Multi-File Analysis<br/>Components"]
        GT["Ground Truth<br/>Components"]
        DET["Determinism<br/>Components"]
        SHR["Shared UI<br/>Components"]
    end

    subgraph "Orchestration Layer"
        direction LR
        AOS["Agent Orchestration<br/>Service"]
        ACC["Agent Communication<br/>Coordinator"]
        BAO["Batch Analysis<br/>Orchestrator"]
        PRC["Peer Review<br/>Coordinator"]
        CRS["Conflict Resolver"]
        CON["Consensus Calculator"]
        RCS["Recommendation<br/>Synthesizer"]
    end

    subgraph "AI Services Layer"
        direction LR
        AIA["AI Analysis Service"]
        PBD["Prompt Builder"]
        RJE["Robust JSON<br/>Extractor"]
        RST["Result Transformer"]
        TKE["Token Estimation"]
        RCC["Resilient Chat<br/>Completion"]
        LCF["Legacy Context<br/>Formatter"]
        ESG["Executive Summary<br/>Generator"]
    end

    subgraph "Analysis Layer"
        direction LR
        ANR["Analyzer Router"]
        LNG["Language<br/>Detector"]
        ROS["Roslyn C#<br/>Analyzer"]
        TSR["Tree-Sitter<br/>Language Registry"]
        TSP["Tree-Sitter<br/>Python Analyzer"]
        TSJ["Tree-Sitter<br/>JavaScript Analyzer"]
        TST["Tree-Sitter<br/>TypeScript Analyzer"]
        TSJA["Tree-Sitter<br/>Java Analyzer"]
        TSG["Tree-Sitter<br/>Go Analyzer"]
        HML["Hybrid Multi-Language<br/>Analyzer"]
    end

    subgraph "Specialist Agent Layer"
        direction LR
        AAA["Architectural<br/>Analyst Agent"]
        PAA["Performance<br/>Analyst Agent"]
        SAA["Security<br/>Analyst Agent"]
        ARS["Agent Registry<br/>Service"]
        ARL["Agent Rate<br/>Limiter"]
    end

    subgraph "Business Services Layer"
        direction LR
        BIC["Business Impact<br/>Calculator"]
        BMC["Business Metrics<br/>Calculator"]
        RAS["Risk Assessment<br/>Service"]
        RGS["Recommendation<br/>Generator"]
        CTS["Cost Tracking<br/>Service"]
    end

    subgraph "Infrastructure Layer"
        direction LR
        FPS["File Pre-Processing"]
        FCM["File Cache<br/>Manager"]
        FFS["File Filtering"]
        FDS["File Download"]
        RLS["Rate Limit<br/>Service"]
        RDS["Request<br/>Deduplication"]
        IVS["Input<br/>Validation"]
        EHS["Error<br/>Handling"]
        LSS["Log<br/>Sanitization"]
        TRS["Tracing<br/>Service"]
    end

    subgraph "Quality Assurance Layer"
        direction LR
        GTV["Ground Truth<br/>Validation"]
        DMS["Determinism<br/>Measurement"]
        CVS["Confidence<br/>Validation"]
        FVS["Finding<br/>Validation"]
    end

    subgraph "Persistence Layer"
        direction LR
        BSS["Browser Storage<br/>Service"]
        BAR["Browser Analysis<br/>Repository"]
        BPR["Browser Preferences<br/>Repository"]
        SCI["Secure Client<br/>Interop"]
        EKS["Encryption Key<br/>Strategy"]
        ARC["Agent Response<br/>Cache"]
    end

    CFG & ANA & MFA --> AOS
    AOS --> ACC & BAO & PRC
    AOS --> AAA & PAA & SAA
    AOS --> ARS & ARL
    AAA & PAA & SAA --> AIA
    AIA --> PBD & RJE & RST & TKE & RCC
    AIA --> LCF & ESG
    AOS --> ANR
    ANR --> LNG & ROS & TSR
    TSR --> TSP & TSJ & TST & TSJA & TSG
    ANR --> HML
    AOS --> BIC & BMC & RAS & RGS & CTS
    PRC --> CRS & CON & RCS
    AOS --> FPS & FCM & RLS & RDS
    ANA --> GTV & DMS
    GTV --> CVS & FVS
    BSS --> BAR & BPR & SCI & EKS
    AIA --> ARC

    style AOS fill:#FFEBEE,stroke:#D32F2F,stroke-width:2px
    style AIA fill:#E3F2FD,stroke:#1976D2,stroke-width:2px
    style ANR fill:#E8F5E9,stroke:#388E3C,stroke-width:2px
```

## 2. Deployment Architecture

```mermaid
graph TB
    subgraph "Azure Cloud"
        subgraph "Azure App Service"
            ASP["App Service Plan<br/>(Linux/Windows)"]
            WEB["Web App<br/>(Blazor Server)"]
        end

        subgraph "Azure AI Services"
            AOAI["Azure OpenAI<br/>GPT-4 / GPT-4o"]
            AAIF["Azure AI Foundry"]
            AIB["AI Builder"]
        end

        subgraph "Security & Identity"
            KV["Azure Key Vault<br/>(API Keys, Secrets)"]
            AAD["Microsoft Entra ID<br/>(Authentication)"]
        end

        subgraph "Monitoring & Observability"
            AI["Application Insights"]
            LA["Log Analytics<br/>Workspace"]
            AM["Azure Monitor"]
        end

        subgraph "Data Platform"
            DV["Dataverse"]
            SQL["Azure SQL<br/>(Optional)"]
            BS["Blob Storage<br/>(File Staging)"]
        end
    end

    subgraph "Client Browser"
        BW["Modern Web Browser"]
        IDB["IndexedDB /<br/>Encrypted SQLite"]
    end

    subgraph "Power Platform"
        PA["Power Apps"]
        PP["Power Pages"]
        PAU["Power Automate"]
        PBI["Power BI"]
    end

    BW <-->|"SignalR<br/>WebSocket"| WEB
    BW --> IDB
    WEB --> ASP
    WEB -->|"REST API"| AOAI
    WEB --> KV
    WEB --> AI
    AI --> LA --> AM
    WEB --> AAD
    PA & PP --> DV
    PA --> PAU
    PA --> PBI
    PA --> AOAI
    PP --> AAD

    style WEB fill:#FFEBEE,stroke:#D32F2F,stroke-width:2px
    style AOAI fill:#E3F2FD,stroke:#1976D2,stroke-width:2px
    style KV fill:#FFF3E0,stroke:#F57C00
    style DV fill:#E8F5E9,stroke:#388E3C
```

## 3. Security Architecture

```mermaid
graph TB
    subgraph "Identity & Access"
        AAD["Microsoft Entra ID<br/>(SSO / MFA)"]
        RBAC["Role-Based<br/>Access Control"]
    end

    subgraph "Network Security"
        TLS["TLS 1.3<br/>Encryption"]
        WAF["Web Application<br/>Firewall"]
    end

    subgraph "Application Security"
        IV["Input Validation<br/>Service"]
        LS["Log Sanitization<br/>(PII Removal)"]
        RL["Rate Limiting<br/>(DDoS Protection)"]
        RD["Request<br/>Deduplication"]
        EH["Error Handling<br/>(No Stack Leak)"]
    end

    subgraph "Data Security"
        KV["Azure Key Vault<br/>(Secret Management)"]
        CSE["Client-Side<br/>Encryption"]
        PBK["Per-Browser<br/>Encryption Keys"]
        BS["Browser Secure<br/>Storage (SQLite)"]
    end

    subgraph "Monitoring"
        AI["Application Insights<br/>(Anomaly Detection)"]
        HC["Health Checks<br/>(Azure OpenAI, Memory)"]
    end

    AAD --> RBAC
    TLS --> WAF
    WAF --> IV --> LS
    IV --> RL --> RD --> EH
    KV --> CSE
    CSE --> PBK --> BS
    EH --> AI
    AI --> HC

    style AAD fill:#E3F2FD,stroke:#1976D2,stroke-width:2px
    style KV fill:#FFF3E0,stroke:#F57C00,stroke-width:2px
    style AI fill:#F3E5F5,stroke:#7B1FA2
```

## 4. Power Apps & Power Pages Integration Architecture

```mermaid
graph TB
    subgraph "Power Apps Layer"
        direction TB
        MA["Model-Driven Apps<br/>(Complex Business Logic)"]
        CA["Canvas Apps<br/>(Custom UI/UX)"]
        PA["Portal Apps<br/>(External Users)"]
    end

    subgraph "Power Pages Layer"
        direction TB
        CSP["Citizen Service Portal"]
        PP["Patient Portal"]
        VP["Vendor Portal"]
        AP["Admission Portal"]
    end

    subgraph "Automation Layer"
        direction LR
        PAU["Power Automate<br/>(Workflow Engine)"]
        CF["Cloud Flows"]
        DF["Desktop Flows<br/>(RPA)"]
    end

    subgraph "AI Layer"
        direction LR
        CS["Copilot Studio<br/>(Conversational AI)"]
        AIB["AI Builder<br/>(Custom AI Models)"]
        AOAI["Azure OpenAI<br/>(Advanced AI)"]
        AIF["Azure AI Foundry"]
    end

    subgraph "Data Layer"
        direction LR
        DV["Dataverse<br/>(Unified Data)"]
        CON["400+ Connectors"]
        API["Custom APIs"]
    end

    subgraph "Governance Layer"
        direction LR
        COE["Center of Excellence<br/>Toolkit"]
        DLP["Data Loss Prevention<br/>Policies"]
        ENV["Environment<br/>Management"]
        ALM["Application Lifecycle<br/>Management"]
    end

    MA & CA --> PAU
    PA --> DV
    CSP & PP & VP & AP --> DV
    PAU --> CF & DF
    CS & AIB --> AOAI & AIF
    MA & CA & PA --> DV & CON & API
    COE --> DLP & ENV & ALM

    style DV fill:#E8F5E9,stroke:#388E3C,stroke-width:2px
    style AOAI fill:#E3F2FD,stroke:#1976D2,stroke-width:2px
    style COE fill:#FFF3E0,stroke:#F57C00,stroke-width:2px
```

# ADR-006: Azure OpenAI as the LLM Provider

## Status
Retrospective

## Context
The application requires a large language model capable of nuanced code analysis, security vulnerability identification, and structured output generation. Two primary options were considered: the public OpenAI API and Azure OpenAI Service.

## Decision
Use **Azure OpenAI Service** (GPT-3.5-turbo for lower-cost calls, GPT-4 for high-fidelity analysis) as the exclusive LLM provider. Credentials are managed via `AzureOpenAI:Endpoint`, `AzureOpenAI:ApiKey`, and `AzureOpenAI:Deployment` configuration keys, with optional Azure Key Vault for secret management.

## Consequences
### Positive
- Azure OpenAI is deployed within the Microsoft Azure trust boundary — data residency and compliance requirements are more easily satisfied than with the public OpenAI API.
- Seamless integration with Azure Key Vault, Application Insights, and DefaultAzureCredential for managed identity authentication.
- Enterprise SLA, content filtering, and abuse monitoring are provided by the platform.
- Same models (GPT-3.5, GPT-4) as the public API; Semantic Kernel supports both with minimal configuration change.
### Negative
- Requires provisioning an Azure OpenAI resource and a model deployment before the app can run.
- Model availability and quota are managed per Azure subscription, which can create friction during development.
- Switching to a non-Azure provider (e.g., Anthropic, Google) would require replacing the chat completion registration and credential model.

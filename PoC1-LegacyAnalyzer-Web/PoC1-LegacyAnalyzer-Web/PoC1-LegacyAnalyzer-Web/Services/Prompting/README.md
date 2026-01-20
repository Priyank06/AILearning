# Prompting Services

This folder contains services related to prompt building, prompt transformation, and JSON extraction from LLM responses.

## Purpose

Services in this folder handle:
- **Prompt Building** - Constructing prompts for LLM calls
- **Result Transformation** - Transforming LLM responses into structured data
- **JSON Extraction** - Robustly extracting JSON from LLM responses

## Services

- `PromptBuilderService` - Builds prompts for LLM analysis
- `ResultTransformerService` - Transforms LLM responses to structured models
- `RobustJsonExtractor` - Extracts JSON from LLM responses with error handling
- `LegacyContextFormatter` - Formats legacy code context for prompts

## Dependencies

These services depend on:
- Configuration models for prompt templates
- `Services/Infrastructure/` - Logging, error handling

## Lifetime

Prompting services are registered as `Scoped` for per-request isolation.

## Usage

Prompting services are used by:
- `Services/AI/` - Agent implementations use prompt builders
- `Services/Orchestration/` - Orchestration uses result transformers

## Related Folders

- `Services/AI/` - Agents that use prompting services
- `Models/` - Prompt templates and configuration


# Validation Services

This folder contains services for validating inputs, findings, and analysis results.

## Purpose

Services in this folder handle:
- **Input Validation** - Validating user inputs and requests
- **Finding Validation** - Validating analysis findings
- **Confidence Validation** - Validating confidence scores and metrics

## Services

- `InputValidationService` - Input and request validation
- `FindingValidationService` - Analysis finding validation
- `ConfidenceValidationService` - Confidence score validation

## Dependencies

These services depend on:
- Configuration models for validation rules
- `Services/Infrastructure/` - Logging

## Lifetime

Validation services are registered as `Scoped` for per-request isolation.

## Usage

Validation services are used by:
- `Services/Orchestration/` - Validating inputs before orchestration
- `Services/AI/` - Validating agent outputs
- `Services/CodeAnalysis/` - Validating analysis results


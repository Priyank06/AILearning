# Business Services

This folder contains services that implement business logic, calculations, and domain-specific rules. These services translate technical analysis into business metrics and recommendations.

## Purpose

Services in this folder handle:
- **Business Metrics** - Calculating business impact metrics (cost, effort, risk)
- **Business Impact** - Assessing business impact of technical findings
- **Cost Tracking** - Tracking and calculating costs
- **Risk Assessment** - Business risk assessment
- **Recommendation Generation** - Generating business-focused recommendations

## Services

- `BusinessMetricsCalculator` - Calculates business metrics from technical analysis
- `BusinessImpactCalculator` - Calculates business impact
- `CostTrackingService` - Tracks and calculates costs
- `RiskAssessmentService` - Assesses business risk
- `RecommendationGeneratorService` - Generates business recommendations

## Dependencies

These services depend on:
- `Services/CodeAnalysis/` - Technical analysis results
- `Services/Infrastructure/` - Logging, tracing
- Configuration models for business rules

## Lifetime

Business services are registered as `Scoped` for per-request isolation.

## Related Folders

- `Services/Orchestration/` - Uses business services to calculate impact
- `Models/` - Business calculation rules and configuration


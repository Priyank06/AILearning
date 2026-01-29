# Hosting and Pilot Program Notes

**Document Purpose**: Considerations for hosting this PoC as a production pilot
**Last Updated**: 2026-01-29
**Status**: Pre-Pilot Planning

---

## Executive Summary

This document outlines key considerations for transitioning the Legacy Code Analyzer PoC to a hosted pilot program. Management should review these areas before deploying to real users with production codebases.

---

## 1. Infrastructure and Hosting Requirements

### 1.1 Azure App Service Configuration

| Resource | Recommended (Pilot) | Production Scale |
|----------|---------------------|------------------|
| App Service Plan | S2/S3 Standard | P2v3 Premium |
| Memory | 4-8 GB | 16+ GB |
| CPU | 2 cores | 4+ cores |
| Region | Single region | Multi-region with Traffic Manager |

### 1.2 Azure OpenAI Resource Planning

| Consideration | Details |
|---------------|---------|
| TPM (Tokens Per Minute) | Start with 60K TPM, scale based on usage |
| Deployment Model | `gpt-35-turbo` (current), consider `gpt-4` for critical analyses |
| Rate Limiting | Built-in circuit breaker handles throttling |
| Regional Availability | Ensure region has Azure OpenAI capacity |
| Quota Requests | Submit quota increase requests 2-4 weeks before pilot |

### 1.3 Estimated Infrastructure Costs (Monthly)

| Component | Pilot (10 users) | Expanded Pilot (50 users) |
|-----------|------------------|---------------------------|
| Azure App Service (S2) | ~$150 | ~$300 (S3) |
| Azure OpenAI (gpt-35-turbo) | ~$100-300 | ~$500-1,500 |
| Key Vault | ~$5 | ~$5 |
| Application Insights | ~$30 | ~$100 |
| Storage (logs, cache) | ~$10 | ~$50 |
| **Total Estimate** | **~$300-500/mo** | **~$1,000-2,000/mo** |

*Note: Azure OpenAI costs are highly variable based on codebase sizes and analysis frequency.*

### 1.4 Required Azure Resources Checklist

- [ ] Azure App Service (with deployment slots for staging)
- [ ] Azure Key Vault (with Managed Identity access)
- [ ] Azure OpenAI resource with approved quota
- [ ] Application Insights workspace
- [ ] Azure Monitor alerts configured
- [ ] Azure Storage (for large report exports, if needed)
- [ ] Azure AD App Registration (if SSO required)

---

## 2. Security and Compliance Considerations

### 2.1 Data Handling

| Data Type | Handling | Retention |
|-----------|----------|-----------|
| Uploaded Source Code | In-memory processing only | Not persisted after session |
| AI Analysis Results | Session-only by default | Optional export to user's device |
| User Session Data | Standard Blazor Server state | Cleared on disconnect |
| Logs (Application Insights) | Sanitized via LogSanitizationService | 90-day default |

### 2.2 Code Confidentiality Concerns

**Critical Consideration**: Source code is sent to Azure OpenAI for analysis.

| Concern | Mitigation |
|---------|------------|
| Code sent to LLM | Azure OpenAI does NOT train on customer data |
| Data residency | Deploy in same region as OpenAI resource |
| Network security | HTTPS only, consider Private Endpoints for production |
| Code preview truncation | Max 1200 chars per file reduces exposure |

**Recommendation**: Obtain written acknowledgment from pilot users about AI processing of their code.

### 2.3 Access Control

| Level | Recommendation |
|-------|----------------|
| Application Access | Azure AD authentication (SSO) |
| Role-Based Access | Admin, Analyst, Viewer roles |
| API Access | API keys or OAuth2 for programmatic access |
| Audit Trail | All analyses logged with user identity |

### 2.4 Compliance Checklist

- [ ] Privacy Impact Assessment (PIA) completed
- [ ] Data Processing Agreement (DPA) with Azure
- [ ] Internal security review completed
- [ ] Penetration testing scheduled
- [ ] GDPR/data residency requirements documented
- [ ] User consent flow for AI processing implemented
- [ ] Log retention policy defined

---

## 3. Pilot Program Structure

### 3.1 Pilot Phases

| Phase | Duration | Users | Scope |
|-------|----------|-------|-------|
| Alpha | 2 weeks | 3-5 internal | Test infrastructure, workflows |
| Limited Pilot | 4-6 weeks | 10-15 | Real projects, feedback collection |
| Extended Pilot | 6-8 weeks | 25-50 | Cross-team validation |
| GA Decision | Week 12-16 | - | Go/No-Go based on metrics |

### 3.2 Pilot User Selection Criteria

Recommended pilot participant profile:
- Teams with active legacy modernization initiatives
- Mix of C# (strong support) and other languages (growing support)
- Varying codebase sizes (small <10k LOC, medium 10-100k LOC)
- Users comfortable providing constructive feedback
- Representation from Security, Architecture, and Dev teams

### 3.3 Success Metrics and KPIs

| Metric | Target | Measurement |
|--------|--------|-------------|
| **User Adoption** | 70%+ weekly active users | Application Insights |
| **Analysis Completion Rate** | >95% | Success/failure logs |
| **Finding Accuracy (Precision)** | >80% | User validation surveys |
| **Finding Recall** | >70% | Comparison with known issues |
| **Determinism Score** | >0.75 (Good) | Built-in measurement |
| **User Satisfaction (NPS)** | >40 | Survey |
| **Time to First Analysis** | <5 minutes | Session tracking |
| **Mean Analysis Time** | <2 min for 10 files | Performance logs |

### 3.4 Feedback Collection Mechanisms

| Method | Frequency | Purpose |
|--------|-----------|---------|
| In-app feedback button | Continuous | Quick reactions to findings |
| Weekly survey | Weekly | Structured usability feedback |
| User interviews | Bi-weekly | Deep dive on workflows |
| Bug reports (GitHub/Jira) | Continuous | Technical issues |
| Usage analytics | Continuous | Behavior patterns |

---

## 4. Known Limitations to Communicate

### 4.1 Must Communicate to Pilot Users

| Limitation | Impact | Workaround |
|------------|--------|------------|
| **File-level analysis only** | Cross-file dependencies not detected | Manual architectural review still needed |
| **C# analysis is strongest** | Python/JS/Java have limited pattern detection | Set expectations for non-C# projects |
| **AI may hallucinate** | Some findings may reference non-existent code | Users must verify findings against source |
| **Large files truncated** | Context lost for files >1200 chars | Consider splitting large files |
| **No IDE integration** | No VS/VS Code extension yet | Export reports manually |
| **Single user sessions** | No shared/team analysis views | Export and share reports |

### 4.2 Pilot Scope Exclusions

Clearly communicate what is NOT included:
- Automated code fixes/refactoring
- Integration with CI/CD pipelines
- Historical trend tracking
- Multi-repository analysis
- Custom rule creation by users
- Offline/disconnected mode

---

## 5. Operational Readiness

### 5.1 Monitoring and Alerting

| Alert | Threshold | Action |
|-------|-----------|--------|
| Error rate | >5% in 5 min | Page on-call |
| Response time (P95) | >10 seconds | Investigate |
| Azure OpenAI failures | >3 in 1 min | Circuit breaker activates (auto) |
| Memory usage | >80% | Scale up consideration |
| Health check failures | 2 consecutive | Auto-restart |

### 5.2 Incident Response

| Severity | Response Time | Example |
|----------|---------------|---------|
| P1 - Critical | 15 minutes | Service completely down |
| P2 - High | 1 hour | Analysis failures >50% |
| P3 - Medium | 4 hours | Feature degradation |
| P4 - Low | 24 hours | UI glitches, minor bugs |

### 5.3 Maintenance Windows

- **Planned maintenance**: Weekends, 2AM-6AM local time
- **Deployment strategy**: Blue-green with staging slot
- **Rollback capability**: One-click via deployment slots
- **Notification**: 48 hours advance notice for planned downtime

### 5.4 Support Model for Pilot

| Support Type | Channel | Response SLA |
|--------------|---------|--------------|
| Bug reports | Email/Jira | 24-48 hours |
| How-to questions | Teams channel | Same day |
| Feature requests | Feedback form | Triaged weekly |
| Critical issues | Direct escalation | 4 hours |

---

## 6. Pre-Launch Checklist

### 6.1 Technical Readiness

- [ ] Load testing completed (target: 20 concurrent users)
- [ ] Security scan completed (no critical findings)
- [ ] Azure Key Vault secrets configured
- [ ] Application Insights dashboards created
- [ ] Health check endpoints verified (`/health`, `/health/ready`)
- [ ] Error handling tested (graceful degradation works)
- [ ] Backup/restore procedures documented
- [ ] SSL certificates configured and valid
- [ ] Custom domain configured (if applicable)

### 6.2 Documentation Ready

- [ ] User guide/quick start documentation
- [ ] FAQ document for common questions
- [ ] Known limitations document (share with users)
- [ ] Admin guide for operations team
- [ ] Incident response runbook
- [ ] Architecture diagram for stakeholders

### 6.3 Organizational Readiness

- [ ] Pilot user list finalized and communicated
- [ ] Stakeholder buy-in confirmed
- [ ] Budget approved for pilot duration
- [ ] Support team trained
- [ ] Feedback channels established
- [ ] Success criteria agreed upon
- [ ] Exit criteria defined (see below)

---

## 7. Go/No-Go Decision Framework

### 7.1 Pilot Exit Criteria

| Outcome | Criteria | Action |
|---------|----------|--------|
| **Expand to GA** | All KPIs met, NPS >40, <5 P1/P2 bugs | Full rollout planning |
| **Extend Pilot** | Most KPIs met, fixable issues identified | 4-week extension |
| **Pivot** | Core assumptions invalid, major redesign needed | Reassess approach |
| **Stop** | Security issues, unacceptable accuracy, no user adoption | Archive and lessons learned |

### 7.2 Decision Points

| Milestone | Date (Relative) | Decision |
|-----------|-----------------|----------|
| Alpha complete | Week 2 | Proceed to limited pilot? |
| Mid-pilot review | Week 6 | Adjust scope/resources? |
| Pilot complete | Week 12 | GA, extend, or stop? |

---

## 8. Risk Register

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Azure OpenAI rate limiting | Medium | High | Pre-approved quota, circuit breaker |
| Low user adoption | Medium | High | Targeted onboarding, quick wins focus |
| False positive findings erode trust | Medium | High | Improve evidence verification, user education |
| Performance issues with large codebases | Medium | Medium | File/batch limits, async processing |
| Data sensitivity concerns | Low | Critical | Clear DPA, consent flow, no persistence |
| Key personnel unavailable | Low | Medium | Cross-training, documentation |
| Cost overruns from high usage | Medium | Medium | Usage monitoring, alerts at 80% budget |

---

## 9. Post-Pilot Roadmap (If Successful)

### Short-term (0-3 months post-pilot)
- Address top feedback items
- Implement evidence verification (critical gap)
- Add user authentication (Azure AD SSO)
- Create VS Code extension for inline findings

### Medium-term (3-6 months)
- Cross-file architectural analysis
- Language-specific pattern expansion
- CI/CD integration (GitHub Actions, Azure DevOps)
- Team/project dashboard views

### Long-term (6-12 months)
- Historical trend tracking
- Custom rule creation
- Multi-repository analysis
- API for third-party integrations

---

## 10. Contacts and Escalation

| Role | Name | Contact |
|------|------|---------|
| Product Owner | [TBD] | |
| Technical Lead | [TBD] | |
| Operations/SRE | [TBD] | |
| Security Contact | [TBD] | |
| Executive Sponsor | [TBD] | |

---

## Appendix A: Azure OpenAI Data Privacy Statement

For pilot communication, include this clarification:

> **Data Handling by Azure OpenAI Service:**
> - Your code is processed by Azure OpenAI Service
> - Microsoft does NOT use customer data to train, retrain, or improve Azure OpenAI models
> - Data is encrypted in transit (TLS 1.2+) and at rest
> - Data is processed in the Azure region of your OpenAI resource
> - For details: [Azure OpenAI Data Privacy](https://learn.microsoft.com/azure/ai-services/openai/concepts/data-privacy)

---

## Appendix B: Sample User Consent Statement

> **Consent for AI-Powered Code Analysis**
>
> By using this application, you acknowledge that:
> 1. Source code you upload will be sent to Azure OpenAI for analysis
> 2. Analysis results are AI-generated and should be verified by humans
> 3. Microsoft/Azure does not train on your code
> 4. You have authorization to upload the code you are analyzing
> 5. Analysis results are not persisted beyond your session unless you export them

---

## Appendix C: Quick Reference Card for Pilot Users

```
LEGACY CODE ANALYZER - PILOT QUICK REFERENCE

GET STARTED:
1. Navigate to application URL
2. Select files or folder to analyze
3. Click "Analyze" and wait for results
4. Review findings by category (Security, Performance, Architecture)
5. Export report if needed

WHAT IT DOES WELL:
- C# code analysis (security, patterns, complexity)
- Business impact translation
- Prioritized recommendations

CURRENT LIMITATIONS:
- File-by-file analysis (no cross-file dependencies)
- Large files are truncated
- Some findings may need verification

PROVIDE FEEDBACK:
- Use in-app feedback button
- Email: [pilot-feedback@company.com]
- Weekly survey link: [TBD]

NEED HELP?
- Teams channel: #legacy-analyzer-pilot
- FAQ: [link]
- Report bugs: [link]
```

---

*This document should be reviewed and updated as the pilot progresses.*

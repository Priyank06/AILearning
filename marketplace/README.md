# mtSmartBuild - Microsoft AppSource Marketplace Artifacts

## Overview

This directory contains the architecture diagrams, documentation, and keyword strategy for publishing the **mtSmartBuild: AI-Powered Low-Code Solutions driving Rapid Innovation** offering on Microsoft AppSource (appsource.microsoft.com).

**Offering ID:** `mtSmartBuild_ai_powered_low_code_apps`
**Specialization:** Business Applications > Microsoft Low Code Application Development
**Products:** Power Platform - Power Apps | Power Platform - Power Pages

---

## Directory Structure

```
marketplace/
├── README.md                          (this file)
├── diagrams/
│   ├── 01-high-level-architecture.md      High-level system architecture
│   ├── 02-system-interaction-diagram.md   Sequence & data flow diagrams
│   ├── 03-component-architecture.md       Component, deployment & security diagrams
│   └── 04-industry-solution-architecture.md  Industry-specific solution diagrams
└── docs/
    ├── mtSmartBuild-Marketplace-Listing.md   Full listing document (summary, description, getting started)
    └── mtSmartBuild-Keywords.md              Keyword strategy and SEO guidelines
```

---

## Architecture Diagrams

All diagrams use **Mermaid** syntax and can be rendered in:
- GitHub / Azure DevOps markdown preview
- VS Code with Mermaid extension
- Any Mermaid-compatible renderer (mermaid.live)
- Exported to PNG/SVG for inclusion in Word documents

### Diagram Catalog

| # | File | Contents |
|---|------|----------|
| 01 | `high-level-architecture.md` | Full system architecture overview with all layers, Microsoft ecosystem integration map |
| 02 | `system-interaction-diagram.md` | End-to-end code analysis sequence diagram, multi-agent orchestration flow, data flow diagram, Microsoft ecosystem integration map |
| 03 | `component-architecture.md` | Service layer components, deployment architecture, security architecture, Power Platform integration architecture |
| 04 | `industry-solution-architecture.md` | Healthcare, Manufacturing, Financial Services, Government, and Education solution architectures |

### How to Render Diagrams

**Option 1: GitHub Preview**
Push to GitHub and view the `.md` files directly - GitHub renders Mermaid natively.

**Option 2: Mermaid Live Editor**
Copy any Mermaid code block to [mermaid.live](https://mermaid.live) and export as PNG/SVG.

**Option 3: VS Code**
Install the "Markdown Preview Mermaid Support" extension and preview the files.

**Option 4: CLI Export**
```bash
npm install -g @mermaid-js/mermaid-cli
mmdc -i diagrams/01-high-level-architecture.md -o output.png
```

---

## Documentation

| Document | Purpose |
|----------|---------|
| `mtSmartBuild-Marketplace-Listing.md` | Complete AppSource listing content: summary, detailed description, service offerings, industry use cases, getting started guide, and support information |
| `mtSmartBuild-Keywords.md` | Primary AppSource keywords (3), extended keyword list across 6 tiers, SEO guidelines, and meta tag recommendations |

---

## AppSource Listing Checklist

- [ ] **Offer Name:** mtSmartBuild: AI-Powered Low-Code Solutions driving Rapid Innovation
- [ ] **Offer ID:** mtSmartBuild_ai_powered_low_code_apps
- [ ] **Short Description:** Provided in listing document
- [ ] **Detailed Description:** Provided in listing document
- [ ] **Search Keywords (3):** mtSmartBuild, Power Apps, Power Pages
- [ ] **Privacy Policy Link:** (Marketing to provide)
- [ ] **Logo (216-350px square, PNG):** (Marketing to provide)
- [ ] **Screenshots (1280x720 PNG, up to 5):** (Practice/Marketing to create)
- [ ] **Supplemental Materials:** Architecture diagrams, white papers, brochures
- [ ] **Markets:** Up to 141 markets (confirm exclusions)
- [ ] **Price & Currency:** (Practice to provide with approvals)

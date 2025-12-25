# Quick Start Guide - UI Improvements

## Overview
This guide provides a quick reference for using the new standardized UI components created during the UI improvement implementation.

---

## 🎨 New Shared Components

### 1. CardHeader Component
**Location:** `Components/Shared/CardHeader.razor`

**Usage:**
```razor
<CardHeader Title="Card Title"
            Subtitle="Optional subtitle text"
            Icon="bi bi-icon-name"
            Variant="success"
            BadgeText="Optional badge"
            BadgeVariant="light" />
```

**Variants:** `primary`, `success`, `warning`, `danger`, `dark`, `info`

**Example:**
```razor
<CardHeader Title="Code Quality Assessment"
            Subtitle="Maintenance optimization opportunities"
            Icon="bi bi-shield-check"
            Variant="success" />
```

---

### 2. MetricCard Component
**Location:** `Components/Shared/MetricCard.razor`

**Usage:**
```razor
<MetricCard Value="123"
           Label="Hours Saved"
           SubLabel="vs Manual Analysis"
           Icon="bi bi-clock-history"
           Color="primary"
           Hoverable="true" />
```

**Colors:** `primary`, `success`, `warning`, `danger`, `info`, `secondary`

**Responsive:** Automatically stacks on mobile, 2x2 on tablet, 4-column on desktop

---

### 3. ErrorAlert Component
**Location:** `Components/Shared/ErrorAlert.razor`

**Usage:**
```razor
<!-- Single message -->
<ErrorAlert Message="Error message here"
           Severity="danger"
           Dismissible="true" />

<!-- List of messages -->
<ErrorAlert Title="Validation Errors"
           Messages="@errorList"
           Severity="warning"
           IsList="true" />
```

**Severities:** `danger`, `warning`, `info`, `success`

---

### 4. SkeletonCard Component
**Location:** `Components/Shared/SkeletonCard.razor`

**Usage:**
```razor
<!-- Text skeleton -->
<SkeletonCard Lines="5" />

<!-- Table skeleton -->
<SkeletonCard ShowTable="true"
             TableRows="5"
             TableColumns="4" />
```

**Use Case:** Display while loading data to improve perceived performance

---

### 5. CollapsibleSection Component
**Location:** `Components/Shared/CollapsibleSection.razor`

**Usage:**
```razor
<CollapsibleSection Title="Section Title"
                   Subtitle="Optional subtitle"
                   Icon="bi bi-icon"
                   Variant="primary"
                   DefaultExpanded="true">
    <!-- Your content here -->
</CollapsibleSection>
```

**Use Case:** Progressive disclosure for dense information

---

## 🎨 Enhanced Components

### BadgeWithIcon (Enhanced)
**Location:** `Components/Shared/BadgeWithIcon.razor`

**New Features:**
- Semantic type support (Status, Severity, Type)
- ARIA labels for accessibility

**Usage:**
```razor
<!-- Status badge -->
<BadgeWithIcon Text="Good"
               BadgeType="status"
               SemanticValue="success" />

<!-- Severity badge -->
<BadgeWithIcon Text="Critical"
               BadgeType="severity"
               SemanticValue="critical" />
```

---

## 📐 CSS Classes

### Spacing Classes
- `.section-major` - Major section spacing (mb-5)
- `.section-sub` - Subsection spacing (mb-4)

**Usage:**
```razor
<div class="card shadow section-sub">
    <!-- Content -->
</div>
```

### Progress Section Classes
- `.progress-section-primary` - Primary progress bar styling
- `.progress-section-secondary` - Subordinate progress bar styling

---

## 🎯 Best Practices

### 1. Card Structure
Always use this pattern:
```razor
<div class="card shadow section-sub">
    <CardHeader Title="..." Variant="..." />
    <div class="card-body">
        <!-- Content -->
    </div>
</div>
```

### 2. Tables
Always include these classes:
```razor
<table class="table table-striped table-hover">
```

### 3. Badges
Use semantic types when possible:
```razor
<!-- Instead of -->
<span class="badge bg-success">Good</span>

<!-- Use -->
<BadgeWithIcon Text="Good" BadgeType="status" SemanticValue="success" />
```

### 4. Error Messages
Always use ErrorAlert component:
```razor
<ErrorAlert Message="@errorMessage" Severity="danger" />
```

---

## 🔄 Migration Guide

### Updating Existing Cards

**Before:**
```razor
<div class="card shadow">
    <div class="card-header bg-success text-white">
        <h5 class="mb-0"><i class="bi bi-icon"></i> Title</h5>
    </div>
    <div class="card-body">
        <!-- Content -->
    </div>
</div>
```

**After:**
```razor
@using PoC1_LegacyAnalyzer_Web.Components.Shared

<div class="card shadow section-sub">
    <CardHeader Title="Title"
                Icon="bi bi-icon"
                Variant="success" />
    <div class="card-body">
        <!-- Content -->
    </div>
</div>
```

### Updating Tables

**Before:**
```razor
<table class="table table-sm">
```

**After:**
```razor
<table class="table table-sm table-striped table-hover">
```

---

## 📚 Component Reference

| Component | Purpose | Key Props |
|-----------|---------|-----------|
| `CardHeader` | Standardized card headers | Title, Subtitle, Icon, Variant |
| `MetricCard` | Responsive metric display | Value, Label, Icon, Color |
| `ErrorAlert` | Error/validation messages | Message, Severity, IsList |
| `SkeletonCard` | Loading placeholders | Lines, ShowTable |
| `CollapsibleSection` | Progressive disclosure | Title, DefaultExpanded |
| `BadgeWithIcon` | Semantic badges | BadgeType, SemanticValue |

---

## ✅ Checklist for New Components

When creating a new card component:

- [ ] Use `CardHeader` component
- [ ] Add `section-sub` or `section-major` class
- [ ] Use `table-striped table-hover` for tables
- [ ] Use `ErrorAlert` for error messages
- [ ] Use semantic badges where applicable
- [ ] Test responsive behavior
- [ ] Verify accessibility (ARIA labels)

---

## 🚀 Quick Examples

### Complete Card Example
```razor
@using PoC1_LegacyAnalyzer_Web.Components.Shared

<div class="card shadow section-sub">
    <CardHeader Title="My Analysis"
                Subtitle="Detailed results"
                Icon="bi bi-graph-up"
                Variant="primary"
                BadgeText="5 items"
                BadgeVariant="light" />
    <div class="card-body">
        <div class="table-responsive">
            <table class="table table-striped table-hover">
                <!-- Table content -->
            </table>
        </div>
    </div>
</div>
```

### Metric Grid Example
```razor
<div class="row g-3">
    <div class="col-lg-3 col-md-6">
        <MetricCard Value="123" Label="Files" Icon="bi bi-file" Color="primary" />
    </div>
    <div class="col-lg-3 col-md-6">
        <MetricCard Value="456" Label="Classes" Icon="bi bi-box" Color="info" />
    </div>
    <!-- More metrics -->
</div>
```

---

**Last Updated:** [Current Date]  
**Version:** 1.0


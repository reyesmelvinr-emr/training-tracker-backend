---
businessCapability: [PascalCaseCapabilityName]
feature: [FEATURE_ID_US_ID]
layer: [API / Domain / Infrastructure]
project_type: api-backend
status: draft
compliance_mode: pilot
---

# Module: [Module Name]

**Module Scope**: Multi-file domain unit  
**Files in Module**: N (see Module Files section below)  
**Primary Domain Noun**: [DomainNoun]  
**Complexity**: [Simple / Medium / Complex]  
**Documentation Level**: 🔶 Baseline (70% complete)

---

<!-- akr:section id="quick_reference" required=true order=1 authorship="mixed" human_columns="business_outcome,specific_caller,watch_out" -->
## Quick Reference (TL;DR)

**What it does:**  
🤖 [AI: 1-2 sentences describing the module's primary responsibility across all files]  
❓ [HUMAN: Cite the specific business outcome this module enables — do not infer from module name alone]

**When to use it:**  
❓ [HUMAN: Name the actual caller — UI page, background job, or specific API client — do not guess]

**Watch out for:**  
❓ [HUMAN: Cite a specific code path or error scenario — do not infer from module name alone]

---

<!-- akr:section id="module_files" required=true order=2 authorship="ai" -->
## Module Files

| File | Role | Primary Responsibilities |
|------|------|-------------------------|
| 🤖 `[path]/[FileName].cs` | 🤖 [Controller / Service / Repository / DTO] | 🤖 [Brief description of this file's responsibilities] |
| 🤖 `[path]/[FileName].cs` | 🤖 [Service Interface / Implementation] | 🤖 [Brief description] |
| 🤖 `[path]/[FileName].cs` | 🤖 [Repository Interface] | 🤖 [Brief description] |
| 🤖 `[path]/[FileName].cs` | 🤖 [Repository Implementation] | 🤖 [Brief description] |
| 🤖 `[path]/[FileName].cs` | 🤖 [DTO / Models] | 🤖 [Brief description] |

---

<!-- akr:section id="purpose_scope" required=true order=3 authorship="mixed" human_columns="business_purpose,scope_boundaries" -->
## Purpose and Scope

### Purpose

**Technical:**  
🤖 [AI: Technical description of what this module does]

**Business:**  
❓ [HUMAN: Business purpose - what problem does this module solve? Why did we build it?]

### Not Responsible For

🤖 [AI: What this module explicitly does NOT do]  
❓ [HUMAN: Clarify scope boundaries - what's handled elsewhere?]

---

<!-- akr:section id="operations_map" required=true order=4 authorship="ai" -->
## Operations Map

This section covers ALL operations across ALL files in the module. Operations are grouped by public surface (API endpoints, service methods) that consumers interact with.

### Public Operations

| Operation | File | Parameters | Returns | Business Purpose |
|-----------|------|------------|---------|-----------------|
| 🤖 `[MethodName]` | 🤖 `[FileName].cs` | 🤖 [parameter list] | 🤖 [return type] | 🤖 [business purpose] |
| 🤖 `[MethodName]` | 🤖 `[FileName].cs` | 🤖 [parameter list] | 🤖 [return type] | 🤖 [business purpose] |

### Internal Operations (for module completeness)

| Operation | File | Purpose | Called By |
|-----------|------|---------|-----------|
| 🤖 `[PrivateMethod]` | 🤖 `[FileName].cs` | 🤖 [purpose] | 🤖 [which public operation calls it] |
| 🤖 `[PrivateMethod]` | 🤖 `[FileName].cs` | 🤖 [purpose] | 🤖 [which public operation calls it] |

---

<!-- akr:section id="how_it_works" required=true order=5 authorship="mixed" human_columns="business_context,failure_impact" -->
## How It Works

### Primary Operation: [Main Method Name]

**Purpose:**  
🤖 [AI: What this method accomplishes]  
❓ [HUMAN: Business context - why do we need this operation?]

**Input:**  
🤖 [AI: Parameters and types]

**Output:**  
🤖 [AI: Return type and success/failure scenarios]

**Step-by-Step Flow (Across All Module Files):**

```
┌──────────────────────────────────────────────────────────────┐
│ Step 1: [Action] - [FileName].cs                            │
│  What  → 🤖 [AI: Technical action taken]                     │
│  Why   → ❓ [HUMAN: Business reason for this step]           │
│  Error → 🤖 [AI: What errors can occur]                      │
│         ❓ [HUMAN: Business impact of error]                 │
└──────────────────────────────────────────────────────────────┘
                          ↓
┌──────────────────────────────────────────────────────────────┐
│ Step 2: [Action] - [FileName].cs                            │
│  What  → 🤖 [AI: Technical action taken]                     │
│  Why   → ❓ [HUMAN: Business reason for this step]           │
│  Error → 🤖 [AI: What errors can occur]                      │
│         ❓ [HUMAN: Business impact of error]                 │
└──────────────────────────────────────────────────────────────┘
                          ↓
┌──────────────────────────────────────────────────────────────┐
│ Step 3: [Action] - [FileName].cs                            │
│  What  → 🤖 [AI: Technical action taken]                     │
│  Why   → ❓ [HUMAN: Business reason for this step]           │
│  Error → 🤖 [AI: What errors can occur]                      │
│         ❓ [HUMAN: Business impact of error]                 │
└──────────────────────────────────────────────────────────────┘
                          ↓
                    [SUCCESS] or [FAILURE]
```

**Success Path:**  
🤖 [AI: What happens on successful completion]

**Failure Paths:**  
🤖 [AI: What errors can occur and when]  
❓ [HUMAN: Business implications of each failure]

---

<!-- akr:section id="architecture_overview" required=true order=6 authorship="mixed" human_columns="consumer_impact" -->
## Architecture Overview

### Full-Stack Module Architecture

```
┌─────────────────────────────────────┐
│ API Layer - Entry Point             │
│ [Controller Name]                   │
│ └─ Receives HTTP requests           │
│ └─ Validates input parameters       │
└─────────────────────────────────────┘
         ↓
┌─────────────────────────────────────┐
│ Service Layer - Business Logic      │
│ [ServiceInterface]                  │
│ ├─ Defines contract                 │
│ └─ [ServiceImplementation]          │
│    └─ Enforces business rules       │
│    └─ Orchestrates operations       │
│    └─ Handles error scenarios       │
└─────────────────────────────────────┘
         ↓
┌─────────────────────────────────────┐
│ Repository Layer - Data Abstraction │
│ [RepositoryInterface]               │
│ ├─ Defines data contract            │
│ └─ [EfRepositoryImplementation]     │
│    └─ Queries database              │
│    └─ Maps ORM entities to DTOs     │
│    └─ Handles database errors       │
└─────────────────────────────────────┘
         ↓
┌─────────────────────────────────────┐
│ Data Layer - Persistence            │
│ [DatabaseTable]                     │
│ └─ Stores entity data               │
│ └─ Enforces constraints             │
└─────────────────────────────────────┘
```

### Module Composition

🤖 [AI: Explanation of how the files in this module work together]

<!-- conditional: if no external dependencies visible in listed module files, omit this heading and table entirely -->
### Dependencies (What This Module Needs)

| Dependency | Purpose | Failure Mode | Critical? |
|------------|---------|--------------|----------|
| 🤖 `I[DependencyName]` | 🤖 [AI: What it's used for] | 🤖 [AI: What exception occurs] | ❓ [HUMAN: Blocking? Can module degrade gracefully?] |

<!-- conditional: if no actual callers are visible in listed module files, omit this heading and table entirely — do not guess from module name -->
### Consumers (Who Uses This Module)

| Consumer | Use Case | Impact of Failure |
|----------|----------|-------------------|
| 🤖 [Controller/Service name] | 🤖 [AI: How they use it] | ❓ [HUMAN: User-facing? Background?] |

---

<!-- akr:section id="business_rules" required=true order=7 authorship="mixed" human_columns="why_it_exists,since_when" -->
## Business Rules

| Rule | Why It Exists | Since When | Where Enforced |
|------|---------------|------------|----------------|
| 🤖 **BR-[MOD]-001** | 🤖 [AI: Rule description from code] | ❓ [HUMAN: Exact date or sprint — do not estimate] | 🤖 [AI: Service/Validator/DB - where checked?] |
| 🤖 **BR-[MOD]-002** | 🤖 [AI: Rule description from code] | ❓ [HUMAN: Exact date or sprint — do not estimate] | 🤖 [AI: Service/Validator/DB - where checked?] |
| 🤖 **BR-[MOD]-003** | 🤖 [AI: Rule description from code] | ❓ [HUMAN: Exact date or sprint — do not estimate] | 🤖 [AI: Service/Validator/DB - where checked?] |

**Rule ID Format:** BR-[ModuleAbbreviation]-### (e.g., BR-CRS-001 for Course module)

**Enforcement Points** (per rule above):
- `Service`: Enforced in service layer before data operations
- `Validator`: Enforced in FluentValidation validators
- `DB`: Enforced as database constraints (unique, foreign key, check)

---

<!-- conditional: include only if module contains a controller with [Http*] attributes or explicit external DTO contracts visible in listed module files -->
<!-- akr:section id="api_contract" required=false order=8 authorship="ai" -->
## API Contract (AI Context)

> 📋 **Interactive Documentation:** [API Portal - [ModuleName]](https://apim.gateway.emerson.com/...) — use for testing
> 
> **Purpose:** This section provides AI assistants (Copilot) with API context for this module.
> **Sync Status:** Last verified on ❓ [HUMAN: Date]

### Endpoints

🤖 [AI: Extract from ApiRoutes.cs and controller [Http*] attributes - module-specific endpoints only]

| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| 🤖 `GET` | 🤖 `/v1/[resource]` | 🤖 Get all | 🤖 Yes |
| 🤖 `GET` | 🤖 `/v1/[resource]/{id}` | 🤖 Get by ID | 🤖 Yes |
| 🤖 `POST` | 🤖 `/v1/[resource]` | 🤖 Create | 🤖 Yes |
| 🤖 `PUT` | 🤖 `/v1/[resource]/{id}` | 🤖 Update | 🤖 Yes |
| 🤖 `DELETE` | 🤖 `/v1/[resource]/{id}` | 🤖 Delete | 🤖 Yes |

### Request Example

🤖 [AI: Populate field values from DTO property types and constraints only — use the actual type (string, int, Guid, bool) and any explicit constraints (max length, range) visible in the DTO file. Do not invent domain-specific values.]

```json
{
  "propertyName": "🤖 [AI: value from DTO property type/constraint — no invented domain values]",
  "propertyId": "🤖 [AI: value from DTO property type/constraint]",
  "isActive": true
}
```

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| 🤖 `propertyName` | 🤖 `string` | 🤖 Yes | 🤖 [AI: Business purpose of field] |
| 🤖 `propertyId` | 🤖 `int` | 🤖 Yes | 🤖 [AI: Business purpose of field] |
| 🤖 `isActive` | 🤖 `bool` | 🤖 No | 🤖 [AI: Default behavior and purpose] |

### Success Response Example (200)

🤖 [AI: Generate from response DTO]

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "propertyName": "value",
  "createdDate": "2024-01-01T00:00:00Z"
}
```

### Error Response Example

🤖 [AI: Extract from error model classes]

```json
{
  "statusCode": 400,
  "message": "Validation failed.",
  "validationErrors": [
    { "fieldName": "propertyName", "message": "Required" }
  ]
}
```

---

<!-- conditional: include only if *Validator.cs files, DTO data annotations, or explicit guard clauses exist in the listed module files -->
<!-- akr:section id="validation_rules" required=false order=9 authorship="mixed" human_columns="business_rationale" -->
## Validation Rules

🤖 [AI: Extract from *Validator.cs FluentValidation classes]

| Property | Rule | Error Message |
|----------|------|---------------|
| 🤖 `[Property]` | 🤖 `NotEmpty()` | 🤖 "[Property] is required" |
| 🤖 `[Property]` | 🤖 `MaximumLength(N)` | 🤖 "Cannot exceed N characters" |

❓ [HUMAN: Add business rationale for non-obvious validation rules]

---

<!-- akr:section id="data_operations" required=true order=10 authorship="mixed" human_columns="business_context,trigger" -->
## Data Operations

### Reads From

| Database Object | Purpose | Business Context | Performance Notes |
|-----------------|---------|------------------|-------------------|
| 🤖 `schema.TableName` | 🤖 [What data retrieved, which columns] | ❓ [HUMAN: Why needed? Business rule context] | 🤖 [AI: Query pattern, indexes, optimization hints] |

---

### Writes To

| Database Object | Purpose | Business Context | Performance Notes |
|-----------------|---------|------------------|-------------------|
| 🤖 `schema.TableName` | 🤖 [Which columns modified by this module] | ❓ [HUMAN: What business event triggers this?] | 🤖 [AI: Transaction scope] ❓ [HUMAN: High volume? Indexing needs?] |

---

<!-- akr:section id="failure_modes" required=true order=11 authorship="ai" -->
## Failure Modes & Exception Handling

### Common Failure Scenarios

| Exception Type | Trigger | Operation | Impact | Mitigation |
|---|---|---|---|---|
| 🤖 `InvalidOperationException` | 🤖 [What causes it] | 🤖 [Which operation fails] | 🤖 [Business consequence] | 🤖 [How module handles it] |
| 🤖 `DbUpdateException` | 🤖 [DB constraint violated] | 🤖 [Which write operation fails] | 🤖 [Data not persisted] | 🤖 [Retry? Rollback? User message?] |

---

<!-- akr:section id="questions_gaps" required=true order=12 authorship="human" -->
## Questions & Gaps

### AI-Flagged Questions

🤖 [AI will identify unclear logic, magic numbers, assumptions]

### Human-Flagged Questions

❓ [HUMAN: Add questions you have while reviewing]

---


<!-- conditional: include only if doc_output paths for related modules are present in modules.yaml -->
<!-- akr:section id="related_documentation" required=false order=13 authorship="ai" -->
## Related Documentation

**Other Modules:** Link to related module docs (confirmed paths from modules.yaml only):
- `[ModuleName](./[ModuleName]_module_doc.md)`

**Database Tables:** See database documentation:
- `[Table Name](../../database-repo/docs/tables/TableName_doc.md)`




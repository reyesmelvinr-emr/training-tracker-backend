# AKR Backend Service Condensed Instructions

Version: 1.0
Extends: .akr/charters/AKR_CHARTER.md
Source charter: .akr/charters/AKR_CHARTER_BACKEND.md
Audience: Agent Skill Mode B for api-backend modules

## Scope
Apply these rules when generating module documentation for backend service modules. Focus on service-layer behavior, orchestration, and business rules. Do not treat controller-only details as the primary documentation target.

## Required Front Matter
Every module document must begin with YAML front matter containing all fields below:

```yaml
---
businessCapability: PascalCaseTagFromRegistry
feature: FN12345_US678
layer: API
project_type: api-backend
status: draft
compliance_mode: pilot
---
```

Rules:
- businessCapability: required PascalCase key from tag-registry.
- feature: required work-item tag in FN#####_US##### format.
- layer: required and must match module layer.
- project_type: required, expected api-backend.
- status: draft or approved based on workflow stage.
- compliance_mode: pilot or production.

## Metadata Header Requirements
Before section content, include an AKR metadata header block:

- Header marker: <!-- akr-generated -->
- Required fields: skill, mode, template, steps-completed, generated-at
- For section-scoped generation include: generation-strategy, passes-completed, pass-timings-seconds, total-generation-seconds

## Transparency Marker Rules
Use markers consistently and do not omit them:
- 🤖 for AI-inferred statements.
- ❓ for unknowns that require human validation.
- NEEDS for required data pending completion.
- VERIFY for assumptions requiring confirmation.
- DEFERRED only with explicit justification text.

Placement rules:
- Markers must appear inline in the affected sentence/table row.
- Do not leave unresolved ambiguity without a marker.
- If a section cannot be completed from source, add explicit ❓ entries and continue.

## Module Files Rules
Document every file in modules.yaml for the target module.
Include:
- File path
- Role (Controller, Service, Repository, DTO, Validator, Mapper, etc.)
- Responsibility summary

Validation expectation:
- No module file may be omitted.
- Shared files outside module should be listed as dependencies, not module files.

## Operations Map Rules
Provide complete operation coverage across all module files.
Include:
- Operation/method name
- Owning file/class
- Input contract
- Output contract
- Side effects
- Called dependencies

Coverage rules:
- Include public and internal operations relevant to behavior.
- Include async and background operations.
- Include validation and guard paths where they alter flow.
- Avoid partial maps that only list endpoint methods.

## Architecture Overview Rules
Provide a text-based full-stack flow for the module:
- Entry points (controllers/handlers/jobs)
- Service orchestration
- Data access/repositories
- External systems/events
- Persistence targets

Do not use Mermaid. Use concise text/ASCII flow only.

## Business Rules Requirements
Business Rules section is mandatory. Use a table with these columns:
- Rule ID
- Rule Description
- Why It Exists
- Since When

Rule guidance:
- Rule ID format: BR-[MODULE]-###
- Describe enforceable logic, not UI behavior.
- Include constraints, eligibility checks, thresholds, and policy gates.
- If date is unknown, set Since When to ❓ with context.

## Data Operations Requirements
Document all reads and writes caused by module behavior.
Include:
- Data source/target
- Read/Write action
- Triggering operation
- Conditions/filters
- Transactional or consistency notes

Coverage rules:
- Include indirect writes (events, audit rows, cache invalidations) when present.
- Include side effects (emails, notifications, queue messages).

## Questions And Gaps Rules
Create explicit unresolved-items list:
- Unknown business rule intent
- Missing lifecycle dates
- Ambiguous ownership or dependency behavior
- Missing source references

Each unresolved item must use ❓ and include next action/owner if known.

## Section-Scoped Generation Rules
Use section-by-section pass behavior to avoid context overload.

Required pass discipline:
- Load only needed charter slice per section.
- Build forward payload with extracted facts only.
- Do not re-expand full context in later passes.
- If pass split is used (example 2A/2B), record split in metadata.

## Quality Thresholds
Minimum quality checks before completion:
- All required sections present.
- 100 percent module file coverage.
- Operations Map and Data Operations are complete and non-truncated.
- Business Rules table contains Why It Exists and Since When columns.
- Marker usage is explicit for all unknowns.

## Exclusions
Do not add these as primary content in module docs:
- Change History sections (Git is the source of truth).
- Long speculative roadmap details.
- Database object deep schema detail (link DB docs instead).

## Reference
Full charter for detailed rationale and examples:
- .akr/charters/AKR_CHARTER_BACKEND.md

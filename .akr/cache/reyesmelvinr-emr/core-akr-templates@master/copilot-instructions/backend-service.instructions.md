# AKR Backend Service Condensed Instructions

Version: 1.0
Extends: .akr/charters/AKR_CHARTER.md
Source charter: .akr/charters/AKR_CHARTER_BACKEND.md
Audience: Agent Skill GenerateDocumentation for api-backend modules

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
- status: governs the generated document's maturity state, not the module grouping approval state.
- for first-generation Mode B output, set status to draft unless document-content approval has already occurred through the documented review flow.
- do not copy `modules.yaml` module status directly into document front matter.
- compliance_mode: pilot or production.

## Metadata Header Requirements
Before section content, include an AKR metadata header block:

- Header marker: <!-- akr-generated -->
- Required fields: skill, mode, template, steps-completed, generated-at
- For section-scoped generation include: generation-strategy, passes-completed, pass-timings-seconds, total-generation-seconds

## Transparency Marker Rules
Use markers consistently and do not omit them:
- Prefer unmarked factual statements for content directly evidenced by the listed module source files.
- 🤖 for AI synthesis or inference across multiple files — not for single-source facts.
- ❓ for missing business context, intent, dates, or ownership — see Unknowns Discipline for detailed ❓ placement rules.
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
SSG pass discipline is governed by SKILL.md §SSG rules. This file does not restate workflow steps.

## Grounding Rules
All factual claims must be directly traceable to files listed in modules.yaml for this module.
- Do not infer authorization schemes, persistence constraints, index names, or cross-module dependencies unless they appear in the listed module files.
- Do not claim a consumer, caller, or external integration exists unless it is visible in the listed files.
- Prefer concise factual statements over expansive narrative. If the source does not support a statement, mark it ❓ or omit it.

## Readability Floor
Generated documentation must be readable by a non-implementing reviewer.
- Include a Quick Reference (TL;DR) that a Product Owner or QA reviewer can understand without reading the rest of the document.
- Include one end-to-end operational flow narrative if the source supports it.

## Unknowns Discipline
- Use ❓ for missing business intent, missing lifecycle dates, and unverifiable ownership.
- Do not use ❓ for information directly observable in the module source files. If the code answers the question, state the answer without a marker.
- If an entire section cannot be evidenced from listed files, open with NEEDS [reason] rather than generating speculative content. Do not combine ❓ and NEEDS as a compound marker.

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

---
name: akr-docs
description: >
  Generate AKR module documentation following charters and templates.
  Invoke explicitly via /akr-docs [groupings | generate | resolve] [target].
disable-model-invocation: true
compatibility:
  models:
    - claude-sonnet-4-6
    - gpt-4o
metadata:
  skill-version: 1.0.0
  optimized-for: claude-sonnet-4-6
  tested-on:
    - claude-sonnet-4-6
    - gpt-4o
user-invocable: true
---
<!-- SKILL_VERSION: v1.0.0 -->
<!-- Distribution: Managed by core-akr-templates. Do not edit directly in application repositories. -->

CRITICAL: When this skill is loaded, begin EVERY response with this confirmation block.

✅ akr-docs INVOKED AND STEPS EXECUTED
Steps followed: 1. [step] - completed | 2. [step] - completed | ...

# AKR Documentation Workflow

## ProposeGroupings - Propose Module Groupings
Use when asked to propose module groupings or initialize modules.yaml.

1. Check for modules.yaml in the project root.
2. If modules exist, proceed only for module targets with status approved; stop and request approval for draft/review targets.
3. Scan source files and group by dominant business/domain noun.
4. Assign roles by file patterns:
- Backend: controller, service interface/impl, repository interface/impl, DTO/contracts.
- UI: page, component, hook, type definition.
5. Assign SQL/migration files to database_objects only.
6. Add uncertain files to unassigned with reason text.
7. Detect project_type:
- api-backend
- ui-component
- microservice
- general
7.5. Write committed review artifact to docs/modules/.akr/{project}_review.md.
7.6. Stop and wait for explicit human approval before applying module regrouping changes.
8. Write draft modules.yaml with status: draft for all module entries.
9. Produce grouping review checklist and stop for human approval.

ProposeGroupings checklist:
- All groupings reviewed for semantic correctness.
- Module names reflect domain language.
- No module exceeds max_files.
- Misplaced and shared files reviewed.
- businessCapability keys align with tag registry.

## GenerateDocumentation - Generate Module Documentation
Use only after ProposeGroupings approval. If target module status is draft, stop and request approval first.

### GenerateDocumentation workflow steps
1. Read modules.yaml and resolve the requested module.
2. Load condensed charter by project_type from copilot-instructions:
- PATH A (@github available — VS Code): Use `@github get file <charter_name>` to fetch the condensed charter from `core-akr-templates/copilot-instructions/`:
  - api-backend -> backend-service.instructions.md
  - ui-component -> ui-component.instructions.md
  - microservice -> backend-service.instructions.md
  - general -> backend-service.instructions.md
- PATH B (@github unavailable or Visual Studio): Load from `.github/copilot-instructions.md` or local file path directly.
- PROHIBITION: Do NOT re-read charter files or source files via @github in Passes 2-7. Charter content must be placed in the forward payload in Pass 1 and carried forward as a condensed summary only. Each @github call consumes one premium request; exceeding 2 @github calls per run is prohibited.
3. Select base template:
- api-backend/microservice/general -> templates/lean_baseline_service_template_module.md
- ui-component -> templates/ui_component_template_module.md
4. Read only files listed in module files.
5. Generate documentation using Section-Scoped Generation (SSG).
5.5. Write committed draft to docs/modules/.akr/{module}_draft.md.
5.6. Surface preview for human review and wait for explicit approval before finalization.
6. Strip draft-only front matter fields and write final document to module doc_output path.
7. Run validate_documentation.py for the final target output.
8. Ensure metadata header is present at top of output file.
9. Open/update draft PR with completion checklist.

### Required metadata header contract
Write this before document sections:

<!-- akr-generated
skill: akr-docs
skill-version: v1.0.0
mode: generation
template: {template}
charter: {condensed charter}
modules-yaml-status: approved
generation-strategy: section-scoped
passes-completed: {pass-list for this run, e.g. 1,2A,2B,3,4,5,6,7}
pass-timings-seconds: {comma-separated or unavailable}
total-generation-seconds: {value or unavailable}
steps-completed: {workflow-step list completed in this run}
generated-at: {ISO-8601 timestamp}
-->

### Section-Scoped Generation (SSG) passes
Pass 1: Module Files and role mapping.
Pass 2: Operations extraction and map build. If context pressure occurs, split into 2A and 2B.
Pass 3: Architecture Overview and dependency flow.
Pass 4: Business Rules table generation using forward payload only.
Pass 5: Data Operations coverage using forward payload only.
Pass 6: Questions and Gaps plus marker normalization.
Pass 7: Final assembly, front matter check, metadata header check, truncation check, and template section-order conformance check (sections must appear in template order unless marked conditional).

SSG rules:
- Keep forward payload compact and structured.
- Do not reload unrelated sources in late passes.
- If generation exceeds 45 minutes, trigger slow-generation handler:
- Continue if near completion.
- Split module and restart if needed.
- Developer-elected single-pass is allowed only in pilot mode and must set generation-strategy accordingly.

Marker policy: Apply placement rules as defined in the loaded condensed charter (copilot-instructions/). For grounding-specific marker decisions (when to use 🤖 vs unmarked vs ❓), the Source Grounding rules in this file take precedence.

## Source Grounding
Apply in every GenerateDocumentation pass. These rules are not optional and are not overridden by template placeholders.
- Do not invent auth requirements, consumers, DB indexes, external integrations, future features, or cross-module dependencies unless directly evidenced by files listed in modules.yaml for this module.
- Prefer unmarked factual statements for content directly evidenced by source files. Use 🤖 only for synthesis or inference across multiple files. Use ❓ only for missing business context, intent, dates, or ownership that cannot be determined from the listed module files alone.
- Do not emit ❓ for information directly recoverable from the listed module files. If the code answers the question, state the answer.

## Conditional Sections
Evaluate conditionality in Pass 1 using the module file list. Record the decision in the committed draft front matter.
When a conditional section is excluded, record the exclusion reason in the committed draft front matter under `excluded-sections` (e.g., `excluded-sections: [API Contract — no [Http*] controller found]`). Pass 7 treats absent sections listed here as conformant omissions, not errors.
- API Contract: include only if the module contains a controller with [Http*] attributes or explicit external DTO contracts visible in the listed module files.
- Validation Rules: include only if *Validator.cs files, DTO data annotations, or explicit guard clauses exist in the listed module files.
- Consumer Map: include only if actual callers or explicit dependencies are visible in the listed module files — not inferred from module name.
- Related Documentation: include only if doc_output paths for related modules are present in modules.yaml.

## ResolveUnknowns - Interactive HITL Completion
Use for existing documents with unresolved ❓ markers.

1. Read target document and enumerate unresolved markers by section.
2. Prioritize critical sections when compliance_mode is production.
3. Ask targeted questions and propose edits grounded in source evidence.
4. Apply accepted edits immediately.
5. Convert unresolved critical items to DEFERRED only with owner and rationale.
6. Re-run validation after each batch.
7. Summarize resolved vs deferred items and remaining blockers.

ResolveUnknowns completion checklist:
- Critical unresolved markers addressed.
- Deferred items include owner and follow-up trigger.
- Validation passes for current compliance mode.

## Output quality requirements
- Include all required sections for detected doc type, in template section order.
- Ensure Operations Map and Data Operations are complete and non-truncated.
- Ensure business rules include Why It Exists and Since When columns.
- Do not include Change History sections.
- Include Quick Reference (TL;DR) readable by a Product Owner or QA reviewer without reading the rest of the document.
- Include one primary flow narrative and one error/status table if the module has ≥2 files and at least one operation with a documented failure path.

---
name: akr-docs
description: >
  Generate AKR module documentation following charters and templates.
  Invoke explicitly via /akr-docs [mode-a | mode-b | mode-c] [target].
disable-model-invocation: true
optimized-for: claude-sonnet-4-6
tested-on:
  - claude-sonnet-4-6
  - gpt-4o
user-invocable: true
skill-version: 1.0.0
---
<!-- SKILL_VERSION: v1.0.0 -->
<!-- Distribution: Managed by core-akr-templates. Do not edit directly in application repositories. -->

CRITICAL: When this skill is loaded, begin EVERY response with this confirmation block.

✅ akr-docs INVOKED AND STEPS EXECUTED
Steps followed: 1. [step] - completed | 2. [step] - completed | ...

# AKR Documentation Workflow

## Mode A - Propose Module Groupings
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

Mode A checklist:
- All groupings reviewed for semantic correctness.
- Module names reflect domain language.
- No module exceeds max_files.
- Misplaced and shared files reviewed.
- businessCapability keys align with tag registry.

## Mode B - Generate Module Documentation
Use only after Mode A approval. If target module status is draft, stop and request approval first.

### Mode B workflow steps
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
- api-backend/microservice/general -> lean_baseline_service_template.md (module variant)
- ui-component -> ui_component_template.md (module variant)
4. Read only files listed in module files.
5. Generate documentation using Section-Scoped Generation (SSG).
5.5. Write committed draft to docs/modules/.akr/{module}_draft.md.
5.6. Surface preview for human review and wait for explicit approval before finalization.
6. Strip draft-only front matter fields and write final document to module doc_output path.
7. Run validate_documentation.py for the final target output.
8. Open/update draft PR with completion checklist.
9. Ensure metadata header is present at top of output file.

### Required metadata header contract
Write this before document sections:

<!-- akr-generated
skill: akr-docs
skill-version: v1.0.0
mode: B
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
Pass 7: Final assembly, front matter check, metadata header check, and truncation check.

SSG rules:
- Keep forward payload compact and structured.
- Do not reload unrelated sources in late passes.
- If generation exceeds 45 minutes, trigger slow-generation handler:
- Continue if near completion.
- Split module and restart if needed.
- Developer-elected single-pass is allowed only in pilot mode and must set generation-strategy accordingly.

Marker policy:
- Use 🤖 for inferred statements.
- Use ❓ for unresolved required inputs.
- Use NEEDS and VERIFY for outstanding checks.
- Use DEFERRED only with explicit rationale.

## Mode C - Interactive HITL Completion
Use for existing documents with unresolved ❓ markers.

1. Read target document and enumerate unresolved markers by section.
2. Prioritize critical sections when compliance_mode is production.
3. Ask targeted questions and propose edits grounded in source evidence.
4. Apply accepted edits immediately.
5. Convert unresolved critical items to DEFERRED only with owner and rationale.
6. Re-run validation after each batch.
7. Summarize resolved vs deferred items and remaining blockers.

Mode C completion checklist:
- Critical unresolved markers addressed.
- Deferred items include owner and follow-up trigger.
- Validation passes for current compliance mode.

## Output quality requirements
- Include all required sections for detected doc type.
- Ensure Operations Map and Data Operations are complete.
- Ensure business rules include Why It Exists and Since When columns.
- Do not include Change History sections.

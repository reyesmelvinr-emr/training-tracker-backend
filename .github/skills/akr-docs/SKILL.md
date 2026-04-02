---
name: akr-docs
description: >
  Generate AKR module documentation following charters and templates.
  Invoke explicitly via /akr-docs [groupings | generate | resolve] [target] [--use-ssg].
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

modules.yaml is a minimal grouping manifest. Its only job is to record which files belong to which module and where the output document goes. All other metadata (project_type, businessCapability, feature, layer, roles, descriptions) is derived from source files during GenerateDocumentation and written into the generated document — not into modules.yaml.

1. Check for modules.yaml in the project root.
2. If modules.yaml exists, check whether any modules have grouping_status: approved. If so, do not modify those modules. Only add new modules or update modules still at grouping_status: draft.
3. Scan source files and group by dominant business/domain noun.
4. Assign each file to the module whose domain noun it belongs to:
   - Backend pattern: controller, service interface/impl, repository interface/impl, entity, DTO/contracts.
   - UI pattern: page component, sub-components, hooks, type definitions.
5. Assign SQL/migration/SSDT files to database_objects only. Never group them with application code.
6. Add uncertain or shared infrastructure files to unassigned with a one-line reason.
7. Write modules.yaml using the exact schema below. Do not add any fields not shown.

   ```yaml
   project:
     name: {project-name}
     layer: {UI|API|Database|Integration|Infrastructure|Full-Stack}
     standards_version: v1.0.0
     minimum_standards_version: v1.0.0
     compliance_mode: pilot

   modules:
     - name: {lowercase-kebab or camelCase module name matching the domain noun}
       grouping_status: draft
       doc_output: docs/modules/{module-name}.md
       files:
         - {relative file path}
         - {relative file path}

   database_objects: []

   unassigned:
     - path: {file path}
   ```

   Schema rules — strictly enforced:
   - files: plain string list of relative paths. No path/role/responsibility sub-keys.
   - grouping_status: always draft. This is the only status field in modules.yaml.
   - Do not add: project_type, businessCapability, feature, layer, status, max_files, description, compliance_mode, or any other field. Those belong in the generated document, not in modules.yaml.
   - database_objects: empty array unless confirmed SQL/SSDT/migration files exist.
   - unassigned: include scaffold artifacts (WeatherForecast*, etc.) with a deletion recommendation.

8. Display the proposed groupings as a summary table in chat (module name → file count → files), then instruct the reviewer to inspect modules.yaml in VS Code and confirm or request changes before GenerateDocumentation is run.

ProposeGroupings checklist:
- All groupings reviewed for semantic correctness.
- Module names reflect domain language.
- No module exceeds max_files.
- Each module contains files that share a single domain noun.
- No file appears in more than one module.
- PLATFORM files are grouped under a platform module (if two or more exist).
- EXCLUDED files (config, project metadata, dev tooling, local docs) are silently omitted — not in unassigned.
- TEST-PROJECT files are silently omitted — not in unassigned.
- SCAFFOLD files and template placeholders are in unassigned with a deletion recommendation.
- database_objects is [] if no DB artifacts were found.

## GenerateDocumentation - Generate Module Documentation
Use only after ProposeGroupings approval. If target module grouping_status is draft, stop and request approval first.

### GenerateDocumentation workflow steps
1. Read modules.yaml and resolve the requested module.
2. Infer `project_type` from the module file set, then load the matching condensed charter from copilot-instructions:
- Infer `api-backend` when backend patterns are present (controller, service interface/impl, repository interface/impl, entity, DTO/contracts).
- Infer `ui-component` when UI patterns are present (page component, sub-components, hooks, type definitions).
- Infer `microservice` only when the module is orchestration-heavy and lacks the standard CRUD vertical-slice shape.
- Default to `general` if the file set is ambiguous.
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
5. Generate documentation. Default strategy is single-pass; if `--use-ssg` is provided, run Section-Scoped Generation (SSG) passes.
5.5. Write committed draft to docs/modules/.akr/{module}_draft.md.
5.6. Surface preview for human review and wait for explicit approval before finalization.
6. Strip draft-only front matter fields and write final document to module doc_output path.
  Status rule: first-generation output must use `status: draft` in generated document front matter unless document-content approval has already been explicitly completed and recorded.
  Do not copy module grouping status from modules.yaml into generated document front matter.
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
generation-strategy: {single-pass (default) or section-scoped when --use-ssg is specified}
passes-completed: {single-pass (default) or pass-list for --use-ssg runs, e.g. 1,2A,2B,3,4,5,6,7}
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
- Single-pass is the default generation strategy; use `--use-ssg` for large-file modules or when higher-fidelity multi-pass coverage is required.

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

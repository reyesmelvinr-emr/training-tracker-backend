# AKR Mode Script: GenerateDocumentation

<!-- Loaded on demand by SKILL.md dispatcher. -->
<!-- This file contains ZERO section names, ZERO section order, ZERO conditional logic. -->
<!-- All structural knowledge lives in the template via akr: directives. -->

## Purpose

Execute documentation generation for a named module. Read structural knowledge
from the template's `akr:` directives. Do not encode structural knowledge here.

---

## Step 1: Pre-flight

Read `modules.yaml`. Locate the target module.

Capture `generation_started_at` as UTC timestamp immediately when `/akr-docs generate` is invoked. This timestamp is used to compute per-stage timing metrics recorded in Steps 2–8.

Initialize stage timers at invocation:
```
stage_timers = {
  preflight_seconds: null,
  template_fetch_seconds: null,
  charter_fetch_seconds: null,
  source_extraction_seconds: null,
  assembly_seconds: null,
  write_seconds: null
}
```
Record `stage_timers.preflight_seconds = now_utc - generation_started_at` after module lookup and pre-flight checks complete.

**Cross-chat template and charter cache:** `.akr/cache/` in the workspace root. Templates and charters fetched via `@github` are written here keyed by `{owner}/{repo}@{branch}/{encoded-path}`. A new VS Code chat session in the same workspace can read from this cache without a remote fetch, eliminating repeat network round-trips. To force refresh cache contents, run `/akr-docs refresh-assets` before generating. Add `.akr/cache/` to `.gitignore` to prevent committing cached remote assets.

- If `grouping_status: draft` → stop. Tell the user to approve the grouping first.
- If module not found → stop. Tell the user to run `/akr-docs groupings` first.
- If `feature` value matches the all-zero placeholder (e.g. `FN00000_US000`) → do not copy it. Write `feature: ❓ NEEDS real work-item tag` in the output front matter instead.

Infer `project_type` from the module file list:

| Signal | project_type |
|---|---|
| Controller + Service + Repository + Entity + DTO present | `api-backend` |
| Page/View + sub-components + hooks + types present | `ui-component` |
| Orchestration-heavy, no standard CRUD vertical slice | `microservice` |
| Ambiguous | `general` |

---

## Step 2: Fetch Template Metadata

Identify the template path from the project_type:

| project_type | Template path in core-akr-templates |
|---|---|
| `api-backend` / `microservice` / `general` | `templates/lean_baseline_service_template_module.md` |
| `ui-component` | `templates/ui_component_template_module.md` |

**Fetch the full template file; parse and carry forward only the `akr:` directive
blocks. Discard the template prose body.** Parse and carry forward:

- **Section registry:** ordered list of `{id, required, order, condition}` objects
- **Condition definitions:** token → detection description mapping

**Before fetching, check cache:**
1. Compute cache key: `{owner}/{repo}@{branch}/{template_path}` (e.g. `reyesmelvinr-emr/core-akr-templates@master/templates/lean_baseline_service_template_module.md`)
2. Cache file: `.akr/cache/{encoded_cache_key}.md`
3. If cache file exists: read template from cache, skip `@github` fetch, set `template-cache: hit`.
4. If cache file does not exist: fetch via `@github get file`, write content to cache file, set `template-cache: miss`.

If the template cannot be fetched via `@github` and no cache hit exists → report the failure and stop. Templates are not included in the distributed workspace bundle; PATH A (`@github get file`) is required for template access when no cache is available.

Record `stage_timers.template_fetch_seconds = now_utc - stage_start` after template content is available (cache hit or live fetch).

---

## Step 3: Fetch Charter Slice

Load the charter for the inferred `project_type`. This is the 2nd and final
allowed `@github` call.

| project_type | Charter |
|---|---|
| `api-backend` | `@github get file core-akr-templates/copilot-instructions/backend-service.instructions.md` |
| `ui-component` | `@github get file core-akr-templates/copilot-instructions/ui-component.instructions.md` |
| `microservice` | `@github get file core-akr-templates/copilot-instructions/backend-service.instructions.md` |
| `general` | `@github get file core-akr-templates/copilot-instructions/backend-service.instructions.md` |

**Before fetching, check cache:**
1. Compute cache key: `{owner}/{repo}@{branch}/{charter_path}` (e.g. `reyesmelvinr-emr/core-akr-templates@master/copilot-instructions/backend-service.instructions.md`)
2. Cache file: `.akr/cache/{encoded_cache_key}.md`
3. If cache file exists: read charter from cache, skip `@github` fetch, set `charter-cache: hit`.
4. If cache file does not exist: fetch via `@github get file`, write content to cache file, set `charter-cache: miss`.

If the charter cannot be fetched via `@github` and no cache hit exists → report the failure and stop. Charters are not included in the distributed workspace bundle; PATH A is required for charter access when no cache is available.

Record `stage_timers.charter_fetch_seconds = now_utc - stage_start` after charter content is available (cache hit or live fetch).

Compress into a forward payload summary (~400 tokens). Carry only:
- Marker placement rules (🤖 / ❓ / NEEDS / VERIFY / DEFERRED)
- Grounding rules
- Quality threshold checklist items

---

## Step 4: Read Source Files → Structured Facts Payload

Record `stage_source_extraction_start = now_utc` before reading any source file.

Read only files listed under `files:` for this module in `modules.yaml`.

Build a structured facts payload — no raw file content forward:

```
facts = {
  files: [{ path, role, public_methods: [{name, params, return_type}] }],
  conditions_detected: {
    controller_with_http_attributes: bool,
    validator_or_annotations: bool,
    visible_callers: bool,
    related_modules_in_manifest: bool
  },
  db_tables, exception_types, di_dependencies, validation_rules, side_effects
}
```

Record `stage_timers.source_extraction_seconds = now_utc - stage_source_extraction_start` after the structured facts payload is complete.

---

## Step 5: Resolve Section Plan

For each section in the section registry (sorted by order):
- `condition == null` → include
- `condition != null` → include only if `facts.conditions_detected[condition] == true`
- Excluded sections → record in draft front matter under `excluded-sections` with reason

---

## Step 6: Generate Documentation

Record `stage_assembly_start = now_utc` before generating the first section.

Generate sections in section plan order using:
1. `akr:section` directive guidance for field coverage, markers, violations, format
2. Charter grounding rules from Step 3 forward payload
3. Targeted template section inspection from already loaded template content when exact format verification is needed
4. Transparency markers — every AI-inferred statement must include a 🤖 marker inline. Do not emit unmarked AI narrative.

---

## Step 7: Generation Strategy

**Default: single-pass.**

**Use SSG passes (`--use-ssg`) only when:**
- Module has 6+ files with complex inter-file dependencies
- Context pressure observed (output truncating mid-section)
- First-pass output fails coverage quality checks

**SSG pass sequence (only when --use-ssg):**

| Pass | Sections | Forward payload carries |
|---|---|---|
| 1 | Module Files + condition evaluation + section plan | File-role map, section plan, charter summary |
| 2A | Operations Map — controller + service | Operation signatures |
| 2B | Operations Map — repository + DTO (split if pressure) | Repository operations, DTO contracts |
| 3 | Architecture Overview + Dependency table | Architecture text |
| 4 | Business Rules | Rule table |
| 5 | Data Operations + Side Effects | Read/write/side-effect tables |
| 6 | How It Works + Quick Reference + Questions and Gaps | Flow narrative, gap list |
| 7 | Conditional sections + Final assembly | Assembled document |

SSG rules: never re-read source files or charter after Pass 1. Never re-parse
template directives after Step 2.

Record `stage_timers.assembly_seconds = now_utc - stage_assembly_start` after all sections are assembled and the document body is complete.

---

## Step 8: Write Draft Artifact

Resolve draft path in this order:

1. If module defines `draft_output`, write to that path.
2. Otherwise default to `docs/modules/.akr/{ModuleName}_draft.md`.

Record `stage_write_start = now_utc` before writing the draft file.
Compute `draft_generation_seconds = now_utc - generation_started_at` at the time the draft file is written.

Write draft with draft-only front matter:
```yaml
preview-generated-at: {ISO-8601}
generation-started-at: {ISO-8601}
draft-generation-seconds: {integer}
stage-timings:
  preflight-seconds: {integer}
  template-fetch-seconds: {integer}
  template-cache: {hit | miss}
  charter-fetch-seconds: {integer}
  charter-cache: {hit | miss}
  source-extraction-seconds: {integer}
  assembly-seconds: {integer}
  write-seconds: {integer}
review-mode: full
generation-strategy: {single-pass | section-scoped}
passes-completed: {list}
excluded-sections:
  - "section_id — reason"
```
Record `stage_timers.write_seconds = now_utc - stage_write_start` after the file is written and add to the `stage-timings` block above.

Surface draft path in chat and include this confirmation prompt payload:
- `Draft path: {draft_output_path}`
- `Final path: {doc_output_path}`
- `Total draft generation time: {draft_generation_seconds}s`
- Stage breakdown: `preflight {N}s | template-fetch {N}s ({hit|miss}) | charter-fetch {N}s ({hit|miss}) | source-extraction {N}s | assembly {N}s | write {N}s`

If `draft_output_path` and `doc_output_path` differ, explicitly warn that finalize will promote content to a different path.

Wait for explicit user confirmation before Step 9.

---

## Step 9: Write Final Document

On user confirmation:
1. Strip draft-only front matter fields (`preview-generated-at`, `generation-started-at`, `draft-generation-seconds`, `stage-timings`, `review-mode`)
2. Set `status: draft` — never copy grouping status from modules.yaml
3. Confirm `<!-- akr-generated -->` metadata header is present
4. Finalize by promoting the reviewed draft artifact:
  - If draft path differs from `doc_output`, move/rename the sanitized draft file to `doc_output`
  - If draft path equals `doc_output`, sanitize in place
5. Cleanup policy:
  - Default: do not leave a duplicate draft artifact after successful promotion
  - Optional `--keep-draft`: keep a copy at `draft_output` for audit workflows

---

## Step 10: Inline Validation (Immediate — No External Files Required)

Run the inline validator immediately after writing the final document.
This validator is self-contained Python — it requires no modules.yaml lookup,
no vale installation, and no distribution to the application repo.

**How to invoke:**

The inline validator is embedded in the skill bundle at:
`.github/skills/akr-docs/scripts/akr_inline_validate.py`

```bash
python .github/skills/akr-docs/scripts/akr_inline_validate.py \
  {doc_output_path} \
  --output text
```

If the script is not yet present locally (first run before distribution):

```bash
# Fetch from core-akr-templates runtime cache
python ~/.akr/templates/.github/skills/akr-docs/scripts/akr_inline_validate.py \
  {doc_output_path}
```

**What the inline validator checks:**

| Check | What it catches |
|---|---|
| YAML front matter presence | Missing `---` block |
| Required front matter fields | Missing businessCapability, feature, layer, project_type, status, compliance_mode |
| Field value validity | Invalid layer, project_type, status, compliance_mode values |
| Draft-only field cleanliness | preview-generated-at, generation-started-at, draft-generation-seconds, stage-timings, or review-mode present in final output |
| akr-generated header | Missing `<!-- akr-generated` comment |
| Required section headings | Missing sections (discovered from akr:section directives or baseline fallback) |
| Unresolved ❓ markers | Warning in pilot, error in production |
| DEFERRED markers | Warning — verify each has owner and follow-up |

**What the inline validator does NOT check** (deferred to CI full validation):

| Check | Why deferred |
|---|---|
| modules.yaml schema validity | Needs full YAML parse and cross-field logic — CI scope |
| doc_output path registration | Needs modules.yaml cross-reference — CI scope |
| Duplicate doc_output paths | Needs full manifest scan — CI scope |
| declared-artifacts warnings | Needs filesystem check of draft/review paths — CI scope |
| Vale prose linting | Needs Vale binary and rule files — CI scope |
| Completeness scoring (penalty model) | Needs full section analysis — CI scope |
| Cross-module relationship checks | Needs full manifest — CI scope |

**Interpreting inline validation output:**

```
AKR Inline Validation: docs/modules/Course_doc.md
Status:  ✅ PASSED
Errors:  0
Warnings: 2

  [WARNING] [transparency-markers] Found 8 unresolved ❓ marker(s). Resolve before graduating to production.
  [WARNING] [transparency-markers] Found 2 DEFERRED marker(s). Verify each has owner and follow-up.

✅ Inline checks passed. Open PR to trigger full CI validation.
```

**If inline validation fails:** Fix errors before opening a PR. The specific
errors surfaced here (missing front matter, missing header, missing required
sections) are the same ones that will fail CI — catching them now avoids a
failed PR round-trip.

**If inline validation passes:** Surface the result in chat and instruct the
user to open a PR. The full CI pipeline (`validate-documentation.yml`) runs
automatically on PR open and provides the complete validation report including
Vale, completeness scoring, and modules.yaml cross-checks.

---

## Step 11: Surface Result to User

After inline validation, show this summary in chat:

```
## Generation Complete: {ModuleName}

Document written to: {doc_output_path}
Draft source:        {draft_output_path}
Total draft time:    {draft_generation_seconds}s

Stage breakdown:
  preflight:          {N}s
  template-fetch:     {N}s  ({hit | miss})
  charter-fetch:      {N}s  ({hit | miss})
  source-extraction:  {N}s
  assembly:           {N}s
  write:              {N}s

Inline validation: {✅ PASSED / ❌ FAILED — N errors}
  Warnings: {N} (resolve before production graduation)

{If PASSED}
Next step: Open a PR. Full CI validation runs automatically and will check:
  - Vale prose linting
  - modules.yaml cross-references
  - Completeness scoring
  - All required sections with penalty model

{If FAILED}
Fix required before PR:
  {list errors with line references}
```

---

## Quality Threshold Checklist

Applies before writing final document (Step 9), independent of validation:

- [ ] All required sections present in section plan order
- [ ] No required section truncated
- [ ] `module_files`: every file in modules.yaml for this module appears
- [ ] `operations_map`: every public method in every module file appears
- [ ] `business_rules`: Why It Exists and Since When on every row
- [ ] `data_operations`: reads, writes, and side effects covered
- [ ] All unknowns marked ❓ or DEFERRED with owner
- [ ] `<!-- akr-generated -->` header present
- [ ] Draft-only front matter fields (`preview-generated-at`, `generation-started-at`, `draft-generation-seconds`, `stage-timings`, `review-mode`) absent from final output
- [ ] Excluded sections recorded in draft front matter with reasons
- [ ] All stage timing metrics captured in draft front matter and surfaced in confirmation prompt

---

## What This File Must Never Contain

- ❌ Section names or headings
- ❌ Section order (order lives in `akr:section order=N` directives)
- ❌ Conditional inclusion logic by name
- ❌ Table column names for any section
- ❌ Marker placement rules by section
- ❌ Required vs optional classification of any section

If any of the above appear here, move them to the template.

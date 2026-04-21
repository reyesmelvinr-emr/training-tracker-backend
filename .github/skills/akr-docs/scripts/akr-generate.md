# AKR Mode Script: GenerateDocumentation

<!-- Loaded on demand by SKILL.md dispatcher. -->
<!-- This file contains ZERO section names, ZERO section order, ZERO conditional logic. -->
<!-- All structural knowledge lives in the template via akr: directives. -->

## Purpose

Execute documentation generation for one or more named modules. Read structural knowledge
from the template's `akr:` directives. Do not encode structural knowledge here.

**Single-module mode:** `/akr-docs generate [ModuleName]`
**Batch mode:** `/akr-docs generate --batch [ModuleA ModuleB ModuleC ...]` (max 5 modules, auto-skips scoring)

---

## Step 1: Pre-flight

### Single-module mode

Read `modules.yaml`. Locate the target module.

Capture `generation_started_at` as UTC timestamp immediately when `/akr-docs generate [ModuleName]` is invoked. This timestamp is used to compute per-stage timing metrics recorded in Steps 2–8.

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

### Batch mode

When `--batch` flag is present, immediately parse the module list and validate before any generation starts. Batch size limit: **5 modules maximum**.

**Batch pre-flight validation:**
1. Count modules: if count > 5, stop with message: `Batch limit exceeded: listed {count} modules, max is 5. Split into multiple runs.`
2. For each module name in the list:
   - Confirm it exists in `modules.yaml`
   - Confirm `grouping_status: approved` (reject `draft` status)
   - Record module's `doc_output` path
3. If any module fails validation, stop and surface ALL failures at once:
   ```
   Batch pre-flight validation failed:
     ModuleB — not found in modules.yaml
     ModuleD — grouping_status: draft (must be approved)
   Correct modules.yaml entries before re-running.
   ```
4. If all validations pass, surface batch plan preview:
   ```
   Batch plan (3 modules):
     1. CourseDomain       → docs/modules/CourseDomain_draft.md
     2. EnrollmentDomain   → docs/modules/EnrollmentDomain_draft.md
     3. InstructorDomain   → docs/modules/InstructorDomain_draft.md
   
   Template cache: {hit | miss} (reyesmelvinr-emr/core-akr-templates@master)
   Charter: shared ({project_type} — fetched once)
   
   Proceed with batch generation? (yes / no)
   ```
5. Wait for explicit user confirmation before beginning Step 1 iteration.
6. Initialize batch results tracking structure:
   ```
   batch_results = {
     total_modules: count,
     modules: [],  # array of {name, status, draft_path, doc_path, generation_seconds, validation_status, error}
     successful: [],
     failed: []
   }
   ```

In batch mode, **Steps 1–8 run per module in sequence.** Each module execution resets `generation_started_at` to mark the beginning of that module's generation (not the batch start time). This ensures each module's draft front matter records independent timing metrics.

**Cross-chat template and charter cache:** `.akr/cache/` in the workspace root. Templates and charters fetched via `@github` are written here keyed by `{owner}/{repo}@{branch}/{encoded-path}`. A new VS Code chat session in the same workspace can read from this cache without a remote fetch, eliminating repeat network round-trips. To force refresh cache contents, run `/akr-docs refresh-assets` before generating. Add `.akr/cache/` to `.gitignore` to prevent committing cached remote assets.

- If `grouping_status: draft` → stop. Tell the user to approve the grouping first.
- If module not found → stop. Tell the user to run `/akr-docs groupings` first.
- If `feature` value matches the all-zero placeholder (e.g. `FN00000_US000`) → do not copy it. Write `feature: ❓ NEEDS real work-item tag` in the output front matter instead.

Infer `project_type` from the module file list.

Normalize module file entries first (backward compatible):
- Legacy entry: `"src/Foo.cs"` → treat as `{ path: "src/Foo.cs", tier: "primary" }`
- New entry: `{ path: "src/Foo.cs", tier: "primary|supporting" }`

For documentation decisions, use only files where `tier=primary`.

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
| `api-backend` / `microservice` / `general` | `.akr/templates/lean_baseline_service_template_module.md` |
| `ui-component` | `.akr/templates/ui_component_template_module.md` |

**Fetch the full template file; parse and carry forward only the `akr:` directive
blocks. Discard the template prose body.** Parse and carry forward:

- **Section registry:** ordered list of `{id, required, order, condition}` objects
- **Condition definitions:** token → detection description mapping

**Before fetching, check cache:**
1. Compute cache key: `{owner}/{repo}@{branch}/{template_path}` (e.g. `reyesmelvinr-emr/core-akr-templates@master/.akr/templates/lean_baseline_service_template_module.md`)
2. Encode key per `SKILL.md` section **Cache Key Encoding Contract**.
3. Cache file: `.akr/cache/{encoded_cache_key}.md`
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
2. Encode key per `SKILL.md` section **Cache Key Encoding Contract**.
3. Cache file: `.akr/cache/{encoded_cache_key}.md`
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

Read module `files:` from `modules.yaml` using backward-compatible parsing:
- If entry is string, treat as `path` with implicit `tier=primary`.
- If entry is object, read `path` and `tier`.

Then read only files where `tier=primary` for documentation generation.
Supporting files remain in `modules.yaml` for coding-assistance context and must not be used as primary documentation evidence.

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
- Excluded sections → record in draft front matter under `excluded-sections` with deterministic reason format:
  - `section_id — condition=<token>; observed=false`
  - If exclusion is due to missing source evidence (not condition token), use:
    - `section_id — insufficient_source_evidence`

---

## Step 6: Generate Documentation

Record `stage_assembly_start = now_utc` before generating the first section.

Generate sections in section plan order using:
1. `akr:section` directive guidance for field coverage, markers, violations, format
2. Charter grounding rules from Step 3 forward payload
3. Targeted template section inspection from already loaded template content when exact format verification is needed
4. Transparency markers — every AI-inferred statement must include a 🤖 marker inline. Do not emit unmarked AI narrative.

Cache-invariant output rule:
- Cache hit/miss may change retrieval timings only.
- Cache hit/miss must NOT change section inclusion/exclusion decisions, required section coverage, metadata key shape, or output style conventions.

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
2. Otherwise use the directory of `doc_output` and write `{ModuleName}_draft.md` in that same directory.

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

Write `<!-- akr-generated -->` metadata header using canonical key/value formats:
- `template`: full identity `{owner}/{repo}@{branch}/{path}`
- `charter`: full identity `{owner}/{repo}@{branch}/{path}`
- `steps-completed`: comma-separated ascending sequence, exact format: `1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12`
- `generation-strategy`: `single-pass` or `section-scoped`
- `passes-completed`: `single` for single-pass, else comma-separated pass IDs (e.g., `1, 2A, 2B, 3, 4, 5, 6, 7`)
- `pass-timings-seconds`: fixed-order key/value list: `preflight=<N> | template-fetch=<N> | charter-fetch=<N> | source-extraction=<N> | assembly=<N> | write=<N>`
- `total-generation-seconds`: integer

Do not emit short template/charter names (e.g., `lean_baseline_service_template_module.md`) in final metadata when full identity is available.
Record `stage_timers.write_seconds = now_utc - stage_write_start` after the file is written and add to the `stage-timings` block above.

Determinism check:
- Verify metadata shape is canonical (same keys/order/format regardless of cache hit/miss).
- Verify `excluded-sections` entries follow deterministic reason format.

### Single-module mode: confirmation gate

Surface draft path in chat and include this confirmation prompt payload:
- `Draft path: {draft_output_path}`
- `Final path: {doc_output_path}`
- `Total draft generation time: {draft_generation_seconds}s`
- Stage breakdown: `preflight {N}s | template-fetch {N}s ({hit|miss}) | charter-fetch {N}s ({hit|miss}) | source-extraction {N}s | assembly {N}s | write {N}s`

If `draft_output_path` and `doc_output_path` differ, explicitly warn that finalize will promote content to a different path.

Wait for explicit user confirmation before Step 9.

### Batch mode: collect result and continue

In batch mode, do NOT surface confirmation prompt or pause. Instead:
1. Record this module's result in `batch_results.modules[]`:
   ```
   {
     name: {ModuleName},
     status: "draft_written",
     draft_path: {draft_output_path},
     doc_path: {doc_output_path},
     generation_seconds: {draft_generation_seconds},
     validation_status: null,  # filled in Step 10
     error: null
   }
   ```
2. Add module name to `batch_results.modules[]` — do NOT yet add to successful/failed (pending Step 10)
3. Continue to the next module in the batch (loop continues)
4. If any module fails during Steps 1–8 (e.g., source file missing, project_type ambiguous):
   - Record the error in `batch_results.modules[{module_index}].error`
   - Set `status: "generation_failed"`
   - Add to `batch_results.failed[]`
   - Continue to the next module (do not abort the entire batch)

---

## Step 8.5: Consolidated Confirmation Gate (Batch Mode Only)

**Applies only to batch mode.** This step runs AFTER all drafts have been written in the batch loop.

All modules from the batch have been processed. Some may have failed during generation (Step 1–8), while others are ready for promotion.

**Display consolidated results table:**

```
All drafts processed (3 total):
┌──────────────────┬─────────────────────────┬────────┬────────────────┐
│ Module           │ Draft Path              │ Time   │ Status         │
├──────────────────┼─────────────────────────┼────────┼────────────────┤
│ CourseDomain     │ docs/.../draft.md       │ 42s    │ ✅ ready       │
│ EnrollmentDomain │ docs/.../draft.md       │ 38s    │ ✅ ready       │
│ InstructorDomain │ —                       │ —      │ ❌ failed      │
└──────────────────┴─────────────────────────┴────────┴────────────────┘

Failure reasons:
  InstructorDomain — Source file missing: src/Controllers/InstructorsController.cs

2 of 3 modules ready for promotion.

Promote successful modules to final documents? (yes / no)
```

### Single confirmation decision

Wait for explicit user confirmation. Do NOT ask per-module.

**On yes:** Proceed to Step 9 with only the successful modules from `batch_results.successful[]`

**On no:** Stop. Skip Step 9 and proceed to Step 10 (inline validation for all modules with outputs). No documents are promoted.

---

## Step 9: Write Final Document

### Single-module mode

On user confirmation:
1. Strip draft-only front matter fields (`preview-generated-at`, `generation-started-at`, `draft-generation-seconds`, `stage-timings`, `review-mode`, `generation-strategy`, `passes-completed`, `excluded-sections`)
2. Set `status: draft` — never copy grouping status from modules.yaml
3. Confirm `<!-- akr-generated -->` metadata header is present
4. Finalize by promoting the reviewed draft artifact:
  - If draft path differs from `doc_output`, move/rename the sanitized draft file to `doc_output`
  - If draft path equals `doc_output`, sanitize in place
5. Cleanup policy:
  - Default: do not leave a duplicate draft artifact after successful promotion
  - Optional `--keep-draft`: keep a copy at `draft_output` for audit workflows

### Batch mode

On user confirmation in Step 8.5, promote all modules in `batch_results.successful[]`:

**For each successful module:**
1. Strip draft-only front matter fields (`preview-generated-at`, `generation-started-at`, `draft-generation-seconds`, `stage-timings`, `review-mode`, `generation-strategy`, `passes-completed`, `excluded-sections`)
2. Set `status: draft` — never copy grouping status from modules.yaml
3. Confirm `<!-- akr-generated -->` metadata header is present
4. Finalize by promoting the reviewed draft artifact:
  - If draft path differs from `doc_output`, move/rename the sanitized draft file to `doc_output`
  - If draft path equals `doc_output`, sanitize in place
5. Record promotion success in `batch_results.modules[{module_index}].status = "promoted"`

Cleanup policy (batch mode):
- Default: do not leave duplicate draft artifacts after successful promotion
- Optional `--keep-draft`: keep copies at draft paths for audit workflows

**For failed modules in `batch_results.failed[]`:**
- Do NOT process; draft remains at draft path for manual intervention
- Status remains "generation_failed" in results

If user chose "no" in Step 8.5, skip all promotion. Status for all modules remains "draft_written" or "generation_failed".

---

## Step 10: Inline Validation (Immediate — No External Files Required)

### Single-module mode

Run the inline validator immediately after writing the final document in Step 9.
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
| Draft-only field cleanliness | preview-generated-at, generation-started-at, draft-generation-seconds, stage-timings, review-mode, generation-strategy, passes-completed, or excluded-sections present in final output |
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

**If inline validation passes:** Proceed directly to Step 11 (auto-score). 

The Step 12 summary will then instruct the user to open a PR. The full CI pipeline (`validate-documentation.yml`) runs automatically on PR open and provides the complete validation report including Vale, completeness scoring, and modules.yaml cross-checks.

### Batch mode

Run inline validation **per module** for all produced documents:

**For each module in `batch_results.modules[]` where status is "promoted" or "draft_written":**
1. If final document exists, validate it:
   ```bash
   python .github/skills/akr-docs/scripts/akr_inline_validate.py \
     {doc_output_path} \
     --output json
   ```
2. Record validation result in `batch_results.modules[{i}].validation_status`:
   - If validation passes: `"validation_passed"` + add module to `batch_results.successful[]`
   - If validation fails: `"validation_failed"` + capture error list
3. Collect all validation outputs (passed and failed)

**For modules where status is "generation_failed":**
- Skip validation (no output file exists)
- Record `validation_status: "skipped_generation_failed"`

**After validating all modules:** Proceed to Step 12 with consolidated validation summary (no Step 11 auto-score for batch mode).

---

## Step 11: Auto-Score (runs only in single-module mode when Step 10 passes with 0 errors)

**Batch mode:** auto-skip this step unconditionally. Proceed directly to Step 12 after Step 10 completion.

**Single-module mode:** proceed only if Step 10 output includes `Status: ✅ PASSED` and `Errors: 0`.
If Step 10 output includes `Status: ❌ FAILED` or any `[ERROR]` lines,
skip this step entirely and go to Step 12.

This scoring step runs in the same Copilot session as generation — zero
additional LLM API cost beyond the current session.

Use the scoring rubric and weighting rules from `/akr-docs score` as the
authoritative source (`akr-score.md`, Steps 3-4). Do not redefine alternate
rubrics or weights in this mode.

**Evaluate sections:**

1. Scan the final document for `akr:section` HTML comment directive blocks.
2. For each directive block, extract `authorship` and `human_columns`.
3. Score sections:
   - `authorship: human` → evaluate full section content
   - `authorship: mixed` → evaluate only the `human_columns` content
   - `authorship: ai` → skip
4. If no `akr:section` directives found, apply the default scoring policy:
   score Quick Reference (TL;DR), Purpose and Scope, Business Rules
   (`why_it_exists` and `since_when` columns), and Questions & Gaps if present.

Apply the rubric, weights, and score formula exactly as defined in
`akr-score.md` Steps 3-4.

**Display per-section summary in chat:**

```
AKR Semantic Score — {ModuleName}
────────────────────────────────────────────────
Section                   Score  Tier              Weight
Quick Reference (TL;DR)   ?/10   ?                 1.5
Purpose and Scope         ?/10   ?                 1.5
Business Rules            ?/10   ?                 2.0
Questions & Gaps          ?/10   ?                 1.0
────────────────────────────────────────────────
Semantic Score: ?/100
────────────────────────────────────────────────
Write score to front matter? Reply "yes" to continue or "skip" to proceed without scoring.
```

**On "yes":** Write these three fields into the document's YAML front matter in-place:

```yaml
semantic-score: {integer 0–100}
semantic-scored-at: "{ISO 8601 timestamp}"
semantic-score-version: "v1.0"
```

Do NOT modify any other part of the document. Confirm in chat:
"Semantic score written to {doc_output_path}."

**On "skip":** CI will emit a warning that no semantic score is present but will
not block merge.

**SAFETY:** Never add `semantic-score`, `semantic-scored-at`, or
`semantic-score-version` to `DRAFT_ONLY_FIELDS` in `akr_inline_validate.py`.
These fields must survive through PR and merge.

---

## Step 12: Surface Result to User

### Single-module mode

After inline validation and scoring (if completed), show this summary in chat:

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

Semantic score:     {N}/100 ({tier}) | skipped

{If PASSED}
Next step: resolve open items using the right flow, then open a PR.
  - Use `/akr-docs resolve` for source-grounded draft cleanup.
  - Use `/akr-interview` for callout-driven collaboration and owner-specific routing.

After resolution, open a PR. Full CI validation runs automatically and will check:
  - Vale prose linting
  - modules.yaml cross-references
  - Completeness scoring
  - All required sections with penalty model

{If FAILED}
Fix required before PR:
  {list errors with line references}

Metadata snapshot (canonical):
  template:           {owner}/{repo}@{branch}/{template_path}
  charter:            {owner}/{repo}@{branch}/{charter_path}
  steps-completed:    1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12
  passes-completed:   {single | pass list}
```

### Batch mode

After inline validation of all modules, show batch summary:

```
## Batch Generation Complete

Processed 3 modules:
┌──────────────────┬──────────┬──────────────────┬────────────┬──────────────────────┐
│ Module           │ Final    │ Draft → Final    │ Validation │ Next Steps           │
├──────────────────┼──────────┼──────────────────┼────────────┼──────────────────────┤
│ CourseDomain     │ ✅ docs/ │ docs/.../draft   │ ✅ 0 errors│ Resolve ❓ markers   │
│ EnrollmentDomain │ ✅ docs/ │ docs/.../draft   │ ❌ 2 errors│ Fix validation errors │
│ InstructorDomain │ ❌ —     │ generation_failed│ —          │ Check source files   │
└──────────────────┴──────────┴──────────────────┴────────────┴──────────────────────┘

Results:
  Promoted: 2 modules
  Validation passed: 1 module
  Requires fixes: 1 module
  Generation failed: 1 module

Total batch time: {sum of all module generation_seconds}s

Next steps:
  - Promoted + validated modules: resolve ❓ markers, then open PR
  - Promoted + validation errors: fix errors, then commit and push before PR
  - Generation failures: check source files and re-run generation

Semantic scores: skipped (batch mode)

CI validation will check all promoted modules when PR opens (Vale, cross-refs, completeness).
```

Detailed per-module validation results (if any failures):
```
EnrollmentDomain — Validation Failed:
  [ERROR] [required-fields] Missing businessCapability
  [WARNING] [transparency-markers] 3 unresolved ❓ markers

InstructorDomain — Generation Failed:
  Source file missing: src/Controllers/InstructorsController.cs
  Cannot infer project_type without primary files.
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
- [ ] Draft-only front matter fields (`preview-generated-at`, `generation-started-at`, `draft-generation-seconds`, `stage-timings`, `review-mode`, `generation-strategy`, `passes-completed`, `excluded-sections`) absent from final output
- [ ] Score front matter fields (`semantic-score`, `semantic-scored-at`, `semantic-score-version`) present if scoring was not skipped
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

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

- If `grouping_status: draft` → stop. Tell the user to approve the grouping first.
- If module not found → stop. Tell the user to run `/akr-docs groupings` first.

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

If the template cannot be fetched via `@github`, read from
`~/.akr/templates/templates/` (local AKR cache).

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

Compress into a forward payload summary (~400 tokens). Carry only:
- Marker placement rules (🤖 / ❓ / NEEDS / VERIFY / DEFERRED)
- Grounding rules
- Quality threshold checklist items

---

## Step 4: Read Source Files → Structured Facts Payload

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

---

## Step 5: Resolve Section Plan

For each section in the section registry (sorted by order):
- `condition == null` → include
- `condition != null` → include only if `facts.conditions_detected[condition] == true`
- Excluded sections → record in draft front matter under `excluded-sections` with reason

---

## Step 6: Generate Documentation

Generate sections in section plan order using:
1. `akr:section` directive guidance for field coverage, markers, violations, format
2. Charter grounding rules from Step 3 forward payload
3. Targeted template section fetch only when exact format verification is needed

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

---

## Step 8: Write Draft Artifact

Write to `docs/modules/.akr/{ModuleName}_draft.md` with draft-only front matter:
```yaml
preview-generated-at: {ISO-8601}
review-mode: full
generation-strategy: {single-pass | section-scoped}
passes-completed: {list}
excluded-sections:
  - "section_id — reason"
```

Surface draft path in chat. Wait for explicit user confirmation before Step 9.

---

## Step 9: Write Final Document

On user confirmation:
1. Strip draft-only front matter fields (`preview-generated-at`, `review-mode`)
2. Set `status: draft` — never copy grouping status from modules.yaml
3. Confirm `<!-- akr-generated -->` metadata header is present
4. Write to `doc_output` path from modules.yaml

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
| Draft-only field cleanliness | preview-generated-at or review-mode present in final output |
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
- [ ] Draft-only front matter fields absent from final output
- [ ] Excluded sections recorded in draft front matter with reasons

---

## What This File Must Never Contain

- ❌ Section names or headings
- ❌ Section order (order lives in `akr:section order=N` directives)
- ❌ Conditional inclusion logic by name
- ❌ Table column names for any section
- ❌ Marker placement rules by section
- ❌ Required vs optional classification of any section

If any of the above appear here, move them to the template.

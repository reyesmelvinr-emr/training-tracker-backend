# AKR Mode Script: ResolveUnknowns

<!-- Loaded on demand by SKILL.md dispatcher. Do not load unless /akr-docs resolve was invoked. -->

## Purpose

Interactively resolve unresolved ❓ markers in an existing draft document. Apply accepted answers as surgical edits without regenerating the whole document.

Token budget: 0 `@github` calls by default. This mode operates on the local
document plus local source context only and must not fetch templates/charters.

## Pre-flight

1. Read the target document specified by the user.
2. Extract all ❓ markers with their surrounding section context.
3. Check `compliance_mode` in document front matter.
   - `production`: unresolved ❓ markers are blocking — prioritize all.
   - `pilot`: flag critical gaps, allow DEFERRED for non-critical.

## Resolution Workflow

### Phase 1: Inventory

Build and display an inventory of unresolved items grouped by section:

```
Unresolved markers in {ModuleName}_doc.md:

Section: Business Rules (3 items)
  BR-001 Since When: ❓ (critical — blocks production mode)
  BR-002 Why It Exists: ❓
  BR-003 Since When: ❓

Section: Architecture Overview (1 item)
  Consumer map: ❓ who calls this module externally?

Section: Questions and Gaps (2 items)
  [item 1]
  [item 2]

Total: 6 unresolved items
```

### Phase 2: Interactive Resolution

For each unresolved item, ask one targeted question. Do not ask multiple questions at once.

**Question format:**
```
[Section: Business Rules — BR-ENR-001]
Since When was this rule introduced?
(Provide a sprint number, date, or version. If unknown, type "defer" to mark as DEFERRED.)
```

On user response:
- **If answered:** Apply the edit immediately. Mark the item resolved in your inventory. Move to the next item.
- **If "defer":** Ask for a one-line rationale and an owner name. Apply `DEFERRED: {rationale} — Owner: {name}` inline. Mark resolved.
- **If "skip":** Leave the ❓ marker. Note the skip in your summary. Move to the next item.

### Phase 3: Source-Grounded Proposals

For items where the answer might be inferrable from source code (e.g., a business rule's enforcement point), propose an answer based on the source files and ask for confirmation:

```
[Section: Business Rules — BR-CRS-001]
Based on CourseService.cs line ~45, this rule was introduced alongside the title uniqueness check.
Proposed Since When: "Version 1.0 (initial implementation)"
Accept this? (yes / no / provide different answer)
```

### Phase 4: Re-Validation

After resolving a batch of items (or all items), run the inline validator
locally. The full CI validator (`validate_documentation.py`) is not available
in the local development environment — it runs only in CI after PR open.

```bash
python .github/skills/akr-docs/scripts/akr_inline_validate.py \
  {doc_path} \
  --output text
```

If the inline validator is not yet present locally (first run before distribution):

```bash
python ~/.akr/templates/.github/skills/akr-docs/scripts/akr_inline_validate.py \
  {doc_path}
```

The inline validator checks front matter completeness, metadata header presence,
required sections, and unresolved ❓ markers. Full CI validation (Vale, completeness
scoring, modules.yaml cross-checks) runs automatically when a PR is opened.

Report the before/after unresolved ❓ count from the inline validator output.

### Phase 5: Summary

Display a completion summary:

```
ResolveUnknowns complete for {ModuleName}_doc.md

Resolved:  4 items
Deferred:  1 item (owner: Jane Smith — review in Sprint 42)
Skipped:   1 item (still unresolved)

Validator: 0 errors, 1 warning (1 remaining ❓ in pilot mode)
PR readiness: ✅ Ready for pilot mode review
```

## Surgical Edit Rules

- Edit only the specific line or cell containing the ❓ marker.
- Do not regenerate surrounding content.
- Do not alter section headings, table structure, or unaffected rows.
- Preserve all 🤖 markers — do not remove or add them during resolution.
- Preserve existing content that is not being resolved.

## DEFERRED Format

When an item is deferred, replace the ❓ with:

```
DEFERRED: {one-line rationale} — Owner: {name}, Follow-up: {trigger or sprint}
```

Example:
```
DEFERRED: Business rule origin predates current team — Owner: Alice Smith, Follow-up: Sprint 43 retrospective
```

## Checklist Before Closing

- [ ] All critical ❓ markers addressed (answered or DEFERRED with owner)
- [ ] All DEFERRED items have owner and follow-up trigger
- [ ] Validator passes for current compliance_mode
- [ ] Document `status` field updated if compliance milestone reached

---
mode: score
description: Semantic quality scoring for AKR module documentation. Reads the final module document, evaluates human-authored sections per the scoring rubric, writes semantic-score/semantic-scored-at/semantic-score-version into YAML front matter in-place, and displays a per-section score summary.
---

# AKR Score Mode — `/akr-docs score [ModuleName]`

## Overview

This mode computes a semantic quality score for a completed module document. Run this after content review and before opening the PR. The score is written directly into the document's YAML front matter so CI can compute the combined score automatically.

**Inference cost:** Uses the current Copilot session — zero marginal API cost beyond your existing session usage.

**Advisory only:** In PoC mode, the semantic score never blocks merge. It is a quality signal for the tech lead and product owner.

---

## Step 1: Locate the Module Document

1. Read `modules.yaml` in the workspace root.
2. Find the entry where `name` matches the requested `[ModuleName]` (case-insensitive).
3. Read the `doc_output` path from that entry.
4. Confirm the file exists at that path. If not found, stop and report:
   - The module name provided
   - The path from `modules.yaml`
   - Instruction: "Confirm the module name matches an entry in modules.yaml and the doc_output file has been written."

---

## Step 2: Read Directive Authorship Assignments

1. Scan the document for `akr:section` HTML comment directive blocks.
2. For each directive block, extract:
   - `id` — section identifier
   - `authorship` attribute — `ai`, `human`, or `mixed` (default: `human` if absent)
   - `human_columns` attribute — comma-separated list (only relevant for `mixed`)
3. Build a list of sections to score:
   - Include sections with `authorship: human` → score full section content
   - Include sections with `authorship: mixed` → score only the columns named in `human_columns`
   - Exclude sections with `authorship: ai` → skip entirely
4. If no `akr:section` directives are found in the document, apply the default scoring policy: score these sections if present —
   - Quick Reference (TL;DR)
   - Purpose and Scope
   - Business Rules (`why_it_exists` and `since_when` columns only)
   - Questions & Gaps

---

## Step 3: Evaluate Each Human-Authored Section

For each section identified in Step 2, read its content and assign a score using the rubric below.

### Scoring Rubric (0–10 per section)

| Score | Tier | Criteria |
|-------|------|----------|
| 0–2 | Template placeholder | Content is unchanged from template; bare `❓` with no additional text; empty required cells |
| 3–4 | Generic | Some content present but applies to any module; no specific business/domain context |
| 5–6 | Acknowledged gap | Explicitly notes what is unknown: "For further confirmation", "Pending confirmation from product owner", "DEFERRED: needs domain-owner input" — **explicitly rewarded** |
| 7–8 | Substantive | Business context evident; describes this module's specific behavior or rules; minor gaps acceptable |
| 9–10 | Complete | Genuine domain knowledge; no template filler; all required sub-fields populated with real content |

**Scoring note for acknowledged gaps (5–6):** A developer who explicitly acknowledges uncertainty provides more governance value than one who leaves a template placeholder unchanged. Reward honest documentation of what is unknown.

**For `mixed` sections:** Score only the `human_columns` content. Ignore AI-generated columns. Average the column scores to produce the section score.

### Section Weights

| Section (by id or heading) | Weight |
|----------------------------|--------|
| `business_rules` / Business Rules | 2.0 |
| `quick_reference` / Quick Reference (TL;DR) | 1.5 |
| `purpose_scope` / Purpose and Scope | 1.5 |
| `questions_gaps` / Questions & Gaps | 1.0 |
| All other human-authored sections | 1.0 |

---

## Step 4: Compute Weighted Score

```
semantic_score = sum(section_score × section_weight) / sum(section_weight)
semantic_score = round(semantic_score × 10, 0)   # scale 0–10 → 0–100
```

Round the final semantic score to the nearest integer (0–100).

---

## Step 5: Display Per-Section Summary

Before writing the score to the document, display this summary in the chat:

```
AKR Semantic Score — [ModuleName]
──────────────────────────────────────────────────────
Section                    Score  Tier                  Weight
Quick Reference (TL;DR)    7/10   Substantive           1.5
Purpose and Scope          6/10   Acknowledged gap      1.5
Business Rules             5/10   Acknowledged gap      2.0
  └─ why_it_exists         5/10
  └─ since_when            5/10
Questions & Gaps           8/10   Substantive           1.0
──────────────────────────────────────────────────────
Semantic Score: 62/100
──────────────────────────────────────────────────────
Writing scores to front matter. Confirm? (yes/no)
```

Wait for user confirmation before writing. If the user responds "no" or asks for changes, stop and explain what would need to change (document content, not the rubric).

---

## Step 6: Write Score to Front Matter

1. Read the current front matter block from the document.
2. Add or update these three fields:
   ```yaml
   semantic-score: [integer 0-100]
   semantic-scored-at: "[ISO 8601 timestamp, e.g. 2026-04-06T14:32:00Z]"
   semantic-score-version: "v1.0"
   ```
3. Write the updated front matter back to the document at `doc_output` path.
4. Do NOT modify any other part of the document.
5. Confirm in chat: "Score written to [doc_output path]. Combined score will be computed by CI when the PR is opened."

---

## Safety Rules

- **NEVER add `semantic-score`, `semantic-scored-at`, or `semantic-score-version` to `DRAFT_ONLY_FIELDS`** in `akr_inline_validate.py`. These fields must survive through PR and merge.
- Score fields are written to the final document path (`doc_output`), never to a draft path.
- If the document does not have YAML front matter, stop and report: "Front matter block not found. Generate the final document first using `/akr-docs generate [ModuleName]`."
- This mode does not run the validator, does not open a PR, and does not call any external API. It is a content review step only.

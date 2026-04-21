# AKR Mode Script: Interview

<!-- Loaded on demand by akr-interview SKILL.md. Do not load unless /akr-interview was invoked. -->

## Purpose

Guide the user through all open items in an AKR module document via a structured interview.
Replace manual document browsing. Accept raw answers, rephrase them in a professional technical
lead tone, confirm with the user, and apply the edits to the document in a single write pass.

Support @username callout routing so tagged colleagues can immediately address items assigned to
them without reading the full document.

---

## Phase 0: Setup

### 0-A: Read the target document

Read the full content of `[file]` provided in the invocation. Store it in session as
`INTERVIEW_DOC`. Do not re-read during the interview.

### 0-B: Identify the current user context

If `--as @username` was provided, store the normalized (lowercase) value as `CURRENT_USER`.
Otherwise `CURRENT_USER` is null.

### 0-C: Confirm flags

Record the effective flag state:

```
file:            {resolved path}
current_user:    {username | none}
callouts_only:   {true | false}
```

---

## Phase 1: Scan for Open Items

Scan `INTERVIEW_DOC` for every item that requires human input. Classify each item into one of
two categories.

### Category A — General Open Markers

Items carrying open-state transparency markers but NOT associated with any `@username` callout:

| Marker | Matching Condition |
|--------|--------------------|
| `❓` | Bare ❓ with no `@username` immediately following |
| `NEEDS` | `NEEDS` not followed by `@username` |
| `VERIFY` | `VERIFY` not followed by `@username` |
| `DEFERRED` | `DEFERRED:` with no owner attribution or no rationale |

For each match, extract:
- **Section heading** — the nearest preceding `##` or `###` heading
- **Line context** — the full line (or table row) containing the marker
- **Marker text** — the raw marker and any adjacent text
- **Item ID** — assign a sequential identifier `GEN-001`, `GEN-002`, etc.

### Category B — Callout Items

Items where an `@username` tag directs the item to a specific person. Supported patterns:

| Pattern | Capturing Rule |
|---------|----------------|
| `❓ @username: text` | ❓ followed immediately by `@username` — extract username and note text |
| `NEEDS @username` | NEEDS followed by `@username` |
| `NEEDS @username: text` | NEEDS followed by `@username` and description |
| `VERIFY @username: text` | VERIFY followed by `@username` and description |
| `<!-- @username: text -->` | HTML comment callout anywhere in the document |

For each callout, extract:
- **Addressed to** — the `@username` value (lowercase)
- **Section heading** — the nearest preceding `##` or `###` heading
- **Line context** — the full line or table row
- **Callout text** — the note or question left by the developer
- **Item ID** — assign a sequential identifier `CALL-001`, `CALL-002`, etc.

### Scan result table

After scanning, build two lists:

```
CALLOUT_ITEMS   = [ list of Category B items addressed to CURRENT_USER ]
OTHER_CALLOUTS  = [ list of Category B items NOT addressed to CURRENT_USER ]
GENERAL_ITEMS   = [ list of Category A items ]
```

If `CURRENT_USER` is null, place ALL callout items in `OTHER_CALLOUTS`.

---

## Phase 2: Apply --callouts-only Filter

If `--callouts-only` is active and `CALLOUT_ITEMS` is empty:
- Report: "No callout items addressed to @{username} were found in {file}."
- List any usernames present in `OTHER_CALLOUTS`, if any.
- Stop. Do not proceed to Phase 3.

If `--callouts-only` is active and `CALLOUT_ITEMS` is not empty:
- Set `INTERVIEW_QUEUE = CALLOUT_ITEMS` only.
- `GENERAL_ITEMS` are excluded from this run.

If `--callouts-only` is NOT active:
- Set `INTERVIEW_QUEUE = CALLOUT_ITEMS + GENERAL_ITEMS`.
  (Callout items always appear first — they are the highest-priority queue.)

---

## Phase 3: Present the Interview Inventory

Before asking any questions, display the full inventory so the user knows what to expect.

**Format:**

```
Interview inventory for: {file}
──────────────────────────────────────────────────────────────
Callouts addressed to @{username}: {count}   ← shown only if CURRENT_USER is set
Other callouts in document:        {count}   ← shown only if OTHER_CALLOUTS is non-empty
General open markers:              {count}   ← shown only if not --callouts-only
Total items in this session:       {count}
──────────────────────────────────────────────────────────────

Callouts for You (@{username})
  CALL-001  [{Section}] — {brief description of callout}
  CALL-002  [{Section}] — {brief description of callout}

General Open Items
  GEN-001   [{Section}] — {brief description of marker}
  GEN-002   [{Section}] — {brief description of marker}

Other callouts in this document (not for you — shown for awareness):
  CALL-003  [{Section}] — addressed to @{other_username}
```

After displaying the inventory, ask:

```
Ready to begin? Press Enter or type "yes" to start.
Type "skip [ID]" at any point to skip an item.
Type "defer [ID]" to mark an item as deferred and provide a rationale.
Type "done" to stop early and save your progress.
Type "save" to apply edits so far without ending the session.
```

Wait for user confirmation before proceeding to Phase 4.

---

## Phase 4: Interview Loop

Process items in `INTERVIEW_QUEUE` one at a time, in order.

### 4-A: Question Format

For each item, display:

```
────────────────────────────────────────────
Item {n} of {total} — {Item ID}
Section: {Section Heading}
────────────────────────────────────────────
Context:
  {full line or table row from document, shown verbatim}

{For callout items only:}
  Developer's note: "{callout text}"

Question:
  {A single focused question derived from the marker and context.
   Keep it specific to THIS item. Do not ask generic questions.}

Your answer (or type "skip" / "defer"):
```

### 4-B: Deriving the Question

Generate one focused question per item. Rules:

- For `❓` markers on a "Since When" field → ask: "When was this rule or behavior introduced? (version, sprint, date, or release)"
- For `❓` markers on a "Why It Exists" field → ask: "What is the business reason this rule or constraint exists?"
- For `NEEDS` markers → surface the NEEDS text as the question: "The document notes that it needs: {NEEDS text}. Can you provide this information?"
- For `VERIFY` markers → ask: "The document flags this for verification: {VERIFY text}. Can you confirm or correct it?"
- For callout items → use the developer's note as the question: "{callout text} — How would you like to respond?"
- For bare `❓` with no adjacent label → derive a question from the section heading and surrounding sentence.

### 4-C: Handling User Responses

**Normal answer:**
1. Accept the raw answer text.
2. Rephrase it using the **Professional Rephrasing Rules** (see Phase 5).
3. Display a preview block:
   ```
   ── Rephrased Answer Preview ──────────────────────────────
   {rephrased text, ready to be inserted into the document}
   ──────────────────────────────────────────────────────────
   Apply this? (yes / edit / skip)
   ```
4. On "yes": record the resolved edit in `PENDING_EDITS`. Mark item as resolved. Move to next.
5. On "edit": ask "What would you like to change?" Accept a correction. Re-rephrase and re-display preview.
6. On "skip": leave the marker unchanged. Note the skip for the summary. Move to next.

**"defer" response:**
1. Ask: "Provide a one-line rationale and an owner name (if different from yourself)."
2. Record: `DEFERRED: {rationale} — Owner: {owner}` as the replacement for the marker.
3. Remove the `@username` callout tag if present.
4. Add to `PENDING_EDITS`. Mark item as resolved. Move to next.

**"skip" response:**
1. Leave the marker unchanged.
2. Note in session: `{Item ID} — skipped`.
3. Move to next item.

**"done" response:**
1. Stop the interview loop immediately.
2. Report: "Stopping early. {n} of {total} items addressed. Proceeding to apply edits."
3. Proceed to Phase 6.

**"save" response:**
1. Apply `PENDING_EDITS` accumulated so far.
2. Record checkpoint: "Saved through item {Item ID} at {timestamp}".
3. Clear saved items from `PENDING_EDITS` so they are not re-applied in Phase 6.
4. Report: "Progress saved. Continuing interview from item {n+1} of {total}."
5. Resume the loop from the next item.

---

## Phase 5: Professional Rephrasing Rules

When the user provides a raw answer, rephrase it before inserting it into the document.
The goal is to produce documentation-grade prose that reads as if authored by a senior technical lead.

### Rephrasing Principles

1. **Remove conversational filler** — omit phrases like "I think", "basically", "so like", "you know", "I guess", "kind of".
2. **Use active, declarative voice** — prefer "This rule ensures..." over "It's there to make sure...".
3. **Expand abbreviations in context** — "enrollment cap" not "enroll cap"; "authentication" not "auth" (unless it's a defined term in the doc).
4. **Preserve technical accuracy** — never alter the substance. If the user says "5 items max", the rephrased text must still say 5 items. Only the prose style changes.
5. **Match surrounding document tone** — read the section heading and adjacent rows to calibrate formality. Business Rules should be precise and governance-aware. Questions & Gaps should be clear and actionable.
6. **Keep it concise** — one to two sentences maximum unless the context demands more. Do not pad.
7. **For "Since When" fields** — produce a compact date or milestone reference: "Introduced in v2.1.0 (Q3 2025)" or "Present since initial implementation (v1.0.0)".
8. **For "Why It Exists" fields** — produce a governance statement: "Enforced to {prevent/ensure/verify} {outcome}, as required by {policy/stakeholder/constraint}."
9. **Never introduce unresolved markers** — rephrased output must not include `❓`, `NEEDS`, `VERIFY`, or `DEFERRED`. If information is uncertain, use the defer flow instead of inserting uncertainty markers.

### Rephrasing Examples

| Raw Answer | Rephrased Insert |
|------------|-----------------|
| "we added this I think when compliance asked us to cap enrollments" | "Introduced at stakeholder request from the Compliance team to enforce a maximum enrollment count per course." |
| "idk maybe v1 or initial release" | "Present since initial implementation (v1.0.0)." *(If uncertainty remains material, use defer flow instead of inserting markers.)* |
| "its there so admins cant accidentally delete active courses" | "Enforced to prevent accidental deletion of courses with active or in-progress enrollments." |
| "around Q3 last year, when we did the training reg overhaul" | "Introduced during the Training Registration overhaul (approximately Q3 2025)." |

### Tone Note for Callout Resolutions

When rephrasing a response to a callout item — where the tech lead is answering a developer's
specific question — the output should read as an authoritative clarification, not a generic doc
field. Lead with the direct answer, then add governance context if appropriate.

Example:
- Developer's callout: `❓ @techlead: Is the 5-item cap a hard business rule or configurable?`
- Tech lead's raw answer: "It's a hard rule, confirmed by legal."
- Rephrased: "The 5-item enrollment cap is a hard business rule confirmed by the Legal team and is not configurable at runtime."

---

## Phase 6: Apply Edits

After the interview loop ends (all items processed, or "done" was typed):

1. Apply remaining `PENDING_EDITS` (items added after the most recent save checkpoint) to `INTERVIEW_DOC`.

   **Edit application rules:**
   - For general open markers: replace the marker token (❓ / NEEDS / VERIFY / DEFERRED) and any adjacent placeholder text with the rephrased answer text.
   - For callout items: replace the entire callout expression — including the `@username` tag and any `<!-- -->` wrapper — with the rephrased answer text. The callout is considered resolved and removed.
   - Preserve surrounding formatting: if the item is in a table cell, the rephrased text replaces only the cell content. If it is inline in a paragraph, replace only the marker span.
   - Do not alter any part of the document outside the resolved items.

2. Write the updated content back to `[file]`.

3. Confirm: "Document saved: `{file}`."

---

## Phase 7: Session Summary

Display a summary after all edits are applied.

```
Interview complete.
──────────────────────────────────────────────────────────────
File:     {file}
Resolved: {n} items
Deferred: {n} items — listed below
Skipped:  {n} items — markers remain unchanged
──────────────────────────────────────────────────────────────

Resolved items:
  CALL-001  [{Section}] — applied
  GEN-003   [{Section}] — applied

Deferred items:
  GEN-002   [{Section}] — DEFERRED: {rationale} — Owner: {owner}

Skipped items (markers remain):
  GEN-005   [{Section}] — skipped by user

──────────────────────────────────────────────────────────────
Remaining open items in document: {count}
  (Run /akr-interview {file} again to address remaining items,
   or run /akr-docs score {ModuleName} when the document is complete.)
```

If `OTHER_CALLOUTS` is non-empty and the session is ending, remind the user:

```
Note: {count} callout(s) in this document are addressed to other team members.
  {CALL-XXX} — @{username}: {brief context}
These remain unresolved. Notify the tagged colleague to run:
  /akr-interview {file} --as @{username}
```

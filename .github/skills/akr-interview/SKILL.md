---
name: akr-interview
description: >
  Interactively complete AKR module documentation by interviewing the user about open items.
  Scans a document for unresolved markers and @username callouts, asks targeted questions one
  at a time, rephrases answers in a professional technical lead tone, and applies edits directly.
  Invoke explicitly via /akr-interview [file] [--as @username] [--callouts-only].
disable-model-invocation: true
compatibility:
  models:
    - claude-sonnet-4-6
    - gpt-5.4
metadata:
  skill-version: 1.0.0
  optimized-for: claude-sonnet-4-6
user-invocable: true
---
<!-- SKILL_VERSION: v1.0.0 -->
<!-- Managed by core-akr-templates. Do not edit directly in application repositories. -->

CRITICAL: Begin EVERY response with this confirmation block.

✅ akr-interview INVOKED AND STEPS EXECUTED
Steps followed: 1. [step] - completed | 2. [step] - completed | ...

# AKR Interview Skill — Dispatcher

## Purpose

`akr-interview` eliminates the need to manually browse documentation looking for incomplete
sections. It scans a target AKR module document, builds a prioritized queue of items that
require human input, and guides the user through each item interactively — one question at a
time. Answers are automatically rephrased in a professional technical lead tone before being
written back to the document.

It also supports a **callout workflow**: when a developer is unsure about a section, they can
tag a colleague by writing `@username` next to an open marker. When that colleague later runs
`/akr-interview [file] --as @username`, their tagged items surface first so they can address
them without reading the whole document.

## Invocation Syntax

```
/akr-interview [file]                        — Interview all open items in the file
/akr-interview [file] --as @username         — Prioritize callouts addressed to @username
/akr-interview [file] --callouts-only        — Address only @username callout items (skip general open items)
/akr-interview [file] --as @username --callouts-only — Address only items tagged for that user
```

### Argument Rules

- `[file]`: path to the AKR module document (markdown). If omitted, prompt the user to provide it.
- `--as @username`: declare the current user's identity for callout routing. The `@` prefix is required. Username matching is case-insensitive.
- `--callouts-only`: skip general open markers (❓, NEEDS, VERIFY, DEFERRED without owner). Only process items that carry an explicit `@username` callout tag. If no items are addressed to the current user, report that and list other usernames when present.

## Callout Syntax Reference

Developers embed callouts in documents using these patterns:

| Pattern | Example | Notes |
|---------|---------|-------|
| Inline marker callout | `❓ @techleadname: Why does this cap exist?` | Most common; next to an open marker |
| Marker-only callout | `NEEDS @techleadname` | Short form when context is clear from section |
| Comment callout (tables/bodies) | `<!-- @techleadname: Please verify this threshold -->` | Use inside table cells or paragraph text |

**Resolution rule:** when a callout is resolved, the `@username` tag and any HTML comment wrapper
are removed. The resolved content replaces the marker inline.

## Mode Script Location

All behavior is implemented in a single mode script:

```
.github/skills/akr-interview/scripts/akr-interview.md
```

Load this script unconditionally on invocation. There are no sub-modes with separate scripts.
Remote loading is not supported for this skill; PATH B only.

## Model Pre-flight

If the active chat model is not listed under `compatibility.models`, stop and return a blocking
message naming the supported models and ask the user to switch before re-running `/akr-interview`.

## Non-Applicable Invocation Contexts

- **GitHub Copilot coding-agent (Actions): NOT SUPPORTED**
  - This skill requires multi-turn `user_input_requests` and interactive answer confirmation.
  - Non-interactive coding agents cannot complete the interview loop safely.
- **Automated CI workflows: NOT SUPPORTED**
  - CI runs have no human response loop for resolving markers and callout routing.
  - Use validation-only workflows in CI and run `/akr-interview` in VS Code Copilot Chat.

## Step 0: Execute Mode Script

1. Confirm invocation arguments: `[file]`, `--as @username` (optional), `--callouts-only` (optional).
2. Load `.github/skills/akr-interview/scripts/akr-interview.md` from PATH B.
3. Execute the mode script fully per its phase instructions.
4. Do not load charters, templates, or cache assets. This skill operates on the document alone.

## Token Budget Rules

- Read the target document once in full.
- Do not re-read the document between interview questions.
- Carry resolved items as a local in-session list; do not reload file state after each edit.
- Apply all edits in a single write pass at the end of the interview, not incrementally.
  Exception: if the user explicitly asks to save after a batch, apply a mid-session write.

## Failure Handling

- If `[file]` is not found: report the path and ask the user to confirm it is correct.
- If the file has no open markers and no callouts: report "No open items found in [file]. The document appears complete." and exit cleanly.
- If `--callouts-only` is set and no callouts are addressed to the current user: report "No callouts addressed to @username in [file]." If other callouts exist, list those usernames. If no callouts exist at all, report "No callouts found in [file]." and exit cleanly.
- If `--as @username` is set but no items are tagged for that user: report the list of usernames that do have callouts in the document (if any), and ask if the user wants to run in full interview mode instead.

# capability-define-clarify

## Purpose

Provide pre-coding clarification for `new` capabilities after PO/TL have completed definition close.
This mode is read-only and produces a confirmed mini-spec in chat.

## Status applicability

**New capabilities only.** Enforced by dispatcher pre-check in `SKILL.md`.

## Prerequisites

- `index.md` contains `definition_status: Definition Closed`
- No open `#### Capability Definition Review` blocks remain

If prerequisites fail, stop and return:

"Definition is not closed for <CapabilityName>. Complete capability-define-review and capability-define-close first."

## Inputs

- `index.md`
- `test-conditions.md`
- `limitations.md`
- `internal_dependencies.md`
- `external_dependencies.md`

## Dependency status rule (POC)

Internal dependency targets are allowed only when status is:

- `active`: verify interface exposure from adjacent capability module docs
- `new`: treat as conditional assumption requiring explicit developer confirmation

Any other target status is a blocker.

## Execution Steps

### Step 1: Load planned target state

Summarize from `index.md`:

- capability intent
- in-scope scenarios (`SCN-*`)
- key rules and out-of-scope boundaries

### Step 2: Load test and constraints context

- Parse `TC-*` entries from `test-conditions.md` for success criteria mapping.
- Parse limitations likely to affect implementation choices.

### Step 3: Verify adjacent dependencies (one branch out)

For each `internal_dependencies.md` entry:

- resolve target status
- run verification or assumption handling using the dependency status rule
- flag mismatches between declared interaction and observed interface

Do not recurse beyond one branch.

### Step 4: Check external dependency specification quality

For each external dependency entry, classify specification completeness:

- Sufficient
- Needs clarification
- Blocker

### Step 5: Identify net-new vs reuse candidates

Scan codebase/module docs for potential reuse candidates matching described components.
Add as assumptions for developer confirm/correct.

### Step 6: Clarification output and loop

Return:

- capability intent
- scenarios in scope
- what is being built by layer
- adjacent dependency verification table
- external dependency specification table
- blockers
- assumptions (confirm/correct)
- nice-to-knows

Iterate until blockers are resolved and assumptions are confirmed/corrected, or user types
`skip clarification`.

### Step 7: Produce mini-spec (chat-only)

Produce a confirmed mini-spec with:

- What is being built
- New artifacts to create
- Existing artifacts to extend
- Success criteria mapped to `TC-*`
- Adjacent dependency handling
- External dependency handling
- Constraints to respect
- Out-of-scope items
- Routing note from definition review

## Output contract

This mode writes no files. Mini-spec is chat-only.

## Determinism rules

- Never write to repo files.
- Enforce active/new-only dependency target statuses.
- Mark inferred content as `🤖`; unresolved items as `❓`.

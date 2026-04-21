# enhancement-clarify

## Purpose

Bridge the gap between a closed `enhancements.md` and the actual coding session. This mode
gives the developer and the Copilot coding agent a complete, grounded picture of:

- The current business capability and its baseline behavior
- The related code components across all relevant layers (UI, API, Database)
- The full enhancement scope as approved in `enhancements.md`
- Dependency impact across directly listed internal and external dependencies

From that complete picture, the skill runs a structured clarification loop with the developer,
surfaces blockers and assumptions, and produces a confirmed mini-spec the developer uses to
brief the Copilot coding agent.

**This mode does not write to any file.** The mini-spec is a chat artifact only.

## Status applicability

**Active capabilities only.** Enforced by dispatcher pre-check in `SKILL.md`.

## Prerequisite

`enhancements.md` must have status `Review Closed` in the Enhancement Activity table for every
ENH-xxx entry. If any entry contains an open `<!-- akr-capability: review-in-progress -->` marker,
stop immediately and return:

"Enhancement review is still open for <CapabilityName>. Run
`/akr-capability enhancement-review` → `/akr-capability enhancement-review-close` before running
enhancement-clarify."

If no ENH-xxx entries with status `Review Closed` are found, stop and return:

"No closed enhancements found for <CapabilityName>. Complete the enhancement-review →
enhancement-review-close cycle first."

## Workspace requirement

This mode reads from two repositories. The developer must have both open in a single multi-root
VS Code workspace:

| Repository | Role |
|---|---|
| Consolidation repo | Source of requirements truth — capability docs folder |
| Application codebase repo | Source of code truth — module docs and source files |

If consolidation repo files are not accessible, surface a blocker for every file that cannot
be read. Continue building what context is available from the application codebase repo.

## Inputs

All paths below are relative to the capability folder in the consolidation repo unless
otherwise noted.

| File | Role in this mode |
|---|---|
| `index.md` | Primary source of current business behavior, `SCN-*` scenario IDs, and rules baseline |
| `enhancements.md` | Source of closed Business Requirements and Technical Requirements per ENH-xxx |
| `limitations.md` | Known constraints that affect implementation scope or test design |
| `internal_dependencies.md` | In-application capability impacts — checked one branch out only |
| `external_dependencies.md` | Cross-application integration impacts — checked one branch out only |
| `enhancement-test-conditions.md` | BTC-/TTC-/RTC- test conditions already derived; used to validate scope coverage |
| Source-repo module docs | Files tagged `businessCapability: [CapabilityName]` in the application codebase repo — typically under `docs/services/`, `docs/modules/`, or equivalent |

## Dependency scope rule

**One branch out only.** When reading `internal_dependencies.md` and `external_dependencies.md`,
check only the dependencies directly listed for the target capability. Do not recurse into the
dependency documents of those dependencies. This rule keeps the context window bounded and the
analysis focused on the immediate impact surface of the planned change.

## Execution Steps

### Step 1 — Load business context

Read `index.md` for the target capability. Extract:

- Capability description and business purpose
- All `SCN-*` scenario IDs and their descriptions
- All stated business rules, constraints, and conditional logic
- Status (must be `active`)
- Known limitations from `limitations.md` that constrain current behavior

Build an internal summary of the **current capability baseline** before proceeding. Do not
output this summary to the developer yet; it feeds Steps 5–6.

### Step 2 — Build code component map

Locate and read all source-repository module docs tagged `businessCapability: [CapabilityName]`.
Accepted tag formats:

```yaml
businessCapability: [CapabilityName]
```
or front matter equivalent used in the application repo.

For each module doc found, extract:

- Layer (`UI`, `API`, `Database`)
- Module or service name
- Key functions, endpoints, data entities, or UI components described
- Documented constraints, data contracts, or integration points

Build an internal **code component map** organized by layer. If no module docs are found or
accessible, record this as a blocker to surface in Step 6:
"Code component map incomplete — no module docs tagged `businessCapability: [CapabilityName]`
were found. Developer must confirm affected files manually."

### Step 3 — Load enhancement context

Read `enhancements.md`. For each ENH-xxx entry with status `Review Closed`:

- Extract Business Requirements: outcomes, acceptance criteria, out-of-scope statements, edge
  cases, affected roles
- Extract Technical Requirements: specific artifacts listed in Implementation Scope, dependencies
  declared by the TL, known limitations
- Note the Azure Boards work item link if present (traceability reference only)
- Note the routing recommendation from the closed review (Copilot-ready / Copilot-assisted /
  Human required)

Cross-reference enhancement scope against the `SCN-*` scenario baseline in `index.md` to
identify which existing scenarios are affected or extended by each enhancement.

Cross-reference against `enhancement-test-conditions.md` to confirm test condition coverage
matches declared scope. Note gaps between declared scope and derived test conditions as
assumptions to surface in Step 6.

### Step 4 — Dependency impact check

**Internal dependencies** — read `internal_dependencies.md`:

For each listed internal dependency, assess whether the planned enhancement changes could
propagate to that dependent capability. Flag as:

- **Impact Possible** — the enhancement directly changes a data entity or behavior the
  dependency consumes
- **Impact Unlikely** — the enhancement is contained within the target capability's own scope
- **Needs Confirmation** — insufficient information to determine; developer must verify

**External dependencies** — read `external_dependencies.md`:

For each listed external integration, assess whether the planned enhancement changes the data
contract, authentication flow, or integration behavior. Apply the same three-level flag.

Do not read the dependency documents of the listed dependencies themselves.

### Step 5 — Ambiguity and gap analysis

Using the complete picture from Steps 1–4, classify all identified items into three tiers:

**Blockers** — must be resolved before coding can proceed:

- Acceptance criteria in the Business Requirements with no clear implementation path in the
  code component map
- Technical Requirements that reference a specific artifact not found in the code component map
- Enhancement scope touching a dependency flagged as **Impact Possible** with no declared
  handling in the Technical Requirements
- Missing specific file, endpoint, or entity names the Technical Requirements reference but
  do not name precisely
- Routing recommendation of ⚠️ Copilot-assisted or 🚫 Human required that requires the
  developer to explicitly acknowledge supervision expectations before proceeding
- Code component map incomplete due to missing or inaccessible module docs

**Assumptions** — inferences being made that the developer should confirm:

- Mapping of Business Requirements to specific code components identified in Step 2
- Scope interpretation where Technical Requirements are partially specified
- Dependency impact assessments flagged as **Needs Confirmation**
- Test condition coverage gaps between `enhancement-test-conditions.md` and declared scope

**Nice-to-knows** — lower-priority items that do not block coding but improve quality:

- Suggested implementation sequence for multiple ENH-xxx entries in the same session
- Code components in the map that are adjacent to the enhancement but not explicitly in scope
- Edge cases from `index.md` not addressed in the enhancement test conditions

### Step 6 — Clarification output

Present the following in chat. Do not write to any file.

```
## Pre-Coding Clarification: [CapabilityName]

### Business Baseline (from index.md)
[2–4 sentence summary of the current capability and its key rules]

### Enhancement Scope Summary
[One sentence per ENH-*: what it changes and which SCN-* scenarios it affects]

### Code Components in Scope
[Bulleted list by layer: component/module name and what is changing]

### Dependency Impact
| Dependency | Type | Assessment | Rationale |
|---|---|---|---|
| [name] | Internal / External | Impact Possible / Impact Unlikely / Needs Confirmation | [brief reason] |

### Blockers — Must resolve before coding
[Numbered list. If none: "No blockers identified."]

### Assumptions — Please confirm
[Numbered list. Each item ends with "(confirm / correct)"]

### Nice-to-knows
[Numbered list. If none: "None."]
```

### Step 7 — Clarification loop

Wait for the developer to respond.

On each developer response:

1. For each addressed blocker: mark `✅ Resolved` with a one-line summary of the resolution
2. For each confirmed assumption: mark `✅ Confirmed`
3. For each corrected assumption: mark `✏️ Corrected: [correction]`
4. For any new blocker emerging from the developer's responses: add as `🆕 New Blocker`
5. If all blockers are resolved and all assumptions are confirmed or corrected: proceed to Step 8
6. If blockers remain: present only the outstanding items and wait for another response

**`skip clarification` path:** If the developer types `skip clarification`, record all outstanding
blockers and unconfirmed assumptions in the mini-spec under "Out of scope / Unresolved" and
proceed to Step 8. Use this path only when outstanding items are genuinely non-blocking for the
current session.

### Step 7A — Discovery classification during clarification loop

If during the clarification loop the developer surfaces factual information about the codebase
that was not known at the time the PO or TL wrote the requirements, apply this classification:

| Discovery Level | Definition | Mini-spec handling | Post-clarification action |
|---|---|---|---|
| **Informational** | Confirms or clarifies existing scope without changing it (e.g., the actual file path differs slightly from the TL's reference) | Capture the corrected detail in the mini-spec directly | None — proceed normally |
| **Additive** | Adds a factual detail previously unknown but within the declared scope (e.g., an undocumented DB index constraint that affects the migration plan) | Record in mini-spec; mark `🔶 Additive — TL amendment required` | TL must amend Technical Requirements in `enhancements.md`; developer adds new test condition(s) to `enhancement-test-conditions.md` following existing ID sequencing; TL confirms amendment in writing before coding proceeds |
| **Scope-changing** | Changes what the PO accepted or what the TL committed to building (e.g., a missing integration contract, an undocumented authorization rule that alters acceptance criteria) | Record in mini-spec under "Out of scope / Unresolved"; mark `🔴 Scope-changing — suspend required` | Coding is suspended; TL/PO update requirements; ENH-xxx status is reset to `Under Review`; team re-runs `enhancement-review` → `enhancement-review-close` → `enhancement-test-generation`; developer re-runs `enhancement-clarify` after re-close |

**Classification decision guide:**

```
Does the discovery change any acceptance criterion the PO wrote?
  Yes → Scope-changing → Suspend

Does the discovery void or contradict the specific implementation path the TL declared?
  Yes → Scope-changing → Suspend

Does the discovery add a previously unknown technical constraint the TL's plan must account for
but does not change the overall approach or the PO's acceptance criteria?
  Yes → Additive → TL amends before coding proceeds

Does the discovery simply clarify a naming, path, or minor detail that does not change what is
being built?
  Yes → Informational → Capture in mini-spec only
```

When the developer is uncertain between Additive and Scope-changing, classify upward as
Scope-changing. A re-run of the review cycle is faster than discovering mid-coding that
the requirements were materially wrong.

**This mode does not reset `enhancements.md` status.** Any status reset for a Scope-changing
discovery is performed manually by the TL or PO before the PO/TL cycle is re-run.

### Step 8 — Mini-spec confirmation

Produce the mini-spec and present it in chat. Do not write to any file.

```
## Mini-Spec: [CapabilityName] — [ENH-* scope]

**What is being built:**
[1–3 sentences describing the agreed implementation, incorporating all ✅ Confirmed and
✏️ Corrected items from the clarification loop]

**Files / components to change:**
[Bulleted list of specific artifacts from Technical Requirements, corrected by any developer
clarifications. Organized by layer (UI / API / Database) where relevant.]

**Success criteria:**
[Numbered list. Each item is a verifiable condition. Map to BTC-/TTC-/RTC- IDs
from enhancement-test-conditions.md where available.]

**Dependency handling:**
[One line per flagged dependency: what was agreed, or "No change required"]

**Additive discoveries (pending TL amendment):**
[Bulleted list of 🔶 items, or "None". Coding should not begin until each item is confirmed
amended in enhancements.md by the TL.]

**Out of scope for this session:**
[Items explicitly excluded, including any 🔴 scope-changing discoveries not yet resolved,
and any unresolved blockers if skip clarification was used]

**Routing note:**
[Repeat the routing recommendation from enhancements.md exactly. If ⚠️ Copilot-assisted:
state "Developer must remain present during the coding session to review agent decisions."
If 🚫 Human developer required: state "Copilot coding agent must not proceed without active
developer supervision at every step."]
```

Present the mini-spec and ask:

> **Does this mini-spec correctly represent what we are building?
> Type "confirmed" to proceed to coding, or state any corrections.**

On `confirmed`: the skill completes. The developer attaches `enhancements.md` and the
mini-spec as context for the Copilot coding session.

On correction: update the mini-spec with the stated change and present again. Repeat until
`confirmed`.

**If any 🔶 Additive discoveries are listed:** before accepting `confirmed`, remind the developer:

> "The mini-spec includes one or more Additive discoveries pending TL amendment in
> `enhancements.md`. Confirm the TL has completed those amendments before starting the coding
> session."

**If any 🔴 Scope-changing discoveries are listed:** do not accept `confirmed`. State:

> "The mini-spec contains a Scope-changing discovery. Coding must not proceed. Work with the
> PO and TL to update `enhancements.md`, reset the ENH-xxx status to Under Review, and
> re-run the enhancement-review cycle."

## Output contract

This mode **does not write to any file**. It is a read-and-clarify mode only. All output is
in-chat. The mini-spec is a chat artifact. No capability artifact — in either the consolidation
repo or the application codebase repo — is modified by this mode.

## Determinism rules

- Never invent file names, endpoint names, or entity names not present in the input files or
  confirmed by the developer during the clarification loop.
- Mark inferred content with `🤖` so the developer can confirm or reject it.
- Never proceed past Step 7 if blockers remain and the developer has not typed `skip clarification`.
- Do not classify a discovery without presenting it to the developer first. Classification is
  a tri-party determination (PO + TL + Developer); the skill proposes a classification, but
  the team confirms it.
- Respect the one-branch-out dependency rule strictly. Do not read dependency documents of
  listed dependencies.

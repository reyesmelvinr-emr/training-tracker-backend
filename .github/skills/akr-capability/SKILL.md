---
name: akr-capability
description: >
  Iteratively assess business and technical requirements declared in an active capability's enhancements.md,
  identify gaps and missing details, propose implementation guidance, and produce a routing
  recommendation (Copilot coding agent ready / Copilot-assisted / human developer required).
  Supports multiple review iterations as PO/TL resolve gaps. When complete, confirm and clean
  the file with the review-close mode, then generate tiered test conditions with enhancement-test-generation.
  For new capabilities, supports capability-define review/close/clarify workflow on canonical
  new capability artifacts before coding begins.
  After PO/TL handoff, developer runs enhancement-clarify from the application codebase repo to map
  approved requirements to code, resolve blockers, and produce a confirmed mini-spec before coding begins.
  Invoke via /akr-capability [enhancement-review | enhancement-review-close | enhancement-test-generation | enhancement-clarify | capability-define-review | capability-define-close | capability-define-clarify] [CapabilityName].
disable-model-invocation: true
compatibility:
  models:
    - claude-sonnet-4-6
    - gpt-5.4
metadata:
  skill-version: 1.1.0
  optimized-for: claude-sonnet-4-6
user-invocable: true
---
<!-- SKILL_VERSION: v1.1.0 -->
<!-- Managed by core-akr-templates. Do not edit directly in consolidation or application repositories. -->

CRITICAL: Begin EVERY response with this confirmation block.

✅ akr-capability INVOKED AND STEPS EXECUTED
Steps followed: 1. [step] - completed | 2. [step] - completed | ...

# AKR Capability Skill - Dispatcher

## Invocation Routing

Load only the mode script required by the command.

| Command | Mode Script | Primary Use | Invocation Repo |
|---|---|---|---|
| `/akr-capability enhancement-review [CapabilityName]` | `.github/skills/akr-capability/scripts/enhancement-review.md` | Iteratively assess requirements quality, surface gaps, propose implementation guidance, and recommend coding routing | Consolidation repo |
| `/akr-capability enhancement-review-close [CapabilityName]` | `.github/skills/akr-capability/scripts/enhancement-review-close.md` | Confirm review is complete, then strip all review blocks for a clean enhancements.md ready for coding handoff | Consolidation repo |
| `/akr-capability enhancement-test-generation [CapabilityName]` | `.github/skills/akr-capability/scripts/enhancement-test-generation.md` | Generate tiered Business and Technical test conditions from closed enhancements into enhancement-test-conditions.md | Consolidation repo |
| `/akr-capability enhancement-clarify [CapabilityName]` | `.github/skills/akr-capability/scripts/enhancement-clarify.md` | Pre-coding developer gate: map closed requirements to code components, surface blockers and assumptions, produce confirmed mini-spec for Copilot coding session | Application codebase repo (multi-root workspace) |
| `/akr-capability capability-define-review [CapabilityName]` | `.github/skills/akr-capability/scripts/capability-define-review.md` | Iteratively assess PO/TL-authored new-capability artifacts and write capability definition review outcomes into index.md | Consolidation repo |
| `/akr-capability capability-define-close [CapabilityName]` | `.github/skills/akr-capability/scripts/capability-define-close.md` | Validate close-readiness and mark new capability definition as Definition Closed | Consolidation repo |
| `/akr-capability capability-define-clarify [CapabilityName]` | `.github/skills/akr-capability/scripts/capability-define-clarify.md` | Developer pre-coding clarification for new capabilities (read-only, mini-spec in chat) | Application codebase repo (multi-root workspace) |

Dispatcher pre-checks:

- Determine capability status from `docs/business-capabilities/{active|new|archived}/<CapabilityName>/`.
- For `enhancement-*` modes: if status is not `active`, stop and return:
  "Enhancement workflow is only available for active capabilities. <CapabilityName> is currently <status>."
- For `capability-define-*` modes: if status is not `new`, stop and return:
  "Capability-define workflow is only available for new capabilities. <CapabilityName> is currently <status>."
- For `enhancement-*` modes: if `enhancements.md` is absent in the capability folder, stop and return:
  "No enhancements.md found for <CapabilityName>. Enhancement workflow requires an existing enhancements file."
- For `capability-define-review` only: if `index.md` is absent in the `new` capability folder, stop and return:
  "No index.md found for <CapabilityName>. capability-define-review requires index.md as the primary artifact."
- Additional pre-check for `enhancement-review-close` only: if no `<!-- akr-capability: review-in-progress -->` marker is found in `enhancements.md`, stop and return:
  "No active review blocks found for <CapabilityName>. Run `/akr-capability enhancement-review [CapabilityName]` before closing."
- Additional pre-check for `enhancement-test-generation` only: if any `<!-- akr-capability: review-in-progress -->` marker is still present in `enhancements.md`, stop and return:
  "Enhancement review is still in progress for <CapabilityName>. Run `/akr-capability enhancement-review-close [CapabilityName]` before generating tests."
- Additional pre-check for `enhancement-clarify` only: if any `<!-- akr-capability: review-in-progress -->` marker is found in `enhancements.md`, or if no ENH-xxx entry has status `Review Closed` in the Enhancement Activity table, stop and return:
  "enhancement-clarify requires all enhancements to have status Review Closed. Run `/akr-capability enhancement-review` → `/akr-capability enhancement-review-close` before running enhancement-clarify."
- Additional pre-check for `enhancement-clarify` only: confirm that `enhancement-test-conditions.md` exists in the capability folder. If absent, stop and return:
  "enhancement-test-conditions.md not found for <CapabilityName>. Run `/akr-capability enhancement-test-generation [CapabilityName]` before running enhancement-clarify."
- Additional pre-check for `capability-define-close` only: if no `#### Capability Definition Review` block is found in `index.md`, stop and return:
  "No capability definition review blocks found for <CapabilityName>. Run `/akr-capability capability-define-review [CapabilityName]` before closing."
- Additional pre-check for `capability-define-clarify` only: if `index.md` does not contain `definition_status: Definition Closed`, or still contains open `#### Capability Definition Review` blocks, stop and return:
  "capability-define-clarify requires Definition Closed with no open review blocks. Run `/akr-capability capability-define-review` → `/akr-capability capability-define-close` first."

## Output Contract

`enhancement-review` and `enhancement-review-close` modify only `enhancements.md`.
`enhancement-test-generation` modifies only `enhancement-test-conditions.md` (creating it if absent).
`enhancement-clarify` **does not modify any file**. All output is in-chat only. The mini-spec produced is a chat artifact and is never written to a repository file.

## Required Metadata and Governance

All generated or updated capability files must include front matter with:

- `businessCapability` (approved value from registry)
- `feature` (format: `FN#####_US#####`)
- `layer` (for consolidation outputs use `Business` when applicable)
- `project_type` (for consolidation outputs use `business-consolidation`)
- `status` (`draft` or `approved`)
- `compliance_mode` (`pilot` or `production`)

Registry source of truth:

- `core-akr-templates/.akr/tags/tag-registry.json`

Mode scripts must reference this section as the authoritative metadata contract.

## Output Contract

This skill does not create new capability files. It produces:

- An in-chat readiness assessment report per enhancement entry.
- A structured list of gaps, suggested clarifications, and implementation recommendations.
- A per-enhancement routing decision written back into `enhancements.md` as a Review Outcome block.

The only file modified by the current mode script is the capability's existing `enhancements.md`.

Output path (active capabilities only):

- `docs/business-capabilities/active/<CapabilityName>/enhancements.md`

## Determinism Rules

- Never invent capability names not present in registry.
- Never assume missing requirements; raise them as explicit `❓` gaps for PO or TL to resolve.
- Mark inferred or suggested content with `🤖` so the reviewer can confirm or reject it.
- Do not modify any file other than `enhancements.md` (PO/TL modes) or `enhancement-test-conditions.md` (test-generation mode).
- `enhancement-clarify` must never write to any file under any circumstance.
- Do not block the PO or TL from proceeding; produce a recommendation and let the human decide.
- In `enhancement-clarify`, propose discovery classifications but do not apply them unilaterally. Classification is a tri-party determination; the skill labels and the team confirms.

## Consolidation Mode

Source access follows the same mode contract as `akr-business-consolidation`. Determine source location using that skill's **Consolidation Mode** section in `.github/skills/akr-business-consolidation/SKILL.md`.

Mode scripts must reference that section rather than duplicating the table here.

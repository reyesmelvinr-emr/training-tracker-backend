# SKILL-COMPAT Matrix

Skill: akr-capability
Version: v1.1.0
Last updated: 2026-04-18

## Model Compatibility Matrix

### PO/TL modes (enhancement-review, enhancement-review-close, enhancement-test-generation, capability-define-review, capability-define-close)
| Model | Pass Rate | Known Issues | Workaround |
|---|---|---|---|
| claude-sonnet-4-6 | TBD | TBD | Use explicit mode invocation; review all 🤖 suggestions before accepting routing decision |
| gpt-5.4 | TBD | TBD | Validate routing decision manually against scoring rubric in mode script |

### Developer modes (enhancement-clarify, capability-define-clarify)
| Model | Pass Rate | Known Issues | Workaround |
|---|---|---|---|
| claude-sonnet-4-6 | TBD | TBD | Confirm code component map completeness manually if module docs are not tagged; verify discovery classification before accepting |
| gpt-5.4 | TBD | TBD | Validate one-branch-out dependency rule is respected; confirm mini-spec does not include content beyond declared scope |

## Invocation Surface Matrix

### PO/TL modes
| Surface | Supported | Notes |
|---|---|---|
| coding-agent | Yes | Preferred; produces structured review output written back into enhancements.md |
| custom-agent | Yes | Use explicit mode naming |

### Developer modes (enhancement-clarify, capability-define-clarify)
| Surface | Supported | Notes |
|---|---|---|
| coding-agent | Yes | Preferred; run from application codebase repo in a multi-root workspace that also has the consolidation repo open |
| custom-agent | Yes | Use explicit mode naming; ensure both repos are in workspace context before invocation |

## Workspace Requirements (clarify modes)

| Requirement | Detail |
|---|---|
| Multi-root VS Code workspace | Both consolidation repo and application codebase repo must be open as separate folders in a single workspace |
| Module doc tagging | Source-repo module docs must carry `businessCapability: [CapabilityName]` front matter for the code component map to populate automatically |
| Read access to consolidation repo | Developer must have read access to the capability folder in the consolidation repo; clarify modes do not write to it |

## Governance Notes

- PO/TL modes are read-then-write: they read enhancements.md (or enhancement-test-conditions.md) and write only structured output blocks back to that file.
- PO and TL must confirm all gaps and routing decisions before a coding agent is invoked.
- In pilot mode, routing recommendations are advisory; human override is always permitted.
- Do not invoke a coding agent for any enhancement whose routing decision is 🚫 Human Developer Required without active developer supervision.
- `enhancement-clarify` is strictly read-only: it never writes to any file in either repository. Any back-documentation from a discovery found during clarification is performed manually by the TL or PO, not by the skill.
- `capability-define-clarify` is strictly read-only: it never writes to any file in either repository. Mini-spec remains a chat artifact.
- Discovery classification (Informational / Additive / Scope-changing) is proposed by the skill but confirmed by the tri-party team (PO + TL + Developer). The skill must not apply a classification unilaterally.
- A Scope-changing discovery suspends coding. The ENH-xxx status must be manually reset to `Under Review` before the PO/TL review cycle can be re-run.
- An Additive discovery allows coding to proceed only after the TL has confirmed the amendment to enhancements.md in writing (chat acknowledgment is sufficient).

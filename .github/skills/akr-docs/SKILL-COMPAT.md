# SKILL-COMPAT Matrix

Skill: akr-docs
Version: v1.0.0
Last updated: 2026-03-17

## Model Compatibility Matrix
| Model | Pass Rate | Known Issues | Workaround |
|---|---|---|---|
| claude-sonnet-4-6 | TBD | TBD | Use explicit /akr-docs mode command and validate metadata header |
| gpt-4o | TBD | Potential Mode B truncation on large modules | Prefer SSG with split pass 2A/2B and strict validator checks |
| gpt-4o | TBD | @github tool re-call tendency at high pass depths: model may re-request charter or source files after Pass 2 in violation of forward payload discipline | Explicit PROHIBITION block in SKILL.md Mode B Step 2; monitor premium request counts post-run for unexpected spikes |
| gpt-4o | TBD | @github tool call output truncation: file content returned by @github may be silently truncated when model context window is near capacity during late SSG passes | Monitor charter completeness in Pass 1 forward payload; if charter appears partial, restart run in PATH B mode |

## Invocation Surface Matrix
| Surface | Supported | Notes |
|---|---|---|
| coding-agent | Yes | Use issue template with explicit Mode B instructions and metadata header checks |
| custom-agent | Yes | Ensure explicit mode selection and validator invocation |
| code-skills (run_skill_script) | Yes | Prefer for deterministic script-backed support tasks |

## @github MCP Tool Call Surface Availability
| Surface | @github Available | Notes |
|---|---|---|
| VS Code Copilot Chat | Yes (confirmed 2026-03-23) | GitHub MCP extension installed and authenticated; supports on-demand file fetch via `@github` tool calls |
| Visual Studio Copilot Chat | TBD — Deliverable 5 | Parity vs VS Code not yet confirmed; determine in Phase 2 Deliverable 5 |
| GitHub Copilot coding-agent (Actions) | Not applicable | Actions runs do not use VS Code `@github` MCP extension; charter access via local file path only |

## Known Gap Tracking
| Gap ID | Description | Impact | Mitigation | Status |
|---|---|---|---|---|
| KG-001 | Hook log availability can vary by execution surface (including Copilot sessions) | Criterion 10 evidence collection may be incomplete | If hooks are unavailable, run `python .akr/scripts/validate_documentation.py --changed-files "<space-separated file list>" --fail-on needs` manually before PR (or `--all docs/modules` when no file list is available) | Mitigated |

## Charter Version Compatibility
Map condensed charter versions to the SKILL.md version they were validated against. Update this table in the same commit as any material charter change. The version header in each instructions file is machine-readable; bump it here when a validator or distribution check will actually consume the version number.

| Charter File | Charter Version | Compatible SKILL Version | Last Validated | Notes |
|---|---|---|---|---|
| backend-service.instructions.md | 1.0 | v1.0.0 | 2026-03-31 | Grounding Rules, Readability Floor, Unknowns Discipline added. Bump charter version when SKILL-COMPAT validator reads this table. |
| ui-component.instructions.md | 1.0 | v1.0.0 | ❓ | Existence confirmed; compatibility not yet validated. |
| database.instructions.md | 1.0 | v1.0.0 | ❓ | Existence not confirmed for this charter version; validate in D11-A. |

## Re-Evaluation Policy
- Re-run evals after any SKILL.md change.
- Re-run evals after any major Copilot model update.
- Update this file and benchmark data in the same change set.
- Update Charter Version Compatibility table in the same commit as any material condensed charter change.

## Future Enhancement Paths
| Enhancement | Description | Trigger Condition | Estimated Effort |
|---|---|---|---|
| Dynamic resource-based skill hydration | Replace static condensed charters and benchmark data with runtime resources served by a custom skills provider using @skill.resource patterns | Charter staleness or benchmark drift observed during pilot or multi-repo runs | Medium |

## HITL Role Mapping (Seed)
| Workflow Stage | Human Role | Expected Action |
|---|---|---|
| Mode A grouping review | Tech lead / module owner | Approve or request regrouping edits before modules.yaml updates are finalized |
| Mode B draft preview | Module owner / reviewer | Review committed draft artifact and confirm readiness for final doc_output write |
| Mode C unresolved markers | Product owner / domain SME | Resolve critical ❓ items or explicitly approve DEFERRED rationale |
| Final PR merge | Code owner | Confirm validator results and merge when governance checks pass |

## Governance Stability Assessment Seed (Phase 2.6)
| Assessment Date | Surface | Observation | Determinism Risk | Proposed Migration | Owner | Status |
|---|---|---|---|---|---|---|
| TBD | SKILL Mode A | TBD | Low/Medium/High | Keep in SKILL or migrate to script | TBD | Planned |
| TBD | SKILL Mode B | TBD | Low/Medium/High | Keep in SKILL or migrate to script | TBD | Planned |
| TBD | SKILL Mode C | TBD | Low/Medium/High | Keep in SKILL or migrate to script | TBD | Planned |

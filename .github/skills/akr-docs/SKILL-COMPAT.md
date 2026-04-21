# SKILL-COMPAT Matrix

Skill: akr-docs
Version: v1.0.0
Last updated: 2026-03-31

## Model Compatibility Matrix
| Model | Pass Rate | Known Issues | Workaround |
|---|---|---|---|
| claude-sonnet-4-6 | TBD | TBD | Use explicit /akr-docs command and validate metadata header |
| gpt-4o | TBD | Potential GenerateDocumentation truncation on large modules | Prefer SSG with split pass 2A/2B and strict validator checks |
| gpt-4o | TBD | @github tool re-call tendency at high pass depths: model may re-request charter or source files after Pass 2 in violation of forward payload discipline | Explicit PROHIBITION block in SKILL.md GenerateDocumentation Step 2; monitor premium request counts post-run for unexpected spikes |
| gpt-4o | TBD | @github tool call output truncation: file content returned by @github may be silently truncated when model context window is near capacity during late SSG passes | Monitor charter completeness in Pass 1 forward payload; if charter appears partial, restart run in PATH B mode |

## Invocation Surface Matrix
| Surface | Supported | Notes |
|---|---|---|
| coding-agent | Yes | Use issue template with explicit GenerateDocumentation instructions and metadata header checks |
| custom-agent | Yes | Ensure explicit skill invocation and validator invocation |
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
| ui-component.instructions.md | 1.0 | v1.0.0 | 2026-04-12 | Charter presence and compatibility validated against current condensed charter mapping. |
| database.instructions.md | 1.0 | v1.0.0 | 2026-04-12 | Charter presence and compatibility validated; follow-up behavior eval remains tracked under D11-A. |

## Re-Evaluation Policy
- Re-run evals after any SKILL.md change.
- Re-run evals after any major Copilot model update.
- Update this file and benchmark data in the same change set.
- Update Charter Version Compatibility table in the same commit as any material condensed charter change.
- **Eval re-run deferred for 2026-03-31 skill command rename**: This change is a rename-only update with no behavior logic change. Existing eval baseline data remains valid. Evals must be re-run at the next behavioral change to SKILL.md.

## Future Enhancement Paths
| Enhancement | Description | Trigger Condition | Estimated Effort |
|---|---|---|---|
| Dynamic resource-based skill hydration | Replace static condensed charters and benchmark data with runtime resources served by a custom skills provider using @skill.resource patterns | Charter staleness or benchmark drift observed during pilot or multi-repo runs | Medium |

---

# SKILL-COMPAT Addendum: akr-interview

Skill: akr-interview
Version: v1.0.0
Last updated: 2026-04-10

## Model Compatibility Matrix
| Model | Pass Rate | Known Issues | Workaround |
|---|---|---|---|
| claude-sonnet-4-6 | TBD | TBD | Use explicit /akr-interview command |
| gpt-5.4 | TBD | TBD | Use explicit /akr-interview command |

## Invocation Surface Matrix
| Surface | Supported | Notes |
|---|---|---|
| VS Code Copilot Chat | Yes | Primary surface; interactive Q&A loop works as expected |
| coding-agent | No | Interactive interview loop is not suitable for non-interactive CI agents |
| custom-agent | Partial | Supported only when the agent surface supports multi-turn user_input_requests |

## Known Gap Tracking
| Gap ID | Description | Impact | Mitigation | Status |
|---|---|---|---|---|
| KIG-001 | @username callout matching is case-insensitive string match only; no GitHub identity resolution | Users must type the exact username used in the document | Document callout convention in onboarding; username is arbitrary label, not GitHub handle | Open |
| KIG-002 | Mid-session "save" applies edits to the live file; if the session crashes after save, the remaining queue is lost | Partial save may leave document in mixed state | Run /akr-interview again on the same file; it will scan and queue only remaining open items | Open |

## HITL Role Mapping
| Workflow Stage | Human Role | Expected Action |
|---|---|---|
| CALL-* items (callouts) | Tech lead / named colleague | Address developer questions by running /akr-interview --as @username |
| GEN-* items (open markers) | Module owner / domain SME | Answer targeted questions to complete document sections |
| Rephrase preview confirmation | Any user | Confirm, edit, skip, or defer proposed rephrased text before document write |

## HITL Role Mapping (Seed)
| Workflow Stage | Human Role | Expected Action |
|---|---|---|
| ProposeGroupings grouping review | Tech lead / module owner | Approve or request regrouping edits before modules.yaml updates are finalized |
| GenerateDocumentation draft preview | Module owner / reviewer | Review committed draft artifact and confirm readiness for final doc_output write |
| ResolveUnknowns unresolved markers | Product owner / domain SME | Resolve critical ❓ items or explicitly approve DEFERRED rationale |
| Final PR merge | Code owner | Confirm validator results and merge when governance checks pass |

## Governance Stability Assessment Seed (Phase 2.6)
| Assessment Date | Surface | Observation | Determinism Risk | Proposed Migration | Owner | Status |
|---|---|---|---|---|---|---|
| TBD | SKILL ProposeGroupings | TBD | Low/Medium/High | Keep in SKILL or migrate to script | TBD | Planned |
| TBD | SKILL GenerateDocumentation | TBD | Low/Medium/High | Keep in SKILL or migrate to script | TBD | Planned |
| TBD | SKILL ResolveUnknowns | TBD | Low/Medium/High | Keep in SKILL or migrate to script | TBD | Planned |

# SKILL-COMPAT Matrix

Skill: akr-docs
Version: v1.0.0
Last updated: 2026-03-17

## Model Compatibility Matrix
| Model | Pass Rate | Known Issues | Workaround |
|---|---|---|---|
| claude-sonnet-4-6 | TBD | TBD | Use explicit /akr-docs mode command and validate metadata header |
| gpt-4o | TBD | Potential Mode B truncation on large modules | Prefer SSG with split pass 2A/2B and strict validator checks |

## Invocation Surface Matrix
| Surface | Supported | Notes |
|---|---|---|
| coding-agent | Yes | Use issue template with explicit Mode B instructions and metadata header checks |
| custom-agent | Yes | Ensure explicit mode selection and validator invocation |
| code-skills (run_skill_script) | Yes | Prefer for deterministic script-backed support tasks |

## Known Gap Tracking
| Gap ID | Description | Impact | Mitigation | Status |
|---|---|---|---|---|
| KG-001 | Hook log availability can vary by execution surface | Criterion 10 evidence collection may be incomplete | Treat as hard gate only when hook support is confirmed in Phase 1 | Open |

## Re-Evaluation Policy
- Re-run evals after any SKILL.md change.
- Re-run evals after any major Copilot model update.
- Update this file and benchmark data in the same change set.

## Future Enhancement Paths
| Enhancement | Description | Trigger Condition | Estimated Effort |
|---|---|---|---|
| Dynamic resource-based skill hydration | Replace static condensed charters and benchmark data with runtime resources served by a custom skills provider using @skill.resource patterns | Charter staleness or benchmark drift observed during pilot or multi-repo runs | Medium |

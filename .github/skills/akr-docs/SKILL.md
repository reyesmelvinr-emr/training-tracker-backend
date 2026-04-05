---
name: akr-docs
description: >
  Generate AKR module documentation following charters and templates.
  Invoke explicitly via /akr-docs [groupings | generate | resolve] [target] [--use-ssg] [--remote].
disable-model-invocation: true
compatibility:
  models:
    - claude-sonnet-4-6
    - gpt-4o
metadata:
  skill-version: 1.1.0
  optimized-for: claude-sonnet-4-6
user-invocable: true
---
<!-- SKILL_VERSION: v1.1.0 -->
<!-- Managed by core-akr-templates. Do not edit directly in application repositories. -->

CRITICAL: Begin EVERY response with this confirmation block.

✅ akr-docs INVOKED AND STEPS EXECUTED
Steps followed: 1. [step] - completed | 2. [step] - completed | ...

# AKR Documentation Skill — Dispatcher

## Invocation Routing

This skill operates in three modes. Load only the script for the requested mode.

| Command | Mode Script | When to Use |
|---------|-------------|-------------|
| `/akr-docs groupings` | `@github get file core-akr-templates/.github/skills/akr-docs/scripts/akr-groupings.md` | No modules.yaml, or re-grouping needed |
| `/akr-docs generate [ModuleName] [--remote]` | `@github get file core-akr-templates/.github/skills/akr-docs/scripts/akr-generate.md` | modules.yaml approved, generate docs |
| `/akr-docs resolve [file] [--remote]` | `@github get file core-akr-templates/.github/skills/akr-docs/scripts/akr-resolve.md` | Draft has unresolved ❓ markers |

## PATH Selection for @github Calls

- **PATH A (VS Code with GitHub MCP extension):** Use `@github get file` syntax above. Always fetches live remote content. Standard path for all end users.
- **PATH B (offline or no MCP extension):** Load mode scripts from `.github/skills/akr-docs/scripts/` in the current workspace (the distributed copy delivered by `distribute-skill.yml`). Note: templates and charters are not included in the distributed bundle — PATH A is required for all template and charter fetches within mode scripts.
- **PATH C (CI / coding-agent):** The GitHub Actions workflow clones `core-akr-templates` to `~/.akr/templates/` during setup. All assets are available from that path on the runner.
- **`--remote` flag (generate and resolve modes only):** Force PATH A for mode script, template, and charter fetches. Skip the PATH B workspace copy for the mode script. Use when you need to confirm live remote content before the next distribution cycle.

## Token Budget Rules (apply across all modes)

- Load only the mode script for the requested operation. Never load all three.
- Each mode script fetches only the charter slice it needs. Do not pre-load full charters.
- Templates are fetched by reference path. Do not embed template content in context.
- Forward payload between SSG passes must be structured facts only — no raw source re-expansion.
- Maximum 2 `@github` calls per generate/resolve run (1 for mode script, 1 for charter slice).

## Failure Handling

If mode script cannot be fetched via PATH A:
1. Confirm the GitHub MCP extension is installed and authenticated in VS Code.
2. Fall back to PATH B: load mode script from `.github/skills/akr-docs/scripts/` in the current workspace.
3. If PATH B files are absent, re-run the `distribute-skill.yml` workflow to populate the distributed bundle.

If modules.yaml is absent when `generate` is invoked, redirect to `groupings` mode automatically.

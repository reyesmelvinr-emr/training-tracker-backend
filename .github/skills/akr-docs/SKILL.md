---
name: akr-docs
description: >
  Generate AKR module documentation following charters and templates.
  Invoke explicitly via /akr-docs [groupings | generate | resolve] [target] [--use-ssg].
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
| `/akr-docs generate [ModuleName]` | `@github get file core-akr-templates/.github/skills/akr-docs/scripts/akr-generate.md` | modules.yaml approved, generate docs |
| `/akr-docs resolve [file]` | `@github get file core-akr-templates/.github/skills/akr-docs/scripts/akr-resolve.md` | Draft has unresolved ❓ markers |

## PATH Selection for @github Calls

- **PATH A (VS Code with GitHub MCP extension):** Use `@github get file` syntax above.
- **PATH B (Visual Studio or no MCP extension):** Load from `~/.akr/templates/.github/skills/akr-docs/scripts/` using local file read.
- **PATH C (CI / coding-agent):** Scripts are available at `~/.akr/templates/.github/skills/akr-docs/scripts/` after the standard clone step.

## Token Budget Rules (apply across all modes)

- Load only the mode script for the requested operation. Never load all three.
- Each mode script fetches only the charter slice it needs. Do not pre-load full charters.
- Templates are fetched by reference path. Do not embed template content in context.
- Forward payload between SSG passes must be structured facts only — no raw source re-expansion.
- Maximum 2 `@github` calls per run (1 for mode script, 1 for charter slice).

## Failure Handling

If mode script cannot be fetched:
1. Check PATH selection above.
2. Verify that `~/.akr/templates/` cache exists locally.
3. Run: `git clone https://github.com/[org]/core-akr-templates ~/.akr/templates` to populate cache.
4. Fall back to PATH B/C using local file.

If modules.yaml is absent when `generate` is invoked, redirect to `groupings` mode automatically.

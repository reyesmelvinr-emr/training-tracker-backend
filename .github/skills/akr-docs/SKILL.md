---
name: akr-docs
description: >
  Generate AKR module documentation following charters and templates.
  Invoke explicitly via /akr-docs [groupings | generate | resolve] [target] [--use-ssg] [--remote] [--remote-refresh].
disable-model-invocation: true
compatibility:
  models:
    - claude-sonnet-4-6
    - gpt-5.4
metadata:
  skill-version: 1.1.1
  optimized-for: claude-sonnet-4-6
user-invocable: true
---
<!-- SKILL_VERSION: v1.1.1 -->
<!-- Managed by core-akr-templates. Do not edit directly in application repositories. -->

CRITICAL: Begin EVERY response with this confirmation block.

✅ akr-docs INVOKED AND STEPS EXECUTED
Steps followed: 1. [step] - completed | 2. [step] - completed | ...

# AKR Documentation Skill — Dispatcher

## Invocation Routing

This skill operates in three modes. Load only the script for the requested mode.

Default behavior: load the mode script from the bundled workspace copy under `.github/skills/akr-docs/scripts/`. Use live remote mode scripts only when the invocation includes `--remote` or `--remote-refresh`.

Prerequisite reminder: for `--remote` or `--remote-refresh` runs, start the GitHub MCP server (and confirm GitHub extension auth) before invoking `/akr-docs`. If GitHub MCP is not running, remote mode-script, template, and charter fetches will fail.

| Command | Mode Script | When to Use |
|---------|-------------|-------------|
| `/akr-docs groupings` | `.github/skills/akr-docs/scripts/akr-groupings.md` by default; `@github get file core-akr-templates/.github/skills/akr-docs/scripts/akr-groupings.md` only when remote mode-script loading is explicitly forced | No modules.yaml, or re-grouping needed |
| `/akr-docs generate [ModuleName] [--remote] [--remote-refresh]` | `.github/skills/akr-docs/scripts/akr-generate.md` by default; `@github get file core-akr-templates/.github/skills/akr-docs/scripts/akr-generate.md` only when remote mode-script loading is explicitly forced | modules.yaml approved, generate docs |
| `/akr-docs resolve [file] [--remote] [--remote-refresh]` | `.github/skills/akr-docs/scripts/akr-resolve.md` by default; `@github get file core-akr-templates/.github/skills/akr-docs/scripts/akr-resolve.md` only when remote mode-script loading is explicitly forced | Draft has unresolved ❓ markers |

## PATH Selection for @github Calls

- **PATH A (VS Code with GitHub MCP extension):** Use `@github get file` for live remote mode-script, template, and charter content when explicitly required.
- **PATH B (bundled workspace copy):** Load mode scripts from `.github/skills/akr-docs/scripts/` in the current workspace (the distributed copy delivered by `distribute-skill.yml`). This is the default path for end users in onboarded application repositories. Note: templates and charters are not included in the distributed bundle — PATH A is still required for template and charter fetches within mode scripts when cache is unavailable.
- **PATH C (CI / coding-agent):** The GitHub Actions workflow clones `core-akr-templates` to `~/.akr/templates/` during setup. All assets are available from that path on the runner.
- **`--remote` flag (generate and resolve modes only):** Force PATH A for the mode script, template, and charter fetches. Skip the PATH B workspace copy for the mode script. Use when you need to confirm live core-akr-templates content before the next distribution cycle.
- **`--remote-refresh` flag (generate and resolve modes only):** Same as `--remote` for the mode script, and also forces re-fetch of templates and charters even when a local `.akr/cache/` file exists. Use when upstream template or charter content has changed and the cache may be stale.

## Token Budget Rules (apply across all modes)

- Load only the mode script for the requested operation. Never load all three.
- Each mode script fetches only the charter slice it needs. Do not pre-load full charters.
- Templates are fetched by reference path. Do not embed template content in context.
- Forward payload between SSG passes must be structured facts only — no raw source re-expansion.
- Default generate/resolve runs should require at most 2 `@github` calls total when cache is cold: 1 template fetch and 1 charter fetch.
- Generate/resolve runs with `--remote` or `--remote-refresh` may require 3 `@github` calls total: 1 remote mode-script fetch, 1 template fetch, and 1 charter fetch.

## Step 0: MCP Pre-flight Check

Before loading a mode script, choose the execution lane:
- If the invocation does not include `--remote` or `--remote-refresh`: load the mode script from PATH B.
- If the invocation includes `--remote` or `--remote-refresh`: load the mode script from PATH A after a lightweight `@github get file` pre-flight check.

Model pre-flight: if the active chat model is not listed under `compatibility.models`, stop and return a blocking message that names the supported models and asks the user to switch models before re-running `/akr-docs`.

Cache readiness: check for `.akr/cache/` directory in the workspace root. If it exists, subsequent template and charter requests in generate/resolve mode will use cached files instead of live `@github` fetches (unless `--remote-refresh` is passed). Surface cache availability as part of the pre-flight confirmation block.

- If PATH B is selected: continue immediately after model and cache checks. Remote pre-flight is not required to load the mode script.
- If PATH A is selected and pre-flight succeeds: continue normally.
- If PATH A is selected and pre-flight fails: stop immediately and return a blocking reminder to start GitHub MCP server and verify extension authentication, then re-run the same command.
- If a later template or charter fetch requires PATH A and no cache hit exists: stop and explain that the mode script was loaded locally, but live remote access is still required to continue the generate/resolve run.

## Execution Path Constraint

Do not generate or validate documentation by running Python scripts or terminal commands directly. The ONLY valid execution lanes are PATH A (`@github get file`), PATH B (workspace distributed scripts in `.github/skills/akr-docs/scripts/`), and PATH C (CI runner clone), as defined above.

## Failure Handling

If PATH A fetch cannot be completed:
1. Confirm the GitHub MCP extension is installed and authenticated in VS Code.
2. Confirm GitHub MCP server is started and available.
3. If invocation includes `--remote` or `--remote-refresh`: do not fall back; stop and instruct user to restart MCP and re-run with the same command.
4. If invocation does not include remote flags: continue using PATH B for the mode script if it is available.
5. If a template or charter fetch fails and no cache hit exists, stop and instruct the user to restore GitHub MCP connectivity or re-run after cache has been populated by a successful remote-backed run.
6. If PATH B files are absent, re-run the `distribute-skill.yml` workflow to populate the distributed bundle.

If modules.yaml is absent when `generate` is invoked, redirect to `groupings` mode automatically.

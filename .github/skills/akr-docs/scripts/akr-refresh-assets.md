# AKR Mode Script: RefreshAssets

<!-- Loaded on demand by SKILL.md dispatcher. Do not load unless /akr-docs refresh-assets was invoked. -->

## Purpose

Refresh cached template and/or charter assets under `.akr/cache/` for all modules.

This mode does not generate documentation. It only updates local cache files so subsequent `/akr-docs generate` and `/akr-docs resolve` runs can reuse fresh assets while still loading local bundled mode scripts.

## Invocation Contract

Supported forms:

- `/akr-docs refresh-assets`
- `/akr-docs refresh-assets --template-only`
- `/akr-docs refresh-assets --charter-only`

Rules:

- `--template-only` and `--charter-only` are mutually exclusive. If both are provided, stop with a usage error.
- No module name is accepted. This mode applies to all modules.

## Step 1: Pre-flight

1. Read `modules.yaml` if present.
2. Build `project_types_in_scope` from `modules[].project_type` values.
3. If `modules.yaml` is missing, empty, or project types cannot be determined, fall back to all supported project types:
   - `api-backend`
   - `microservice`
   - `general`
   - `ui-component`
4. Ensure `.akr/cache/` exists in workspace root. Create it if missing.
5. Run a lightweight `@github get file` pre-flight check before the first refresh fetch. If it fails, stop and return a blocking MCP connectivity/authentication reminder.

## Step 2: Resolve Asset Targets

Derive target assets from `project_types_in_scope`.

Template targets:

- `api-backend`, `microservice`, `general` -> `.akr/templates/lean_baseline_service_template_module.md`
- `ui-component` -> `.akr/templates/ui_component_template_module.md`

Charter targets:

- `api-backend`, `microservice`, `general` -> `copilot-instructions/backend-service.instructions.md`
- `ui-component` -> `copilot-instructions/ui-component.instructions.md`

Deduplicate target lists.

Switch behavior:

- If `--template-only`: refresh template targets only.
- If `--charter-only`: refresh charter targets only.
- If no switch: refresh both target lists.

## Step 3: Refresh Cache Entries

For each selected target asset:

1. Compute cache key: `{owner}/{repo}@{branch}/{asset_path}`
2. Encode key per `SKILL.md` section **Cache Key Encoding Contract**.
3. Cache file: `.akr/cache/{encoded_cache_key}.md`
3. Fetch live content via `@github get file` from `core-akr-templates`.
4. Overwrite cache file with fetched content.
5. Record status as `refreshed`.

Failure handling:

- If an asset fetch fails, record it as `failed` with reason and continue with remaining assets.
- After processing all assets, if any failures occurred, return a partial-success summary and mark the run as incomplete.

## Step 4: Report Result

Return a concise refresh summary:

- Mode: `templates-only`, `charters-only`, or `templates+charters`
- Project types in scope
- Total targets selected
- Total refreshed
- Total failed
- Refreshed cache files (paths)
- Failed assets with reason and next action

If there are failures, include:

"Some cache entries were not refreshed. Resolve MCP connectivity/authentication issues, then rerun `/akr-docs refresh-assets` with the same switches."

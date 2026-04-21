# AKR Mode Script: CacheManagement

<!-- Loaded on demand by SKILL.md dispatcher. -->
<!-- Handles /akr-docs cache-status and /akr-docs update-cache commands. -->

## Purpose

Manage local AKR fallback assets in the application repository at `.akr/cache/`.

This mode supports current POC resiliency requirements where teams need local
template and instruction copies available if GitHub connectivity is intermittent.

This script does not generate documentation. It only:

1. Reports cache readiness (`cache-status`)
2. Refreshes cache entries from core-akr-templates (`update-cache`)

## Scope and Safety

- Cache storage is repository-local (`.akr/cache/`) rather than user-global.
- Refresh writes cache files only; it does not change module docs.
- Any refresh failure must be reported as partial success, not silent success.

---

## Command: `/akr-docs cache-status`

Report local cache state without modifying files.

### Step 1: Check cache directory

Check whether `.akr/cache/` exists in workspace root.

- If missing, report:

```
AKR cache not found at .akr/cache/
Run: /akr-docs update-cache
```

Stop after reporting.

### Step 2: Evaluate expected cache assets

Determine expected targets using existing module scope logic:

- If `modules.yaml` is readable, derive project types from `modules[].project_type`.
- If not readable or empty, assume all supported project types.

Expected template keys:

- `.akr/templates/lean_baseline_service_template_module.md`
- `.akr/templates/ui_component_template_module.md`

Expected charter keys:

- `copilot-instructions/backend-service.instructions.md`
- `copilot-instructions/ui-component.instructions.md`
- `copilot-instructions/database.instructions.md`

### Step 3: Inspect cache files

For each expected asset, map to encoded cache filename under `.akr/cache/` and report:

- present/missing
- last modified timestamp
- age in days

### Step 4: Return status summary

Return:

- cache directory status
- total expected assets
- present vs missing count
- stale entries (older than 30 days)
- recommendation

Recommendation rules:

- If missing > 0: recommend `/akr-docs update-cache`.
- If stale > 0 and missing = 0: recommend optional refresh.
- If no missing and no stale: cache is ready.

---

## Command: `/akr-docs update-cache`

Refresh local fallback cache from core-akr-templates.

Supported switches:

- `--template-only`
- `--charter-only`

Switches are mutually exclusive.

### Step 1: Resolve target asset set

Reuse the same target derivation used by `refresh-assets`:

- project types from `modules.yaml` when available
- fallback to all supported project types otherwise

Switch behavior:

- `--template-only`: refresh template assets only
- `--charter-only`: refresh charter assets only
- no switch: refresh both

### Step 2: Ensure cache directory exists

Create `.akr/cache/` if missing.

### Step 3: Pre-flight remote fetch capability

Before first fetch, run a lightweight `@github get file` pre-flight.

- If pre-flight fails, stop and return a blocking message to restore MCP/auth.

### Step 4: Refresh targets

For each target asset:

1. Fetch live content from `core-akr-templates`.
2. Encode cache key per `SKILL.md` section **Cache Key Encoding Contract**.
3. Write/overwrite encoded cache key under `.akr/cache/`.
3. Record as `refreshed`.

On fetch/write failure:

- Record as `failed` with reason.
- Continue remaining targets.

### Step 5: Return update summary

Return:

- refresh mode (`templates-only`, `charters-only`, `templates+charters`)
- targets selected
- refreshed count
- failed count
- refreshed file list
- failed assets with cause and next action

If failures occurred, include:

"Some cache entries were not refreshed. Resolve MCP/auth/connectivity issues and rerun `/akr-docs update-cache`."

---

## Relationship to `/akr-docs refresh-assets`

`update-cache` and `refresh-assets` operate on the same cache surface and use the
same remote source of truth.

- Keep behavior deterministic between both commands.
- Prefer `update-cache` for cache health workflows.
- `refresh-assets` remains available for existing automation compatibility.
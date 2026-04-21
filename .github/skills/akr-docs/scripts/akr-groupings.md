# AKR Mode Script: ProposeGroupings

<!-- Loaded on demand by SKILL.md dispatcher. Do not load unless /akr-docs groupings was invoked. -->

## Purpose

Scan project source files and produce a `modules.yaml` manifest grouping files by domain noun.

`modules.yaml` must include both `tier=primary` and `tier=supporting` files for each module so coding assistance can use full module context. Documentation generation scope remains `tier=primary` only.

Token budget: 0-1 `@github` calls.
- 0 calls for straightforward grouping.
- 1 call only when charter loading criteria below are met.

## Charter Reference

Load the project-type charter slice on demand only if the user requests rationale or you hit one of these ambiguous grouping conditions:
- A file cannot be assigned to any module using domain noun matching alone.
- Two or more modules can plausibly claim the same file based on naming patterns.

- Backend projects: `@github get file core-akr-templates/copilot-instructions/backend-service.instructions.md`
- UI projects: `@github get file core-akr-templates/copilot-instructions/ui-component.instructions.md`
- Database objects: `@github get file core-akr-templates/copilot-instructions/database.instructions.md`

Do NOT load a charter if grouping is straightforward from file names and directory structure alone.

## modules.yaml Contract

`modules.yaml` is a grouping manifest only. Write exactly this schema — no extra fields.

```yaml
project:
  name: {project-name}
  layer: {UI|API|Database|Integration|Infrastructure|Full-Stack}
  standards_version: v1.1.0
  minimum_standards_version: v1.0.0
  compliance_mode: pilot

modules:
  - name: {DomainNoun}
    grouping_status: draft
    doc_output: docs/modules/{domain-noun}.md
    files:
      - path: {relative/path/to/file.cs}
        tier: {primary|supporting}

database_objects: []

unassigned:
  - path: {file/path}
    reason: {one-line reason}
```

**Strictly forbidden fields in modules.yaml:**
`project_type`, `businessCapability`, `feature`, `layer`, `status`, `max_files`, `description`, `compliance_mode` at the module level. These go in the generated document, not here.

Tier rules:
- Every file entry in `modules.yaml` must include `tier` with value `primary` or `supporting`.
- `modules.yaml` is the full module context contract (primary + supporting) for coding assistance workflows.
- Documentation generation must analyze only `tier=primary` files.

## Grouping Algorithm

1. Check for existing `modules.yaml`. If present, read `grouping_status` for each module. Do not touch modules with `grouping_status: approved`. Only propose additions or changes to `draft` modules.

2. Scan source files. Apply these assignment rules in order:

   **Backend pattern (C#/.NET)** — group into one module when files share the same domain noun:
   - `{Noun}Controller.cs` → Controller role (`tier=primary`)
   - `I{Noun}Service.cs` + `{Noun}Service.cs` → Service interface + implementation (`tier=primary`)
   - `I{Noun}Repository.cs` + `Ef{Noun}Repository.cs` or `InMemory{Noun}Repository.cs` → Repository pair (`tier=primary`)
   - `{Noun}Entity.cs` or `{Noun}.cs` in Entities/ → Domain entity (`tier=primary`)
   - `{Noun}Dtos.cs` or `{Noun}Requests.cs` → Contracts (`tier=primary`)
   - Base classes, plumbing extensions, and generic helpers with no domain behavior → Shared supporting files (`tier=supporting`)

   **Backend pattern (Python/Django/FastAPI)** — group when files contribute to the same business capability:
   - Route entry points: `urls.py` (URL routing), `views.py` (view handlers/route functions) → View Handler (`tier=primary`)
   - Business logic: `services.py`, `helpers.py` (orchestration, domain algorithms) → Service (`tier=primary`)
   - Data validation: `forms.py` (Django), `schemas.py` (Pydantic/FastAPI), serializers → Validator (`tier=primary`)
   - ORM models: `models.py` (Django) → Domain Entity (`tier=primary`)
   - Repository/data access layer: custom query methods, `repositories.py` → Repository (`tier=primary`)
   - Shared utilities, decorators, and infrastructure → Shared supporting files (`tier=supporting`)

   **UI pattern** — group when files operate on the same domain noun:
   - `{Noun}Page.tsx` or `{Noun}View.tsx` → Page/container (`tier=primary`)
   - `use{Noun}.ts` → Custom hook (`tier=primary`)
   - Domain service clients/state providers (`{noun}Api.ts`, context providers, route wrappers) → Integration files (`tier=primary`)
   - `{Noun}Card.tsx`, `{Noun}List.tsx`, etc. → Domain sub-components (`tier=supporting` unless they own business state or side effects)
   - `{Noun}Types.ts` or `{noun}.types.ts` → Type definitions (`tier=supporting`)

   **UI shared-component rule**:
   - Shared design-system/common components may be reused across many feature modules.
   - For UI screen modules, include all shared component files (from `components/`) that are directly imported and rendered by the screen, regardless of tier. Each screen module must be self-contained and list every component that contributes to its rendered output.
   - The same shared component file may appear in multiple screen modules. This is intentional — it means the generated doc for each screen fully describes its rendered composition.
   - Additionally, maintain a dedicated shared module (for example: `Common*`, `Shared*`, `DesignSystem*`) that serves as the canonical documentation owner for those shared component files.
   - Non-component files (hooks, services, utilities, type definitions) must NOT be duplicated across non-shared modules. Only files from `components/` are exempt from the single-ownership rule.

  For every assigned file, write an explicit file entry in `modules.yaml` with `path` and `tier`.

3. Supporting-only module handling (documentation scope):
  - For SUMMARY_V2 output, include only modules that have at least one `tier=primary` file.
  - If a module has zero `tier=primary` files, exclude it from the `modules` list in SUMMARY_V2.
  - Add excluded modules to `merge_recommendations` with reason: "No primary files; merge into a related module or remove from grouping."
  - Do not auto-delete excluded modules from `modules.yaml`; keep `modules.yaml` as source of truth and require reviewer decision.
  - Even when excluded from SUMMARY_V2, keep both primary and supporting file entries in `modules.yaml`.

4. **Silently omit** (do not add to unassigned):
   - Config files (`.NET`: `appsettings*.json`, `*.csproj`, `Program.cs`; Python: `settings.py`, `wsgi.py`, `asgi.py`, `manage.py`, `pyproject.toml`, `requirements.txt`) with the following exception:

     Include `Program.cs` in a `Runtime` or `Platform` module if it meets ANY of these
     criteria (evaluated in order — stop at first match):
       1. Registers 3 or more non-infrastructure services via `builder.Services.Add*`
          where the registered type is NOT one of: `AddDbContext`, `AddCors`,
          `AddAuthentication`, `AddAuthorization`, `AddControllers`, `AddEndpointsApiExplorer`,
          `AddSwaggerGen`, `AddHealthChecks`, `AddLogging`, `AddHttpClient`.
       2. Contains factory registrations (`builder.Services.AddSingleton<T>(sp => ...)`)
          for domain types.
       3. Contains conditional registration blocks (`if (env.IsDevelopment())`) that
          affect domain services.

     If `Program.cs` qualifies under any criterion, add it to a `Runtime` module alongside
     other shared infrastructure files (middleware, `DbContext`, global exception handler).
     If it does not qualify, omit it silently.
   - Test files (`*.test.*`, `*.spec.*`, `*Tests.cs`)
   - Dev tooling (`.editorconfig`, `Dockerfile`, `*.yml` workflows, `conftest.py`)
   - Local documentation (`RUN_LOCAL.md`, `README.md`)

5. **Add to unassigned with reason:**
   - Scaffold files (`WeatherForecast.cs`, `WeatherForecastController.cs`) → "Sample scaffold, recommend deletion"
   - Shared infrastructure with no clear domain noun → "Shared infrastructure, group manually"
   - SQL/migration files → "Database artifact, belongs in database_objects"

6. **Max files per module:** Aim for 4-7. At 8 files, add a warning note in reviewer output and set `max_files: 8` explicitly where that field is used by downstream validators. The declared max is the enforced limit.

7. **Platform/shared files:** If 2+ files serve cross-cutting infrastructure (middleware, DbContext, startup), group them into a `Runtime` or `Platform` module.

## Output Steps

1. Write `modules.yaml` to project root.
2. Display a **Grouping Review Summary** block in chat using nested YAML (SUMMARY_V2):

   ```yaml
   # SUMMARY_V2
   schema_version: 2
   source_manifest: modules.yaml
   project:
     name: {project-name}
     layer: {layer}
     compliance_mode: {compliance_mode}

   modules:
     - name: {ModuleName}
       grouping_status: {grouping_status}
       doc_output: {doc_output}
       files:
         - path: {relative/file/path}
           role: {role}
         - path: {relative/file/path}
           role: {role}

   merge_recommendations:
     - module: {ModuleNameWithoutPrimary}
       reason: No primary files; merge into a related module or remove from grouping.

   unassigned:
     - path: {file/path}
       reason: {reason}
   ```

  Notes:
   - Only include `tier=primary` files in the `files` list. Omit `tier=supporting` files entirely. Do not write a `tier` field in the output — inclusion in the list implies primary.
  - If a module has no primary files, exclude it from `modules` and add it to `merge_recommendations`.
  - Include: `supporting_files_not_shown: {count}` at summary level to signal omitted supporting files.
   - `role` must be a snake_case enum. Use the set that matches the project layer:
     - **Backend (`layer: API`):** `controller`, `service_interface`, `service_impl`, `repository_interface`, `repository_impl`, `domain_entity`, `dto_contract`, `middleware`, `db_context`, `shared_infrastructure`
     - **UI (`layer: UI`):** `page_container`, `custom_hook`, `api_client`, `context_provider`, `route_registration`, `shared_component`, `entry_point`, `route_fallback`, `utility`, `mock_data`
     - **Both:** `utility`, `test`, `test_setup`
   - `modules.yaml` remains the source of truth and retains all files including supporting ones.

3. Instruct the reviewer: "Review `modules.yaml` in your editor. If SUMMARY_V2 contains `merge_recommendations`, resolve those first by merging or removing the listed modules. Then change `grouping_status: draft` to `grouping_status: approved` for modules you confirm. Run `/akr-docs generate [ModuleName]` only for approved modules that have at least one primary file. Treat `modules.yaml` as the source of truth; use SUMMARY_V2 as a review aid for primary-file coverage only."

4. Add a note in the chat summary:
  - `modules.yaml` includes both `primary` and `supporting` files for coding assistance context.
  - Documentation generation and quality scoring use only `tier=primary` files.

## Checklist Before Completing

- [ ] No approved module was modified
- [ ] All module names reflect domain language (nouns, not verbs)
- [ ] Shared component files (from `components/`) may appear in multiple screen modules; all other files (hooks, services, utilities) must not be duplicated across non-shared modules
- [ ] A dedicated shared module (e.g. `CommonComponents`) exists as the canonical owner of shared component files
- [ ] Every `modules.yaml` file entry uses object form with `path` and `tier` (`primary` or `supporting`)
- [ ] Each module in SUMMARY_V2 has at least one `tier=primary` file
- [ ] Supporting-only modules are excluded from SUMMARY_V2 and listed in `merge_recommendations`
- [ ] Scaffold files are in unassigned with deletion recommendation
- [ ] `database_objects` is `[]` if no SQL/migration files found


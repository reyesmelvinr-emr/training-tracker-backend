# AKR Mode Script: ProposeGroupings

<!-- Loaded on demand by SKILL.md dispatcher. Do not load unless /akr-docs groupings was invoked. -->

## Purpose

Scan project source files and produce a `modules.yaml` manifest grouping files by domain noun.

## Charter Reference

Load the project-type charter slice on demand only if the user requests rationale or you encounter an ambiguous grouping decision:

- Backend projects: `@github get file core-akr-templates/copilot-instructions/backend-service.instructions.md`
- UI projects: `@github get file core-akr-templates/copilot-instructions/ui-component.instructions.md`
- Database objects: `@github get file core-akr-templates/copilot-instructions/database.instructions.md`

Do NOT load a charter if the grouping is straightforward from file names and directory structure alone.

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
      - {relative/path/to/file.cs}

database_objects: []

unassigned:
  - path: {file/path}
    reason: {one-line reason}
```

**Strictly forbidden fields in modules.yaml:**
`project_type`, `businessCapability`, `feature`, `layer`, `status`, `max_files`, `description`, `compliance_mode` at the module level. These go in the generated document, not here.

## Grouping Algorithm

1. Check for existing `modules.yaml`. If present, read `grouping_status` for each module. Do not touch modules with `grouping_status: approved`. Only propose additions or changes to `draft` modules.

2. Scan source files. Apply these assignment rules in order:

   **Backend pattern** — group into one module when files share the same domain noun:
   - `{Noun}Controller.cs` → Controller role
   - `I{Noun}Service.cs` + `{Noun}Service.cs` → Service interface + implementation
   - `I{Noun}Repository.cs` + `Ef{Noun}Repository.cs` or `InMemory{Noun}Repository.cs` → Repository pair
   - `{Noun}Entity.cs` or `{Noun}.cs` in Entities/ → Domain entity
   - `{Noun}Dtos.cs` or `{Noun}Requests.cs` → Contracts

   **UI pattern** — group when files operate on the same domain noun:
   - `{Noun}Page.tsx` or `{Noun}View.tsx` → Page component
   - `{Noun}Card.tsx`, `{Noun}List.tsx`, etc. → Sub-components
   - `use{Noun}.ts` → Custom hook
   - `{Noun}Types.ts` or `{noun}.types.ts` → Type definitions

3. **Silently omit** (do not add to unassigned):
   - Config files (`appsettings*.json`, `*.csproj`, `Program.cs`) with the following exception:

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
   - Dev tooling (`.editorconfig`, `Dockerfile`, `*.yml` workflows)
   - Local documentation (`RUN_LOCAL.md`, `README.md`)

4. **Add to unassigned with reason:**
   - Scaffold files (`WeatherForecast.cs`, `WeatherForecastController.cs`) → "Sample scaffold, recommend deletion"
   - Shared infrastructure with no clear domain noun → "Shared infrastructure, group manually"
   - SQL/migration files → "Database artifact, belongs in database_objects"

5. **Max files per module:** Aim for 4–7. Flag but do not block if a module reaches 8.

6. **Platform/shared files:** If 2+ files serve cross-cutting infrastructure (middleware, DbContext, startup), group them into a `Runtime` or `Platform` module.

## Output Steps

1. Write `modules.yaml` to project root.
2. Display a summary table in chat:

   | Module | Files | File List |
   |--------|-------|-----------|
   | CourseDomain | 6 | CoursesController.cs, ICourseService.cs, ... |

3. Instruct the reviewer: "Review `modules.yaml` in your editor. Change `grouping_status: draft` to `grouping_status: approved` for modules you confirm. Then run `/akr-docs generate [ModuleName]` for each approved module."

## Checklist Before Completing

- [ ] No approved module was modified
- [ ] All module names reflect domain language (nouns, not verbs)
- [ ] No file appears in more than one module
- [ ] Scaffold files are in unassigned with deletion recommendation
- [ ] `database_objects` is `[]` if no SQL/migration files found
- [ ] Summary table shown in chat

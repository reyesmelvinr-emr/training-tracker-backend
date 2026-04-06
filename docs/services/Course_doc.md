---
businessCapability: CourseManagement
feature: "❓ NEEDS real work-item tag"
layer: API
project_type: api-backend
status: draft
compliance_mode: pilot
generation-strategy: single-pass
passes-completed: single
excluded-sections: []
---

<!-- akr-generated
skill: akr-docs
mode: GenerateDocumentation
template: reyesmelvinr-emr/core-akr-templates@master/templates/lean_baseline_service_template_module.md
charter: reyesmelvinr-emr/core-akr-templates@master/copilot-instructions/backend-service.instructions.md
steps-completed: 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12
generated-at: 2026-04-06T10:00:00Z
generation-strategy: single-pass
passes-completed: single
pass-timings-seconds: preflight=2 | template-fetch=0 | charter-fetch=0 | source-extraction=10 | assembly=20 | write=1
total-generation-seconds: 33
-->

# Module: Course

**Module Scope**: Multi-file domain unit  
**Files in Module**: 6 (see Module Files section below)  
**Primary Domain Noun**: Course  
**Complexity**: Medium  
**Documentation Level**: 🔶 Baseline (70% complete)

---

<!-- akr:section id="quick_reference" required=true order=1 authorship="mixed" human_columns="business_outcome,specific_caller,watch_out" -->
## Quick Reference (TL;DR)

**What it does:**  
Manages the full lifecycle of training course records — create, read, update, and delete — through a paginated REST API backed by an EF Core persistence layer. The service layer enforces title uniqueness and maps domain entities to response DTOs.  
❓ [HUMAN: Cite the specific business outcome this module enables — e.g., "allows L&D administrators to maintain the course catalog used during employee onboarding enrollment."]

**When to use it:**  
When there are needed changes to the list of courses available

**Watch out for:**  
❓ [HUMAN: Cite a specific code path or scenario operators should know about. Note: `CreatedUtc` on the `Course` entity is marked `[NotMapped]` and is not persisted to the database — records retrieved by `GetAsync` after initial creation will return `DateTime.MinValue` for this field. Confirm whether this is intentional or a gap requiring a DB schema migration.]

---

<!-- akr:section id="module_files" required=true order=2 authorship="ai" -->
## Module Files

| File | Role | Primary Responsibilities |
|------|------|--------------------------|
| `TrainingTracker.Api/Controllers/CoursesController.cs` | Controller | Receives HTTP requests for `api/courses`; validates `ModelState`; delegates to `ICourseService`; maps service results to HTTP status codes (200, 201, 204, 400, 404, 409). |
| `TrainingTracker.Api/Domain/Services/ICourseService.cs` | Service Interface + Implementation | Defines `ICourseService` contract; `CourseService` implements pagination normalization, title-uniqueness enforcement (`ExistsByTitleAsync`), entity construction, and entity-to-DTO mapping via private `MapSummary` / `MapDetail` methods. |
| `TrainingTracker.Api/Domain/Repositories/ICourseRepository.cs` | Repository Interface + In-Memory Implementation | Defines `ICourseRepository` contract with six data operations; `InMemoryCourseRepository` provides a thread-safe, lock-based in-memory implementation with deterministic seed data for testing. |
| `TrainingTracker.Api/Infrastructure/Persistence/EfCourseRepository.cs` | EF Core Repository Implementation | Implements `ICourseRepository` using `TrainingTrackerDbContext`; uses `AsNoTracking()` for all reads; orders list results by `Title`; sets `CreatedUtc` and `Id` at application level before EF `SaveChangesAsync`. |
| `TrainingTracker.Api/Contracts/Courses/CourseDtos.cs` | DTOs / Request-Response Contracts | Defines `CourseSummaryDto`, `CourseDetailDto`, `CreateCourseRequest`, `UpdateCourseRequest`, and `PagedResponse<T>`; enforces DataAnnotations constraints (title required/max 200, validity months 1–120, category max 100, description max 2000). |
| `TrainingTracker.Api/Domain/Entities/Course.cs` | Domain Entity | Represents a course record with properties: `Id` (Guid, default new), `Title`, `IsRequired`, `ValidityMonths` (nullable), `Category` (nullable), `Description` (nullable), `IsActive` (default `true`), and `CreatedUtc` (`[NotMapped]`, set at application layer, not persisted). |

---

<!-- akr:section id="purpose_scope" required=true order=3 authorship="mixed" human_columns="business_purpose,scope_boundaries" -->
## Purpose and Scope

### Purpose

**Technical:**  
Provides a CRUD REST API for the `Course` domain entity, bridging HTTP request handling through a service layer that enforces business constraints (title uniqueness, pagination normalization) to an EF Core-backed persistence layer. The module covers the full vertical slice from controller through repository for course catalog management.

**Business:**  
❓ [HUMAN: Why did the organization build a centralized course catalog? What business problem does maintaining course records solve — e.g., compliance tracking, training assignment eligibility, audit requirements?]

### Not Responsible For

- Enrollment management (handled by the `Enrollment` module).  
- User management or role assignment (handled by the `User` module).  
- Authentication or authorization — no `[Authorize]` attributes are visible in the listed module files. ❓ [HUMAN: Confirm whether auth enforcement occurs upstream via middleware or API gateway policy.]  
- Admin-level bulk operations (handled by the `Admin` module).  
- Course completion tracking or certification issuance. ❓ [HUMAN: Confirm scope boundary.]

---

<!-- akr:section id="operations_map" required=true order=4 authorship="ai" -->
## Operations Map

This section covers ALL operations across ALL files in the module.

### Public Operations

| Operation | File | Parameters | Returns | Business Purpose |
|-----------|------|------------|---------|-----------------|
| `List` | `CoursesController.cs` | `page` (int, default 1), `pageSize` (int, default 10), `CancellationToken` | `ActionResult<PagedResponse<CourseSummaryDto>>` | Serve paginated course list to callers; always returns 200 with summary data. |
| `Get` | `CoursesController.cs` | `id` (Guid), `CancellationToken` | `ActionResult<CourseDetailDto>` | Return full course detail by ID; 404 with `traceId` if not found. |
| `Create` | `CoursesController.cs` | `CreateCourseRequest` (body), `CancellationToken` | `ActionResult<CourseDetailDto>` | Create new course; 400 on invalid model, 409 on duplicate title, 201 with `Location` header on success. |
| `Update` | `CoursesController.cs` | `id` (Guid), `UpdateCourseRequest` (body), `CancellationToken` | `ActionResult<CourseDetailDto>` | Update existing course; 400 invalid model, 404 not found, 409 duplicate title, 200 on success. |
| `Delete` | `CoursesController.cs` | `id` (Guid), `CancellationToken` | `IActionResult` | Delete course by ID; 404 if not found, 204 No Content on success. |
| `ListAsync` | `ICourseService.cs` (`CourseService`) | `page` (int), `pageSize` (int), `CancellationToken` | `PagedResponse<CourseSummaryDto>` | Normalizes page/pageSize (floor to 1/10 if ≤ 0); delegates to repository; projects entities to `CourseSummaryDto`. |
| `GetAsync` | `ICourseService.cs` (`CourseService`) | `id` (Guid), `CancellationToken` | `CourseDetailDto?` | Fetches course entity by ID; returns `null` if not found (controller converts to 404). |
| `CreateAsync` | `ICourseService.cs` (`CourseService`) | `CreateCourseRequest`, `CancellationToken` | `CourseDetailDto` | Enforces title uniqueness; builds `Course` entity; persists via repository; maps result to `CourseDetailDto`. |
| `UpdateAsync` | `ICourseService.cs` (`CourseService`) | `id` (Guid), `UpdateCourseRequest`, `CancellationToken` | `CourseDetailDto?` | Verifies course existence; enforces title uniqueness excluding current ID; applies field updates; persists and maps. |
| `DeleteAsync` | `ICourseService.cs` (`CourseService`) | `id` (Guid), `CancellationToken` | `bool` | Delegates delete to repository; returns `false` if not found. |
| `ListAsync` | `ICourseRepository.cs` (`ICourseRepository` / `EfCourseRepository`) | `page` (int), `pageSize` (int), `CancellationToken` | `(IReadOnlyList<Course> Items, int TotalCount)` | Returns paginated, title-ordered course entities and total record count. |
| `GetAsync` | `ICourseRepository.cs` (`ICourseRepository` / `EfCourseRepository`) | `id` (Guid), `CancellationToken` | `Course?` | Fetch single `Course` entity by primary key; returns `null` if absent. |
| `CreateAsync` | `ICourseRepository.cs` (`ICourseRepository` / `EfCourseRepository`) | `Course`, `CancellationToken` | `Course` | Assigns new `Id` if empty; sets `CreatedUtc`; persists entity via `SaveChangesAsync`. |
| `UpdateAsync` | `ICourseRepository.cs` (`ICourseRepository` / `EfCourseRepository`) | `Course`, `CancellationToken` | `Course?` | Loads tracked entity by ID; applies field mutations; persists via `SaveChangesAsync`; returns `null` if not found. |
| `DeleteAsync` | `ICourseRepository.cs` (`ICourseRepository` / `EfCourseRepository`) | `id` (Guid), `CancellationToken` | `bool` | Removes `Course` entity from context; persists via `SaveChangesAsync`; returns `false` if not found. |
| `ExistsByTitleAsync` | `ICourseRepository.cs` (`ICourseRepository` / `EfCourseRepository`) | `title` (string), `excludeId` (Guid?), `CancellationToken` | `bool` | Queries for any course with the given title, optionally excluding the specified ID (used during update). |

### Internal Operations (for module completeness)

| Operation | File | Purpose | Called By |
|-----------|------|---------|-----------|
| `MapSummary` | `ICourseService.cs` (`CourseService`) | Projects `Course` entity to `CourseSummaryDto` (Id, Title, IsRequired, IsActive, ValidityMonths, Category). | `CourseService.ListAsync` |
| `MapDetail` | `ICourseService.cs` (`CourseService`) | Projects `Course` entity to `CourseDetailDto` (all summary fields plus Description and CreatedUtc). | `CourseService.GetAsync`, `CourseService.CreateAsync`, `CourseService.UpdateAsync` |

---

<!-- akr:section id="how_it_works" required=true order=5 authorship="mixed" -->
## How It Works

### Primary Operation: CreateAsync (Course Creation)

**Purpose:**  
Creates a new course record after enforcing title uniqueness and validating the request payload via DataAnnotations.  
❓ [HUMAN: What business event triggers course creation? Is this initiated by an L&D administrator, an automated import, or another process?]

**Input:**  
`POST api/courses` with `CreateCourseRequest` body — Title (required, 1–200 chars), IsRequired (bool), IsActive (bool, default true), ValidityMonths (nullable, 1–120), Category (nullable, max 100), Description (nullable, max 2000).

**Output:**  
`CourseDetailDto` on success (HTTP 201 Created with `Location` header pointing to `GET api/courses/{id}`); HTTP 400 on model validation failure; HTTP 409 on duplicate title.

**Step-by-Step Flow:**

```
┌──────────────────────────────────────────────────────────────┐
│ Step 1: HTTP POST → CoursesController.Create                 │
│  What  → ASP.NET model binding deserializes request body;    │
│           ModelState checked against DataAnnotations.        │
│  Why   → ❓ [HUMAN: Business rationale for server-side       │
│           model validation guard before service call]        │
│  Error → 400 Bad Request returned if ModelState invalid;     │
│           flow stops here.                                   │
└──────────────────────────────────────────────────────────────┘
   ↓
┌──────────────────────────────────────────────────────────────┐
│ Step 2: CourseService.CreateAsync — Title Uniqueness Check   │
│  What  → Calls ICourseRepository.ExistsByTitleAsync with     │
│           excludeId = null (new course, no exclusion needed).│
│  Why   → ❓ [HUMAN: Why must course titles be unique?        │
│           Is this a system constraint or business policy?]   │
│  Error → Throws InvalidOperationException if title exists;   │
│           controller catches and returns 409 Conflict.       │
└──────────────────────────────────────────────────────────────┘
   ↓
┌──────────────────────────────────────────────────────────────┐
│ Step 3: CourseService.CreateAsync — Entity Construction      │
│  What  → New Course object built from request fields:        │
│           Title, IsRequired, IsActive, ValidityMonths,       │
│           Category, Description.                             │
│  Why   → ❓ [HUMAN: Are there any default values set at      │
│           this layer beyond what the DTO provides?]          │
│  Error → No errors at this step; construction is pure mapping│
└──────────────────────────────────────────────────────────────┘
   ↓
┌──────────────────────────────────────────────────────────────┐
│ Step 4: EfCourseRepository.CreateAsync — Persistence         │
│  What  → Assigns new Guid if Id empty; sets CreatedUtc =     │
│           DateTime.UtcNow; adds entity to DbContext;         │
│           calls SaveChangesAsync.                            │
│  Why   → ❓ [HUMAN: Why is CreatedUtc set at application     │
│           layer rather than DB default? Note: this field is  │
│           [NotMapped] and is NOT persisted to the database.] │
│  Error → DbUpdateException propagates if DB constraint       │
│           violated (e.g., unique index at DB layer).         │
└──────────────────────────────────────────────────────────────┘
   ↓
┌──────────────────────────────────────────────────────────────┐
│ Step 5: CourseService.MapDetail → HTTP 201 Created           │
│  What  → Entity projected to CourseDetailDto; controller     │
│           returns 201 Created with Location header.          │
│  Why   → ❓ [HUMAN: Is the Location header consumed by any   │
│           known client?]                                     │
│  Error → None expected at this step.                         │
└──────────────────────────────────────────────────────────────┘
                    [SUCCESS] or [FAILURE at any step above]
```

**Success Path:**  
HTTP 201 Created with `CourseDetailDto` body and `Location: api/courses/{new-id}` header.

**Failure Paths:**  
- HTTP 400 Bad Request — DataAnnotations violated in request body (ModelState check, Step 1).  
- HTTP 409 Conflict — Title already exists (`InvalidOperationException` caught at controller, Step 2).  
- HTTP 500 / middleware-handled — Unhandled `DbUpdateException` from EF `SaveChangesAsync` (Step 4) propagates to `ExceptionHandlingMiddleware`. ❓ [HUMAN: Does the exception middleware return a standardized error body for 500s?]

---

<!-- akr:section id="architecture_overview" required=true order=6 authorship="ai" -->
## Architecture Overview

### Full-Stack Module Architecture

```
┌─────────────────────────────────────┐
│ API Layer — Entry Point             │
│ CoursesController                   │
│ Route: api/courses                  │
│ └─ Receives HTTP requests           │
│ └─ Validates ModelState             │
│ └─ Catches InvalidOperationException│
└─────────────────────────────────────┘
         ↓
┌─────────────────────────────────────┐
│ Service Layer — Business Logic      │
│ ICourseService                      │
│ ├─ Defines contract                 │
│ └─ CourseService (implementation)   │
│    └─ Title uniqueness enforcement  │
│    └─ Pagination normalization      │
│    └─ Entity ↔ DTO mapping          │
└─────────────────────────────────────┘
         ↓
┌─────────────────────────────────────┐
│ Repository Layer — Data Abstraction │
│ ICourseRepository                   │
│ ├─ Defines data contract            │
│ ├─ InMemoryCourseRepository         │
│ │   (testing / in-memory runtime)   │
│ └─ EfCourseRepository               │
│    └─ Queries TrainingTrackerDbCtx  │
│    └─ AsNoTracking for reads        │
│    └─ SaveChangesAsync for writes   │
└─────────────────────────────────────┘
         ↓
┌─────────────────────────────────────┐
│ Data Layer — Persistence            │
│ Courses table (EF DbSet<Course>)    │
│ └─ Stores course catalog records    │
│ └─ Note: CreatedUtc is [NotMapped]  │
│    and is NOT persisted in the DB   │
└─────────────────────────────────────┘
```

### Module Composition

The six files form a complete CRUD vertical slice. `CoursesController` handles HTTP concerns only; it has no direct knowledge of persistence. `CourseService` (in `ICourseService.cs`) is the single class responsible for domain rules — uniqueness enforcement lives here, not in the controller or repository. `EfCourseRepository` handles all persistence concerns; the `InMemoryCourseRepository` mirrors the contract for test and in-memory runtime scenarios. `CourseDtos.cs` defines the external-facing API surface; `Course.cs` defines the internal domain entity — the `MapSummary`/`MapDetail` private methods in `CourseService` are the explicit translation boundary between the two.

### Dependencies (What This Module Needs)

| Dependency | Purpose | Failure Mode | Critical? |
|------------|---------|--------------|-----------|
| `ICourseRepository` | All data read/write operations for Course records | Service returns null or throws; no data reads/writes possible | Yes — all operations require a functional repository. |
| `TrainingTrackerDbContext` | EF Core database access for `EfCourseRepository` | `DbUpdateException` on writes; `null` returns on reads | Yes — production persistence unavailable without it. ❓ [HUMAN: Does the module degrade gracefully (e.g., fallback to in-memory) or hard-fail?] |
| `ILogger<CoursesController>` | Structured logging for warning-level service exceptions | Logging silently fails; no user-facing impact | No — logging failure does not affect API response correctness. |
| `CorrelationIdMiddleware` | `traceId` injected into 404/409 error responses via `HttpContext.Items` | `traceId` missing from error body; observability reduced | No — error responses still returned with `message` field. ❓ [HUMAN: Is `traceId` required by the API consumer contract?] |

### Consumers (Who Uses This Module)

❓ [HUMAN: No callers are visible in the listed module files. Which UI pages, background jobs, or API clients call `api/courses`?]

---

<!-- akr:section id="business_rules" required=true order=7 authorship="mixed" human_columns="why_it_exists,since_when" -->
## Business Rules

| Rule ID | Rule Description | Why It Exists | Since When | Where Enforced |
|---------|-----------------|---------------|------------|----------------|
| BR-CRS-001 | Course titles must be unique across all courses. Duplicate title on create or update throws `InvalidOperationException` (HTTP 409). | ❓ [HUMAN: Is this a business policy to prevent confusion in enrollment selection, or a system constraint?] | ❓ [HUMAN: Exact sprint or date this rule was introduced — do not estimate] | `CourseService.CreateAsync`, `CourseService.UpdateAsync` via `ICourseRepository.ExistsByTitleAsync` |
| BR-CRS-002 | Course title is required and must be between 1 and 200 characters. | ❓ [HUMAN: Why 200 characters? Is this driven by a display field length in a UI or reporting tool?] | ❓ [HUMAN: Date or sprint — do not estimate] | DataAnnotations on `CreateCourseRequest.Title` and `UpdateCourseRequest.Title`; ASP.NET `ModelState` validation |
| BR-CRS-003 | `ValidityMonths` must be between 1 and 120 if provided (nullable). | ❓ [HUMAN: Does validity period drive renewal reminders or enrollment expiration logic? Why is 120 the maximum?] | ❓ [HUMAN: Date or sprint — do not estimate] | DataAnnotations `[Range(1, 120)]` on `CreateCourseRequest.ValidityMonths` and `UpdateCourseRequest.ValidityMonths` |
| BR-CRS-004 | `Category` cannot exceed 100 characters if provided. | ❓ [HUMAN: What category taxonomy is expected? Is this free-text or constrained to a list?] | ❓ [HUMAN: Date or sprint — do not estimate] | DataAnnotations `[StringLength(100)]` in request DTOs |
| BR-CRS-005 | `Description` cannot exceed 2000 characters if provided. | ❓ [HUMAN: Is 2000 characters aligned with a storage column constraint or a UI display limit?] | ❓ [HUMAN: Date or sprint — do not estimate] | DataAnnotations `[StringLength(2000)]` in request DTOs |
| BR-CRS-006 | `IsActive` defaults to `true` on new course creation. | ❓ [HUMAN: Does this mean all newly created courses are immediately available for enrollment? Is there an approval gate?] | ❓ [HUMAN: Date or sprint — do not estimate] | Default property value on `CreateCourseRequest.IsActive` |
| BR-CRS-007 | Pagination `page` and `pageSize` values ≤ 0 are normalized to 1 and 10 respectively. Normalization occurs in both `CourseService.ListAsync` and `EfCourseRepository.ListAsync`. | Defensive normalization prevents zero-page arithmetic errors in skip/take queries. | ❓ [HUMAN: Date or sprint — do not estimate] | `CourseService.ListAsync` (service layer normalization); `EfCourseRepository.ListAsync` (repository-level guard) |

---

<!-- akr:section id="api_contract" required=false order=8 condition="controller_with_http_attributes" authorship="mixed" -->
## API Contract (AI Context)

> 📋 **Interactive Documentation:** ❓ [HUMAN: Provide API portal or Swagger URL if available]  
> **Purpose:** This section provides AI assistants (Copilot) with API context for this module.  
> **Sync Status:** Last verified on ❓ [HUMAN: Date]

### Endpoints

| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| `GET` | `api/courses` | List courses (paginated) | ❓ [HUMAN: Auth required? No `[Authorize]` visible in source.] |
| `GET` | `api/courses/{id}` | Get course by ID | ❓ [HUMAN: Auth required?] |
| `POST` | `api/courses` | Create new course | ❓ [HUMAN: Auth required?] |
| `PUT` | `api/courses/{id}` | Update existing course | ❓ [HUMAN: Auth required?] |
| `DELETE` | `api/courses/{id}` | Delete course by ID | ❓ [HUMAN: Auth required?] |

### Request Examples

**POST / PUT `CreateCourseRequest` / `UpdateCourseRequest`:**

```json
{
  "title": "string (required, 1–200 chars)",
  "isRequired": true,
  "isActive": true,
  "validityMonths": 12,
  "category": "string (optional, max 100 chars)",
  "description": "string (optional, max 2000 chars)"
}
```

| Property | Type | Required | Constraints | Description |
|----------|------|----------|-------------|-------------|
| `title` | `string` | Yes | 1–200 characters | Identifies the course; must be unique across all courses. |
| `isRequired` | `bool` | No | — | Indicates whether this course is mandatory for all eligible users. ❓ [HUMAN: What determines eligibility?] |
| `isActive` | `bool` | No | Defaults to `true` on create; no default on update | Controls whether the course is available for enrollment. |
| `validityMonths` | `int?` | No | 1–120 if provided | Duration after course completion before re-certification is required. |
| `category` | `string?` | No | Max 100 characters | Grouping or taxonomy label for the course. |
| `description` | `string?` | No | Max 2000 characters | Long-form description of course content. |

### Success Response Examples

**GET `api/courses` (200):**

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "Safety Orientation",
      "isRequired": true,
      "isActive": true,
      "validityMonths": 12,
      "category": "Safety"
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 1,
  "totalPages": 1
}
```

**GET / POST / PUT success (200 / 201):**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Safety Orientation",
  "isRequired": true,
  "isActive": true,
  "validityMonths": 12,
  "category": "Safety",
  "description": "Mandatory safety introduction for all new hires.",
  "createdUtc": "2026-04-06T10:00:00Z"
}
```

### Error Response Examples

```json
{
  "traceId": "00-abc123-01",
  "message": "Course not found"
}
```

```json
{
  "traceId": "00-abc123-02",
  "message": "A course with the title 'Safety Orientation' already exists."
}
```

**ModelState 400 response** — ASP.NET default format (keys = field names, values = error messages):

```json
{
  "title": ["Title must be between 1 and 200 characters"]
}
```

---

<!-- akr:section id="validation_rules" required=false order=9 condition="validator_or_annotations" authorship="mixed" -->
## Validation Rules

Validation is applied via ASP.NET DataAnnotations on `CreateCourseRequest` and `UpdateCourseRequest`. No FluentValidation validators are present in the listed module files.

| Property | Rule | Error Message | Applies To |
|----------|------|---------------|------------|
| `Title` | `[Required]` | "Title is required" | Create, Update |
| `Title` | `[StringLength(200, MinimumLength = 1)]` | "Title must be between 1 and 200 characters" | Create, Update |
| `ValidityMonths` | `[Range(1, 120)]` | "Validity months must be between 1 and 120" | Create, Update |
| `Category` | `[StringLength(100)]` | "Category cannot exceed 100 characters" | Create, Update |
| `Description` | `[StringLength(2000)]` | "Description cannot exceed 2000 characters" | Create, Update |

❓ [HUMAN: Are there additional business validation rules implemented outside DataAnnotations — for example, validation that `IsActive = false` cannot be set on courses with active enrollments? If so, they should be documented here.]

---

<!-- akr:section id="data_operations" required=true order=10 authorship="ai" -->
## Data Operations

### Reads From

| Database Object | Purpose | Business Context | Performance Notes |
|-----------------|---------|------------------|-------------------|
| `Courses` (EF `DbSet<Course>`) | Paginated list query — all records ordered by `Title`, paginated via skip/take. | Returns summary-level course records to callers requesting the course catalog. | `AsNoTracking()` applied; `OrderBy(c => c.Title)` executed per page request. ❓ [HUMAN: Is an index on `Title` defined in the schema? Without it, sorting is a table scan.] |
| `Courses` (EF `DbSet<Course>`) | Single-record fetch by primary key `Id`. | Used by `GetAsync`, `UpdateAsync` (EF `FindAsync`), and `DeleteAsync`. | `AsNoTracking()` on `GetAsync` read path; `FindAsync` (tracking) used for update/delete. |
| `Courses` (EF `DbSet<Course>`) | Title uniqueness check — `Where(c => c.Title == title)` with optional `Where(c => c.Id != excludeId)`. | Invoked before every Create and Update to enforce BR-CRS-001. | No `AsNoTracking()` explicitly on uniqueness check query; uses `AnyAsync`. ❓ [HUMAN: Is there a unique index on `Title` in the database to back this application-level check?] |

### Writes To

| Database Object | Purpose | Business Context | Performance Notes |
|-----------------|---------|------------------|-------------------|
| `Courses` (EF `DbSet<Course>`) | INSERT — new course record. | Triggered by `POST api/courses` after title uniqueness confirmed. | `SaveChangesAsync` called once per create operation; single-entity transaction. |
| `Courses` (EF `DbSet<Course>`) | UPDATE — existing course fields (Title, IsRequired, IsActive, ValidityMonths, Category, Description). | Triggered by `PUT api/courses/{id}` after existence and uniqueness confirmed. | EF change-tracking based; `SaveChangesAsync` called once. |
| `Courses` (EF `DbSet<Course>`) | DELETE — course record removed by `Id`. | Triggered by `DELETE api/courses/{id}`. | EF `Remove` + `SaveChangesAsync`; no cascade behavior visible in listed files. ❓ [HUMAN: Does deleting a course cascade-delete related Enrollment records, or is referential integrity enforced elsewhere?] |

### Side Effects

No email, notification, event, or queue side effects are visible in the listed module files. ❓ [HUMAN: Are there domain events, audit log writes, or messaging side effects triggered by course create/update/delete that are handled outside this module?]

---

<!-- akr:section id="failure_modes" required=true order=11 authorship="ai" -->
## Failure Modes & Exception Handling

### Common Failure Scenarios

| Exception Type | Trigger | Operation | Impact | Mitigation |
|---|---|---|---|---|
| `InvalidOperationException` | `ExistsByTitleAsync` returns `true` — title already in use | `CourseService.CreateAsync`, `CourseService.UpdateAsync` | Request rejected; no entity persisted | `CoursesController` catches and returns HTTP 409 Conflict with `traceId` and `message` body |
| `DbUpdateException` (EF Core) | Database constraint violation on `SaveChangesAsync` (e.g., unique index, FK constraint) | `EfCourseRepository.CreateAsync`, `UpdateAsync`, `DeleteAsync` | Write operation fails; no data persisted or removed | Not caught in module files — propagates to `ExceptionHandlingMiddleware`. ❓ [HUMAN: Does the middleware return a user-readable error body or a raw 500?] |
| `OperationCanceledException` / `TaskCanceledException` | `CancellationToken` signalled during async DB calls | All async operations in `EfCourseRepository` | In-flight DB operation cancelled | Not explicitly caught in listed files — propagates to ASP.NET request pipeline. ❓ [HUMAN: Is there a cancellation-specific handler in `ExceptionHandlingMiddleware`?] |
| Implicit null (not an exception) | `GetAsync` returns `null` for unknown `Id` | `CoursesController.Get`, `Update`, and `Delete` | HTTP 404 returned by controller null check | Handled inline in controller with null-check + `NotFound()` return |

---

<!-- akr:section id="questions_gaps" required=true order=12 authorship="mixed" human_columns="human_flagged" -->
## Questions & Gaps

### AI-Flagged Questions

1. ❓ **`CreatedUtc` is `[NotMapped]` on `Course` entity** — `CreatedUtc` is set at application level in both `InMemoryCourseRepository.CreateAsync` and `EfCourseRepository.CreateAsync`, but the `[NotMapped]` attribute means this field is NOT persisted to the database. Consequently, any course fetched via `GetAsync` after being created will return `CreatedUtc = DateTime.MinValue` (the CLR default). This appears to be a data access gap — either a DB column is missing, or the intent is to track creation time only in the create-response body. **Next action:** DB schema owner to confirm whether a `CreatedUtc` column should be added to the `Courses` table and the `[NotMapped]` attribute removed.

2. ❓ **Authentication / Authorization not visible** — No `[Authorize]` attributes or policy checks appear in `CoursesController`. Confirm whether auth is enforced via global middleware, API gateway policy, or is intentionally absent. **Next action:** API owner to document auth enforcement point.

3. ❓ **Double pagination normalization** — `page`/`pageSize` floor-to-positive normalization is applied in both `CourseService.ListAsync` and `EfCourseRepository.ListAsync`. This is defensive but creates duplication. Confirm whether the repository-layer normalization is intentional (standalone testability) or a copy-paste artifact.

4. ❓ **No DB-level unique constraint visible** — Title uniqueness is enforced only in the service layer (`ExistsByTitleAsync`). Under concurrent requests, a TOCTOU (time-of-check/time-of-use) race condition could allow duplicate titles to be inserted. **Next action:** DB schema owner to confirm whether a unique index on `Title` exists in the `Courses` table.

5. ❓ **Cascade delete behavior on `Enrollment`** — Deleting a course via `DELETE api/courses/{id}` does not appear to handle referential integrity with `Enrollment` records. If `Enrollment` has a FK to `Courses`, this could result in orphaned enrollments or a DB FK constraint violation. **Next action:** Confirm EF cascade configuration and enrollment behavior.

6. ❓ **DI registration for `ICourseService` → `CourseService`** — The service and interface are both defined in `ICourseService.cs`. Confirm DI container registration in `Program.cs` maps `ICourseService` → `CourseService` and `ICourseRepository` → `EfCourseRepository` (production) / `InMemoryCourseRepository` (test). **Next action:** Verify `Program.cs` registrations.

### Human-Flagged Questions

❓ [HUMAN: Add any additional questions identified during review — e.g., field-level business rules, missing error handling, integration patterns with downstream systems.]

---

<!-- akr:section id="related_docs" required=false order=13 condition="related_modules_in_manifest" authorship="ai" -->
## Related Documentation

**Other Modules (from modules.yaml):**

- [Enrollment Module](./Enrollment_doc.md) — manages enrollment records that reference Course entities. Cascade behavior on course delete should be reviewed in context of this module.
- [User Module](./User_doc.md) — manages user records that are candidates for enrollment.
- [Admin Module](./Admin_doc.md) — provides administrative endpoints; relationship to course management ❓ [HUMAN: Does Admin module expose bulk course operations?].
- [Runtime Module](./Runtime_doc.md) — covers middleware, DbContext, and startup configuration referenced by this module. *Note: Runtime module `grouping_status: draft` — doc not yet generated.*

**Database Objects:** ❓ [HUMAN: Link to database documentation for the `Courses` table when available, e.g., `../../database-repo/docs/tables/Courses_doc.md`.]

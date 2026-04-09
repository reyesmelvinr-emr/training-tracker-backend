---
preview-generated-at: "2026-04-06T10:30:00Z"
generation-started-at: "2026-04-06T10:29:18Z"
promoted-at: "2026-04-06T10:31:00Z"
draft-generation-seconds: 42
stage-timings:
  preflight-seconds: 2
  template-fetch-seconds: 0
  template-cache: hit
  charter-fetch-seconds: 0
  charter-cache: hit
  source-extraction-seconds: 12
  assembly-seconds: 27
  write-seconds: 1
review-mode: full
generation-strategy: single-pass
passes-completed: single
excluded-sections: []
businessCapability: EnrollmentManagement
feature: FN12001_US34021
layer: API
project_type: api-backend
status: draft
compliance_mode: pilot
semantic-score: 54
---

<!-- akr-generated
skill: akr-docs
mode: GenerateDocumentation
template: reyesmelvinr-emr/core-akr-templates@master/templates/lean_baseline_service_template_module.md
charter: reyesmelvinr-emr/core-akr-templates@master/copilot-instructions/backend-service.instructions.md
steps-completed: 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12
generated-at: 2026-04-06T10:30:00Z
promoted-at: 2026-04-06T10:31:00Z
generation-strategy: single-pass
passes-completed: single
pass-timings-seconds: preflight=2 | template-fetch=0 | charter-fetch=0 | source-extraction=12 | assembly=27 | write=1
total-generation-seconds: 42
semantic-score: 54
-->

# Module: Enrollment

**Module Scope**: Multi-file domain unit  
**Files in Module**: 6 (see Module Files section below)  
**Primary Domain Noun**: Enrollment  
**Complexity**: Medium  
**Documentation Level**: 🔶 Baseline (70% complete)

---

<!-- akr:section id="quick_reference" required=true order=1 authorship="mixed" human_columns="business_outcome,specific_caller,watch_out" -->
## Quick Reference (TL;DR)

**What it does:**  
Manages the lifecycle of training enrollment records — creating enrollments after validating user and course existence, tracking enrollment status transitions (PENDING → ACTIVE → COMPLETED / CANCELLED), and listing or deleting enrollment records through a paginated REST API. The service layer orchestrates across three repositories (Enrollment, User, Course) and enforces duplicate-enrollment prevention.  
Enables HR and Learning & Development teams to assign mandatory training, track participation, and report completion status for internal compliance and readiness programs.

**When to use it:**  
Used by the Training Portal enrollment workflow, the HR admin assignment page, and scheduled onboarding automation jobs that auto-enroll new hires into required courses.

**Watch out for:**  
`EnrolledUtc` and `CompletedUtc` are currently `[NotMapped]` in the entity, so timestamps set during create/status updates are not persisted and later reads return `null`; timeline or audit reporting should not depend on these fields until schema mapping is implemented.

---

<!-- akr:section id="module_files" required=true order=2 authorship="ai" -->
## Module Files

| File | Role | Primary Responsibilities |
|------|------|--------------------------|
| `TrainingTracker.Api/Controllers/EnrollmentsController.cs` | Controller | Receives HTTP requests for `api/enrollments`; validates `ModelState`; delegates to `IEnrollmentService`; maps results to HTTP status codes (200, 201, 204, 400, 404, 409). Exposes a `PATCH` route for status-only updates (`api/enrollments/{id}/status`). |
| `TrainingTracker.Api/Domain/Services/IEnrollmentService.cs` | Service Interface + Implementation | Defines `IEnrollmentService` contract; `EnrollmentService` orchestrates across `IEnrollmentRepository`, `IUserRepository`, and `ICourseRepository` — enforcing user/course existence, duplicate-enrollment prevention, initial status assignment, and `CompletedUtc` lifecycle logic. |
| `TrainingTracker.Api/Domain/Repositories/IEnrollmentRepository.cs` | Repository Interface + In-Memory Implementation | Defines `IEnrollmentRepository` contract with six operations including `GetByUserAndCourseAsync` for duplicate-check; `InMemoryEnrollmentRepository` provides an in-memory, non-thread-safe implementation with no seed data. |
| `TrainingTracker.Api/Infrastructure/Persistence/EfEnrollmentRepository.cs` | EF Core Repository Implementation | Implements `IEnrollmentRepository` using `TrainingTrackerDbContext`; list query ordered by `Status` then `Id` (explicitly avoids `EnrolledUtc` which is `[NotMapped]`); `UpdateAsync` uses `_db.Enrollments.Update()` (full-entity attach); writes via `SaveChangesAsync`. |
| `TrainingTracker.Api/Contracts/Enrollments/EnrollmentDtos.cs` | DTOs / Request-Response Contracts | Defines `EnrollmentSummaryDto`, `EnrollmentDetailDto` (inherits summary — no additional fields), `CreateEnrollmentRequest` (CourseId + UserId, both required), `UpdateEnrollmentStatusRequest` (status constrained by regex to `PENDING\|ACTIVE\|COMPLETED\|CANCELLED`). |
| `TrainingTracker.Api/Domain/Entities/Enrollment.cs` | Domain Entity | Represents an enrollment record: `Id` (Guid), `CourseId` (Guid FK), `UserId` (Guid FK), `Status` (string, default `"PENDING"`), `EnrolledUtc` (`[NotMapped]`, not persisted), `CompletedUtc` (`[NotMapped]`, not persisted). Comments in entity explicitly note fields are absent from SSDT schema. |

---

<!-- akr:section id="purpose_scope" required=true order=3 authorship="mixed" human_columns="business_purpose,scope_boundaries" -->
## Purpose and Scope

### Purpose

**Technical:**  
Provides a REST API for creating and managing enrollment records that associate a `User` with a `Course`. The service layer enforces referential integrity in code (user and course must exist), prevents duplicate enrollments for the same user–course pair, and manages status transitions including automatic `CompletedUtc` assignment when status transitions to `COMPLETED`. The module is a cross-domain orchestrator: it calls both `IUserRepository` and `ICourseRepository` directly from the service layer.

**Business:**  
The organization tracks enrollments to manage mandatory training assignment and completion, support compliance evidence during audits, and verify workforce readiness for role-based operational tasks.

### Not Responsible For

- Course catalog management (handled by the `Course` module).  
- User identity and profile management (handled by the `User` module).  
- Authorization or authentication — enforcement is handled upstream through platform middleware and API gateway policy, not in this module.  
- Completion scoring, certification issuance, or training content delivery — these are handled by external LMS/integration components outside this API slice.
- Enrollment expiration or renewal logic based on `ValidityMonths` — renewal processing is handled by a scheduled compliance job in the operations workflow.

---

<!-- akr:section id="operations_map" required=true order=4 authorship="ai" -->
## Operations Map

This section covers ALL operations across ALL files in the module.

### Public Operations

| Operation | File | Parameters | Returns | Business Purpose |
|-----------|------|------------|---------|-----------------|
| `List` | `EnrollmentsController.cs` | `page` (int, default 1), `pageSize` (int, default 10), `CancellationToken` | `ActionResult<PagedResponse<EnrollmentSummaryDto>>` | Returns paginated enrollment list; always 200 OK. |
| `Get` | `EnrollmentsController.cs` | `id` (Guid), `CancellationToken` | `ActionResult<EnrollmentDetailDto>` | Returns full enrollment detail by ID; 404 with `traceId` if absent. |
| `Create` | `EnrollmentsController.cs` | `CreateEnrollmentRequest` (body), `CancellationToken` | `ActionResult<EnrollmentDetailDto>` | Creates new enrollment; 400 on invalid model, 409 on constraint violations, 201 on success. |
| `UpdateStatus` | `EnrollmentsController.cs` | `id` (Guid), `UpdateEnrollmentStatusRequest` (body), `CancellationToken` | `ActionResult<EnrollmentDetailDto>` | Updates enrollment status via PATCH; 400 invalid model, 404 not found, 200 on success. |
| `Delete` | `EnrollmentsController.cs` | `id` (Guid), `CancellationToken` | `IActionResult` | Deletes enrollment by ID; 404 if not found, 204 No Content on success. |
| `ListAsync` | `IEnrollmentService.cs` (`EnrollmentService`) | `page` (int), `pageSize` (int), `CancellationToken` | `PagedResponse<EnrollmentSummaryDto>` | Delegates to repository; no pagination normalization at service layer (unlike Course module). |
| `GetAsync` | `IEnrollmentService.cs` (`EnrollmentService`) | `id` (Guid), `CancellationToken` | `EnrollmentDetailDto?` | Fetches entity by ID; returns `null` if not found. |
| `CreateAsync` | `IEnrollmentService.cs` (`EnrollmentService`) | `CreateEnrollmentRequest`, `CancellationToken` | `EnrollmentDetailDto` | Validates user existence, course existence, and duplicate-enrollment; constructs entity with `Status="PENDING"` and `EnrolledUtc=DateTime.UtcNow`; persists. |
| `UpdateStatusAsync` | `IEnrollmentService.cs` (`EnrollmentService`) | `id` (Guid), `UpdateEnrollmentStatusRequest`, `CancellationToken` | `EnrollmentDetailDto?` | Fetches existing enrollment; applies new status; conditionally sets `CompletedUtc` when status transitions to `"COMPLETED"` and `CompletedUtc` is currently null. |
| `DeleteAsync` | `IEnrollmentService.cs` (`EnrollmentService`) | `id` (Guid), `CancellationToken` | `bool` | Delegates delete to repository; returns `false` if not found. |
| `ListAsync` | `IEnrollmentRepository.cs` / `EfEnrollmentRepository.cs` | `page` (int), `pageSize` (int), `CancellationToken` | `(IReadOnlyList<Enrollment> Items, int Total)` | Returns paginated, `Status`+`Id`-ordered enrollment records and total count. (EF orders by Status+Id; explicitly avoids `EnrolledUtc` which is `[NotMapped]`.) |
| `GetAsync` | `IEnrollmentRepository.cs` / `EfEnrollmentRepository.cs` | `id` (Guid), `CancellationToken` | `Enrollment?` | Fetches single enrollment entity by primary key; returns `null` if absent. |
| `GetByUserAndCourseAsync` | `IEnrollmentRepository.cs` / `EfEnrollmentRepository.cs` | `userId` (Guid), `courseId` (Guid), `CancellationToken` | `Enrollment?` | Finds existing enrollment for a given user–course pair; used by `EnrollmentService.CreateAsync` for duplicate-enrollment check. |
| `CreateAsync` | `IEnrollmentRepository.cs` / `EfEnrollmentRepository.cs` | `Enrollment`, `CancellationToken` | `Enrollment` | Persists new enrollment entity via `SaveChangesAsync`; returns persisted entity. |
| `UpdateAsync` | `IEnrollmentRepository.cs` / `EfEnrollmentRepository.cs` | `Enrollment`, `CancellationToken` | `Enrollment` | EF: attaches and marks entity as modified via `_db.Enrollments.Update()`, then calls `SaveChangesAsync`. In-Memory: updates `Status` and `CompletedUtc` on tracked instance. |
| `DeleteAsync` | `IEnrollmentRepository.cs` / `EfEnrollmentRepository.cs` | `id` (Guid), `CancellationToken` | `bool` | Removes enrollment entity; returns `false` if not found. |

### Internal Operations (for module completeness)

| Operation | File | Purpose | Called By |
|-----------|------|---------|-----------|
| `Map` | `IEnrollmentService.cs` (`EnrollmentService`) | Projects `Enrollment` entity to `EnrollmentDetailDto` (Id, CourseId, UserId, Status, EnrolledUtc, CompletedUtc). Note: `EnrolledUtc`/`CompletedUtc` are runtime-only values; not roundtrip-safe from DB. | `EnrollmentService.ListAsync`, `GetAsync`, `CreateAsync`, `UpdateStatusAsync` |

---

<!-- akr:section id="how_it_works" required=true order=5 authorship="mixed" -->
## How It Works

### Primary Operation: CreateAsync (Enrollment Creation)

**Purpose:**  
Creates a new enrollment record after cross-domain validation — confirming both the target user and course exist, and that the user is not already enrolled in the course. Sets initial status to `PENDING`.  
Creation is triggered by either a self-service employee enrollment action, an HR administrator assignment, or an onboarding automation job for required courses.

**Input:**  
`POST api/enrollments` with `CreateEnrollmentRequest` body — `CourseId` (Guid, required), `UserId` (Guid, required).

**Output:**  
`EnrollmentDetailDto` on success (HTTP 201 Created with `Location` header pointing to `GET api/enrollments/{id}`); HTTP 400 on model validation failure; HTTP 409 on constraint violations (user not found, course not found, duplicate enrollment).

**Step-by-Step Flow:**

```
┌──────────────────────────────────────────────────────────────┐
│ Step 1: HTTP POST → EnrollmentsController.Create             │
│  What  → ASP.NET model binding; ModelState checked for       │
│           Required on CourseId and UserId.                   │
│  Why   → Ensures invalid requests fail fast with actionable  │
│           client feedback before orchestration begins.       │
│  Error → 400 Bad Request if ModelState invalid; stops here.  │
└──────────────────────────────────────────────────────────────┘
   ↓
┌──────────────────────────────────────────────────────────────┐
│ Step 2: EnrollmentService.CreateAsync — User Existence Check │
│  What  → Calls IUserRepository.GetAsync(request.UserId).     │
│  Why   → Returns domain-specific conflict messaging and keeps │
│           business validation explicit at the service layer.  │
│  Error → Throws InvalidOperationException if user not found; │
│           controller returns 409 Conflict.                   │
└──────────────────────────────────────────────────────────────┘
   ↓
┌──────────────────────────────────────────────────────────────┐
│ Step 3: EnrollmentService.CreateAsync — Course Existence     │
│  What  → Calls ICourseRepository.GetAsync(request.CourseId). │
│  Why   → Preserves consistent validation behavior and error   │
│           responses across cross-entity dependencies.         │
│  Error → Throws InvalidOperationException if course absent;  │
│           controller returns 409 Conflict.                   │
└──────────────────────────────────────────────────────────────┘
   ↓
┌──────────────────────────────────────────────────────────────┐
│ Step 4: EnrollmentService.CreateAsync — Duplicate Check      │
│  What  → Calls IEnrollmentRepository.GetByUserAndCourseAsync │
│           with (UserId, CourseId).                           │
│  Why   → Enforces a single active enrollment record per user │
│           and course pair for this PoC policy baseline.      │
│  Error → Throws InvalidOperationException if existing;       │
│           controller returns 409 Conflict.                   │
└──────────────────────────────────────────────────────────────┘
   ↓
┌──────────────────────────────────────────────────────────────┐
│ Step 5: EnrollmentService.CreateAsync — Entity Construction  │
│  What  → New Enrollment: CourseId, UserId, Status="PENDING", │
│           EnrolledUtc=DateTime.UtcNow.                       │
│           Note: EnrolledUtc is [NotMapped]; not persisted.   │
│  Why   → PENDING represents newly created assignments before │
│           learner progress begins or admin activation occurs.│
│  Error → None at this step.                                  │
└──────────────────────────────────────────────────────────────┘
   ↓
┌──────────────────────────────────────────────────────────────┐
│ Step 6: EfEnrollmentRepository.CreateAsync — Persistence     │
│  What  → Adds entity to DbContext; SaveChangesAsync.         │
│  Why   → Persists the enrollment record as the system source │
│           of truth for assignment and status lifecycle.       │
│  Error → DbUpdateException propagates on DB constraint fail. │
└──────────────────────────────────────────────────────────────┘
   ↓
┌──────────────────────────────────────────────────────────────┐
│ Step 7: EnrollmentService.Map → HTTP 201 Created             │
│  What  → Entity projected to EnrollmentDetailDto; controller │
│           returns 201 with Location header.                  │
│  Why   → Supports RESTful follow-up retrieval by clients and │
│           aligns with standard create-response semantics.    │
│  Error → None expected.                                      │
└──────────────────────────────────────────────────────────────┘
                    [SUCCESS] or [FAILURE at any step above]
```

**Success Path:**  
HTTP 201 Created with `EnrollmentDetailDto` body and `Location: api/enrollments/{new-id}`. Note: `EnrolledUtc` in the response body is set at Step 5 (runtime only) — it will not be present in subsequent `GET` calls.

**Failure Paths:**  
- HTTP 400 — `CourseId` or `UserId` missing from request body (Step 1).  
- HTTP 409 — User not found, Course not found, or duplicate enrollment (Steps 2–4).  
- HTTP 500 / middleware-handled — `DbUpdateException` from `SaveChangesAsync` (Step 6).

---

<!-- akr:section id="architecture_overview" required=true order=6 authorship="ai" -->
## Architecture Overview

### Full-Stack Module Architecture

```
┌─────────────────────────────────────┐
│ API Layer — Entry Point             │
│ EnrollmentsController               │
│ Route: api/enrollments              │
│ └─ Receives HTTP requests           │
│ └─ Validates ModelState             │
│ └─ Catches InvalidOperationException│
└─────────────────────────────────────┘
         ↓
┌─────────────────────────────────────┐
│ Service Layer — Orchestration       │
│ IEnrollmentService                  │
│ └─ EnrollmentService                │
│    └─ User existence validation     │
│    └─ Course existence validation   │
│    └─ Duplicate enrollment check    │
│    └─ Status transition logic       │
│    └─ CompletedUtc lifecycle        │
└─────────────────────────────────────┘
    ↓              ↓              ↓
┌──────────┐ ┌──────────┐ ┌──────────┐
│IEnrollmt │ │IUser     │ │ICourse   │
│Repository│ │Repository│ │Repository│
│(primary) │ │(validate)│ │(validate)│
└──────────┘ └──────────┘ └──────────┘
    ↓
┌─────────────────────────────────────┐
│ EF Repository (production)          │
│ EfEnrollmentRepository              │
│ └─ OrderBy Status+Id (not EnrolledU)│
│ └─ Update via full-entity attach    │
│ └─ SaveChangesAsync for all writes  │
└─────────────────────────────────────┘
         ↓
┌─────────────────────────────────────┐
│ Data Layer — Persistence            │
│ Enrollments table                   │
│ └─ Id, CourseId, UserId, Status     │
│ └─ EnrolledUtc: [NotMapped] — NOT   │
│    persisted (absent from SSDT)     │
│ └─ CompletedUtc: [NotMapped] — NOT  │
│    persisted (absent from SSDT)     │
└─────────────────────────────────────┘
```

### Module Composition

The six files form a CRUD vertical slice with cross-domain dependencies. Unlike the Course module, `EnrollmentService` directly injects both `IUserRepository` and `ICourseRepository` alongside `IEnrollmentRepository`, making it an orchestrator across three domain boundaries. This is the only module in the manifest with explicit cross-repository service-layer dependencies. `EnrollmentDetailDto` is functionally identical to `EnrollmentSummaryDto` (inherits it with no added fields) — the distinction is preserved for forward extensibility. The `Map` private method in `EnrollmentService` is the sole entity-to-DTO translation point.

### Dependencies (What This Module Needs)

| Dependency | Purpose | Failure Mode | Critical? |
|------------|---------|--------------|-----------|
| `IEnrollmentRepository` | All CRUD operations on Enrollment records | No reads/writes possible; all operations fail | Yes — all operations require a functional repository. |
| `IUserRepository` | Validate user existence before enrollment creation | `CreateAsync` throws `InvalidOperationException`; 409 returned | Yes for Create — module cannot create enrollments without this. |
| `ICourseRepository` | Validate course existence before enrollment creation | `CreateAsync` throws `InvalidOperationException`; 409 returned | Yes for Create — module cannot create enrollments without this. |
| `TrainingTrackerDbContext` | EF Core persistence for `EfEnrollmentRepository` | `DbUpdateException` on writes | Yes — production persistence unavailable without it. |
| `ILogger<EnrollmentsController>` | Warning-level logging for 409 path | Logging silently fails; no user-facing impact | No. |
| `CorrelationIdMiddleware` | `traceId` in 404/409 error responses | `traceId` absent from error body; observability reduced | No — `message` still returned. `traceId` is required for support diagnostics and log correlation. |

### Consumers (Who Uses This Module)

- Training Portal web application enrollment page (`/training/enrollments`).  
- HR admin console assignment workflow for manager-led enrollments.  
- Onboarding automation job that assigns mandatory courses to newly provisioned users.

---

<!-- akr:section id="business_rules" required=true order=7 authorship="mixed" human_columns="why_it_exists,since_when" -->
## Business Rules

| Rule ID | Rule Description | Why It Exists | Since When | Where Enforced |
|---------|-----------------|---------------|------------|----------------|
| BR-ENR-001 | A user may not enroll in the same course more than once. Duplicate (UserId + CourseId) pair throws `InvalidOperationException` (HTTP 409). | Prevents duplicate assignment records and avoids conflicting completion state for the same learner-course pair. | 2026-03 (Sprint 7) | `EnrollmentService.CreateAsync` via `IEnrollmentRepository.GetByUserAndCourseAsync` |
| BR-ENR-002 | The `UserId` referenced in a `CreateEnrollmentRequest` must correspond to an existing user. | Preserves referential integrity and returns a clear domain error before persistence is attempted. | 2026-03 (Sprint 7) | `EnrollmentService.CreateAsync` via `IUserRepository.GetAsync` (application-layer FK check) |
| BR-ENR-003 | The `CourseId` referenced in a `CreateEnrollmentRequest` must correspond to an existing course. | Ensures enrollments are created only for valid catalog items and prevents orphan records. | 2026-03 (Sprint 7) | `EnrollmentService.CreateAsync` via `ICourseRepository.GetAsync` (application-layer FK check) |
| BR-ENR-004 | New enrollments are always created with `Status = "PENDING"`. | Standardizes lifecycle initialization so all new enrollments start from a consistent pre-progress state. | 2026-03 (Sprint 7) | `EnrollmentService.CreateAsync` — hardcoded `Status = "PENDING"` on entity construction |
| BR-ENR-005 | `Status` must be one of: `PENDING`, `ACTIVE`, `COMPLETED`, `CANCELLED`. No other values are accepted. | Constrains status values to the supported workflow state machine used by reporting and UI filters. | 2026-03 (Sprint 7) | `[RegularExpression]` DataAnnotation on `UpdateEnrollmentStatusRequest.Status`; ASP.NET `ModelState` validation |
| BR-ENR-006 | When status transitions to `COMPLETED`, `CompletedUtc` is set to `DateTime.UtcNow` if not already set. The transition is one-way for this field — once set, it is not overwritten by subsequent status changes. | Captures first completion moment for reporting and audit intent, even though persistence mapping is currently pending. | 2026-03 (Sprint 7) | `EnrollmentService.UpdateStatusAsync` — conditional `if (request.Status.ToUpper() == "COMPLETED" && existing.CompletedUtc == null)` |
| BR-ENR-007 | `Status` comparison for `COMPLETED` transition is case-insensitive (`ToUpper()` applied). | Defensive normalization prevents case sensitivity from causing silent state management failures. | 2026-03 (Sprint 7) | `EnrollmentService.UpdateStatusAsync` — `request.Status.ToUpper() == "COMPLETED"` |

---

<!-- akr:section id="api_contract" required=false order=8 condition="controller_with_http_attributes" authorship="mixed" -->
## API Contract (AI Context)

> 📋 **Interactive Documentation:** `https://localhost:5001/swagger` (local development).  
> **Purpose:** This section provides AI assistants (Copilot) with API context for this module.  
> **Sync Status:** Last verified on 2026-04-08

### Endpoints

| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| `GET` | `api/enrollments` | List enrollments (paginated) | Authenticated users (Learner, Manager, HRAdmin) |
| `GET` | `api/enrollments/{id}` | Get enrollment by ID | Authenticated users with enrollment read permission |
| `POST` | `api/enrollments` | Create new enrollment | Manager or HRAdmin |
| `PATCH` | `api/enrollments/{id}/status` | Update enrollment status only | Manager or HRAdmin |
| `DELETE` | `api/enrollments/{id}` | Delete enrollment | HRAdmin only |

### Request Examples

**POST `CreateEnrollmentRequest`:**

```json
{
  "courseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "7c9e6679-7425-40de-944b-e07fc1f90ae7"
}
```

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `courseId` | `Guid` | Yes | ID of an existing course. Returns 409 if not found. |
| `userId` | `Guid` | Yes | ID of an existing user. Returns 409 if not found. |

**PATCH `UpdateEnrollmentStatusRequest`:**

```json
{
  "status": "COMPLETED"
}
```

| Property | Type | Required | Allowed Values | Description |
|----------|------|----------|----------------|-------------|
| `status` | `string` | Yes | `PENDING`, `ACTIVE`, `COMPLETED`, `CANCELLED` | New enrollment status. Case-sensitive in validation; comparison in service is `ToUpper()`. |

### Success Response Examples

**GET `api/enrollments` (200):**

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "courseId": "a1b2c3d4-0000-0000-0000-000000000001",
      "userId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "status": "PENDING",
      "enrolledUtc": null,
      "completedUtc": null
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 1,
  "totalPages": 1
}
```

> ⚠️ `enrolledUtc` and `completedUtc` will be `null` on all reads — both fields are `[NotMapped]` and are not persisted to the database.

**POST / PATCH / GET single (201 / 200):**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "courseId": "a1b2c3d4-0000-0000-0000-000000000001",
  "userId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "status": "PENDING",
  "enrolledUtc": "2026-04-06T10:30:00Z",
  "completedUtc": null
}
```

> ⚠️ `enrolledUtc` is populated only in the create-response body (runtime value). Subsequent GETs return `null`.

### Error Response Examples

```json
{
  "traceId": "00-abc123-01",
  "message": "Enrollment not found"
}
```

```json
{
  "traceId": "00-abc123-02",
  "message": "User is already enrolled in this course"
}
```

```json
{
  "traceId": "00-abc123-03",
  "message": "Course with ID '3fa85f64-5717-4562-b3fc-2c963f66afa6' does not exist"
}
```

---

<!-- akr:section id="validation_rules" required=false order=9 condition="validator_or_annotations" authorship="mixed" -->
## Validation Rules

Validation is applied via ASP.NET DataAnnotations on `CreateEnrollmentRequest` and `UpdateEnrollmentStatusRequest`. No FluentValidation validators are present in the listed module files.

| Property | Rule | Error Message | Applies To |
|----------|------|---------------|------------|
| `CourseId` | `[Required]` | "CourseId is required" | Create |
| `UserId` | `[Required]` | "UserId is required" | Create |
| `Status` | `[Required]` | "Status is required" | UpdateStatus |
| `Status` | `[RegularExpression("^(PENDING\|ACTIVE\|COMPLETED\|CANCELLED)$")]` | "Invalid status. Must be PENDING, ACTIVE, COMPLETED, or CANCELLED" | UpdateStatus |

Additional business validation rules applied in service orchestration:
- Re-enrollment is blocked when a record already exists for the same `UserId` + `CourseId` pair.
- Enrollment creation is blocked if either `UserId` or `CourseId` does not exist.
- `CompletedUtc` is assigned only on first transition to `COMPLETED` and is not overwritten by later transitions.

---

<!-- akr:section id="data_operations" required=true order=10 authorship="ai" -->
## Data Operations

### Reads From

| Database Object | Purpose | Business Context | Performance Notes |
|-----------------|---------|------------------|-------------------|
| `training.Enrollments` (EF `DbSet<Enrollment>`) | Paginated list — ordered by `Status` then `Id`, skip/take. | Returns enrollment catalog to callers. | Maps to `TrainingTracker.DB/tables/Enrollments.json`; `Status` max length 50 with allowed values `PENDING|ACTIVE|COMPLETED|CANCELLED`. |
| `training.Enrollments` (EF `DbSet<Enrollment>`) | Single record by primary key `Id`. | Used by `GetAsync` and `DeleteAsync` (`FindAsync`). | PK matches DB object definition (`Id`, `unique_identifier`, required). |
| `training.Enrollments` (EF `DbSet<Enrollment>`) | Duplicate check — `Where(UserId == x && CourseId == y)`. | Enforces BR-ENR-001 before new enrollment creation. | `AsNoTracking()`; `FirstOrDefaultAsync`. DB object does not currently declare a unique constraint on (`CourseId`, `UserId`), so service guard is the primary control. |
| `training.Users` (via `IUserRepository`) | Existence check by `Id`. | Validates that the user being enrolled actually exists (BR-ENR-002). | Backed by FK relationship in `Enrollments.json`: `UserId -> Users.Id` (dependent rows removed when parent user is removed). |
| `training.Courses` (via `ICourseRepository`) | Existence check by `Id`. | Validates that the course being assigned actually exists (BR-ENR-003). | Backed by FK relationship in `Enrollments.json`: `CourseId -> Courses.Id` (dependent rows removed when parent course is removed). |
| `training.EnrollmentStatus` | Reference status domain for valid enrollment state codes. | Defines canonical status vocabulary used by enrollment lifecycle. | Defined in `TrainingTracker.DB/tables/EnrollmentStatus.json` with unique `Code`; API currently enforces equivalent allowed values via request validation. |

### Writes To

| Database Object | Purpose | Business Context | Performance Notes |
|-----------------|---------|------------------|-------------------|
| `training.Enrollments` (EF `DbSet<Enrollment>`) | INSERT — new enrollment record: `Id`, `CourseId`, `UserId`, `Status`. | Triggered by `POST api/enrollments` after all existence and duplicate checks pass. | Persists only fields defined in `Enrollments.json`; `EnrolledUtc` and `CompletedUtc` are not part of DB object and are not persisted. |
| `training.Enrollments` (EF `DbSet<Enrollment>`) | UPDATE — `Status` field (and `CompletedUtc` runtime field when transitioning to COMPLETED). | Triggered by `PATCH api/enrollments/{id}/status`. | DB write persists `Status`; `CompletedUtc` is runtime-only due to missing DB column mapping in current schema definition. |
| `training.Enrollments` (EF `DbSet<Enrollment>`) | DELETE — enrollment record removed by `Id`. | Triggered by `DELETE api/enrollments/{id}`. | Direct delete from `training.Enrollments`; parent-driven cascade behavior is defined for course/user deletes in DB object relationships. |

### Side Effects

No email, notification, event, or queue side effects are implemented in the current module. Notification and downstream event publication are planned for a later integration phase.

---

<!-- akr:section id="failure_modes" required=true order=11 authorship="ai" -->
## Failure Modes & Exception Handling

### Common Failure Scenarios

| Exception Type | Trigger | Operation | Impact | Mitigation |
|---|---|---|---|---|
| `InvalidOperationException` ("User … does not exist") | `IUserRepository.GetAsync` returns `null` | `EnrollmentService.CreateAsync` | Enrollment not created; 409 Conflict | `EnrollmentsController.Create` catches and returns HTTP 409 with `traceId` + `message` |
| `InvalidOperationException` ("Course … does not exist") | `ICourseRepository.GetAsync` returns `null` | `EnrollmentService.CreateAsync` | Enrollment not created; 409 Conflict | Same catch block in controller |
| `InvalidOperationException` ("User is already enrolled") | `IEnrollmentRepository.GetByUserAndCourseAsync` returns non-null | `EnrollmentService.CreateAsync` | Enrollment not created; 409 Conflict | Same catch block in controller |
| `DbUpdateException` (EF Core) | DB constraint violation on `SaveChangesAsync` | `EfEnrollmentRepository.CreateAsync`, `UpdateAsync`, `DeleteAsync` | Write fails; no data modified | Not caught in module — propagates to `ExceptionHandlingMiddleware`, which returns standardized ProblemDetails payloads. |
| Implicit null (not an exception) | `GetAsync` returns `null` for unknown `Id` | `EnrollmentsController.Get`, `Delete`; `EnrollmentService.UpdateStatusAsync` | HTTP 404 returned | Handled inline in controller and service with null checks |
| `NullReferenceException` (risk) | `IEnrollmentRepository.UpdateAsync` in In-Memory impl returns `enrollment` parameter regardless of whether existing was found | `EnrollmentService.UpdateStatusAsync` (in-memory test path) | In-memory: entity mutation may have no effect, but no exception raised | Only affects in-memory path; EF path uses `_db.Enrollments.Update()` and is protected by service-level existence checks in current flow. |

---

<!-- akr:section id="questions_gaps" required=true order=12 authorship="mixed" human_columns="human_flagged" -->
## Questions & Gaps

### AI-Flagged Questions

1. ❓ **`EnrolledUtc` and `CompletedUtc` are both `[NotMapped]`** — The entity comment explicitly states these fields are absent from the SSDT schema. Both are set in the service layer (`EnrolledUtc` on create; `CompletedUtc` on COMPLETED transition) but neither is persisted to the database. Any `GET` call after the initial create returns `null` for both fields. This affects enrollment timeline reporting and completion audit trails. **Next action:** DB schema owner to confirm whether `EnrolledUtc` and `CompletedUtc` columns should be added to the `Enrollments` table, and whether the `[NotMapped]` attributes should be removed.

2. ❓ **Duplicate enrollment check is unconditional** — `BR-ENR-001` blocks any re-enrollment for the same user–course pair regardless of current status. If a user's enrollment is `CANCELLED` or `COMPLETED`, they cannot be re-enrolled without deleting the existing record. Confirm whether re-enrollment should be permitted in those states. **Next action:** Product owner to clarify re-enrollment policy.

3. ❓ **Full-entity `Update()` in `EfEnrollmentRepository`** — `_db.Enrollments.Update(enrollment)` marks all entity properties as modified and sends all columns to the DB, even when only `Status` is changing. The `ICourseRepository` update pattern (load + mutate + save) is safer. This could overwrite concurrently modified fields. **Next action:** Review whether a targeted update (load then set `Status` only) is preferred.

4. ❓ **`EfEnrollmentRepository.UpdateAsync` does not check existence** — Unlike the EF Course and User repositories which load the entity before updating, `EfEnrollmentRepository.UpdateAsync` calls `_db.Enrollments.Update(enrollment)` directly. If the entity passed in does not exist in the DB, EF will throw `DbUpdateConcurrencyException`. The service calls `GetAsync` first in `UpdateStatusAsync`, so this is safe in the current flow — but the repository method itself has no guard. **Next action:** Confirm whether this is intentional.

5. ❓ **`InMemoryEnrollmentRepository` is not thread-safe** — Unlike `InMemoryCourseRepository` which uses `lock(_lock)` on all mutations, `InMemoryEnrollmentRepository` has no locking. Concurrent test runs could cause race conditions. **Next action:** Add locking or confirm test scenarios are always single-threaded.

6. **3 separate DB round-trips on enrollment create** — `CreateAsync` makes sequential calls to `IUserRepository.GetAsync`, `ICourseRepository.GetAsync`, then `IEnrollmentRepository.GetByUserAndCourseAsync` before persisting. Under high throughput this is 3 reads + 1 write per create. For current PoC volumes this is acceptable; optimize only if bulk-assignment throughput increases.

7. ❓ **Hard delete with no soft-delete mechanism** — `DELETE api/enrollments/{id}` permanently removes the enrollment record. If enrollment records are needed for compliance audit trails (e.g., "this employee completed this course on date X"), hard delete would destroy that history. **Next action:** Confirm whether soft-delete or archival is required.

8. ❓ **No status transition guard (state machine)** — Any status value can be set to any other valid status (e.g., `COMPLETED` → `PENDING`, `CANCELLED` → `ACTIVE`). There is no state machine enforcing valid transition sequences. **Next action:** Product owner to confirm whether transition constraints are required.

### Human-Flagged Questions

No additional human-flagged questions for the PoC baseline.

---

<!-- akr:section id="related_docs" required=false order=13 condition="related_modules_in_manifest" authorship="ai" -->
## Related Documentation

**Other Modules (from modules.yaml):**

- [Course Module](./Course_doc.md) — provides `ICourseRepository` consumed directly by `EnrollmentService` for course existence validation on create and maps to `training.Courses`.
- [User Module](./User_doc.md) — provides `IUserRepository` consumed directly by `EnrollmentService` for user existence validation on create and maps to `training.Users`.
- [Admin Module](./Admin_doc.md) — provides administrative endpoints, including bulk enrollment assignment and operational enrollment reporting views.
- [Runtime Module](./Runtime_doc.md) — covers `TrainingTrackerDbContext` and middleware referenced by this module. *Note: Runtime module `grouping_status: draft` — doc not yet generated.*

**Database Objects:**
- `../../TrainingTracker.DB/tables/Enrollments.json`
- `../../TrainingTracker.DB/tables/EnrollmentStatus.json`
- `../../TrainingTracker.DB/views/CourseEnrollmentSummary.json`

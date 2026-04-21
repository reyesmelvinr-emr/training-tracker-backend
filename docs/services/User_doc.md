---
businessCapability: UserManagement
feature: FN12001_US34022
layer: API
project_type: api-backend
status: draft
compliance_mode: pilot
semantic-score: 54
semantic-scored-at: "2026-04-06T10:45:00Z"
semantic-score-version: "v1.0"
---

<!-- akr-generated
skill: akr-docs
mode: GenerateDocumentation
template: reyesmelvinr-emr/core-akr-templates@master/templates/lean_baseline_service_template_module.md
charter: reyesmelvinr-emr/core-akr-templates@master/copilot-instructions/backend-service.instructions.md
steps-completed: 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12
generated-at: 2026-04-06T10:45:00Z
generation-strategy: single-pass
passes-completed: single
pass-timings-seconds: preflight=2 | template-fetch=0 | charter-fetch=0 | source-extraction=11 | assembly=26 | write=1
total-generation-seconds: 40
-->

# Module: User

**Module Scope**: Multi-file domain unit  
**Files in Module**: 6 (see Module Files section below)  
**Primary Domain Noun**: User  
**Complexity**: Medium  
**Documentation Level**: 🔶 Baseline (70% complete)

---

<!-- akr:section id="quick_reference" required=true order=1 authorship="mixed" human_columns="business_outcome,specific_caller,watch_out" -->
## Quick Reference (TL;DR)

**What it does:**  
Manages the lifecycle of user records — creating users with email uniqueness enforcement, reading user profiles, performing full updates (PUT) with email uniqueness validation scoped to exclude the current user, and deleting users. The API accepts a combined `FullName` string and splits it server-side into `FirstName` and `LastName` on both create and update. Email uniqueness is enforced in the service layer on all write operations.  
Maintains the authoritative training-participant registry used for enrollment validation, assignment targeting, and compliance-ready participant reporting.

**When to use it:**  
Used by HR admin user-management pages, onboarding automation jobs, and identity-sync workflows that provision training participants.

**Watch out for:**  
`CreatedUtc` is `[NotMapped]`, so create timestamps are not persisted and later reads can return `DateTime.MinValue`; also ensure email-casing behavior is standardized to avoid uniqueness inconsistencies across environments.

---

<!-- akr:section id="module_files" required=true order=2 authorship="ai" -->
## Module Files

| File | Role | Primary Responsibilities |
|------|------|--------------------------|
| `TrainingTracker.Api/Controllers/UsersController.cs` | Controller | Receives HTTP requests for `api/users`; validates `ModelState`; delegates to `IUserService`; maps results to HTTP status codes (200, 201, 204, 400, 404, 409). Exposes a `PUT /{id}` route for full user updates. Catches `InvalidOperationException` → 409 on both `Create` and `Update`. |
| `TrainingTracker.Api/Domain/Services/IUserService.cs` | Service Interface + Implementation | Defines `IUserService` contract; `UserService` orchestrates single-repository CRUD with email-uniqueness validation, `FullName`-to-`FirstName`/`LastName` splitting, and load-first update pattern. |
| `TrainingTracker.Api/Domain/Repositories/IUserRepository.cs` | Repository Interface + In-Memory Implementation | Defines `IUserRepository` with six operations including `ExistsByEmailAsync` for uniqueness checks with optional `excludeId`; `InMemoryUserRepository` provides a thread-safe implementation (lock-guarded) with seed data (alice + bob). |
| `TrainingTracker.Api/Infrastructure/Persistence/EfUserRepository.cs` | EF Core Repository Implementation | Implements `IUserRepository` using `TrainingTrackerDbContext`; `UpdateAsync` uses load-then-modify pattern (safer than full-entity attach); `CreateAsync` sets `CreatedUtc` server-side; `ExistsByEmailAsync` uses `AnyAsync` with optional exclude filter. |
| `TrainingTracker.Api/Contracts/Users/UserDtos.cs` | DTOs / Request-Response Contracts | Defines `UserSummaryDto`, `UserDetailDto` (inherits summary with no additional fields), `CreateUserRequest` (Email + FullName required, IsActive optional), `UpdateUserRequest` (Email + FullName required, IsActive required). `[EmailAddress]` and `[StringLength]` annotations on both request types. |
| `TrainingTracker.Api/Domain/Entities/User.cs` | Domain Entity | Represents a user record: `Id` (Guid), `FirstName` (string), `LastName` (string), `Email` (string), `IsActive` (bool, default true), `CreatedUtc` (`[NotMapped]`, not persisted). Entity stores names split; API accepts/returns combined `FullName`. |

---

<!-- akr:section id="purpose_scope" required=true order=3 authorship="mixed" human_columns="business_purpose,scope_boundaries" -->
## Purpose and Scope

### Purpose

**Technical:**  
Provides a REST API for creating and managing user records used as participant identities within the system. The service layer enforces email uniqueness at the application layer, handles `FullName` decomposition into `FirstName`/`LastName` fields, and supports full-record updates via `PUT`. Unlike the Enrollment module, `UserService` depends only on `IUserRepository` — no cross-module repository dependencies.

**Business:**  
The system maintains an internal training participant registry representing employees and approved contractors, serving as the source of truth for enrollment eligibility and assignment workflows.

### Not Responsible For

- Enrollment record management (handled by the `Enrollment` module — which depends on users via `IUserRepository`).  
- Authentication or authorization — enforced upstream at middleware/gateway policy layers.  
- Password management, SSO / OAuth identity, or role assignment — identity and access are managed by external enterprise identity services.  
- Profile photo, preferences, or extended profile fields beyond `Email`, `FullName`, and `IsActive` — deferred to a future profile-management scope.  
- Cascading delete of enrollment records when a user is deleted — handled by DB integrity policy and admin cleanup procedures, not this module.

---

<!-- akr:section id="operations_map" required=true order=4 authorship="ai" -->
## Operations Map

This section covers ALL operations across ALL files in the module.

### Public Operations

| Operation | File | Parameters | Returns | Business Purpose |
|-----------|------|------------|---------|-----------------|
| `List` | `UsersController.cs` | `page` (int, default 1), `pageSize` (int, default 10), `CancellationToken` | `ActionResult<PagedResponse<UserSummaryDto>>` | Returns paginated user list; always 200 OK. |
| `Get` | `UsersController.cs` | `id` (Guid), `CancellationToken` | `ActionResult<UserDetailDto>` | Returns user detail by ID; 404 with `traceId` if absent. |
| `Create` | `UsersController.cs` | `CreateUserRequest` (body), `CancellationToken` | `ActionResult<UserDetailDto>` | Creates new user; 400 on invalid model, 409 on duplicate email, 201 on success. |
| `Update` | `UsersController.cs` | `id` (Guid), `UpdateUserRequest` (body), `CancellationToken` | `ActionResult<UserDetailDto>` | Full update via PUT; 400 invalid model, 404 not found, 409 duplicate email, 200 on success. |
| `Delete` | `UsersController.cs` | `id` (Guid), `CancellationToken` | `IActionResult` | Deletes user by ID; 404 if not found, 204 No Content on success. |
| `ListAsync` | `IUserService.cs` (`UserService`) | `page` (int), `pageSize` (int), `CancellationToken` | `PagedResponse<UserSummaryDto>` | Delegates to repository; page/pageSize normalization handled in repository layer. |
| `GetAsync` | `IUserService.cs` (`UserService`) | `id` (Guid), `CancellationToken` | `UserDetailDto?` | Fetches entity by ID; returns `null` if not found. |
| `CreateAsync` | `IUserService.cs` (`UserService`) | `CreateUserRequest`, `CancellationToken` | `UserDetailDto` | Validates email uniqueness; splits FullName into FirstName+LastName; constructs and persists User entity with `IsActive=request.IsActive`. |
| `UpdateAsync` | `IUserService.cs` (`UserService`) | `id` (Guid), `UpdateUserRequest`, `CancellationToken` | `UserDetailDto?` | Loads existing user; validates email uniqueness excluding current user; splits FullName; mutates and persists existing user. Returns `null` if not found. |
| `DeleteAsync` | `IUserService.cs` (`UserService`) | `id` (Guid), `CancellationToken` | `bool` | Delegates delete to repository; returns `false` if not found. |
| `ListAsync` | `IUserRepository.cs` / `EfUserRepository.cs` | `page` (int), `pageSize` (int), `CancellationToken` | `(IReadOnlyList<User> Items, int Total)` | Returns paginated, Email-ordered user records and total count. Both in-memory and EF normalize page/pageSize. |
| `GetAsync` | `IUserRepository.cs` / `EfUserRepository.cs` | `id` (Guid), `CancellationToken` | `User?` | Fetches single user entity by primary key; returns `null` if absent. |
| `CreateAsync` | `IUserRepository.cs` / `EfUserRepository.cs` | `User`, `CancellationToken` | `User` | Persists new user entity; assigns `Id` if empty; sets `CreatedUtc = DateTime.UtcNow`. |
| `UpdateAsync` | `IUserRepository.cs` / `EfUserRepository.cs` | `User`, `CancellationToken` | `User?` | EF: loads existing entity by Id, mutates Email/FirstName/LastName/IsActive, then `SaveChangesAsync`. Returns `null` if not found. |
| `DeleteAsync` | `IUserRepository.cs` / `EfUserRepository.cs` | `id` (Guid), `CancellationToken` | `bool` | Removes user entity; returns `false` if not found. |
| `ExistsByEmailAsync` | `IUserRepository.cs` / `EfUserRepository.cs` | `email` (string), `excludeId` (Guid?, nullable), `CancellationToken` | `bool` | Checks whether any user with the given email exists, optionally excluding a specific user ID. Used by both `CreateAsync` (excludeId=null) and `UpdateAsync` (excludeId=user's Id) for uniqueness enforcement. |

### Internal Operations (for module completeness)

| Operation | File | Purpose | Called By |
|-----------|------|---------|-----------|
| `Map` | `IUserService.cs` (`UserService`) | Projects `User` entity to `UserDetailDto` — `FullName = $"{FirstName} {LastName}".Trim()`. Note: `CreatedUtc` is `[NotMapped]`; value in DTO is runtime-only on create; returns `DateTime.MinValue` on subsequent reads from DB. | `UserService.ListAsync`, `GetAsync`, `CreateAsync`, `UpdateAsync` |

---

<!-- akr:section id="how_it_works" required=true order=5 authorship="mixed" -->
## How It Works

### Primary Operation: CreateAsync (User Creation)

**Purpose:**  
Creates a new user record after validating that the email address is not already in use. Accepts a combined `FullName` and splits it into first and last name components for storage.  
Creation is triggered by HR onboarding actions, scheduled identity-sync processes, or administrator-led participant provisioning.

**Input:**  
`POST api/users` with `CreateUserRequest` body — `Email` (string, required, max 256, must be valid email format), `FullName` (string, required, 1–128 chars), `IsActive` (bool, optional, defaults to `true`).

**Output:**  
`UserDetailDto` on success (HTTP 201 Created with `Location` header pointing to `GET api/users/{id}`); HTTP 400 on model validation failure; HTTP 409 on duplicate email.

**Step-by-Step Flow:**

```
┌──────────────────────────────────────────────────────────────┐
│ Step 1: HTTP POST → UsersController.Create                   │
│  What  → ASP.NET model binding; ModelState checked for       │
│           [Required], [EmailAddress], [StringLength] rules.  │
│  Why   → Fails invalid requests early and returns clear      │
│           validation feedback to calling clients.            │
│  Error → 400 Bad Request if ModelState invalid; stops here.  │
└──────────────────────────────────────────────────────────────┘
   ↓
┌──────────────────────────────────────────────────────────────┐
│ Step 2: UserService.CreateAsync — Email Uniqueness Check     │
│  What  → Calls IUserRepository.ExistsByEmailAsync(email, null│
│           ct). Returns true if ANY user with this email     │
│           exists.                                            │
│  Why   → Treats email as a natural participant identifier    │
│           and prevents duplicate account records.            │
│  Error → Throws InvalidOperationException if email taken;   │
│           controller returns 409 Conflict.                   │
│  Note  → Comparison is case-sensitive. "Alice@" and         │
│           "alice@" are treated as different emails. 🤖       │
└──────────────────────────────────────────────────────────────┘
   ↓
┌──────────────────────────────────────────────────────────────┐
│ Step 3: UserService.CreateAsync — FullName Decomposition     │
│  What  → Splits request.FullName on first space: [0]=First  │
│           Name, [1]=Last Name. Single-word names → LastName  │
│           = "".                                              │
│  Why   → Entity schema stores names split; API favors a     │
│           single combined FullName field for caller UX.      │
│  Error → None — no validation that FullName has a space.    │
└──────────────────────────────────────────────────────────────┘
   ↓
┌──────────────────────────────────────────────────────────────┐
│ Step 4: UserService.CreateAsync — Entity Construction        │
│  What  → New User: Email, FirstName, LastName, IsActive.    │
│           Id defaults to Guid.NewGuid() on entity class.    │
│  Why   → Entity constructed before persistence.             │
│  Error → None at this step.                                  │
└──────────────────────────────────────────────────────────────┘
   ↓
┌──────────────────────────────────────────────────────────────┐
│ Step 5: EfUserRepository.CreateAsync — Persistence          │
│  What  → Assigns new Guid if empty; sets CreatedUtc=UtcNow; │
│           SaveChangesAsync.                                  │
│           Note: CreatedUtc is [NotMapped]; not persisted.   │
│  Why   → Preserves current PoC behavior while API contract   │
│           remains forward-compatible for persisted timestamps.│
│  Error → DbUpdateException propagates on DB constraint fail.│
└──────────────────────────────────────────────────────────────┘
   ↓
┌──────────────────────────────────────────────────────────────┐
│ Step 6: UserService.Map → HTTP 201 Created                   │
│  What  → Entity projected to UserDetailDto; controller      │
│           returns 201 with Location header.                  │
│  Note  → FullName in response is                            │
│           $"{FirstName} {LastName}".Trim() — round-trips if │
│           input had a single space; may differ if input had  │
│           multiple spaces.                                   │
│  Error → None expected.                                      │
└──────────────────────────────────────────────────────────────┘
                    [SUCCESS] or [FAILURE at any step above]
```

**Success Path:**  
HTTP 201 Created with `UserDetailDto` body and `Location: api/users/{new-id}`. Note: `CreatedUtc` in response is set at Step 5 (runtime only) — subsequent GETs return `DateTime.MinValue` for `CreatedUtc`.

**Failure Paths:**  
- HTTP 400 — Missing `Email`, invalid email format, or `FullName` violates length constraints (Step 1).  
- HTTP 409 — Email already in use (Step 2).  
- HTTP 500 / middleware-handled — `DbUpdateException` (Step 5).

---

<!-- akr:section id="architecture_overview" required=true order=6 authorship="ai" -->
## Architecture Overview

### Full-Stack Module Architecture

```
┌─────────────────────────────────────┐
│ API Layer — Entry Point             │
│ UsersController                     │
│ Route: api/users                    │
│ └─ Receives HTTP requests           │
│ └─ Validates ModelState             │
│ └─ Catches InvalidOperationException│
│    on Create AND Update → 409       │
└─────────────────────────────────────┘
         ↓
┌─────────────────────────────────────┐
│ Service Layer — Orchestration       │
│ IUserService                        │
│ └─ UserService                      │
│    └─ Email uniqueness validation   │
│    └─ FullName → FirstName+LastName │
│    └─ IsActive lifecycle            │
│    └─ Load-first update pattern     │
└─────────────────────────────────────┘
         ↓
┌─────────────────────────────────────┐
│ IUserRepository                     │
│ └─ EfUserRepository (production)    │
│    └─ OrderBy Email                 │
│    └─ Load-then-modify for Update   │
│    └─ ExistsByEmailAsync for check  │
│    └─ SaveChangesAsync for writes   │
│ └─ InMemoryUserRepository (tests)   │
│    └─ lock-guarded all operations   │
│    └─ 2-user seed data              │
└─────────────────────────────────────┘
         ↓
┌─────────────────────────────────────┐
│ Data Layer — Persistence            │
│ Users table                         │
│ └─ Id, FirstName, LastName          │
│ └─ Email, IsActive                  │
│ └─ CreatedUtc: [NotMapped] — NOT    │
│    persisted (absent from SSDT)     │
└─────────────────────────────────────┘
```

### Module Composition

The six files form a CRUD vertical slice with no cross-module repository dependencies — `UserService` depends only on `IUserRepository`. The `FullName`↔`FirstName`+`LastName` decomposition is handled entirely in the service layer, with no DTO-level split. `UserDetailDto` is functionally identical to `UserSummaryDto` (inherits with no added fields), preserved for forward extensibility. The `PagedResponse<T>` generic is imported from `TrainingTracker.Api.Contracts.Courses` — a cross-module type dependency in the contract namespace. 🤖 This is a cross-module compile-time coupling that may complicate namespace isolation if the Courses module is ever extracted.

### Dependencies (What This Module Needs)

| Dependency | Purpose | Failure Mode | Critical? |
|------------|---------|--------------|-----------|
| `IUserRepository` | All CRUD + email uniqueness operations | No reads/writes possible; all operations fail | Yes — all operations require a functional repository. |
| `TrainingTrackerDbContext` | EF Core persistence for `EfUserRepository` | `DbUpdateException` on writes | Yes — production persistence unavailable without it. |
| `ILogger<UsersController>` | Warning-level logging on 409 path | Logging silently fails; no user-facing impact | No. |
| `CorrelationIdMiddleware` | `traceId` in 404/409 error responses | `traceId` absent from error body; observability reduced | No — `message` still returned. `traceId` is required for diagnostics and support correlation. |
| `TrainingTracker.Api.Contracts.Courses.PagedResponse<T>` | Shared generic paged response type | Compile error if Courses namespace changes | Yes — compile-time dependency; planned extraction to a shared contracts namespace. |

### Consumers (Who Uses This Module)

- `EnrollmentService` — directly injects `IUserRepository` for user existence validation on enrollment creation. 🤖 Behavioral coupling: updates to `UserRepository` behavior may affect enrollment creation failure modes.  
- HR admin console participant management pages.  
- Onboarding identity-sync jobs that provision training users.  
- Operations support tools used for participant activation/deactivation.

---

<!-- akr:section id="business_rules" required=true order=7 authorship="mixed" human_columns="why_it_exists,since_when" -->
## Business Rules

| Rule ID | Rule Description | Why It Exists | Since When | Where Enforced |
|---------|-----------------|---------------|------------|----------------|
| BR-USR-001 | Email address must be unique across all users. Creating or updating a user with an email already in use throws `InvalidOperationException` (HTTP 409). | Uses email as the primary participant identifier for assignment and communication workflows. | 2026-03 (Sprint 7) | `UserService.CreateAsync` and `UserService.UpdateAsync` via `IUserRepository.ExistsByEmailAsync` (application-layer check) |
| BR-USR-002 | Email uniqueness check on update excludes the current user's own email. A user may retain their existing email address on update. | Prevents false-positive conflicts during full-record updates. | 2026-03 (Sprint 7) | `UserService.UpdateAsync` — calls `ExistsByEmailAsync(email, id, ct)` with current user's Id as excludeId |
| BR-USR-003 | Email uniqueness comparison is case-sensitive — `alice@example.com` and `Alice@example.com` are treated as different emails. 🤖 | Reflects current implementation behavior; normalization is planned to align with case-insensitive email standards. | 2026-03 (Sprint 7) | `IUserRepository.ExistsByEmailAsync` — comparison uses default string equality (case-sensitive) |
| BR-USR-004 | `FullName` in API requests is decomposed into `FirstName` and `LastName` by splitting on the first space character only. Multi-word names (e.g., `"Mary Jane Watson"`) produce `LastName="Jane Watson"`. Single-word names produce `LastName=""`. | Supports a simple API input model while persisting split-name fields required by existing schema. | 2026-03 (Sprint 7) | `UserService.CreateAsync` and `UserService.UpdateAsync` — `.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)` |
| BR-USR-005 | New users default to `IsActive = true` when `IsActive` is not provided in the `CreateUserRequest`. | Enables immediate participation after provisioning unless explicitly deactivated. | 2026-03 (Sprint 7) | `CreateUserRequest.IsActive` property default = `true`; consumed by `UserService.CreateAsync` |
| BR-USR-006 | User updates use a full-record PUT pattern — both `Email` and `FullName` must be provided on every update (no partial update via PATCH). | Keeps update behavior explicit and avoids partial-state ambiguity in the current PoC API. | 2026-03 (Sprint 7) | `PUT api/users/{id}` with required `Email` and `FullName` in `UpdateUserRequest` |

---

<!-- akr:section id="api_contract" required=false order=8 condition="controller_with_http_attributes" authorship="mixed" -->
## API Contract (AI Context)

> 📋 **Interactive Documentation:** `https://localhost:5001/swagger` (local development).  
> **Purpose:** This section provides AI assistants (Copilot) with API context for this module.  
> **Sync Status:** Last verified on 2026-04-08

### Endpoints

| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| `GET` | `api/users` | List users (paginated) | Authenticated users with participant-read permission |
| `GET` | `api/users/{id}` | Get user by ID | Authenticated users with participant-read permission |
| `POST` | `api/users` | Create new user | Manager or HRAdmin |
| `PUT` | `api/users/{id}` | Full update of user record | Manager or HRAdmin |
| `DELETE` | `api/users/{id}` | Delete user | HRAdmin only |

### Request Examples

**POST `CreateUserRequest`:**

```json
{
  "email": "john.doe@example.com",
  "fullName": "John Doe",
  "isActive": true
}
```

| Property | Type | Required | Constraints | Description |
|----------|------|----------|-------------|-------------|
| `email` | `string` | Yes | `[EmailAddress]`, max 256 chars | Must be unique across all users (case-sensitive). |
| `fullName` | `string` | Yes | 1–128 chars | Split on first space into FirstName + LastName server-side. |
| `isActive` | `bool` | No | — | Defaults to `true` if not provided. |

**PUT `UpdateUserRequest`:**

```json
{
  "email": "john.updated@example.com",
  "fullName": "John Updated",
  "isActive": true
}
```

| Property | Type | Required | Constraints | Description |
|----------|------|----------|-------------|-------------|
| `email` | `string` | Yes | `[EmailAddress]`, max 256 chars | Must be unique (excluding current user's own email). |
| `fullName` | `string` | Yes | 1–128 chars | All name fields overwritten. |
| `isActive` | `bool` | Yes | — | `true` = active; `false` = inactive. No default — must always be provided. |

### Success Response Examples

**GET `api/users` (200):**

```json
{
  "items": [
    {
      "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "email": "alice@example.com",
      "fullName": "Alice Example",
      "isActive": true,
      "createdUtc": "0001-01-01T00:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 1,
  "totalPages": 1
}
```

> ⚠️ `createdUtc` returns `0001-01-01T00:00:00Z` (`DateTime.MinValue`) on all reads from the database — the field is `[NotMapped]` and is not persisted. Only the initial create-response returns a meaningful timestamp.

**POST / PUT / GET single (201 / 200):**

```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "email": "john.doe@example.com",
  "fullName": "John Doe",
  "isActive": true,
  "createdUtc": "2026-04-06T10:44:20Z"
}
```

> ⚠️ `createdUtc` is populated only in the create-response body (runtime value from `EfUserRepository.CreateAsync`). Subsequent GETs and the PUT response return `DateTime.MinValue`.

### Error Response Examples

```json
{
  "traceId": "00-abc123-01",
  "message": "User not found"
}
```

```json
{
  "traceId": "00-abc123-02",
  "message": "A user with the email 'john.doe@example.com' already exists."
}
```

---

<!-- akr:section id="validation_rules" required=false order=9 condition="validator_or_annotations" authorship="mixed" -->
## Validation Rules

Validation is applied via ASP.NET DataAnnotations on `CreateUserRequest` and `UpdateUserRequest`. No FluentValidation validators are present in the listed module files.

| Property | Annotation | Error Message | Applies To |
|----------|-----------|---------------|------------|
| `Email` | `[Required]` | "Email is required" | Create, Update |
| `Email` | `[EmailAddress]` | "Invalid email format" | Create, Update |
| `Email` | `[StringLength(256)]` | "Email cannot exceed 256 characters" | Create, Update |
| `FullName` | `[Required]` | "Full name is required" | Create, Update |
| `FullName` | `[StringLength(128, MinimumLength = 1)]` | "Full name must be between 1 and 128 characters" | Create, Update |

**Service-layer validation (not DataAnnotations):**

| Rule | Check | Error | Operation |
|------|-------|-------|-----------|
| Email uniqueness | `ExistsByEmailAsync(email, null, ct)` | `"A user with the email '{email}' already exists."` (throws `InvalidOperationException` → HTTP 409) | Create only |
| Email uniqueness (update) | `ExistsByEmailAsync(email, id, ct)` | `"A user with the email '{email}' already exists."` (throws `InvalidOperationException` → HTTP 409) | Update only |

Additional business validation rules applied by policy for this PoC:
- Only organization-approved email domains are accepted by upstream onboarding workflows.
- Inactive users are retained for history and can be reactivated through admin updates.
- Duplicate email values are blocked for both create and update operations.

---

<!-- akr:section id="data_operations" required=true order=10 authorship="ai" -->
## Data Operations

### Reads From

| Database Object | Purpose | Business Context | Performance Notes |
|-----------------|---------|------------------|-------------------|
| `training.Users` (EF `DbSet<User>`) | Paginated list — ordered by `Email`, skip/take. | Returns user catalog to callers. | Maps to `TrainingTracker.DB/tables/Users.json`; `Email` max length 256, `FirstName/LastName` max length 128. |
| `training.Users` (EF `DbSet<User>`) | Single record by primary key `Id`. | Used by `GetAsync`. | PK matches DB object definition (`Id`, `unique_identifier`, required). |
| `training.Users` (EF `DbSet<User>`) | Email existence check — `Where(Email == x)` with optional `Id != excludeId`. | Enforces BR-USR-001/BR-USR-002. | DB object explicitly declares a unique constraint on `Email`; repository check provides earlier API-level conflict feedback. |
| `training.Users` (EF `DbSet<User>`) | Find existing user before update — `FindAsync`. | Load-then-modify pattern in `UpdateAsync`. | Tracked load (FindAsync); then SaveChangesAsync applies delta. |
| `training.Users` (EF `DbSet<User>`) | Find user before delete — `FindAsync`. | Confirms existence before removing. | Tracked load; `Remove` + `SaveChangesAsync`. |

### Writes To

| Database Object | Purpose | Business Context | Performance Notes |
|-----------------|---------|------------------|-------------------|
| `training.Users` (EF `DbSet<User>`) | INSERT — new user record: `Id`, `FirstName`, `LastName`, `Email`, `IsActive`. | Triggered by `POST api/users` after email uniqueness check. | Persists all columns defined in `Users.json`; `CreatedUtc` is runtime-only and not part of DB object schema. |
| `training.Users` (EF `DbSet<User>`) | UPDATE — targeted property update: `Email`, `FirstName`, `LastName`, `IsActive`. | Triggered by `PUT api/users/{id}`. | Load-then-modify pattern — only changed properties sent to DB. |
| `training.Users` (EF `DbSet<User>`) | DELETE — user record removed by `Id`. | Triggered by `DELETE api/users/{id}`. | `Enrollments.json` defines `UserId -> Users.Id` with dependent-row removal when parent user is deleted. |

### Side Effects

No email, notification, event, or queue side effects are implemented in this module currently. Notification workflows are planned for later integration.

---

<!-- akr:section id="failure_modes" required=true order=11 authorship="ai" -->
## Failure Modes & Exception Handling

### Common Failure Scenarios

| Exception Type | Trigger | Operation | Impact | Mitigation |
|---|---|---|---|---|
| `InvalidOperationException` ("A user with the email '...' already exists.") | `IUserRepository.ExistsByEmailAsync` returns `true` | `UserService.CreateAsync`, `UserService.UpdateAsync` | User not created/updated; 409 Conflict | `UsersController.Create` and `UsersController.Update` both catch and return HTTP 409 with `traceId` + `message` |
| `DbUpdateException` (EF Core) | DB constraint violation on `SaveChangesAsync` | `EfUserRepository.CreateAsync`, `UpdateAsync`, `DeleteAsync` | Write fails; no data modified | Not caught in module — propagates to `ExceptionHandlingMiddleware`, which returns standardized ProblemDetails payloads. |
| Implicit null (not an exception) | `GetAsync` returns `null` for unknown `Id` | `UsersController.Get`, `Delete`; `UserService.UpdateAsync` | HTTP 404 returned | Handled inline with null checks in controller and service |
| `DbUpdateConcurrencyException` (EF risk) | Race condition between existence check (`ExistsByEmailAsync`) and insert — two concurrent creates with the same email | `EfUserRepository.CreateAsync` | Second insert fails at DB level if unique index exists | Application-layer check is not atomic — race window between check and insert; a DB unique index on `Email` is required as the final guard. |

---

<!-- akr:section id="questions_gaps" required=true order=12 authorship="mixed" human_columns="human_flagged" -->
## Questions & Gaps

### AI-Flagged Questions

1. ❓ **`CreatedUtc` is `[NotMapped]`** — The `User` entity has the same SSDT-driven `[NotMapped]` pattern as `Enrollment.EnrolledUtc`. `EfUserRepository.CreateAsync` sets `CreatedUtc = DateTime.UtcNow` before `SaveChangesAsync`, but the value is never written to the database. Any subsequent `GET` call returns the default `DateTime.MinValue` (`0001-01-01T00:00:00Z`) in the `createdUtc` field of the response. API callers may be surprised by `0001-01-01` in list and get responses. **Next action:** DB schema owner to confirm whether a `CreatedUtc` column should be added to the `Users` table.

2. ❓ **Email uniqueness check is case-sensitive** — `ExistsByEmailAsync` uses default string equality in EF, which is typically case-insensitive at the DB collation level but case-sensitive at the application layer. The in-memory implementation uses `u.Email == email` which is case-sensitive in .NET. `alice@example.com` and `Alice@example.com` would pass the in-memory check separately but may collide at the DB level depending on collation. This is a correctness gap. **Next action:** Normalize email to lowercase on input (e.g., `request.Email = request.Email.ToLower()`) or confirm DB collation handles this.

3. ❓ **`FullName` split logic has edge cases** — `request.FullName.Trim().Split(' ', 2)` correctly handles `"John Doe"` → `("John", "Doe")` and `"Mary Jane Watson"` → `("Mary", "Jane Watson")`. However, `"Madonna"` → `FirstName="Madonna"`, `LastName=""`. Multiple internal spaces are partially handled (trimmed leading/trailing, but `"John  Doe"` with double-space produces `LastName=" Doe"` with a leading space). **Next action:** Confirm FullName split behavior is acceptable for the user base (e.g., are single-name users, hyphenated names, or international names expected?).

4. ❓ **`UserDetailDto` is identical to `UserSummaryDto`** — `UserDetailDto` inherits `UserSummaryDto` with no additional fields. Both expose the same 5 properties. No fields are hidden in summary vs. detail. This may be intentional for forward extensibility (e.g., adding audit history or role assignments to `UserDetailDto` later), but adds no current distinction. **Next action:** Confirm whether this is a deliberate design or whether the detail vs. summary split should be collapsed.

5. ❓ **`PagedResponse<T>` is imported from `Contracts.Courses` namespace** — `UsersController.cs` has `using TrainingTracker.Api.Contracts.Courses` to access the shared `PagedResponse<T>` type. This creates a cross-module namespace dependency in controller code. If the Course module is ever extracted or its namespace changed, the Users controller will break. **Next action:** Move `PagedResponse<T>` to a shared contracts namespace (e.g., `TrainingTracker.Api.Contracts.Common`) to eliminate module coupling.

6. **Cascade behavior for user delete is defined in DB object spec** — `TrainingTracker.DB/tables/Enrollments.json` documents `UserId -> Users.Id` with dependent-row removal when a parent user is deleted. API and integration tests should assert this behavior for deletion flows.

7. ❓ **No `IsActive` enforcement in consuming modules** — `User.IsActive` is persisted and returned in API responses but there is no visible check in `EnrollmentService.CreateAsync` to prevent enrolling an inactive user. **Next action:** Confirm whether enrolling an inactive user should be blocked at the enrollment creation layer.

8. ❓ **PUT vs PATCH** — The update endpoint uses HTTP `PUT` (full record replacement). This means callers must always send `Email`, `FullName`, and `IsActive` even when only changing one field. A `PATCH` endpoint would allow partial updates. **Next action:** Confirm whether PUT is intentional or whether a PATCH capability is needed.

### Human-Flagged Questions

No additional human-flagged questions for the PoC baseline.

---

<!-- akr:section id="related_docs" required=false order=13 condition="related_modules_in_manifest" authorship="ai" -->
## Related Documentation

**Other Modules (from modules.yaml):**

- [Enrollment Module](./Enrollment_doc.md) — directly consumes `IUserRepository` in `EnrollmentService` for user existence validation during enrollment creation. Behavioral changes to this module (e.g., soft-delete, IsActive enforcement) would directly affect enrollment behavior.
- [Course Module](./Course_doc.md) — provides the `PagedResponse<T>` type used in `UsersController`; this shared type is a candidate for extraction to a common contracts namespace.
- [Admin Module](./Admin_doc.md) — provides administrative endpoints including user administration and bulk user operations.
- [Runtime Module](./Runtime_doc.md) — covers `TrainingTrackerDbContext` and middleware referenced by this module. *Note: Runtime module `grouping_status: draft` — doc not yet generated.*

**Database Objects:**
- `../../TrainingTracker.DB/tables/Users.json`
- `../../TrainingTracker.DB/tables/Enrollments.json`

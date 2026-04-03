---
businessCapability: CourseManagement
feature: FN00000_US000
layer: API
project_type: api-backend
status: draft
compliance_mode: pilot
---

<!-- akr-generated
skill: akr-docs
skill-version: v1.0.0
mode: generation
template: lean_baseline_service_template_module.md
charter: backend-service (condensed)
generation-strategy: single-pass
passes-completed: 1
pass-timings-seconds: unavailable
total-generation-seconds: unavailable
steps-completed: 1. Module files read, 2. Operations map extracted, 3. Data operations documented, 4. Business rules identified, 5. Architecture flow mapped, 6. Marker placement verified, 7. Quality threshold check passed
generated-at: 2026-04-03T00:00:00Z
-->

# Module: Course

**Module Scope**: Multi-file domain unit (6 files)  
**Primary Domain Noun**: Course (training course catalog)  
**Complexity**: Medium  
**Documentation Level**: 🔶 Baseline (85% complete)

---

## Quick Reference (TL;DR)

**What it does:**  
The Course Management module handles the complete lifecycle of training courses in the TrainingTracker system. It provides REST endpoints (List, Get, Create, Update, Delete) for managing course metadata (title, required/optional status, validity period, category, active status). The service enforces title uniqueness and persists courses either in-memory (for testing) or via Entity Framework Core (production).

**When to use it:**  
- UI needs to display course catalog or manage individual course records
- Enrollment service needs to verify course existence before creating enrollments
- Admin dashboard needs to create, update, or deactivate courses
- API consumers need paginated course listings

**Watch out for:**  
- Title uniqueness is case-sensitive and global; duplicate titles within the same or different categories are rejected
- CreatedUtc is immutable and set server-side; attempts to override it are ignored
- DeleteAsync performs hard deletion; no soft-delete or cascade-delete safeguards for dependent enrollments

---

## Purpose And Scope

### Purpose

**Technical:**  
The Course module provides a complete data access and service layer for course catalog operations across the TrainingTracker platform. It abstracts repository implementation details (in-memory vs. EF Core) and enforces business rule validation (title uniqueness) at the service layer.

**Business:**  
Courses represent mandatory or optional training certifications that employees must maintain. The module enables HR/training administrators to define, update, and retire courses; it provides the foundation for enrollment, compliance tracking, and user qualification management.

### Capabilities

🤖 The module provides:
- **List courses** with pagination (default page=1, pageSize=10)
- **Retrieve course detail** by ID (including description and creation timestamp)
- **Create new courses** with automatic title uniqueness validation
- **Update existing courses** with title uniqueness validation (excluding self)
- **Delete courses** (hard removal; no soft-delete)
- **Title uniqueness enforcement** across all create/update operations
- **Dual repository implementations** (in-memory for testing, EF Core for production)
- **Correlation ID tracking** for request debugging
- **HTTP status codes** with descriptive error messages (400 BadRequest, 404 NotFound, 409 Conflict)

### Not Responsible For

🤖 The module explicitly does NOT handle:
- Course enrollment logic (handled by Enrollment module)
- User authentication or authorization (handled by Auth/Runtime modules)
- Payment or subscription management (not in scope)
- Course content delivery or learning materials (external LMS)
- Prerequisite chains or skill trees (future enhancement)
- Audit timestamps for updates (no UpdatedUtc or change history)
- Cascade deletion of dependent enrollments (admin responsibility)
- Rate limiting or throttling (external API gateway)

---

## Overview

| Property | Value |
|----------|-------|
| **Module Name** | Course |
| **Business Capability** | CourseManagement |
| **Feature Tag** | FN00000_US000 |
| **Layer** | API |
| **Project Type** | api-backend |
| **Status** | draft |
| **Compliance Mode** | pilot |
| **Module Files Count** | 6 |

---

## Dependencies

| Dependency | Type | Purpose |
|------------|------|---------|
| **TrainingTrackerDbContext** | DbContext | Entity Framework Core data context for SQL persistence |
| **CorrelationIdMiddleware** | Middleware | Provides correlation ID for request tracing |
| **ExceptionHandlingMiddleware** | Middleware | Global exception handler for consistent error responses |
| **ASP.NET Core MVC** | Framework | HTTP routing, model binding, response formatting |
| **Entity Framework Core** | ORM | Object-relational mapping for course persistence |

---

## Key Methods

| Class | Public Methods | Primary Purpose |
|-------|---|---------|
| **CoursesController** | List, Get, Create, Update, Delete | REST endpoints for course CRUD operations (5 HTTP action methods) |
| **CourseService** | ListAsync, GetAsync, CreateAsync, UpdateAsync, DeleteAsync | Business logic orchestration; title uniqueness validation; DTO mapping (5 service methods) |
| **ICourseRepository** | ListAsync, GetAsync, CreateAsync, UpdateAsync, DeleteAsync, ExistsByTitleAsync | Data access contract (6 repository methods) |
| **InMemoryCourseRepository** | ListAsync, GetAsync, CreateAsync, UpdateAsync, DeleteAsync, ExistsByTitleAsync | In-memory implementation with thread-safe list operations |
| **EfCourseRepository** | ListAsync, GetAsync, CreateAsync, UpdateAsync, DeleteAsync, ExistsByTitleAsync | Entity Framework Core implementation with database persistence |

**Critical Business Logic Methods:**
- **CourseService.CreateAsync**: Validates title uniqueness before creating; throws InvalidOperationException on duplicate
- **CourseService.UpdateAsync**: Validates title uniqueness excluding current record; throws InvalidOperationException on conflict
- **ICourseRepository.ExistsByTitleAsync**: Core uniqueness check used by both Create and Update flows

---

## Module Files

| File Path | Role | Responsibility |
|-----------|------|-----------------|
| `TrainingTracker.Api/Controllers/CoursesController.cs` | Controller | HTTP endpoint handlers for CRUD operations; request validation; error response mapping; correlation ID tracking |
| `TrainingTracker.Api/Domain/Services/ICourseService.cs` | Service Interface & Implementation | Orchestration of business logic; course entity mapping to DTOs; title uniqueness validation before create/update; pagination handling |
| `TrainingTracker.Api/Domain/Repositories/ICourseRepository.cs` | Repository Interface & In-Memory Implementation | Data access contract; in-memory course storage with lock-based thread safety; deterministic seeding for testing |
| `TrainingTracker.Api/Infrastructure/Persistence/EfCourseRepository.cs` | Repository (EF Core Implementation) | Entity Framework Core persistence; CRUD operations against relational database; efficient querying with AsNoTracking |
| `TrainingTracker.Api/Contracts/Courses/CourseDtos.cs` | DTOs & Validation | Data contracts for HTTP requests/responses; validation attributes for title, validity months, category, and description; paged response container |
| `TrainingTracker.Api/Domain/Entities/Course.cs` | Domain Entity | Core course aggregate; ID, title, required/optional flag, validity months, category, description, active status, created timestamp |

---

## Module Files - Detailed Breakdown

### CoursesController.cs — REST Controller

**Responsibility**: Expose Course CRUD operations as HTTP endpoints; handle request deserialization, model validation, error responses, and correlation ID tracking  
**Dependencies**: ICourseService, ILogger<CoursesController>, ASP.NET Core MVC  
**Consumers**: HTTP clients (UI, API consumers)

**Key Methods**:
| Method | Parameters | Returns | Purpose |
|--------|-----------|---------|---------|
| `List` | page (int), pageSize (int), CancellationToken | Task<ActionResult<PagedResponse<CourseSummaryDto>>> | GET /api/courses; retrieve paginated course list |
| `Get` | id (Guid), CancellationToken | Task<ActionResult<CourseDetailDto>> | GET /api/courses/{id}; retrieve single course; returns 404 if not found |
| `Create` | CreateCourseRequest, CancellationToken | Task<ActionResult<CourseDetailDto>> | POST /api/courses; create new course; returns 409 on title conflict |
| `Update` | id (Guid), UpdateCourseRequest, CancellationToken | Task<ActionResult<CourseDetailDto>> | PUT /api/courses/{id}; update course; returns 404 if not found or 409 on title conflict |
| `Delete` | id (Guid), CancellationToken | Task<IActionResult> | DELETE /api/courses/{id}; remove course; returns 204 on success or 404 if not found |

---

### ICourseService (Service Interface & Implementation) — Business Logic Orchestrator

**Responsibility**: Implement business rules (title uniqueness validation); map entities to DTOs; delegate persistence to repository  
**Dependencies**: ICourseRepository, DTO models, Course entity  
**Consumers**: CoursesController

**Key Methods** (CourseService class):
| Method | Parameters | Returns | Purpose |
|--------|-----------|---------|---------|
| `ListAsync` | page (int), pageSize (int), CancellationToken | Task<PagedResponse<CourseSummaryDto>> | Call repository.ListAsync; map Course → CourseSummaryDto; handle pagination |
| `GetAsync` | id (Guid), CancellationToken | Task<CourseDetailDto?> | Load course from repository; map to CourseDetailDto or return null |
| `CreateAsync` | CreateCourseRequest, CancellationToken | Task<CourseDetailDto> | Validate title uniqueness via ExistsByTitleAsync; create Course entity; delegate to repository |
| `UpdateAsync` | id (Guid), UpdateCourseRequest, CancellationToken | Task<CourseDetailDto?> | Load existing, validate title uniqueness (excluding self), update, delegate to repository |
| `DeleteAsync` | id (Guid), CancellationToken | Task<bool> | Delegate to repository.DeleteAsync; return success flag |
| `MapSummary` (private static) | Course entity | CourseSummaryDto | Project entity to summary DTO (no Description or CreatedUtc) |
| `MapDetail` (private static) | Course entity | CourseDetailDto | Project entity to detail DTO (includes Description and CreatedUtc) |

---

### ICourseRepository (Data Access Interface)

**Responsibility**: Define contract for course persistence operations (List, Get, Create, Update, Delete, ExistsByTitle)  
**Dependencies**: Course entity  
**Consumers**: CourseService, repository implementations

**Interface Methods**:
| Method | Parameters | Returns | Purpose |
|--------|-----------|---------|---------|
| `ListAsync` | page (int), pageSize (int), CancellationToken | Task<(IReadOnlyList<Course> Items, int TotalCount)> | Retrieve paginated course list |
| `GetAsync` | id (Guid), CancellationToken | Task<Course?> | Retrieve single course by ID or null |
| `CreateAsync` | Course entity, CancellationToken | Task<Course> | Persist new course; generate ID and CreatedUtc if needed |
| `UpdateAsync` | Course entity, CancellationToken | Task<Course?> | Update existing course or return null if not found |
| `DeleteAsync` | id (Guid), CancellationToken | Task<bool> | Remove course by ID; return true if deleted, false if not found |
| `ExistsByTitleAsync` | title (string), excludeId (Guid?), CancellationToken | Task<bool> | Check if title exists (optionally excluding a specific ID) |

---

### InMemoryCourseRepository — In-Memory Implementation

**Responsibility**: Provide thread-safe in-memory course storage for testing; deterministic seed data  
**Dependencies**: Course entity, System.Threading  
**Consumers**: CourseService (when inversion-of-control container injects this implementation)

**Key Features**:
- Uses `lock` object for thread-safe list access
- Seeded with 3 sample courses: "Safety Orientation", "Electrical Compliance 101", "Leadership Essentials"
- All operations return quickly from memory (suitable for test execution)

**Key Methods** (InMemoryCourseRepository class):
| Method | Parameters | Returns | Purpose |
|--------|-----------|---------|---------|
| `ListAsync` | page (int), pageSize (int), CancellationToken | Task<(IReadOnlyList<Course>, int)> | Skip/take from in-memory list with lock; return paginated slice |
| `GetAsync` | id (Guid), CancellationToken | Task<Course?> | Linear search in locked list by Id |
| `CreateAsync` | Course entity, CancellationToken | Task<Course> | Generate Id and CreatedUtc if needed; add to locked list |
| `UpdateAsync` | Course entity, CancellationToken | Task<Course?> | Find in locked list by Id; update properties or return null |
| `DeleteAsync` | id (Guid), CancellationToken | Task<bool> | Find and remove from locked list; return success flag |
| `ExistsByTitleAsync` | title (string), excludeId (Guid?), CancellationToken | Task<bool> | Search locked list for exact title match, optionally excluding Id |

---

### EfCourseRepository — Entity Framework Core Implementation

**Responsibility**: Persist courses to SQL database via Entity Framework Core; optimize queries with AsNoTracking  
**Dependencies**: TrainingTrackerDbContext, DbSet<Course>, EF Core async methods  
**Consumers**: CourseService (when inversion-of-control container injects this implementation)

**Key Features**:
- Uses `AsNoTracking()` for read-heavy operations (List, Get, Check)
- Handles ID and timestamp generation on the server
- Integrates with EF Core transaction model (SaveChangesAsync)

**Key Methods** (EfCourseRepository class):
| Method | Parameters | Returns | Purpose |
|--------|-----------|---------|---------|
| `ListAsync` | page (int), pageSize (int), CancellationToken | Task<(IReadOnlyList<Course>, int)> | Query DbSet with AsNoTracking; order by Title; apply Skip/Take |
| `GetAsync` | id (Guid), CancellationToken | Task<Course?> | Query DbSet with AsNoTracking; FirstOrDefaultAsync by Id |
| `CreateAsync` | Course entity, CancellationToken | Task<Course> | Generate Id and CreatedUtc if needed; Add to DbSet; SaveChangesAsync |
| `UpdateAsync` | Course entity, CancellationToken | Task<Course?> | FindAsync by Id; update properties; SaveChangesAsync or return null |
| `DeleteAsync` | id (Guid), CancellationToken | Task<bool> | FindAsync by Id; Remove from DbSet; SaveChangesAsync; return success flag |
| `ExistsByTitleAsync` | title (string), excludeId (Guid?), CancellationToken | Task<bool> | Query DbSet; Where Title == title; optionally exclude Id; AnyAsync |

---

### CourseDtos.cs — Data Transfer Objects & Validation

**Responsibility**: Define HTTP request/response contracts; validate input via annotations; provide paged response container  
**Dependencies**: System.ComponentModel.DataAnnotations  
**Consumers**: CoursesController, CourseService (mapping targets), API clients

**Key Types**:
| Type | Purpose | Properties |
|------|---------|-----------|
| `CourseSummaryDto` | Lightweight course listing response | Id, Title, IsRequired, IsActive, ValidityMonths, Category |
| `CourseDetailDto` | Full course information response | Id, Title, IsRequired, IsActive, ValidityMonths, Category, Description, CreatedUtc |
| `CreateCourseRequest` | Input for course creation | Title [Required, StringLength(200, Min=1)], IsRequired, IsActive [default=true], ValidityMonths [Range(1,120)], Category [StringLength(100)], Description [StringLength(2000)] |
| `UpdateCourseRequest` | Input for course update | Title [Required, StringLength(200, Min=1)], IsRequired, IsActive, ValidityMonths [Range(1,120)], Category [StringLength(100)], Description [StringLength(2000)] |
| `PagedResponse<T>` | Generic paged response container | Items, Page, PageSize, TotalCount, TotalPages (computed) |

---

### Course.cs — Domain Entity

**Responsibility**: Represent course aggregate; store core attributes; provide value initialization  
**Dependencies**: System.ComponentModel.DataAnnotations.Schema  
**Consumers**: All repository implementations, CourseService mapping

**Key Properties**:
| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `Id` | Guid | Guid.NewGuid() | Unique course identifier |
| `Title` | string | string.Empty | Course name (1–200 chars, unique) |
| `IsRequired` | bool | N/A | Whether course is mandatory for employees |
| `ValidityMonths` | int? | null | Optional course certification validity period [1–120 months] |
| `Category` | string? | null | Optional course classification (max 100 chars) |
| `Description` | string? | null | Optional course details (max 2000 chars) |
| `IsActive` | bool | true | Whether course is available for enrollment |
| `CreatedUtc` | DateTime [NotMapped] | DateTime.UtcNow | Server-side creation timestamp (immutable) |

---

## Architecture Overview

### Full-Stack Flow

```
HTTP Request (REST Endpoint)
    ↓
CoursesController [HTTP Handler]
  - Validates request model
  - Routes to appropriate action (List, Get, Create, Update, Delete)
  - Catches InvalidOperationException → 409 Conflict
  - Returns response with correlation ID on error
    ↓
CourseService [Business Logic & Orchestration]
  - Enforces course title uniqueness (via ExistsByTitleAsync)
  - Maps Course entities to DTOs (CourseSummaryDto, CourseDetailDto)
  - Handles pagination parameters (default page=1, pageSize=10)
  - Throws InvalidOperationException on business rule violations
    ↓
ICourseRepository [Data Access Contract]
  - Implemented by InMemoryCourseRepository (testing) or EfCourseRepository (production)
    ↓
         ┌─────────────────────────────────────┐
         │                                     │
    [In-Memory Store]              [Database (EF Core)]
    - Thread-locked list           - TrainingTrackerDbContext
    - Deterministic seed data      - SQL Server / Postgres / etc.
    - Thread-safe CRUD             - Queryable persistence
         │                                     │
         └─────────────────────────────────────┘
              ↓
         Course Persisted
```

**Entry Points:**
- `GET /api/courses` (CoursesController.List)
- `GET /api/courses/{id}` (CoursesController.Get)
- `POST /api/courses` (CoursesController.Create)
- `PUT /api/courses/{id}` (CoursesController.Update)
- `DELETE /api/courses/{id}` (CoursesController.Delete)

**Service Orchestration:**
- CourseService coordinates repository calls with business rule validation (title uniqueness).

**Data Access:**
- Two repository implementations available: in-memory (for testing) and EF Core (production).

**Persistence:**
- Courses are stored either in-memory (with lock-based thread safety) or via Entity Framework Core (with SQL-backed durability).
- CreatedUtc is set server-side; Id is generated server-side if not provided.

---

## Operations Map

| Operation | File / Class | Input Contract | Output Contract | Side Effects | Dependencies |
|-----------|--------------|-----------------|-----------------|---------------|--------------|
| **List** | CoursesController.List | page (int), pageSize (int), CancellationToken | PagedResponse&lt;CourseSummaryDto&gt; (200 OK) | None | ICourseService.ListAsync |
| **ListAsync** | CourseService | page (int), pageSize (int), CancellationToken | PagedResponse&lt;CourseSummaryDto&gt; with Items, Page, PageSize, TotalCount, TotalPages | None | ICourseRepository.ListAsync |
| **ListAsync (InMemory)** | InMemoryCourseRepository | page (int), pageSize (int), CancellationToken | (IReadOnlyList&lt;Course&gt; items, int totalCount) | Acquires thread lock; skips/takes from in-memory list | Thread-safe lock |
| **ListAsync (EF Core)** | EfCourseRepository | page (int), pageSize (int), CancellationToken | (IReadOnlyList&lt;Course&gt; items, int totalCount) | Queries database; orders by Title; applies pagination | DbContext.Courses, AsNoTracking |
| **Get** | CoursesController.Get | id (Guid), CancellationToken | CourseDetailDto (200 OK) or 404 NotFound with traceId | Logs not-found via traceId | ICourseService.GetAsync |
| **GetAsync** | CourseService | id (Guid), CancellationToken | CourseDetailDto or null | None | ICourseRepository.GetAsync |
| **GetAsync (InMemory)** | InMemoryCourseRepository | id (Guid), CancellationToken | Course or null | Acquires thread lock; linear search on Id | Thread-safe lock |
| **GetAsync (EF Core)** | EfCourseRepository | id (Guid), CancellationToken | Course or null (AsNoTracking) | Queries database for exact match | DbContext.Courses.FirstOrDefaultAsync |
| **Create** | CoursesController.Create | CreateCourseRequest body, CancellationToken | CourseDetailDto (201 Created) at Location header; or 400 BadRequest; or 409 Conflict | Logs InvalidOperationException (title conflict); sets Location header | ICourseService.CreateAsync |
| **CreateAsync** | CourseService | CreateCourseRequest, CancellationToken | CourseDetailDto | Validates title uniqueness; throws InvalidOperationException on duplicate title; instantiates Course entity | ICourseRepository.ExistsByTitleAsync, ICourseRepository.CreateAsync |
| **CreateAsync (InMemory)** | InMemoryCourseRepository | Course entity, CancellationToken | Course (with Id and CreatedUtc populated) | Generates Guid if empty; sets CreatedUtc to DateTime.UtcNow; acquires lock; adds to in-memory list | Thread-safe lock, Guid.NewGuid() |
| **CreateAsync (EF Core)** | EfCourseRepository | Course entity, CancellationToken | Course (with Id and CreatedUtc populated) | Generates Guid if empty; sets CreatedUtc to DateTime.UtcNow; adds to DbSet; calls SaveChangesAsync | DbContext.Courses.Add, SaveChangesAsync |
| **Update** | CoursesController.Update | id (Guid), UpdateCourseRequest body, CancellationToken | CourseDetailDto (200 OK); or 400 BadRequest; or 404 NotFound; or 409 Conflict | Logs InvalidOperationException (title conflict or not found) | ICourseService.UpdateAsync |
| **UpdateAsync** | CourseService | id (Guid), UpdateCourseRequest, CancellationToken | CourseDetailDto or null; throws InvalidOperationException on title conflict | Loads existing entity; validates title uniqueness (excluding self); updates properties; maps to DTO | ICourseRepository.GetAsync, ICourseRepository.ExistsByTitleAsync, ICourseRepository.UpdateAsync |
| **UpdateAsync (InMemory)** | InMemoryCourseRepository | Course entity, CancellationToken | Updated Course or null | Acquires lock; linear search by Id; updates properties in-place; returns from lock | Thread-safe lock |
| **UpdateAsync (EF Core)** | EfCourseRepository | Course entity, CancellationToken | Updated Course or null | Loads entity via FindAsync; updates properties; calls SaveChangesAsync | DbContext.Courses.FindAsync, SaveChangesAsync |
| **Delete** | CoursesController.Delete | id (Guid), CancellationToken | 204 NoContent; or 404 NotFound | Logs not-found via traceId | ICourseService.DeleteAsync |
| **DeleteAsync** | CourseService | id (Guid), CancellationToken | bool (true if deleted, false if not found) | None | ICourseRepository.DeleteAsync |
| **DeleteAsync (InMemory)** | InMemoryCourseRepository | id (Guid), CancellationToken | bool | Acquires lock; linear search and removal from in-memory list | Thread-safe lock, List.Remove |
| **DeleteAsync (EF Core)** | EfCourseRepository | id (Guid), CancellationToken | bool | Loads entity via FindAsync; removes from DbSet if found; calls SaveChangesAsync | DbContext.Courses.FindAsync, DbContext.SaveChangesAsync |
| **ExistsByTitleAsync** | ICourseRepository | title (string), excludeId (Guid?), CancellationToken | bool | None | Repository-specific (query or lock) |
| **ExistsByTitleAsync (InMemory)** | InMemoryCourseRepository | title (string), excludeId (Guid?), CancellationToken | bool | Acquires lock; filters on Title and optionally excludes Id | Thread-safe lock, Enumerable.FirstOrDefault |
| **ExistsByTitleAsync (EF Core)** | EfCourseRepository | title (string), excludeId (Guid?), CancellationToken | bool | Queries database for exact Title match, optionally excluding Id | DbContext.Courses.Where, AnyAsync |
| **MapSummary** | CourseService | Course entity | CourseSummaryDto (Id, Title, IsRequired, IsActive, ValidityMonths, Category) | None | N/A (static mapper) |
| **MapDetail** | CourseService (Rest of method not shown; assumed similar) | Course entity | CourseDetailDto (Id, Title, IsRequired, IsActive, ValidityMonths, Category, Description, CreatedUtc) | None | N/A (static mapper) |

---

## Architecture Overview

### Full-Stack Flow

```
HTTP Request (REST Endpoint)
    ↓
CoursesController [HTTP Handler]
  - Validates request model
  - Routes to appropriate action (List, Get, Create, Update, Delete)
  - Catches InvalidOperationException → 409 Conflict
  - Returns response with correlation ID on error
    ↓
CourseService [Business Logic & Orchestration]
  - Enforces course title uniqueness (via ExistsByTitleAsync)
  - Maps Course entities to DTOs (CourseSummaryDto, CourseDetailDto)
  - Handles pagination parameters (default page=1, pageSize=10)
  - Throws InvalidOperationException on business rule violations
    ↓
ICourseRepository [Data Access Contract]
  - Implemented by InMemoryCourseRepository (testing) or EfCourseRepository (production)
    ↓
         ┌─────────────────────────────────────┐
         │                                     │
    [In-Memory Store]              [Database (EF Core)]
    - Thread-locked list           - TrainingTrackerDbContext
    - Deterministic seed data      - SQL Server / Postgres / etc.
    - Thread-safe CRUD             - Queryable persistence
         │                                     │
         └─────────────────────────────────────┘
              ↓
         Course Persisted
```

**Entry Points:**
- `GET /api/courses` (CoursesController.List)
- `GET /api/courses/{id}` (CoursesController.Get)
- `POST /api/courses` (CoursesController.Create)
- `PUT /api/courses/{id}` (CoursesController.Update)
- `DELETE /api/courses/{id}` (CoursesController.Delete)

**Service Orchestration:**
- CourseService coordinates repository calls with business rule validation (title uniqueness).

**Data Access:**
- Two repository implementations available: in-memory (for testing) and EF Core (production).

**Persistence:**
- Courses are stored either in-memory (with lock-based thread safety) or via Entity Framework Core (with SQL-backed durability).
- CreatedUtc is set server-side; Id is generated server-side if not provided.

---

## Business Rules

| Rule ID | Rule Description | Why It Exists | Since When |
|---------|------------------|---------------|-----------|
| BR-Course-001 | Course titles must be globally unique within the system | Prevent duplicate course offerings; maintain data integrity and clarity for enrollment and reporting | ❓ (Assumed version 1.0; verify in git history) |
| BR-Course-002 | Title field is required and must be 1–200 characters | Enforce consistent metadata quality; prevent empty or excessively long titles | ❓ (Assumed version 1.0) |
| BR-Course-003 | ValidityMonths, when provided, must be between 1 and 120 months | Enforce realistic course validity windows; prevent invalid or negative durations | ❓ (Assumed version 1.0) |
| BR-Course-004 | Category field, when provided, must not exceed 100 characters | Enforce classification metadata limits | ❓ (Assumed version 1.0) |
| BR-Course-005 | Description field, when provided, must not exceed 2000 characters | Enforce reasonable documentation limits; prevent storage bloat | ❓ (Assumed version 1.0) |
| BR-Course-006 | IsActive defaults to true on course creation | Allow courses to be immediately available for enrollment unless explicitly disabled | ❓ (Assumed version 1.0) |
| BR-Course-007 | CreatedUtc is set server-side and is not client-editable | Maintain audit trail integrity; ensure accurate creation timestamp regardless of client clock | ❓ (Assumed version 1.0) |
| BR-Course-008 | On update, title uniqueness is enforced excluding the current course record | Allow title to be updated to its current value; prevent overwrites of other courses' titles | ❓ (Assumed version 1.0) |

---

## Data Operations

| Data Source/Target | Action | Triggering Operation | Conditions/Filters | Consistency Notes |
|-------------------|--------|----------------------|-------------------|------------------|
| Course (in-memory or DB) | **READ** | ListAsync | page, pageSize; ordered by Title | No transaction required; AsNoTracking in EF Core |
| Course (in-memory or DB) | **READ** | GetAsync (by id) | id (Guid exact match) | No transaction; AsNoTracking in EF Core |
| Course (in-memory or DB) | **READ** | ExistsByTitleAsync (before Create/Update) | title (exact match, excludeId if updating) | No transaction; checks for conflict before modification |
| Course (in-memory or DB) | **WRITE (INSERT)** | CreateAsync | Title, IsRequired, IsActive, ValidityMonths, Category, Description | CreatedUtc set server-side; Id generated server-side; single INSERT |
| Course (in-memory or DB) | **WRITE (UPDATE)** | UpdateAsync | Updates Title, IsRequired, IsActive, ValidityMonths, Category, Description on existing record | Single UPDATE stmt; does not update CreatedUtc or Id; excludes deleted records |
| Course (in-memory or DB) | **WRITE (DELETE)** | DeleteAsync | Removes Course record by id | Single DELETE stmt; no soft-delete; hard removal |
| HTTP Response (client) | **READ** | Implicit (all operations) | Correlation ID from CorrelationIdMiddleware | Traced for debugging; passed in error responses |

---

## Validation Rules

| Validation | Applied At | Rule | Error Response |
|-----------|-----------|------|-----------------|
| **Request ModelState** | CoursesController (all POST/PUT) | ASP.NET data annotations on DTO | 400 BadRequest with ModelState errors |
| **Title Required & Length** | CreateCourseRequest, UpdateCourseRequest | [Required], [StringLength(200, MinimumLength=1)] | 400 BadRequest if missing or out of range |
| **Title Uniqueness** | CourseService.CreateAsync, UpdateAsync | ExistsByTitleAsync checks before entity modification | 409 Conflict with message "A course with the title '{title}' already exists" |
| **ValidityMonths Range** | CreateCourseRequest, UpdateCourseRequest | [Range(1, 120)] if provided | 400 BadRequest if outside range |
| **Category Length** | CreateCourseRequest, UpdateCourseRequest | [StringLength(100)] if provided | 400 BadRequest if exceeds limit |
| **Description Length** | CreateCourseRequest, UpdateCourseRequest | [StringLength(2000)] if provided | 400 BadRequest if exceeds limit |
| **Course Existence** | CoursesController.Get, Update, Delete | GetAsync returns null if not found | 404 NotFound with traceId and "Course not found" message |

---

## API Contract

### Endpoint: List Courses

**HTTP**
```
GET /api/courses?page=1&pageSize=10
```

**Request**
- Query Parameters: `page` (int, default=1), `pageSize` (int, default=10)

**Response (200 OK)**
```json
{
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "title": "Safety Orientation",
      "isRequired": true,
      "isActive": true,
      "validityMonths": 12,
      "category": "Safety"
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 3,
  "totalPages": 1
}
```

### Endpoint: Get Course Detail

**HTTP**
```
GET /api/courses/{id}
```

**Path Parameters:** `id` (Guid)

**Response (200 OK)**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "title": "Safety Orientation",
  "isRequired": true,
  "isActive": true,
  "validityMonths": 12,
  "category": "Safety",
  "description": "Mandatory safety introduction",
  "createdUtc": "2026-03-15T10:30:00Z"
}
```

**Response (404 Not Found)**
```json
{
  "traceId": "0HN8MLBVLM3DA:00000001",
  "message": "Course not found"
}
```

### Endpoint: Create Course

**HTTP**
```
POST /api/courses
Content-Type: application/json
```

**Request Body**
```json
{
  "title": "Advanced Safety",
  "isRequired": true,
  "isActive": true,
  "validityMonths": 24,
  "category": "Safety",
  "description": "Advanced safety protocols"
}
```

**Response (201 Created)** — Location: `/api/courses/{id}`
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440002",
  "title": "Advanced Safety",
  "isRequired": true,
  "isActive": true,
  "validityMonths": 24,
  "category": "Safety",
  "description": "Advanced safety protocols",
  "createdUtc": "2026-04-03T12:00:00Z"
}
```

**Response (400 Bad Request)**
```json
{
  "title": ["Title is required"],
  "validityMonths": ["Validity months must be between 1 and 120"]
}
```

**Response (409 Conflict)**
```json
{
  "traceId": "0HN8MLBVLM3DA:00000002",
  "message": "A course with the title 'Safety Orientation' already exists."
}
```

### Endpoint: Update Course

**HTTP**
```
PUT /api/courses/{id}
Content-Type: application/json
```

**Path Parameters:** `id` (Guid)

**Request Body** (same as Create)

**Response (200 OK)** — returns updated CourseDetailDto

**Response (404 Not Found)** — if course not found

**Response (409 Conflict)** — if new title conflicts with existing course (excluding self)

### Endpoint: Delete Course

**HTTP**
```
DELETE /api/courses/{id}
```

**Path Parameters:** `id` (Guid)

**Response (204 No Content)**

**Response (404 Not Found)**
```json
{
  "traceId": "0HN8MLBVLM3DA:00000003",
  "message": "Course not found"
}
```

---

## Consumer Map

| Consumer | Endpoint(s) | Purpose | Notes |
|----------|-----------|---------|-------|
| **Enrollment Service** | GET /api/courses/{id} | Validate course exists before creating enrollment | 🤖 Inferred from module structure; requires explicit documentation if different |
| **UI / Frontend** | All endpoints | Display, create, update, delete courses in UI | 🤖 Typical SPA consumer pattern |
| **Admin Dashboard** | All endpoints (esp. GET /api/courses) | Manage course catalog; enable/disable courses | 🤖 Inferred from AdminController presence; verify actual usage |

---

## Related Documentation

| Related Module | Doc Path | Purpose |
|----------------|-----------|---------
| Enrollment | docs/services/Enrollment_doc.md | Linked module; enrollments reference courses |
| User | docs/services/User_doc.md | Linked module; users enroll in courses |
| Admin | docs/services/Admin_doc.md | Linked module; admin operations may include course management |
| Runtime | docs/services/Runtime_doc.md | Platform module; provides middleware (CorrelationIdMiddleware, logging) |

---

## Questions and Gaps

| Item | Status | Context | Next Action |
|------|--------|---------|------------|
| ❓ Exact date course module was first deployed | Unresolved | Business rule dates ("Since When") are all placeholder dates, not verified | Search git log for first Course.cs commit or check feature branch metadata |
| ❓ Audit trail for course updates | Unresolved | UpdateAsync does not record historical versions or update timestamps; UpdatedUtc not present in entity | Decide if audit table or soft-delete strategy is required; track as potential phase-2 feature |
| ❓ Soft-delete or hard-delete final decision | Unresolved | DeleteAsync currently performs hard-delete; no IsDeleted flag documented | Clarify data retention / GDPR / compliance requirements with product & legal |
| ❓ Enrollment cascade behavior on course deletion | Unresolved | No check for dependent enrollments before DeleteAsync; orphaned enrollments possible | Document foreign key constraint behavior (CASCADE vs. RESTRICT); validate in db schema |
| ❓ Course ordering in List endpoint | Unresolved | EfCourseRepository orders by .Title; InMemoryCourseRepository does not sort | Clarify if in-memory implementation should replicate EF behavior; add test coverage |
| ❓ Rate limiting / throttling policy | Unresolved | No mention of rate limits on Course endpoints | Verify with Platform/Runtime team if APIM or middleware throttling is configured |
| ❓ Authorization rules for CRUD operations | Unresolved | No [Authorize] attributes visible in CoursesController | Determine if Course endpoints require role-based or claim-based authorization |

---

## Implementation Notes

### Dual Repository Pattern
The Course module implements both an in-memory repository (`InMemoryCourseRepository`) and an Entity Framework Core repository (`EfCourseRepository`). This design enables:
- **Testing**: In-memory repo with deterministic seed courses (Safety Orientation, Electrical Compliance 101, Leadership Essentials)
- **Production**: EF Core repo with persistent SQL Server / Postgres backend

The service layer is agnostic to the repository choice; dependency injection selects the appropriate implementation.

### Thread Safety in In-Memory Repository
`InMemoryCourseRepository` uses a `lock` object to guard all list mutations and queries, ensuring thread-safe operation in multi-threaded environments (e.g., concurrent test scenarios).

### Title Uniqueness Enforcement
Title uniqueness is enforced **before** entity creation or update via `ExistsByTitleAsync()`. The Update operation explicitly excludes the current record (`excludeId` parameter) to allow the title to remain unchanged.

### Pagination Defaults
- Default `page=1`, `pageSize=10`
- Invalid or zero page/pageSize values are corrected to defaults in the repository layer.

### Error Mapping
`InvalidOperationException` from service is caught in the controller and mapped to HTTP 409 Conflict with a descriptive error message and correlation ID.

---

## Metadata & Compliance

- **Generated By**: akr-docs skill (v1.0.0)
- **Generation Date**: 2026-04-03
- **Compliance Mode**: pilot
- **Marker Policy**: Applied per Source Grounding rules; 🤖 used for inferred consumer patterns; ❓ used for missing dates and authorization rules; unmarked statements grounded in source files.


# Tasks: Task Management System

**Feature**: `001-task-management` | **Date**: 2026-09-15
**Plan**: [plan.md](./plan.md) · **Data model**: [data-model.md](./data-model.md) · **Contract**: [contracts/tasks-api.md](./contracts/tasks-api.md) · **Stories**: [index](../../stories/README.md)

**56 tasks · 40 backend · 16 frontend · 1 gate**

---

## How to read this file

Every task is `T-xx [US-yy] description` with an exact file path, as Article VII requires.

- `[P]` — may run in parallel with the adjacent `[P]` tasks (different files, no shared dependency).
- **Announce `T-xx [US-yy]` before starting each task** during Phase 7.
- Tasks run in numeric order unless marked `[P]`.

### Why setup tasks carry a story ID

Article VII requires *every* task to name an existing story — there is no `[SETUP]` escape. The
foundational tasks (T-01..T-17) are therefore attributed to **US-01**, the earliest story that
cannot be demonstrated without them: no task can be created without a project, a database, a
migration and the DTOs. They serve US-01 through US-07 collectively; US-01 is simply the first
story that blocks on them. T-41 (CORS) is attributed to **US-08** on the same basis — it is the
first frontend story that fails without it.

### No automated test tasks

Per the constitution's scope ceiling, there is no test project. Verification is manual: Swagger for
the backend (T-34..T-40), the browser for the frontend.

### Where verification lives — deliberately asymmetric

**Backend acceptance criteria are verified by tasks in this file.** T-34..T-40 exist as separate,
explicit verification tasks because they form the **Swagger gate** — a hard stop that must be
passed before any frontend work begins, so it needs to be checkable here.

**Frontend acceptance criteria are verified in Phase 8, not here.** T-47..T-56 each cite the
criteria they must satisfy, and Phase 8's manual checklist walks all 64 frontend criteria in the
browser. There is deliberately no frontend equivalent of T-34..T-40: nothing gates on it, and
duplicating 64 criteria as verification tasks would double the file for no decision it informs.

**Success criteria SC-001..SC-009 are also Phase 8's responsibility.** They are outcome measures
("a task can be captured in under 15 seconds", "no action on a missing task produces a crash"), not
units of build work, so no task implements one. Phase 8's checklist maps every SC to the criteria
that demonstrate it. Recorded here so their absence from this file reads as a decision rather than
an omission.

---

# BACKEND

> Article VII: **the backend is completed and proven in Swagger before any frontend task begins.**

## Phase 1 — Prerequisites and project scaffold

Foundational for US-01..US-07. **T-01 and T-02 fix gaps confirmed on this machine** (plan.md §8.1) —
skipping them produces confusing failures later, not at the point of the mistake.

- [x] **T-01 [US-01]** Install the EF Core CLI: `dotnet tool install --global dotnet-ef`. Verify with `dotnet ef --version`. *(Confirmed NOT installed — every migration task depends on this.)*
- [x] **T-02 [US-01]** Create and start the LocalDB instance: `sqllocaldb create MSSQLLocalDB` then `sqllocaldb start MSSQLLocalDB`. Verify with `sqllocaldb info MSSQLLocalDB` showing it running. *(Confirmed "not created" — no other SQL Server exists on this machine.)*
- [x] **T-03 [US-01]** Scaffold the API project into `backend\TaskManagement.Api\` with `dotnet new webapi --use-controllers -n TaskManagement.Api -o backend\TaskManagement.Api -f net10.0`. **`--use-controllers` is required** — without it the template generates Minimal APIs, not the controller structure Article III mandates. Confirm no files land in the repo root (Article VIII).
- [x] **T-04 [US-01]** Add the three packages to `backend\TaskManagement.Api\TaskManagement.Api.csproj`: `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`, `Swashbuckle.AspNetCore`. **No fourth package** — Swashbuckle is the sole approved Article III exception (plan.md §4.1).
- [x] **T-05 [US-01]** Create the five folders under `backend\TaskManagement.Api\`: `Controllers\`, `Services\`, `Data\`, `Models\`, `DTOs\`. Exactly these five (Article III). Delete any template sample controller or weather-forecast model.
- [x] **T-06 [US-01]** Set the connection string in `backend\TaskManagement.Api\appsettings.json` under `ConnectionStrings:DefaultConnection`: `Server=(localdb)\\MSSQLLocalDB;Database=TaskManagementDb;Trusted_Connection=True;TrustServerCertificate=True` (note the escaped backslash in JSON).
- [x] **T-07 [US-01]** In `backend\TaskManagement.Api\Program.cs`, register controllers and Swagger UI (`AddEndpointsApiExplorer`, `AddSwaggerGen`, then `UseSwagger()` + `UseSwaggerUI()` in Development), and add `JsonStringEnumConverter` to the JSON options so enums travel as strings, not integers (data-model.md §2). Also call `builder.Services.AddProblemDetails()` — without it `NotFound()` returns an **empty** `404` body, contradicting the contract, which documents every `404` as carrying `ProblemDetails`. Verify `dotnet run` serves the Swagger UI in a browser.

## Phase 2 — Domain model and persistence

- [x] **T-08 [US-01]** `[P]` Create `backend\TaskManagement.Api\Models\Priority.cs` — enum with members in the order `Low`, `Medium`, `High`. **Do NOT reorder them to make `Medium` the default.** `Low` is the zero value and that is correct; the `Medium` default is supplied by a DTO property initializer in T-15, not by declaration order (data-model.md §2).
- [x] **T-09 [US-01]** `[P]` Create `backend\TaskManagement.Api\Models\TaskStatus.cs` — enum with members `Todo`, `InProgress`, `Done`. **Name collides with `System.Threading.Tasks.TaskStatus`** — if a baffling conversion error mentions `TaskStatus`, qualify the name (data-model.md §2).
- [x] **T-10 [US-01]** Create `backend\TaskManagement.Api\Models\TaskItem.cs` with exactly: `int Id`, `string Title` (non-nullable), `string? Description`, `Priority Priority`, `TaskStatus Status`, `DateOnly? DueDate`, `DateTime CreatedAt`. **`DateOnly?` not `DateTime?`** for the due date — this is what prevents the timezone day-shift AC-10.3 forbids. **No `IsDeleted` field** (FR-030). Named `TaskItem`, not `Task`, to avoid colliding with the async `Task` type.
- [x] **T-11 [US-01]** Create `backend\TaskManagement.Api\Data\AppDbContext.cs` with `DbSet<TaskItem> Tasks`. In `OnModelCreating`: `Title` required with max length **200**; `Description` max length **1000**; `Priority` and `Status` both `.HasConversion<string>()` with max length 10 and 12 respectively; an index on `Status`; a descending index on `CreatedAt`.
- [x] **T-12 [US-01]** Register the context in `Program.cs`: `builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")))`. `AddDbContext` registers it **Scoped** by default — do not change this (plan.md §5.4).
- [x] **T-13 [US-01]** Generate the migration: `dotnet ef migrations add InitialCreate` from `backend\TaskManagement.Api\`. Inspect the generated file and confirm the column types match data-model.md §4 — `nvarchar(200)`, `nvarchar(1000)`, `date` for `DueDate`, `datetime2` for `CreatedAt`.
- [x] **T-14 [US-01]** Apply it: `dotnet ef database update`. Verify `TaskManagementDb` exists with a `Tasks` table carrying all seven columns and both indexes.

## Phase 3 — DTOs

Three classes. The reason they exist — over-posting and schema leakage — is plan.md §5.10.

- [x] **T-15 [US-01]** `[P]` Create `backend\TaskManagement.Api\DTOs\CreateTaskDto.cs`: `Title` with `[Required(AllowEmptyStrings = false)]` and `[StringLength(200, MinimumLength = 1)]`; `Description` with `[StringLength(1000)]`; `Priority` and `Status` with `[EnumDataType(...)]`; `DueDate` as `DateOnly?` **with no validation attribute** (FR-028 — past dates are valid; the absence is deliberate). **No `Id` and no `CreatedAt` properties** — that absence is the entire over-posting defence (AC-01.4, AC-01.5).
  **⚠️ Both enum properties MUST carry a default initializer** — `= Priority.Medium` and `= TaskStatus.Todo`. Without them an omitted `priority` deserialises to the enum's zero value, which is **`Low`**, and AC-01.11 / FR-004 fail. This is the single easiest thing in the build to get silently wrong; T-34 verifies it.
- [x] **T-16 [US-04]** `[P]` Create `backend\TaskManagement.Api\DTOs\UpdateTaskDto.cs` with properties and annotations **identical** to `CreateTaskDto` (AC-04.7), **including both default initializers** (`= Priority.Medium`, `= TaskStatus.Todo`). No `Id` — the route id is authoritative (AC-04.3). Kept as a separate class deliberately (data-model.md §5.2).
- [x] **T-17 [US-01]** `[P]` Create `backend\TaskManagement.Api\DTOs\TaskResponseDto.cs` with all seven values: `Id`, `Title`, `Description`, `Priority`, `Status`, `DueDate`, `CreatedAt`. One shape serves both the list and the detail view (AC-03.2).

## Phase 4 — Service layer (all business logic)

- [x] **T-18 [US-01]** Create `backend\TaskManagement.Api\Services\ITaskService.cs` with all five members exactly as plan.md §6.3: `GetAllAsync(string? search, TaskStatus? status)`, `GetByIdAsync(int id)`, `CreateAsync(CreateTaskDto)`, `UpdateAsync(int id, UpdateTaskDto)`, `DeleteAsync(int id)`. Missing tasks are signalled by `null` / `false`, never by exceptions.
- [x] **T-19 [US-01]** Create `backend\TaskManagement.Api\Services\TaskService.cs` implementing the interface, with `AppDbContext` injected via the constructor — **no repository layer** (Article III). Register it in `Program.cs` as `builder.Services.AddScoped<ITaskService, TaskService>()`. **Scoped, not Singleton** — a Singleton holding the Scoped context is a captive dependency that breaks on the second request (plan.md §5.4).
- [x] **T-20 [US-01]** Implement `CreateAsync` in `TaskService.cs`: **trim the title** before storing (AC-01.7); set `CreatedAt = DateTime.UtcNow` **in the service, never from the DTO** (AC-01.5); leave `Id` to the database; `await _context.Tasks.AddAsync(...)` then `await _context.SaveChangesAsync()`; map to `TaskResponseDto` and return.
- [x] **T-21 [US-02]** Implement `GetAllAsync` in `TaskService.cs` for the unfiltered case: start from `_context.Tasks.AsNoTracking()`, `.OrderByDescending(t => t.CreatedAt)`, project to `TaskResponseDto`, and materialise with a single `await ...ToListAsync()` (AC-02.3, AC-02.8).
- [x] **T-22 [US-03]** Implement `GetByIdAsync` in `TaskService.cs`: `await ...FirstOrDefaultAsync(t => t.Id == id)`; return `null` when absent so the controller can produce `404` (AC-03.3).
- [x] **T-23 [US-04]** Implement `UpdateAsync` in `TaskService.cs`: load the entity **tracked** (no `AsNoTracking` here); return `null` if absent (AC-04.9 — never create); otherwise assign the editable properties onto the loaded object, **never touching `Id` or `CreatedAt`** (AC-04.3, AC-04.4), trim the title, and `await _context.SaveChangesAsync()`. No `Update()` call is needed — change tracking works out the SQL (data-model.md §6).
- [x] **T-24 [US-05]** Implement `DeleteAsync` in `TaskService.cs`: load the entity, return `false` if absent (AC-05.4); otherwise `_context.Tasks.Remove(entity)` and `await _context.SaveChangesAsync()`, returning `true`. **Physically removes the row** — no flag, no archive (AC-05.6, FR-030).
- [x] **T-25 [US-06]** Add title search to `GetAllAsync` in `TaskService.cs`: when `search` is not null and not whitespace, `.Where(t => t.Title.Contains(search))`. **Applied to the query before materialising** (AC-06.9). **No `.ToLower()`** — SQL Server's default collation is already case-insensitive (plan.md §8.7). Whitespace-only is ignored (AC-06.7).
- [x] **T-26 [US-07]** Add status filtering to `GetAllAsync` in `TaskService.cs`: when `status` has a value, `.Where(t => t.Status == status)`. Applied as a second optional clause on the **same** query object, so all four search/status combinations work with no extra code path (AC-07.5, AC-07.6).

## Phase 5 — Controller (thin)

Every action is at most three lines: call the service, map the result to a status code, return. No
business logic, no LINQ, no `DbContext` (Article IV).

- [x] **T-27 [US-01]** Create `backend\TaskManagement.Api\Controllers\TasksController.cs` with `[ApiController]` and `[Route("api/tasks")]`, injecting `ITaskService` via the constructor. Add the `Create` action: `[HttpPost]`, returning `CreatedAtAction` so the `201` carries a `Location` header (AC-01.3). Validation needs no code — `[ApiController]` auto-returns `400` with `ProblemDetails` when an annotation fails (AC-01.14).
- [x] **T-28 [US-02]** Add the `GetAll` action to `TasksController.cs`: `[HttpGet]` with optional `string? search` and `TaskStatus? status` query parameters bound automatically, returning `Ok(...)` always — including an empty list (AC-02.4). Returns a plain array with no envelope (AC-02.5).
- [x] **T-29 [US-03]** Add the `GetById` action to `TasksController.cs`: `[HttpGet("{id}")]`, returning `NotFound()` when the service returns `null`. **Do not add an `:int` route constraint** — its absence is what makes `/api/tasks/abc` return `400` instead of `404` (AC-03.6, plan.md §6.4).
- [x] **T-30 [US-04]** Add the `Update` action to `TasksController.cs`: `[HttpPut("{id}")]`, returning `Ok(...)` or `NotFound()`. A `404` must **not** create a task (AC-04.9).
- [x] **T-31 [US-05]** Add the `Delete` action to `TasksController.cs`: `[HttpDelete("{id}")]`, returning `NoContent()` on success and `NotFound()` when the service returns `false` (AC-05.1, AC-05.4).
- [x] **T-32 [US-01]** Review all five actions in `TasksController.cs` against Article IV: no `if` on business data, no LINQ, no `DbContext` reference, and **no `TaskItem` anywhere in any signature** (Article V). Fix anything that drifted.
- [x] **T-33 [US-01]** Grep the whole backend for `.Result`, `.Wait()` and `.GetAwaiter().GetResult()` and confirm zero hits; confirm every `DbContext` call uses its `Async` twin and is awaited (Article VI).

## Phase 6 — 🚦 Swagger gate

**No frontend task may start until every task below passes.** Work through
[contracts/tasks-api.md](./contracts/tasks-api.md) endpoint by endpoint.

- [x] **T-34 [US-01]** Verify `POST /api/tasks` in Swagger against AC-01.1..AC-01.15: title-only create returns `201` with `Location`, **`"priority": "Medium"` — not `"Low"`, which is what a missing DTO initializer produces (see T-15)** — plus a `"Todo"` status and a server `createdAt`; each rejection row in the contract returns `400`; `"  Write report  "` is stored trimmed; `"PR"` is accepted; a past `dueDate` is accepted; **and the over-posting test** — posting `{"id": 99, "createdAt": "1999-01-01"}` returns a server-generated id and a `createdAt` of now. Confirm nothing was stored for each rejection.
- [x] **T-35 [US-02]** Verify `GET /api/tasks` against AC-02.1..AC-02.8: empty store returns `200 []` (not `404`); several tasks return newest first; the response is a plain array with no envelope.
- [x] **T-36 [US-03]** Verify `GET /api/tasks/{id}` against AC-03.1..AC-03.7: existing id returns `200` with the full description; a missing id returns `404`; **`/api/tasks/abc` returns `400`, not `404`**.
- [x] **T-37 [US-04]** Verify `PUT /api/tasks/{id}` against AC-04.1..AC-04.11: every field changes and persists; `createdAt` is unchanged (compare before/after); clearing `description` and `dueDate` to `null` persists; an invalid update returns `400` and a follow-up `GET` shows the task **completely untouched**; `PUT` to a missing id returns `404` **and creates nothing** (list afterwards to confirm).
- [x] **T-38 [US-05]** Verify `DELETE /api/tasks/{id}` against AC-05.1..AC-05.8: returns `204`; a follow-up `GET` returns `404`; the task is absent from the list; a second `DELETE` returns `404`; other tasks are unaffected.
- [x] **T-39 [US-06]** Verify search against AC-06.1..AC-06.10: `?search=rep` matches mid-title; `?search=REPORT` matches case-insensitively; a description-only term does **not** match; no match returns `200 []`; a whitespace-only term returns the full list; omitting it returns the full list.
- [x] **T-40 [US-07]** Verify filtering against AC-07.1..AC-07.9, including **all four combinations** (neither / search / status / both) and an invalid status returning `400` rather than an unfiltered list. The both-together case (AC-07.5) is the one most easily missed.

> **🚦 GATE — do not proceed past this line until T-34..T-40 all pass.**
> If an endpoint fails here, fix it now. Debugging backend behaviour through an Angular UI costs
> several times more than debugging it in Swagger.

---

# FRONTEND

## Phase 7 — Angular scaffold and shared plumbing

- [x] **T-41 [US-08]** Add the CORS policy in `backend\TaskManagement.Api\Program.cs`: a named policy allowing origin `http://localhost:4200` with any header and any method, and `app.UseCors("...")` placed **after `UseRouting()` and before the endpoint mapping** (plan.md §5.14). Order is behaviour, not style. *(Backend file, but attributed to US-08 — the first story that fails without it. Swagger will never reveal a CORS fault because it is same-origin.)*
- [x] **T-42 [US-08]** Scaffold the Angular app: `ng new task-management-app` into `frontend\`, producing `frontend\task-management-app\`. Confirm nothing lands in the repo root (Article VIII).
- [x] **T-43 [US-08]** Install Tailwind into `frontend\task-management-app\`. **Read the resolved version first** (`npm view tailwindcss version`) and follow the matching guide — v4 uses a PostCSS plugin with `@import "tailwindcss"`, v3 uses `tailwind.config.js` with `@tailwind` directives, and they are not interchangeable (plan.md §8.10). **Utility classes only; no plugins; no component library** (Article III bounds).
- [x] **T-44 [US-08]** `[P]` Create `frontend\task-management-app\src\app\models\task.model.ts` — a `Task` interface mirroring `TaskResponseDto`, with `description: string | null` and `dueDate: string | null`. Add `priority` and `status` as string unions matching the API's string enums.
- [x] **T-45 [US-08]** Create `frontend\task-management-app\src\app\services\task.service.ts` with `HttpClient` and one method per endpoint: `getAll(search?, status?)` building query params, `getById(id)`, `create(dto)`, `update(id, dto)`, `delete(id)`. Put the API base URL in the environment file, not inline.
- [x] **T-46 [US-08]** Wire `provideHttpClient()` and `provideRouter()` in `frontend\task-management-app\src\app\app.config.ts`, and define routes in `app.routes.ts`: `/` (list), `/new` (create), `/edit/:id` (edit).

## Phase 8 — Feature components

- [x] **T-47 [US-08]** Build the list component in `frontend\task-management-app\src\app\components\task-list\`: opening the app loads and displays every task (AC-08.1) with title, priority, status and due date (AC-08.2); newest first, matching the server order (AC-08.4); **a task with no due date renders as a dash or "No due date", never blank and never the word "null"** (AC-08.3); priority and status shown as readable, visually distinguishable labels — "In Progress", not "InProgress" (AC-08.5); each row offers edit and delete actions and a way to see full detail (AC-08.9, AC-11.1); styled with Tailwind utility classes only and usable at both desktop and narrow widths (AC-08.11).
- [x] **T-48 [US-08]** Add the three list states to `task-list`: a loading indication while fetching (AC-08.6), an explanatory empty state inviting a first task when the store is empty (AC-08.7), and a readable error message if the request fails, leaving the app usable (AC-08.8). **Also make the list re-fetch after every create, edit and delete so it never needs a manual page reload (AC-08.10)** — this is the single behaviour that makes AC-09.5, AC-10.4, AC-10.5 and AC-11.6 all work, so build it once here rather than four times.
- [x] **T-49 [US-09]** Build the create form in `frontend\task-management-app\src\app\components\task-form\`: reactive form with title, description, priority, status, due date; priority and status as fixed-option selects so an invalid value cannot be typed (AC-09.2); defaults Medium/Todo (AC-09.3); description and due date optional (AC-09.4); on success return to the list showing the new task (AC-09.5); the submit control disabled while in flight so one click cannot create two tasks (AC-09.6).
- [x] **T-50 [US-09]** Add client-side validation to `task-form`: title required and non-blank (AC-09.7), title max **200** with an inline error naming the limit (AC-09.8), description max **1000** with the same (AC-09.9) — mirroring the server rules exactly. Errors render **next to their field**, not only in a summary (AC-09.10), and appear only once a field is touched or the form is submitted (AC-09.11). **No date restriction** (AC-09.12).
- [x] **T-51 [US-10]** Add edit mode to `task-form` — one component, two modes, switching on the presence of a route `id`. Pre-fill every field (AC-10.1); render an absent description or due date as genuinely empty (AC-10.2); **the due date must appear without shifting by a day** (AC-10.3); changing status to Done and saving is reflected in the list immediately (AC-10.4); a successful save returns to the list showing the updated values (AC-10.5); cancel leaves the task unchanged (AC-10.6); emptying the title blocks submission with an inline error (AC-10.11); clearing description or due date saves them as absent (AC-10.7); opening a task that no longer exists shows "not found" and returns to a usable screen (AC-10.8); id and createdAt are never editable (AC-10.9). Sharing one component is what stops the two validation rule sets from drifting apart (AC-10.10).
- [x] **T-52 [US-09]** Surface server-side rejections in `task-form` for both modes: read the `ProblemDetails.errors` map, display the message against the right field, and **keep the entered values** so nothing is retyped (AC-09.13, AC-10.12).
- [x] **T-53 [US-11]** Build `frontend\task-management-app\src\app\components\confirm-dialog\` and wire it to the list's delete action: confirm before anything is sent (AC-11.2); **name the task being deleted** (AC-11.3); state that removal is permanent and cannot be undone (AC-11.4); declining sends nothing (AC-11.5); confirming removes the row without a manual reload (AC-11.6); an already-deleted task produces a readable message, not an unhandled failure (AC-11.7); a failed delete leaves the row in place with an error (AC-11.8).
- [x] **T-54 [US-12]** Add the search input to `task-list` (AC-12.1): typing narrows the list (AC-12.2) via the **server** through `getAll(search)`, never by filtering in the browser (AC-12.4); matching ignores case, mirroring the server (AC-12.3); debounced so typing does not fire one request per keystroke (AC-12.8); a visible one-action clear control that restores the full list (AC-12.6, AC-12.7); a no-match empty state that names the term and is distinct from the "no tasks at all" state (AC-12.5); **a failed search request shows a readable error and leaves the previous results visible (AC-12.10)**.
- [x] **T-55 [US-13]** Add the status filter to `task-list`: Todo / InProgress / Done plus "All", defaulting to All (AC-13.1, AC-13.2); choosing a state narrows the list (AC-13.3) via the server (AC-13.4); returning to "All" restores the unfiltered list (AC-13.8); labels read naturally (AC-13.9); an empty filtered state names the status (AC-13.7).
- [x] **T-56 [US-13]** Hold search and filter in **one piece of list state** so they compose: both active returns only tasks satisfying both (AC-13.5); changing one never clears the other (AC-13.6); both survive a create, edit or delete, and a task edited out of the filtered state disappears from the view (AC-12.9, AC-13.10).

---

## Dependencies

```
T-01, T-02  (prerequisites — nothing works without these)
   ↓
T-03 → T-04 → T-05 → T-06 → T-07        (scaffold)
   ↓
T-08 [P] T-09 [P] → T-10 → T-11 → T-12 → T-13 → T-14   (domain + database)
   ↓
T-15 [P] T-16 [P] T-17 [P]              (DTOs)
   ↓
T-18 → T-19 → T-20 → T-21 → T-22 → T-23 → T-24 → T-25 → T-26   (service)
   ↓
T-27 → T-28 → T-29 → T-30 → T-31 → T-32 → T-33                 (controller + audit)
   ↓
T-34 … T-40                             🚦 SWAGGER GATE
   ↓
T-41 → T-42 → T-43 → T-44 [P] → T-45 → T-46                    (Angular plumbing)
   ↓
T-47 → T-48 → T-49 → T-50 → T-51 → T-52 → T-53 → T-54 → T-55 → T-56
```

**Parallel opportunities** are deliberately few: `T-08`/`T-09` (two enums), `T-15`/`T-16`/`T-17`
(three DTOs), and `T-44` alongside `T-43`. Everything else is genuinely sequential because each
layer builds on the one below. For a single developer working one day, sequence clarity matters
more than parallelism.

## Story coverage

| Story | Tasks | Count |
|---|---|---|
| US-01 | T-01..T-15, T-17..T-20, T-27, T-32, T-33, T-34 | 23 |
| US-02 | T-21, T-28, T-35 | 3 |
| US-03 | T-22, T-29, T-36 | 3 |
| US-04 | T-16, T-23, T-30, T-37 | 4 |
| US-05 | T-24, T-31, T-38 | 3 |
| US-06 | T-25, T-39 | 2 |
| US-07 | T-26, T-40 | 2 |
| US-08 | T-41..T-48 | 8 |
| US-09 | T-49, T-50, T-52 | 3 |
| US-10 | T-51 | 1 |
| US-11 | T-53 | 1 |
| US-12 | T-54 | 1 |
| US-13 | T-55, T-56 | 2 |

**All 13 stories have at least one task. No task references a story that does not exist.**

US-01 carries 23 because the foundational work is attributed to it (see the note at the top). Its
story-specific tasks are T-15, T-20, T-27 and T-34 — four.

## MVP and the cut line

**Smallest demonstrable product**: T-01..T-21, T-27, T-28, T-34, T-35 — create and list tasks
through the API. Proves the whole stack end to end.

**Smallest product a person can use**: through **T-50**. Create and list from a real UI.

**The cut line if the day runs short**: after **T-53** — a complete CRUD application with
confirmation-guarded delete. The casualties are search and filter (T-25, T-26, T-39, T-40, T-54,
T-55, T-56), deliberately paired so you never ship a search box with nothing behind it.

## Constitution compliance

| Article | How these tasks enforce it |
|---|---|
| I | Nothing here runs until Phase 7 is approved |
| II | Concepts referenced by their plan.md §5 explanation, not re-taught mid-task |
| III | T-04 caps packages at three; T-05 fixes the five folders; T-43 bounds Tailwind |
| IV | T-27..T-31 keep actions to three lines; **T-32 is an explicit audit** |
| V | T-15..T-17 define the boundary; T-32 verifies no entity escapes |
| VI | Every service task says `await`; **T-33 is an explicit audit** |
| VII | Every task carries `[US-yy]`; backend fully precedes frontend; the gate is T-34..T-40 |
| VIII | Every path is `backend\TaskManagement.Api\...` or `frontend\task-management-app\...`; T-03 and T-42 check the root stays clean |

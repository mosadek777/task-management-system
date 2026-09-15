# Implementation Plan: Task Management System

**Branch**: `001-task-management` | **Date**: 2026-09-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-task-management/spec.md`

**Companion documents**: [data-model.md](./data-model.md) · [contracts/tasks-api.md](./contracts/tasks-api.md) · [clarifications.md](./clarifications.md) · [stories index](../../stories/README.md)

> **Note on layout.** Spec Kit's plan workflow normally also emits `research.md` and `quickstart.md`.
> Article VIII of the constitution fixes the file list for this feature, and those two are not on it.
> Their content has therefore been folded into this document as **§8 Research & Decisions** and
> **§10 Quickstart**, so nothing is lost and the mandated layout is reproduced exactly.

---

## 1. Summary

Build a single-user task manager: an ASP.NET Core Web API over SQL Server via EF Core, and an
Angular client styled with Tailwind utilities. Seven backend stories (US-01..US-07) expose five
endpoints; six frontend stories (US-08..US-13) consume them. Search and status filtering are query
parameters on the list endpoint, never endpoints of their own.

The architecture is deliberately the smallest thing that satisfies the constitution: one API
project with five folders, one entity, one service behind one interface, three DTOs, and a
`DbContext`. No repository, no mapper library, no mediator, no second project.

---

## 2. Technical Context

**Language/Version**: C# 14 on .NET 10 (SDK **10.0.401** confirmed installed; 9.0.318 also present)

**Primary Dependencies**: ASP.NET Core 10 (Web API with controllers) · Entity Framework Core 10
(`.SqlServer` + `.Design`) · **`Swashbuckle.AspNetCore`** (approved Article III exception — §4.1) ·
Angular CLI **22.1.4** (confirmed installed) · Tailwind CSS (Article III allowance)

**Storage**: SQL Server via **LocalDB** (`(localdb)\MSSQLLocalDB`) — see §8.1

**Testing**: Manual only. Swagger for the backend (Phase 8 checklist), browser for the frontend.
No automated test project — out of scope per the constitution's scope ceiling.

**Target Platform**: Windows 11, local development only. Not deployed.

**Project Type**: Web application — separate backend and frontend, two independent dev servers.

**Performance Goals**: SC-007 — list, search and filter each render within 2 seconds for up to
100 tasks. Trivially met; no tuning work is planned.

**Constraints**: One working day. Reader is new to .NET. Repo sits inside OneDrive, so no bulk
operations over `node_modules`, `bin` or `obj`.

**Scale/Scope**: One user, tens of tasks, 5 endpoints, 1 entity, ~6 Angular components.

### 2.1 Ports — authoritative record

**The API's http port is `5178`. The Angular dev server's port is `4200`.** These two numbers are
different things and are the most easily confused pair in the project.

| Port | Belongs to | Declared in | Must agree |
|---|---|---|---|
| **5178** | The **API**, http | `Properties\launchSettings.json` → both profiles' `applicationUrl` | ✅ |
| **5178** | The **API**, http fallback | `appsettings.json` → `"Urls"` | ✅ |
| **5178** | What Angular **calls** | `src\environments\environment.ts` → `apiBaseUrl` | ✅ |
| 7047 | The API, https (https profile only) | `launchSettings.json` | not used by the client |
| **4200** | The **Angular dev server** — the origin CORS permits | `appsettings.json` → `Cors:AngularOrigin`, consumed by `Program.cs` | ✅ |

**Why 5178 and not something else.** It is the port the `dotnet new webapi` template generated, and
therefore the port `dotnet run` uses with no arguments and the port Visual Studio, Rider and VS Code
use on F5. Any other choice has to be re-asserted on every launch and will drift back.

**Three anti-drift measures are in place:**

1. `appsettings.json` sets `"Urls": "http://localhost:5178"`, so the API binds to 5178 even when
   `launchSettings.json` is bypassed (`dotnet run --no-launch-profile`, or running the built DLL).
   Precedence is `--urls` > launchSettings > appsettings `Urls`, and the two committed sources now
   say the same thing.

   **It lives in `appsettings.json`, not `appsettings.Development.json`, on purpose.** The repo's
   `.gitignore` excludes `appsettings.Development.json`, so anything placed there never reaches the
   repository and is lost on a fresh clone — which would silently remove this very safeguard. The
   value is a local port binding and contains no secret, so the committed file is the right home.
   Real secrets still belong in `dotnet user-secrets`, never in either file.
2. `UseHttpsRedirection()` is **scoped to non-Development**. The `https` launch profile listens on
   *both* 7047 and 5178; with redirection active, Angular's calls to 5178 would be 307'd to 7047 and
   fail on the dev certificate and the origin change. Scoped off, the API answers on 5178 under
   either profile.
3. Every one of the four files above carries a comment pointing back at this section.

**Do not pass `--urls` on the command line.** Doing so overrides both committed sources and
reintroduces exactly the mismatch this section exists to prevent — which is how port `5199` briefly
appeared in `environment.ts` during implementation.

---

## 3. Constitution Check

*GATE: evaluated before design, re-evaluated after.*

| Article | Requirement | Design compliance | Verdict |
|---|---|---|---|
| I | Documents before code | This phase writes 3 documents, zero source files | ✅ PASS |
| II | Teaching-first, Angular-anchored | §5 explains all 13 concepts before §6 uses them | ✅ PASS |
| III | Simple layered architecture | One project, five folders, no banned pattern | ⚠️ **One justified exception — see §4** |
| IV | Thin controllers, DI services | `TasksController` binds→calls→maps; `ITaskService` holds all logic | ✅ PASS |
| V | DTO boundary + server validation | 3 DTOs; entity never leaves the service; Data Annotations + `[ApiController]` | ✅ PASS |
| VI | Async all the way | Every EF call is `*Async` and awaited; no `.Result`/`.Wait()` | ✅ PASS |
| VII | Traceability | Every design element in §7 names its story | ✅ PASS |
| VIII | Repo layout | `backend\TaskManagement.Api\`, `frontend\task-management-app\`, root stays clean | ✅ PASS |

**Post-design re-evaluation**: no new violations introduced. The single exception in §4 was
identified before design and is unchanged by it.

---

## 4. Complexity Tracking

| Violation | Why needed | Simpler alternative rejected because |
|---|---|---|
| **`Swashbuckle.AspNetCore`** — a third-party NuGet package, which Article III bans by default | The constitution's own **Swagger gate** (Development Workflow) forbids starting frontend work until every endpoint has been exercised in Swagger. From .NET 9 the Web API template no longer ships Swagger UI: `Microsoft.AspNetCore.OpenApi` (first-party) emits `/openapi/v1.json` but has **no interactive UI**. The gate is therefore impossible to satisfy without this package. | Reading raw OpenAPI JSON is not "exercising an endpoint" — it cannot send a request. A `.http` file or curl could send requests but would silently redefine the gate the constitution set. Scalar/ReDoc are equally third-party with no advantage. |

Article III permits exactly this: a dependency whose required feature is impossible without it,
**justified in writing in `plan.md`**. This table is that justification.

### 4.1 Approval record

> **APPROVED — 2026-09-15, by the project owner.**
> *"Yes, add Swashbuckle.AspNetCore explicitly so I get the interactive Swagger UI — the
> constitution's Swagger gate depends on it. Record it in plan.md as a justified exception under
> Article III."*

**Status**: `Swashbuckle.AspNetCore` is an approved, justified exception to Article III's package
ban. This record, plus the justification table above, is what the Phase 9 audit checks against —
so the audit reports Article III as **PASS with one recorded exception**, not as a violation.

**Bounds of this exception** — it is exhaustive, exactly like the Tailwind allowance in Article III:

- **Scope**: the backend only, and only to serve interactive API documentation.
- **It grants nothing else.** No other NuGet package is admitted by it. FluentValidation,
  AutoMapper, MediatR and every other package remain banned (§8.8 rejected FluentValidation on
  precisely these grounds, and that rejection stands).
- **No precedent.** Any further package requires its own justification recorded here, or a
  constitution amendment.

**Phase 9 audit note**: the auditor must confirm `TaskManagement.Api.csproj` contains
*exactly* four PackageReference entries — `Swashbuckle.AspNetCore`,
`Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`, and whatever
the template itself adds. A fifth is a violation until justified here.

Everything else stays within the default rules: `Microsoft.EntityFrameworkCore.SqlServer` and
`Microsoft.EntityFrameworkCore.Design` are required to run EF Core and its migration tooling, so
they need no exception.

---

## 5. .NET concepts, anchored to Angular *(Article II)*

**Read this section before §6.** Every concept used later in this plan is introduced here first,
in a few lines, against the Angular idea you already have. Nothing below is code to write yet.

### 5.1 Solution vs Project

A **project** (`.csproj`) is one compiled unit producing one assembly (`.dll`) — it owns its
source, its NuGet dependencies and its build settings. A **solution** (`.sln`) is just a container
listing projects so an IDE can open them together; it compiles nothing itself.

**Angular anchor:** a project is an Angular *application or library* with its own `package.json`;
a solution is the *workspace* file that lists several of them. We build **one project**
(`TaskManagement.Api`); a `.sln` is optional and only worth adding if you open Visual Studio.

### 5.2 Namespaces and `using`

A **namespace** groups types by name (`TaskManagement.Api.Services`); `using X;` brings a
namespace's types into scope for a file. Namespaces follow folder structure by convention, and
modern templates put `using` statements for common namespaces in an implicit global file.

**Angular anchor:** `using` is `import { Thing } from './path'` — except you import a *namespace*,
not a file, and the compiler finds the file itself. There is no barrel file and no relative path.

### 5.3 Attributes

An **attribute** is declarative metadata attached to a class, method or property in square
brackets — `[Required]`, `[HttpGet]`, `[ApiController]`. The framework reads them at startup or
per request and changes its behaviour accordingly.

**Angular anchor:** attributes *are* decorators. `[Required]` is `@Required()`, `[HttpGet]` is a
route decorator. Square brackets instead of `@`, same idea: metadata that a framework acts on.

### 5.4 Dependency Injection and service lifetimes

.NET has a built-in DI container. You register a mapping in `Program.cs` —
`builder.Services.AddScoped<ITaskService, TaskService>()` — and any class can then ask for
`ITaskService` in its constructor; the container supplies it.

**Lifetimes** decide how long one instance lives:

| Lifetime | Meaning | Angular anchor |
|---|---|---|
| **Singleton** | One instance for the application's whole life | `@Injectable({providedIn: 'root'})` |
| **Scoped** | One instance **per HTTP request**, shared by everything in that request | No true equivalent — the browser has no "request". Closest is a service provided at a lazy-loaded route, alive for that activation |
| **Transient** | A fresh instance every time it is injected | A service listed in a component's own `providers: []` |

**Angular anchor:** constructor injection is identical to Angular's. `AddScoped<I, C>()` is what
`providedIn`/`providers` does — the difference is you register centrally rather than on the class.

**The rule that bites:** a longer-lived service must never depend on a shorter-lived one. A
Singleton holding a Scoped `DbContext` is a *captive dependency* — the context is never disposed
and breaks on the second request. `DbContext` is Scoped, therefore `TaskService` is **Scoped**.

### 5.5 `async` / `await` and `Task<T>`

`Task<T>` is .NET's `Promise<T>`; `await` is the same keyword you use in TypeScript. An `async`
method returns `Task<T>` (or `Task` for "void"), and `await` releases the thread until the result
arrives instead of blocking it.

**Angular anchor:** it is `Promise`, **not** `Observable`. A `Task` is a *single* future value —
no stream, no operators, no `subscribe`, no automatic cancellation. And like a Promise it is
**hot**: it starts the moment it is created, unlike a cold Observable that starts on subscribe.

**Why it is mandatory (Article VI):** ASP.NET Core serves requests from a bounded thread pool.
`.Result` or `.Wait()` parks a pooled thread doing nothing while the database works, wasting the
server's capacity and risking deadlock. Every EF Core database call has an `Async` twin —
`ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync` — and every one gets an `await`.

### 5.6 Nullable reference types and `?`

Modern C# tracks nullability in the type system. `string` means "never null" and `string?` means
"may be null"; the compiler warns when you risk dereferencing the latter. For value types, `int?`
and `DateOnly?` are genuinely optional values.

**Angular anchor:** it is TypeScript's `strictNullChecks` with `string | null`, and `?.` works the
same way. This is how the spec's *optional* description and due date are expressed: `string?` and
`DateOnly?`, while `Title` stays a non-nullable `string`.

### 5.7 `DbContext` and `DbSet<T>`

`DbContext` represents **one conversation with the database**. It exposes `DbSet<TaskItem> Tasks`,
a queryable collection mapped to a table, tracks every change you make to objects it loaded, and
writes them all in one transaction when you call `SaveChangesAsync()`.

**Angular anchor:** the closest thing is an injectable data service wrapping `HttpClient` — but
with a memory. It remembers what it handed you and can work out what changed, so you mutate an
object and call save rather than constructing an update payload.

**Why no repository (Article III):** `DbContext` already *is* a Unit of Work and `DbSet<T>`
already *is* a repository. Wrapping them in `ITaskRepository` adds a layer that forwards calls and
hides LINQ, without adding safety. `TaskService` therefore uses `AppDbContext` directly.

### 5.8 LINQ and deferred execution

LINQ is the query syntax over collections: `.Where(...)`, `.OrderByDescending(...)`,
`.Select(...)`. Against a `DbSet`, EF Core **translates it into SQL** rather than running it in
memory. Nothing executes until you call a materialising method — `ToListAsync()`,
`FirstOrDefaultAsync()`, `AnyAsync()`.

**Angular anchor:** the chaining feels like RxJS operators or `Array.prototype` methods, but with
a crucial difference — the chain is *inspected and turned into SQL*, not executed step by step.

**Why it matters here (AC-06.9, AC-07.8):** build the whole query — search, then filter, then
ordering — and materialise **once** at the end. Calling `ToListAsync()` early drags every row into
memory and filters there, which those acceptance criteria explicitly forbid.

### 5.9 Migrations

Your C# entity classes are the source of truth for the database schema. `dotnet ef migrations add
<Name>` compares your model against the previous migration and generates a C# file describing the
difference; `dotnet ef database update` executes it against SQL Server. Migrations are committed
to git, so the schema has a version history.

**Angular anchor:** no direct equivalent — the nearest is a committed, ordered schema changelog,
except it is generated from your classes and checked by the compiler rather than hand-written SQL.

**Tooling note:** `dotnet-ef` is **not currently installed** on this machine (confirmed in §8.1).
Installing it is a prerequisite task in Phase 6, not an afterthought.

### 5.10 DTO — Data Transfer Object

A DTO is a plain class shaped for **the wire**, not for the database. We use three: `CreateTaskDto`
and `UpdateTaskDto` for input, `TaskResponseDto` for output.

**Angular anchor:** it is exactly the `export interface Task { ... }` you write for API responses —
separate from any internal model. The difference is that here the server owns it too.

**Why entities must never leave the controller (Article V):** returning `TaskItem` leaks the
database schema into your public contract, so a column rename becomes a breaking API change. More
urgently, *accepting* an entity enables **over-posting** — a client posts `{"id": 7,
"createdAt": "1999-01-01"}` and model binding cheerfully overwrites values the spec says the
system alone controls (FR-003, FR-005, AC-01.4, AC-01.5). `CreateTaskDto` simply has no `Id` or
`CreatedAt` property, so the attack surface does not exist.

### 5.11 Model binding and `[ApiController]`

**Model binding** is the framework populating your action's parameters from the HTTP request
automatically: JSON body → DTO object, route segment → `int id`, query string → method parameters.
`[FromBody]`, `[FromRoute]` and `[FromQuery]` state the source explicitly; the `[ApiController]`
attribute infers them so you rarely write them.

**Angular anchor:** it is `ActivatedRoute.snapshot.params` plus the typing you give
`http.get<Task[]>()`, except the framework does the reading and converting for you, and a value
that will not convert becomes an automatic `400` instead of a runtime surprise.

`[ApiController]` also **auto-validates**: if any Data Annotation fails, it returns `400` with a
`ProblemDetails` body *before your action body runs*.

### 5.12 Data Annotations

Attributes on DTO properties that declare validation rules: `[Required]`, `[StringLength(200)]`,
`[MaxLength(1000)]`, `[EnumDataType(typeof(Priority))]`.

**Angular anchor:** `Validators.required` and `Validators.maxLength(200)` on a reactive form
control — the same intent, declared on the model instead of wired into the form. Your Angular form
will mirror these rules for immediate feedback; the server's copy is the one that actually counts
(FR-024, AC-09.13).

### 5.13 The middleware pipeline

Every request passes through an ordered chain of middleware, and every response passes back out
through the same chain in reverse — an onion. You compose it in `Program.cs` by the order of
`app.UseXxx()` calls, and **order is behaviour, not style**.

**Angular anchor:** HTTP interceptors. Identical shape: each one can act before calling `next` and
again after it returns, and the order you register them decides what wraps what.

Ours, in order: exception handling → HTTPS redirection → **CORS** → routing → controllers.

### 5.14 CORS

Browsers refuse a request from one origin to another unless the target server explicitly permits
it. The Angular dev server runs on `http://localhost:4200`; the API runs on a different port, so
it **must** opt in by name.

**Angular anchor:** you may have side-stepped this before with `proxy.conf.json`, which hides the
problem by making the browser think everything is same-origin. Here we solve it properly, on the
server, where it belongs.

**The classic first-day failure:** every endpoint works perfectly in Swagger and every one fails
from Angular with a CORS error. Swagger is same-origin with the API, so it never triggers the
check. If that happens, the API is fine — `app.UseCors()` is missing, misordered, or naming the
wrong origin. It must sit **after `UseRouting()` and before the endpoints**.

### 5.15 `appsettings.json`

The runtime configuration file. `appsettings.Development.json` overrides `appsettings.json` when
`ASPNETCORE_ENVIRONMENT=Development`. It holds our connection string, read via
`builder.Configuration.GetConnectionString("DefaultConnection")`.

**Angular anchor:** `environment.ts` / `environment.development.ts` — with one real difference:
Angular bakes the chosen environment into the bundle **at build time**, while .NET reads
`appsettings.json` **at startup**, so you can change it without rebuilding.

**Note:** real secrets belong in `dotnet user-secrets`, not in this file. A LocalDB connection
string using Windows authentication contains no secret, so it is fine here.

---

## 6. Architecture

### 6.1 Request flow

```
Browser (Angular, :4200)
   │  HTTP + JSON
   ▼
┌─────────────────────────────────────────────────────────┐
│ ASP.NET Core (:5xxx)                                    │
│   middleware: exceptions → https → CORS → routing       │
│      ▼                                                  │
│   TasksController          ← thin: bind, call, map      │
│      │  DTO in / DTO out      (Article IV)              │
│      ▼                                                  │
│   ITaskService → TaskService  ← ALL business logic      │
│      │  entity ⇄ DTO mapping happens here               │
│      ▼                                                  │
│   AppDbContext (DbSet<TaskItem>)  ← no repository       │
└──────┬──────────────────────────────────────────────────┘
       │ SQL (async)
       ▼
   SQL Server LocalDB — TaskManagementDb.Tasks
```

**The boundary rule, concretely:** `TaskItem` exists only in the bottom two layers. The controller's
signature never mentions it. Mapping entity ⇄ DTO is hand-written in `TaskService` — no AutoMapper
(Article III), and for one entity it is a few lines.

### 6.2 Backend structure — `backend\TaskManagement.Api\`

```
backend\TaskManagement.Api\
├── Controllers\
│   └── TasksController.cs         # 5 actions, thin      [US-01..US-07]
├── Services\
│   ├── ITaskService.cs            # the contract
│   └── TaskService.cs             # all logic + mapping  [US-01..US-07]
├── Data\
│   └── AppDbContext.cs            # DbSet + model config
├── Models\
│   ├── TaskItem.cs                # entity — never leaves the service
│   ├── Priority.cs                # enum Low/Medium/High
│   └── TaskStatus.cs              # enum Todo/InProgress/Done
├── DTOs\
│   ├── CreateTaskDto.cs           # input  [US-01]
│   ├── UpdateTaskDto.cs           # input  [US-04]
│   └── TaskResponseDto.cs         # output [US-01..US-04]
├── Migrations\                    # generated by dotnet ef
├── Program.cs                     # DI registration + middleware pipeline
├── appsettings.json
└── appsettings.Development.json
```

Exactly the five folders Article III mandates. Nothing else is added.

### 6.3 `ITaskService` — the whole contract

Seven members, one per backend story. Signatures only; no bodies until Phase 7.

| Member | Returns | Story | Not-found convention |
|---|---|---|---|
| `GetAllAsync(string? search, TaskStatus? status)` | `Task<IEnumerable<TaskResponseDto>>` | US-02, US-06, US-07 | n/a — empty list is valid |
| `GetByIdAsync(int id)` | `Task<TaskResponseDto?>` | US-03 | `null` |
| `CreateAsync(CreateTaskDto dto)` | `Task<TaskResponseDto>` | US-01 | n/a |
| `UpdateAsync(int id, UpdateTaskDto dto)` | `Task<TaskResponseDto?>` | US-04 | `null` |
| `DeleteAsync(int id)` | `Task<bool>` | US-05 | `false` |

**Design note — why `null`/`false` rather than exceptions.** A missing task is an expected outcome,
not an exceptional one (FR-026). Returning `null` lets the controller write
`return task is null ? NotFound() : Ok(task)` — one readable line, no exception filter, no custom
exception type, no extra middleware. Throwing would need machinery to translate exceptions into
`404`, which is more concepts for no benefit at this size.

**Note that `GetAllAsync` takes both narrowing parameters together.** That is what makes FR-020 /
AC-07.5 (search and filter combine) fall out naturally instead of needing special handling.

### 6.4 `TasksController` — five thin actions

| Verb & route | Action | Returns | Story |
|---|---|---|---|
| `GET /api/tasks?search=&status=` | `GetAll` | `200` always | US-02, US-06, US-07 |
| `GET /api/tasks/{id}` | `GetById` | `200` / `404` | US-03 |
| `POST /api/tasks` | `Create` | `201` + `Location` / `400` | US-01 |
| `PUT /api/tasks/{id}` | `Update` | `200` / `400` / `404` | US-04 |
| `DELETE /api/tasks/{id}` | `Delete` | `204` / `404` | US-05 |

Every action body is at most three lines: call the service, translate the result into a status
code, return. No `if` on business data, no LINQ, no `DbContext`. That is Article IV in practice.

**Deliberate route choice:** `{id}` carries **no** `:int` constraint. With a constraint,
`/api/tasks/abc` fails to match the route and returns `404`; without one, model binding fails and
`[ApiController]` returns `400` — which is what **AC-03.6** requires.

### 6.5 `Program.cs` — the two things it does

1. **Register services** (before `builder.Build()`): controllers, `AppDbContext` pointed at the
   connection string, `ITaskService → TaskService` as **Scoped**, the CORS policy, OpenAPI/Swagger.
2. **Compose the middleware pipeline** (after `builder.Build()`), in the order given in §5.13.

**Angular anchor:** `Program.cs` is `main.ts` + `app.config.ts` fused — the composition root where
providers are registered and the application is assembled.

### 6.6 Frontend structure — `frontend\task-management-app\`

```
frontend\task-management-app\src\app\
├── models\
│   ├── task.model.ts              # interface mirroring TaskResponseDto
│   └── task-enums.ts              # Priority + TaskStatus as string unions
├── services\
│   └── task.service.ts            # HttpClient; one method per endpoint
├── components\
│   ├── task-list\                 # [US-08] + hosts search/filter [US-12, US-13]
│   ├── task-form\                 # [US-09, US-10] create and edit
│   └── confirm-dialog\            # [US-11]
├── app.routes.ts                  # /  /new  /edit/:id
└── app.config.ts                  # provideHttpClient, provideRouter
```

**One form component, two modes.** US-09 and US-10 share validation rules identical to the
character (AC-10.10). A single `task-form` that switches on the presence of a route `id` avoids
duplicating those rules in two places — which is precisely how they would drift apart.

**Angular CLI 22.1.4 is installed**, so `ng new` defaults apply; the plan does not pin component
style or state approach beyond "whatever the CLI scaffolds", to avoid inventing version-specific
detail that the implementation phase would have to undo.

---

## 7. Traceability — design element to story

| Design element | Stories | Key criteria it exists to satisfy |
|---|---|---|
| `CreateTaskDto` without `Id`/`CreatedAt` | US-01 | AC-01.4, AC-01.5 — over-posting made impossible |
| Data Annotations on both input DTOs | US-01, US-04 | AC-01.6..AC-01.9, AC-04.7 |
| `[ApiController]` auto-400 | US-01, US-04 | AC-01.14, AC-01.15, AC-04.8 |
| `GetAllAsync(search, status)` — one method, both filters | US-02, US-06, US-07 | AC-07.5, AC-07.6 |
| Query built then materialised once | US-06, US-07 | AC-06.9, AC-07.8 |
| `OrderByDescending(CreatedAt)` | US-02 | AC-02.3 |
| `Task<TaskResponseDto?>` null convention | US-03, US-04 | AC-03.3, AC-04.9 |
| `Task<bool>` from `DeleteAsync` | US-05 | AC-05.4 |
| Row physically removed, no flag | US-05 | AC-05.6, FR-030 |
| No route constraint on `{id}` | US-03 | AC-03.6 |
| `DateOnly?` for due date | US-01, US-10 | AC-10.3 — no timezone day-shift |
| Named CORS policy for `:4200` | US-08..US-13 | every frontend story |
| Shared `task-form` component | US-09, US-10 | AC-10.10 |
| `confirm-dialog` | US-11 | AC-11.2, AC-11.3, AC-11.4 |
| Debounced search input | US-12 | AC-12.8 |
| Search + filter held together in list state | US-12, US-13 | AC-13.5, AC-13.6 |

---

## 8. Research & Decisions *(Phase 0)*

### 8.1 Environment — what is actually installed

Probed on this machine, 2026-09-15:

| Component | Found | Consequence |
|---|---|---|
| .NET SDK | **10.0.401** (and 9.0.318) | Target `net10.0` |
| Angular CLI | **22.1.4** | No install needed |
| Node / npm | 24.19.0 / 11.17.0 | Fine |
| **`dotnet-ef`** | **NOT INSTALLED** | ⚠️ Must be installed before any migration — a Phase 6 prerequisite task |
| Full SQL Server | **No MSSQL services** | Cannot use a full instance |
| **SQL Server LocalDB** | Present, but instance **not yet created** | ⚠️ Use LocalDB; create/start it as a prerequisite task |

These two warnings are the difference between a smooth start and half an hour lost to a confusing
error, so both become explicit tasks rather than assumed steps.

### 8.2 Storage — LocalDB

**Decision:** `Server=(localdb)\MSSQLLocalDB;Database=TaskManagementDb;Trusted_Connection=True;TrustServerCertificate=True`

**Rationale:** LocalDB is the only SQL Server present. It is a real SQL Server engine — the same
provider, the same SQL, the same migrations — started on demand, with Windows authentication and
therefore no password anywhere.

**Alternatives rejected:** SQL Server Express (not installed; an installation would consume a
meaningful slice of the one day). SQLite (different provider, different SQL dialect, and the
constitution fixes SQL Server). Docker (not verified present; more moving parts).

### 8.3 Identifier type — `int`

**Decision:** `int Id`, database identity column.

**Rationale:** readable and typeable in a URL (`/api/tasks/3`), which matters when you are
exercising endpoints by hand in Swagger all day. SQL Server identity values are unique and not
reused, satisfying FR-005.

**Rejected:** `Guid` — unique across systems, but `/api/tasks/8f14e45f-...` is miserable to type
into Swagger repeatedly, and there is no distributed system here to benefit.

### 8.4 Due date type — `DateOnly?`

**Decision:** `DateOnly?` in C#, `date` in SQL Server, `"2026-09-20"` in JSON.

**Rationale:** a due date has no time and no timezone. Using `DateTime` invites the classic bug
where a date entered as the 20th comes back as the 19th at 23:00 after a UTC conversion —
**AC-10.3 exists precisely to forbid that**. `DateOnly` makes it structurally impossible rather
than something to remember. It maps natively in EF Core and serialises as `yyyy-MM-dd`, exactly
what Angular's date input produces and expects.

**Rejected:** `DateTime?` (the day-shift bug); storing a string (loses ordering and validation).

### 8.5 `CreatedAt` — `DateTime` in UTC

**Decision:** `DateTime CreatedAt`, set to `DateTime.UtcNow` inside `TaskService.CreateAsync`,
stored as `datetime2`.

**Rationale:** unlike the due date this is an instant, so it needs a time. Setting it in the
service — never from the DTO — is what makes AC-01.5 true by construction. UTC avoids ambiguity;
the Angular client formats for display.

### 8.6 Enum storage — as strings

**Decision:** store both enums as strings in the database via `HasConversion<string>()`, and
serialise them as strings in JSON via `JsonStringEnumConverter`.

**Rationale:** EF's default stores `0/1/2`, so inspecting the table during debugging shows numbers
you must decode. Strings make the data readable in SSMS and make the API self-describing —
`"status": "InProgress"` rather than `"status": 1`, which the Angular client can bind directly.
Filtering still translates to SQL (`WHERE Status = 'Todo'`); nothing moves into memory.

**Cost:** one line of configuration. **Rejected:** integer storage (opaque when debugging);
a lookup table (a second entity, needless).

### 8.7 Case-insensitive search — rely on collation

**Decision:** `query.Where(t => t.Title.Contains(search))`, with no `ToLower()`.

**Rationale:** SQL Server's default collation (`SQL_Latin1_General_CP1_CI_AS`) is
**C**ase-**I**nsensitive, so `Contains` already satisfies AC-06.3 once translated to
`WHERE Title LIKE '%term%'`. Adding `.ToLower()` on both sides is noise, and in general it can
prevent index use.

**Caveat to verify in Phase 8:** this depends on the database collation. If the case-insensitive
test fails, the collation is case-sensitive and the fix is `EF.Functions.Like` with an explicit
collation — not a redesign.

### 8.8 Validation split

**Decision:** shape and length rules live in Data Annotations on the DTOs; existence rules
(`404`) live in `TaskService`.

**Rationale:** `[ApiController]` turns annotation failures into a `400` with a `ProblemDetails`
body before the action runs, which satisfies FR-024/FR-025 with no controller code. "Does this id
exist?" cannot be expressed as an annotation, so it belongs to the service.

**Rejected:** FluentValidation — a third-party package with no justification available under
Article III, replacing rules that annotations already express.

### 8.9 Error format — `ProblemDetails`

**Decision:** accept the framework default (RFC 7807) rather than inventing an error envelope.

**Rationale:** validation failures already arrive as `ProblemDetails` with a per-field `errors`
map, which is exactly what AC-09.13 and AC-10.12 need in order to display a server rejection next
to the right field. A custom format would be work to lose that.

### 8.10 Tailwind setup — the one thing to confirm at implementation time

**Decision:** install Tailwind into the Angular app per the installation guide that matches the
version npm actually resolves, then use utility classes only (Article III bounds).

**Flagged honestly:** Tailwind v4 configures through a PostCSS plugin and a CSS-first
`@import "tailwindcss"`, whereas v3 uses `tailwind.config.js` and the `@tailwind` directives. They
are not interchangeable. Rather than guess which npm resolves on the day and hand you steps that
fail, the implementation task will read the resolved version first and follow the matching path.
This is the only place in the plan where a step is deliberately left to be confirmed rather than
specified.

---

## 9. Implementation sequence (input to Phase 6)

**Backend, fully, before any frontend work** — Article VII and the Swagger gate.

| # | Block | Stories | Gate |
|---|---|---|---|
| 0 | Prerequisites: install `dotnet-ef`, create/start LocalDB | — | `dotnet ef --version` succeeds |
| 1 | Scaffold API project, folders, `appsettings` | — | `dotnet run` serves Swagger |
| 2 | Models + enums + `AppDbContext` + migration | — | `TaskManagementDb.Tasks` exists |
| 3 | DTOs with annotations | US-01, US-04 | compiles |
| 4 | `ITaskService` + `TaskService` + DI registration | US-01..US-07 | compiles |
| 5 | `TasksController` — five actions | US-01..US-05 | all exercised in Swagger |
| 6 | Search + filter parameters | US-06, US-07 | all four combinations proven |
| 7 | CORS policy | — | — |
| — | **🚦 SWAGGER GATE** | — | **every endpoint proven before step 8** |
| 8 | Scaffold Angular app + Tailwind | — | app serves |
| 9 | Model, service, routes | — | list loads from API |
| 10 | Task list | US-08 | — |
| 11 | Create + edit form with validation | US-09, US-10 | — |
| 12 | Delete with confirmation | US-11 | — |
| 13 | Search + filter controls | US-12, US-13 | — |

**If the day runs short,** the cut line is after step 12: a complete CRUD application. Steps 6 and
13 are paired so you never ship a search box with no server behind it.

---

## 10. Quickstart — how to verify it works

Commands are PowerShell, run from the repository root. **Nothing below runs until Phase 7 is
approved.**

**Prerequisites**

```powershell
dotnet tool install --global dotnet-ef
sqllocaldb create MSSQLLocalDB
sqllocaldb start MSSQLLocalDB
```

**Backend**

```powershell
cd C:\Users\moham\OneDrive\Desktop\project\backend\TaskManagement.Api
dotnet ef database update
dotnet run
```

This serves the API on **http://localhost:5178** (§2.1). Do not add `--urls` — it overrides the
committed configuration and desynchronises the Angular client.

Then open the Swagger UI at http://localhost:5178/swagger and work through
[contracts/tasks-api.md](./contracts/tasks-api.md) endpoint by endpoint.

**Frontend** (a second PowerShell window)

```powershell
cd C:\Users\moham\OneDrive\Desktop\project\frontend\task-management-app
npm start
```

**Verification order** — each must pass before the next:

1. `dotnet ef database update` completes and `TaskManagementDb` contains a `Tasks` table.
2. Swagger lists exactly five operations.
3. `POST` returns `201`; `POST` with an empty title returns `400` naming the title.
4. `GET` all returns the created task, newest first.
5. `GET /{id}` returns `200`; a missing id returns `404`; `/api/tasks/abc` returns `400`.
6. `PUT` returns `200` and the change persists; `PUT` to a missing id returns `404` and creates
   nothing.
7. `DELETE` returns `204`; the same id then returns `404`.
8. Search, filter, and **both together** each return the right set.
9. **🚦 Swagger gate passes — frontend work may begin.**
10. The Angular list loads real data with no CORS error in the browser console.
11. Create, edit and delete each work from the UI and the list updates without a manual reload.
12. Search and filter work together from the UI.

**OneDrive note:** never run recursive commands over `node_modules`, `bin` or `obj`.

---

## 11. Out of scope

Authentication, accounts, roles. Automated tests. Docker. CI/CD. Deployment. Pagination. Soft
delete. Overdue highlighting. Sorting chosen by the person. Logging beyond framework defaults.
Caching. Anything in the "Explicitly out of scope" list in `spec.md`.

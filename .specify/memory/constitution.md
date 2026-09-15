<!--
SYNC IMPACT REPORT (scratch — remove before committing the amended constitution)

=== AMENDMENT 2026-09-15 ===
Version change: 1.0.0 → 1.1.0
Bump rationale: MINOR. Existing guidance materially expanded by a new, explicitly bounded
  allowance. No article removed or redefined (would be MAJOR); more than a wording fix (PATCH).

Modified principles:
  Article III. Simple Layered Architecture — added "Granted allowance — Tailwind CSS
    (frontend styling only)" with four bounds (utility classes only; default plugins only;
    no component library on top; frontend only). Ban paragraph now reads "except where this
    article grants an explicit allowance". Rationale extended.

Consistency edits required by the amendment:
  ## Technology, Scope, and Banned Patterns → "Stack (fixed)" previously read "No substitutions,
    no additions", which would have contradicted the new allowance. Reworded to defer to
    Article III. Flagged to the owner rather than made silently.

Added sections: none        Removed sections: none        Deferred TODOs: none

=== INITIAL RATIFICATION 2026-09-14 ===
Version change: (unfilled template) → 1.0.0
Bump rationale: Initial ratification. All placeholder tokens replaced with concrete governance.

Modified principles:
  [PRINCIPLE_1_NAME] → Article I. Phase Gating — Documents Before Code (NON-NEGOTIABLE)
  [PRINCIPLE_2_NAME] → Article II. Teaching-First Delivery (NON-NEGOTIABLE)
  [PRINCIPLE_3_NAME] → Article III. Simple Layered Architecture
  [PRINCIPLE_4_NAME] → Article IV. Thin Controllers, Logic in DI-Registered Services
  [PRINCIPLE_5_NAME] → Article V. DTO Boundary and Mandatory Server-Side Validation

Added sections (beyond the 5-principle scaffold):
  Article VI. Async All The Way To The Database
  Article VII. Traceability — Every Task Serves a Story
  Article VIII. Repository Layout Discipline (NON-NEGOTIABLE)
  ## Technology, Scope, and Banned Patterns   (fills [SECTION_2_NAME]/[SECTION_2_CONTENT])
  ## Development Workflow and Quality Gates   (fills [SECTION_3_NAME]/[SECTION_3_CONTENT])

Removed sections: none

Deferred TODOs: none
-->

# Task Management System Constitution

## Core Principles

### Article I. Phase Gating — Documents Before Code (NON-NEGOTIABLE)

Work proceeds through nine numbered phases (Constitution, Specification, Clarifications,
Stories, Technical Plan, Tasks, Implementation, Testing, Review). Phases 1–6 produce
**documents only**. No application source file — `.cs`, `.ts`, `.html`, `.csproj`,
`.sln`, `package.json`, migration, or config — may be created, scaffolded, or generated
before the project owner explicitly approves entry into Phase 7.

Exactly one phase runs per turn. At the end of each phase the agent MUST stop, present
the produced artifacts, explain them, and wait for approval. Chaining two phases in a
single turn is a constitutional violation, even when the next phase seems obvious.

*Rationale:* The owner has one day and is learning .NET. Premature code destroys the
review checkpoints that make the work understandable and the scope defensible.

### Article II. Teaching-First Delivery (NON-NEGOTIABLE)

The project owner is an experienced Angular developer and new to .NET. Every .NET or
ASP.NET Core concept MUST be explained in 3–6 lines **before** it is used for the first
time, and each explanation MUST be anchored to the Angular equivalent the owner already
knows (e.g. `Program.cs` service registration ↔ Angular `providers`; attributes ↔
decorators; `Task<T>`/`await` ↔ `Observable`/`Promise`; interfaces ↔ TypeScript
interfaces and injection tokens).

Concepts requiring an anchored explanation include, at minimum: Solution vs Project,
Dependency Injection and service lifetimes, `DbContext`, Migrations, DTO, Model Binding,
Data Annotations, `async`/`await`, the Middleware pipeline, CORS, and `appsettings.json`.

*Rationale:* The deliverable is understanding plus a working app. Code the owner cannot
explain is a failed deliverable regardless of whether it runs.

### Article III. Simple Layered Architecture

The backend is ONE ASP.NET Core Web API project with exactly five folders:
`Controllers/`, `Services/`, `Data/`, `Models/`, `DTOs/`. No additional architectural
layer, project, or assembly may be introduced.

The following are explicitly BANNED: microservices, CQRS, MediatR, the Repository
pattern layered on top of EF Core, Unit of Work, AutoMapper, generic base
controllers/services, and any third-party NuGet or npm package that is not required to
run Angular, ASP.NET Core, or EF Core, except where this article grants an explicit
allowance. Any proposal to add a pattern or dependency MUST be rejected unless a
required feature is impossible without it, and the justification MUST be recorded in
`plan.md`.

**Granted allowance — Tailwind CSS (frontend styling only).** Tailwind CSS is permitted
as the styling solution for the Angular application, bounded as follows:

- **Utility classes only.** Styling is applied through Tailwind's utility classes in
  component templates. Hand-written CSS is permitted only where a utility genuinely does
  not exist for the need.
- **Default plugins only.** No Tailwind plugin beyond what ships in a default
  installation may be added — including, but not limited to, `@tailwindcss/forms`,
  `@tailwindcss/typography`, and `@tailwindcss/aspect-ratio`.
- **No component library on top of it.** DaisyUI, Flowbite, Angular Material, PrimeNG,
  PrimeFlex, Bootstrap, and every other pre-built component or widget library remain
  BANNED. Components are built from Tailwind utilities directly.
- **Frontend only.** This allowance grants nothing to the backend. The .NET package
  rules in the paragraph above are unchanged: no NuGet package beyond what ASP.NET Core
  and EF Core require.

This allowance is exhaustive. It does not establish a precedent for further frontend
packages; any other addition still requires an amendment under Governance.

*Rationale:* `DbContext` is already a Unit of Work and `DbSet<T>` is already a
repository. Wrapping them adds layers to learn, not safety. Tailwind is admitted as a
deliberate exception because styling is otherwise a significant time cost in a one-day
build, and utility classes keep the styling decisions visible in the template the owner
is already reading rather than hidden in a separate stylesheet. The bounds exist so the
exception stays a styling tool and never becomes an architecture.

### Article IV. Thin Controllers, Logic in DI-Registered Services

Controllers MUST only: bind and validate the incoming request, call a single service
method, and map the result to an HTTP status code. Controllers MUST NOT contain business
rules, LINQ queries, `DbContext` access, or entity-to-DTO mapping logic.

All business logic lives in a service class exposed through an interface (e.g.
`ITaskService` / `TaskService`) and registered in the DI container in `Program.cs`. The
service is delivered to the controller by constructor injection only — no service
locator, no `new`, no static access.

*Rationale:* This is the same separation as an Angular component calling an injected
service; keeping it makes both testing and reasoning trivial.

### Article V. DTO Boundary and Mandatory Server-Side Validation

EF Core entities MUST NEVER cross the controller boundary — not as a parameter, not as a
return value, not nested inside another object. Every request and response uses a
purpose-built DTO in `DTOs/`.

Server-side validation is mandatory and authoritative. Request DTOs MUST carry Data
Annotations, every write endpoint MUST check `ModelState` (or rely on
`[ApiController]` automatic 400 responses), and business-rule validation that annotations
cannot express MUST live in the service layer. Client-side Angular validation is a UX
convenience and is NEVER trusted as the enforcement point.

*Rationale:* Entities leak the database schema and open over-posting holes; any HTTP
client can bypass the browser, so the server must be the last line of defence.

### Article VI. Async All The Way To The Database

Every operation that touches the database MUST be asynchronous: `async` service methods
returning `Task`/`Task<T>`, awaited EF Core async APIs (`ToListAsync`,
`FirstOrDefaultAsync`, `SaveChangesAsync`), and `async` controller actions. Blocking
calls — `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`, and synchronous EF Core
methods on request paths — are forbidden.

*Rationale:* ASP.NET Core serves requests from a bounded thread pool; blocking a thread
on I/O wastes the server's capacity and can deadlock.

### Article VII. Traceability — Every Task Serves a Story

Requirements flow one way and never break the chain:
`spec.md` → `clarifications.md` → `stories/` → `plan.md` → `tasks.md` → code.

Stories are numbered `US-01`–`US-07` (backend) and `US-08`–`US-13` (frontend). Each story
file states ID, title, actor, description, acceptance criteria, and the linked endpoint.
Every task in `tasks.md` MUST use the form `T-xx [US-yy] description` and reference a
story that exists. A task with no story, or a story with no task, is a defect that MUST
be fixed before the phase is approved. If Spec Kit emits stories inline in `spec.md`,
they MUST still be mirrored into the `stories/` tree as individual files.

*Rationale:* Traceability is what lets a one-day build prove it is complete rather than
merely finished.

### Article VIII. Repository Layout Discipline (NON-NEGOTIABLE)

The repository layout is fixed and MUST be reproduced exactly:

```
.specify\memory\constitution.md
specs\001-task-management\spec.md
specs\001-task-management\clarifications.md
specs\001-task-management\plan.md
specs\001-task-management\data-model.md
specs\001-task-management\contracts\tasks-api.md
specs\001-task-management\tasks.md
stories\README.md                       (index + traceability matrix)
stories\backend\US-01..US-07.md
stories\frontend\US-08..US-13.md
backend\TaskManagement.Api\             (ALL .NET code)
frontend\task-management-app\           (ALL Angular code)
```

No project, solution, source, or configuration file may ever be created in the repository
root. All .NET code lives under `backend\TaskManagement.Api\`; all Angular code lives
under `frontend\task-management-app\`. The `stories\` tree is a hard requirement and is
not optional even when its content duplicates `spec.md`.

*Rationale:* A predictable tree is what makes the traceability matrix and the final audit
mechanical instead of a search.

## Technology, Scope, and Banned Patterns

**Stack (fixed):** Angular (frontend), ASP.NET Core Web API (backend), SQL Server
(database), Entity Framework Core (data access), plus Tailwind CSS for frontend styling
under the bounds granted in Article III. No substitutions, and no additions beyond the
allowances Article III grants explicitly.

**Domain (fixed):** entity `TaskItem` with `Id`, `Title`, `Description`, `Priority`,
`Status`, `DueDate`, `CreatedAt`. Enum `Priority`: `Low`, `Medium`, `High`. Enum
`Status`: `Todo`, `InProgress`, `Done`.

**API surface (fixed):**

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/tasks` | List all; search and filter via query string |
| GET | `/api/tasks/{id}` | Get one by id |
| POST | `/api/tasks` | Create |
| PUT | `/api/tasks/{id}` | Update |
| DELETE | `/api/tasks/{id}` | Delete |

Search by title and filter by status MUST be query-string parameters on `GET /api/tasks`.
Creating dedicated `/search` or `/filter` endpoints is forbidden.

**Required features:** backend — create, get all, get by id, update, delete, search by
title, filter by status. Frontend — list, create form, edit form, delete, search, filter
by status, client-side validation.

**Scope ceiling:** the build must be completable in one day. Authentication, user
accounts, roles, file upload, real-time updates, caching, background jobs, containers,
CI/CD, and automated test projects are OUT OF SCOPE unless the owner explicitly adds
them by amending this constitution.

**Environment:** Windows 11 with PowerShell. Every command given to the owner MUST use
Windows paths and PowerShell syntax; Bash syntax is never used. The repository lives
inside OneDrive, so recursive or bulk operations over `node_modules`, `bin`, and `obj`
are forbidden.

## Development Workflow and Quality Gates

**The nine phases, executed one per turn:**

1. **Constitution** — `/speckit-constitution` → `.specify\memory\constitution.md`
2. **Specification** — `/speckit-specify` → `spec.md`, business language only, zero
   technology detail
3. **Clarifications** — `/speckit-clarify`; open questions are ASKED of the owner and
   the answers recorded in `clarifications.md`
4. **Stories** — the `stories\` tree plus the traceability matrix in `stories\README.md`
5. **Technical Plan** — `/speckit-plan` → `plan.md`, `data-model.md`,
   `contracts\tasks-api.md`; this is the deep .NET teaching phase
6. **Tasks** — `/speckit-tasks` → `tasks.md` with backend tasks fully ordered before
   frontend tasks, then `/speckit-analyze` and its report shown to the owner
7. **Implementation** — `/speckit-implement`, only after explicit approval; backend
   first and verified in Swagger, then Angular; announce `T-xx [US-yy]` before each task
8. **Testing** — a manual test checklist mapped to story acceptance criteria
9. **Review** — an audit of the delivered code against every article of this constitution

**Gates:**

- **Approval gate** — each phase ends with STOP + present + explain + wait. Silence is
  not approval.
- **Traceability gate** — Phase 6 does not pass until every `T-xx` maps to a real
  `US-yy` and every story has at least one task.
- **Simplicity gate** — any new pattern, layer, or dependency must be justified in
  writing or rejected (Article III).
- **Swagger gate** — no Angular work begins until every backend endpoint has been
  exercised successfully in Swagger.
- **Teaching gate** — a phase is incomplete if it used a .NET concept that was never
  explained with its Angular anchor (Article II).

## Governance

This constitution supersedes all other conventions, habits, and defaults, including any
default behaviour of Spec Kit commands or the assisting agent. Where a Spec Kit template
and this constitution disagree, this constitution wins.

**Amendment procedure.** Amendments are proposed to the project owner in writing, stating
the article affected, the change, and the reason. Only the project owner may approve an
amendment. Approved amendments are written into this file immediately, with the version
and `Last Amended` date updated in the same edit.

**Versioning policy.** Semantic versioning. MAJOR — an article is removed or redefined in
a backward-incompatible way. MINOR — a new article or section is added, or existing
guidance is materially expanded. PATCH — clarifications, wording, and typo fixes that do
not change meaning.

**Compliance review.** Every phase handoff states which articles the produced artifacts
satisfy. Phase 9 is a full audit of the codebase against Articles I–VIII, reporting each
article as PASS or FAIL with file-level evidence. A FAIL must be fixed or explicitly
waived by the owner as a recorded amendment; it is never silently accepted.

**Version**: 1.1.0 | **Ratified**: 2026-09-14 | **Last Amended**: 2026-09-15

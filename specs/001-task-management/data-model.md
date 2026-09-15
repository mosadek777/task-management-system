# Data Model: Task Management System

**Feature**: `001-task-management` | **Date**: 2026-09-15
**Plan**: [plan.md](./plan.md) · **Contracts**: [contracts/tasks-api.md](./contracts/tasks-api.md)

> **Prerequisite reading.** This document uses `DbContext`, `DbSet`, migrations, DTOs, Data
> Annotations and nullable reference types. All are explained against their Angular equivalents in
> **plan.md §5**. Read that first if any term here is unfamiliar.

One entity, two enums, three DTOs, one table. That is the entire data model.

---

## 1. Entity — `TaskItem`

Lives in `Models\TaskItem.cs`. **This class never leaves `TaskService`** (Article V).

| Property | C# type | Null? | Database column | Source of value |
|---|---|---|---|---|
| `Id` | `int` | no | `int`, PK, `IDENTITY(1,1)` | Database |
| `Title` | `string` | **no** | `nvarchar(200)`, `NOT NULL` | Client |
| `Description` | `string?` | yes | `nvarchar(1000)`, `NULL` | Client |
| `Priority` | `Priority` | no | `nvarchar(10)`, `NOT NULL` | Client, defaults `Medium` |
| `Status` | `TaskStatus` | no | `nvarchar(12)`, `NOT NULL` | Client, defaults `Todo` |
| `DueDate` | `DateOnly?` | yes | `date`, `NULL` | Client |
| `CreatedAt` | `DateTime` | no | `datetime2`, `NOT NULL` | **Server only** |

### Why each type was chosen

- **`int Id`** — readable and quick to type into Swagger all day. Identity values are unique and
  never reused, satisfying FR-005. (plan.md §8.3)
- **`string` vs `string?`** — the nullable annotation *is* the optionality rule. `Title` is
  non-nullable, `Description` is nullable. This is TypeScript's `string | null` under
  `strictNullChecks`. (plan.md §5.6)
- **`DateOnly?` for `DueDate`** — a due date has no time and no timezone. `DateTime` here is how
  you get the bug where the 20th is stored and the 19th comes back; **AC-10.3 forbids exactly
  that**. `DateOnly` makes it structurally impossible. Maps to SQL `date`, serialises as
  `"2026-09-20"`. (plan.md §8.4)
- **`DateTime CreatedAt`** — an instant, so it needs a time. Set to `DateTime.UtcNow` **inside
  `TaskService.CreateAsync`**, never bound from a DTO. That is what makes AC-01.5 true by
  construction rather than by discipline.

### Invariants

1. `Id` is assigned by the database and never changes (FR-011, AC-04.3).
2. `CreatedAt` is assigned once at creation and never changes (FR-011, AC-04.4).
3. `Title` is trimmed before storage and is never empty (FR-002, AC-01.7).
4. There is **no** `IsDeleted` flag. Deletion removes the row (FR-030, AC-05.6).
5. No relationships to any other entity — tasks are independent.

---

## 2. Enums

`Models\Priority.cs`

| Member | Underlying value | Stored as |
|---|---|---|
| `Low` | **0** | `"Low"` |
| `Medium` | 1 | `"Medium"` ← the required default (FR-004) |
| `High` | 2 | `"High"` |

> ### ⚠️ The default is NOT the first member — read before writing `CreateTaskDto`
>
> C# numbers enum members `0, 1, 2…` in declaration order, so **`Low` is 0** — and 0 is exactly
> what `Priority` becomes when the JSON omits the property. Left alone, omitting `priority` would
> store **Low**, silently violating FR-004 and AC-01.11.
>
> **The fix is a property initializer on the DTO, not a reordered enum:**
>
> - `public Priority Priority { get; set; } = Priority.Medium;`
> - `public TaskStatus Status { get; set; } = TaskStatus.Todo;`
>
> `System.Text.Json` leaves an initialized property untouched when the incoming JSON omits it, so
> the initializer *is* the default.
>
> **Do not "fix" this by reordering the enum.** `Low, Medium, High` is the correct semantic order
> and the order the API exposes. Putting `Medium` first to make it zero would make the enum read
> wrongly forever to solve a problem one initializer solves cleanly.
>
> `TaskStatus` happens to be correct by luck — `Todo` is declared first, so it is already 0. It
> still gets an explicit initializer, so neither default depends on declaration order.

`Models\TaskStatus.cs`

| Member | Stored as | Displayed as |
|---|---|---|
| `Todo` | `"Todo"` | "To Do" |
| `InProgress` | `"InProgress"` | "In Progress" |
| `Done` | `"Done"` | "Done" |

**Naming collision — worth knowing before it bites.** .NET already has a `System.Threading.Tasks.TaskStatus`.
Our `TaskManagement.Api.Models.TaskStatus` is a different type that happens to share a short name.
If the compiler ever reports a baffling conversion error mentioning `TaskStatus`, this is why; the
fix is a fully-qualified name or an alias, not a redesign. This is also why the entity is called
`TaskItem` rather than `Task` — `Task` is the async return type from plan.md §5.5, and reusing the
name would make every async signature in the project ambiguous.

**Stored as strings** via `HasConversion<string>()`: the table is readable when you inspect it, and
the API is self-describing (`"status": "InProgress"`, not `"status": 1`). Filtering still
translates to SQL. (plan.md §8.6)

**Display labels belong to the frontend.** The API returns `"InProgress"`; the Angular client
renders "In Progress" (AC-08.5, AC-13.9). The wire format is never prettified server-side.

---

## 3. `AppDbContext`

`Data\AppDbContext.cs`. One `DbSet`, one configuration block.

**Configured in `OnModelCreating`:**

| Configuration | Serves |
|---|---|
| `Title` required, max length 200 | FR-023 — enforced at the database too, not only in DTOs |
| `Description` max length 1000 | FR-023 |
| `Priority` → `HasConversion<string>()`, max length 10 | plan.md §8.6 |
| `Status` → `HasConversion<string>()`, max length 12 | plan.md §8.6 |
| Index on `Status` | Filtering (US-07) |
| Index on `CreatedAt` descending | Default ordering (AC-02.3) |

**Registered as Scoped** — one instance per HTTP request. `TaskService` must therefore also be
Scoped; a Singleton holding a Scoped context is a captive dependency that breaks on the second
request. (plan.md §5.4)

**The two indexes are the only performance work in this project.** They exist because every single
list request orders by `CreatedAt` and most filter by `Status`. At 100 rows this changes nothing
measurable — it is one line each and establishes the habit.

---

## 4. Table — `Tasks`

Generated by migration, not hand-written. Shown so you can confirm what EF produced.

```sql
CREATE TABLE [Tasks] (
    [Id]          int             IDENTITY(1,1) NOT NULL,
    [Title]       nvarchar(200)   NOT NULL,
    [Description] nvarchar(1000)  NULL,
    [Priority]    nvarchar(10)    NOT NULL,
    [Status]      nvarchar(12)    NOT NULL,
    [DueDate]     date            NULL,
    [CreatedAt]   datetime2       NOT NULL,
    CONSTRAINT [PK_Tasks] PRIMARY KEY ([Id])
);
CREATE INDEX [IX_Tasks_Status]    ON [Tasks] ([Status]);
CREATE INDEX [IX_Tasks_CreatedAt] ON [Tasks] ([CreatedAt] DESC);
```

**Migration plan:** a single migration, `InitialCreate`. No schema changes are anticipated, because
the model is fully settled by the Phase 3 clarifications.

```powershell
dotnet ef migrations add InitialCreate
dotnet ef database update
```

`dotnet-ef` is **not installed on this machine** — see plan.md §8.1. Installing it is a
prerequisite task, not a step to discover mid-flow.

---

## 5. DTOs

Three classes in `DTOs\`. The reason they exist — schema leakage and over-posting — is in
plan.md §5.10.

### 5.1 `CreateTaskDto` — input for `POST /api/tasks` [US-01]

| Property | Type | Annotations | Enforces |
|---|---|---|---|
| `Title` | `string` | `[Required(AllowEmptyStrings = false)]`, `[StringLength(200, MinimumLength = 1)]` | AC-01.6, AC-01.7, AC-01.8 |
| `Description` | `string?` | `[StringLength(1000)]` | AC-01.9 |
| `Priority` | `Priority` | `[EnumDataType(typeof(Priority))]` **+ `= Priority.Medium`** | AC-01.12, **AC-01.11** |
| `Status` | `TaskStatus` | `[EnumDataType(typeof(TaskStatus))]` **+ `= TaskStatus.Todo`** | AC-01.12, **AC-01.11** |
| `DueDate` | `DateOnly?` | *(none — deliberately)* | AC-01.13 |

**The two initializers are load-bearing, not cosmetic.** Without `= Priority.Medium`, an omitted
`priority` deserialises to `Low` (the zero value) and AC-01.11 fails. See the warning in §2.

**It has no `Id` and no `CreatedAt`.** That is the entire over-posting defence: a client can post
`{"id": 99, "createdAt": "1999-01-01"}` and model binding has nowhere to put those values, so
AC-01.4 and AC-01.5 hold structurally. No code checks for them, because no code needs to.

**`DueDate` carries no validation attribute on purpose.** FR-028 says past dates are accepted and
no date-range rule exists anywhere. The absence is a decision, not an omission — recorded here so
the Phase 9 audit does not "fix" it.

**`[Required]` alone would not catch `"   "`.** A whitespace-only title is a non-null string, so
`[Required]` passes it. `MinimumLength = 1` catches it **only after the service trims** — so
`TaskService` trims the title before validation-sensitive use and before storage (AC-01.7).

### 5.2 `UpdateTaskDto` — input for `PUT /api/tasks/{id}` [US-04]

Identical properties and identical annotations to `CreateTaskDto`, **including the two default
initializers** (`= Priority.Medium`, `= TaskStatus.Todo`). AC-04.7 requires the rules to match
"identically", and a `PUT` replaces the editable values.

**No `Id` property.** The id in the route is authoritative; an id in the body would create a
contradiction to resolve. Having nowhere to put it is simpler than deciding which wins (AC-04.3).

**Kept as a separate class rather than reusing `CreateTaskDto`.** They are identical today, and
sharing one class would be less code. They are separate because they are contracts for two
different operations that are free to diverge — and because a shared class makes Swagger show the
same schema name for both, which is misleading. This is a deliberate, minor duplication.

### 5.3 `TaskResponseDto` — output for every endpoint that returns a task

| Property | Type | Notes |
|---|---|---|
| `Id` | `int` | |
| `Title` | `string` | |
| `Description` | `string?` | `null` when absent — AC-08.3 renders it, never "null" |
| `Priority` | `Priority` | `"Low"` \| `"Medium"` \| `"High"` |
| `Status` | `TaskStatus` | `"Todo"` \| `"InProgress"` \| `"Done"` |
| `DueDate` | `DateOnly?` | `"2026-09-20"` or `null` |
| `CreatedAt` | `DateTime` | UTC |

Carries every stored value, so AC-03.2 ("complete detail") is satisfied by one shape and there is
no need for a separate summary DTO. The list endpoint returns the same shape.

---

## 6. Mapping

Hand-written in `TaskService`. No AutoMapper (Article III) — for one entity it is a few lines, and
those lines are where `CreatedAt` and `Id` are deliberately *not* copied from input.

| Direction | When | Notes |
|---|---|---|
| `CreateTaskDto` → `TaskItem` | `CreateAsync` | Trim `Title`. Set `CreatedAt = DateTime.UtcNow`. `Id` left for the database. |
| `UpdateTaskDto` → existing `TaskItem` | `UpdateAsync` | Assign editable properties onto the **tracked** entity. Never touch `Id` or `CreatedAt`. |
| `TaskItem` → `TaskResponseDto` | all reads | Straight copy. |

**How the update actually saves.** Load the entity with `FirstOrDefaultAsync`; if `null`, return
`null` so the controller produces `404` (AC-04.9). Otherwise assign the changed properties onto the
loaded object and call `SaveChangesAsync()`. You never call an `Update` method — the context is
*tracking* that object and works out the `UPDATE` statement itself. That change tracking is the
part with no Angular equivalent, and the part most likely to feel like magic the first time.

---

## 7. Query composition — one method, both filters

`GetAllAsync(string? search, TaskStatus? status)` builds a query in stages and materialises it
**once**:

1. Start from `_context.Tasks.AsNoTracking()`.
2. If `search` is not null/whitespace → `.Where(t => t.Title.Contains(search))`.
3. If `status` has a value → `.Where(t => t.Status == status)`.
4. `.OrderByDescending(t => t.CreatedAt)`.
5. `.Select(...)` into `TaskResponseDto`.
6. `await ...ToListAsync()` — the only place anything executes.

**Why this satisfies FR-020 without special handling.** Both narrowings are optional `if`s over the
same query object, so all four combinations — neither, search, status, both — fall out of the same
five lines. AC-07.6 needs no dedicated code path.

**`AsNoTracking()`** tells EF not to keep these objects for change detection. Reads never write, so
tracking would be wasted work. (Omit it in `UpdateAsync`, where tracking is exactly what you want.)

**Whitespace-only search is ignored** at step 2 — `string.IsNullOrWhiteSpace` — which is AC-06.7.

**Nothing executes before step 6.** That is LINQ's deferred execution (plan.md §5.8) and it is what
AC-06.9 and AC-07.8 demand: the filtering happens in SQL, not in memory. Calling `ToListAsync()`
earlier would load every row and filter in C# — the exact failure those criteria name.

---

## 8. Requirement coverage

| Requirement | Where it lives in this model |
|---|---|
| FR-002 title required, trimmed, no minimum | `[Required]` + `[StringLength(200, MinimumLength = 1)]` + service trim |
| FR-003 capture moment automatic | `CreatedAt` set in `CreateAsync`; absent from both input DTOs |
| FR-004 defaults Medium / Todo | **Property initializers on the input DTOs** (`= Priority.Medium`, `= TaskStatus.Todo`) — *not* enum member order, which would give `Low`. See the warning in §2. |
| FR-005 unique identifier | `IDENTITY(1,1)` primary key |
| FR-008 newest first | `OrderByDescending(CreatedAt)` + descending index |
| FR-011 id and capture moment preserved | Neither is present on `UpdateTaskDto`; mapping never assigns them |
| FR-013 clearable description / due date | Both nullable end to end: `string?`, `DateOnly?`, `NULL` columns |
| FR-017/018 contains, case-insensitive | `.Contains()` + SQL Server's case-insensitive default collation |
| FR-020 search and filter combine | §7 — two optional `Where` clauses on one query |
| FR-022 reject invalid enum values | `[EnumDataType]` + strings in the database |
| FR-023 length limits | Annotations **and** `nvarchar(200)` / `nvarchar(1000)` |
| FR-028 past due dates accepted | Deliberate absence of any date validation |
| FR-029 no pagination | `GetAllAsync` returns a plain collection; no skip/take, no count |
| FR-030 permanent removal | Row deleted; no `IsDeleted` column exists |

# API Contract: Tasks

**Feature**: `001-task-management` | **Date**: 2026-09-15
**Plan**: [plan.md](../plan.md) · **Data model**: [data-model.md](../data-model.md) · **Stories**: [index](../../../stories/README.md)

**Base path**: `/api/tasks` · **Media type**: `application/json` · **Auth**: none (single user, by
constitution)

**Five operations, no more.** Search and status filtering are query parameters on the list
operation — creating `/api/tasks/search` or `/api/tasks/filter` is forbidden by the constitution's
fixed API surface.

| # | Verb | Route | Story |
|---|---|---|---|
| 1 | `GET` | `/api/tasks?search=&status=` | US-02, US-06, US-07 |
| 2 | `GET` | `/api/tasks/{id}` | US-03 |
| 3 | `POST` | `/api/tasks` | US-01 |
| 4 | `PUT` | `/api/tasks/{id}` | US-04 |
| 5 | `DELETE` | `/api/tasks/{id}` | US-05 |

---

## Shared shapes

### `TaskResponse` — returned by operations 1–4

```json
{
  "id": 3,
  "title": "Write report",
  "description": "Q3 summary for the board",
  "priority": "Medium",
  "status": "InProgress",
  "dueDate": "2026-09-20",
  "createdAt": "2026-09-15T09:14:22.1234567Z"
}
```

| Field | Type | Null? | Notes |
|---|---|---|---|
| `id` | integer | no | Server-assigned |
| `title` | string | no | 1–200 characters, trimmed |
| `description` | string | **yes** | `null` when absent — never `""`, never omitted |
| `priority` | string | no | `"Low"` \| `"Medium"` \| `"High"` |
| `status` | string | no | `"Todo"` \| `"InProgress"` \| `"Done"` |
| `dueDate` | string | **yes** | `yyyy-MM-dd`, date only, no time, no timezone |
| `createdAt` | string | no | ISO 8601 UTC instant |

**Enums travel as strings**, never as numbers, so the Angular client binds them directly
(data-model.md §2). Display labels — "In Progress" — are the client's job (AC-08.5, AC-13.9).

**`dueDate` has no time component by design.** It is a `date`, which is what prevents the
timezone day-shift that AC-10.3 forbids (plan.md §8.4).

### `TaskRequest` — accepted by operations 3 and 4

```json
{
  "title": "Write report",
  "description": "Q3 summary for the board",
  "priority": "Medium",
  "status": "Todo",
  "dueDate": "2026-09-20"
}
```

| Field | Required | Rules |
|---|---|---|
| `title` | **yes** | Non-blank after trimming; max 200 characters |
| `description` | no | Max 1000 characters; `null` or omitted means none |
| `priority` | no | Defaults to `"Medium"` |
| `status` | no | Defaults to `"Todo"` |
| `dueDate` | no | `yyyy-MM-dd`; **any date is valid, including the past** |

**`id` and `createdAt` are not part of the request shape.** They are absent from the DTO
entirely, so sending them has no effect — this is what makes AC-01.4 and AC-01.5 structural rather
than a check someone might forget (data-model.md §5.1).

### `ProblemDetails` — every `400` and `404`

RFC 7807, produced by the framework. Validation failures carry a per-field `errors` map, which is
what lets the Angular forms show a server rejection next to the right field (AC-09.13, AC-10.12).

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Title": ["The Title field is required."]
  }
}
```

---

## 1. `GET /api/tasks` — list, search, filter

**Stories**: US-02 (list) · US-06 (search) · US-07 (filter)

### Query parameters

| Name | Type | Required | Behaviour |
|---|---|---|---|
| `search` | string | no | Case-insensitive **contains** match on `title` only. Omitted, empty or whitespace-only = no search. |
| `status` | string | no | One of `Todo`, `InProgress`, `Done`. Omitted = all states. |

### Responses

| Code | When | Body |
|---|---|---|
| `200` | Always, including no matches | `TaskResponse[]` — possibly `[]` |
| `400` | `status` is not a permitted value | `ProblemDetails` |

**`200` with `[]` is the correct empty response — never `404`** (AC-02.4, AC-06.5, AC-07.4). An
empty result is a normal outcome, and the client distinguishes "no tasks at all" from "nothing
matched your search" from the request it sent, not from the status code.

**Plain array, no envelope.** No `totalCount`, no `page`, no `items` wrapper — FR-029 (AC-02.5).

**Ordering**: always newest `createdAt` first, in every combination (AC-02.3, AC-06.8, AC-07.7).

### The four combinations — all must work (AC-07.6)

| Request | Returns |
|---|---|
| `GET /api/tasks` | Everything |
| `GET /api/tasks?search=report` | Titles containing "report", any status |
| `GET /api/tasks?status=Done` | All done tasks |
| `GET /api/tasks?search=report&status=Done` | **Both** — done tasks whose title contains "report" |

The fourth row is FR-020 / AC-07.5 and is the one most likely to be missed. Test it explicitly.

### Examples

```http
GET /api/tasks?search=REPORT
→ 200  [ { "id": 3, "title": "Write report", ... } ]        # case-insensitive (AC-06.3)

GET /api/tasks?search=rep
→ 200  [ { "id": 3, "title": "Write report", ... } ]        # matches mid-title (AC-06.2)

GET /api/tasks?search=%20
→ 200  [ ...all tasks... ]                                   # whitespace ignored (AC-06.7)

GET /api/tasks?search=zzzznomatch
→ 200  []                                                    # not a 404 (AC-06.5)

GET /api/tasks?status=Archived
→ 400  ProblemDetails                                        # never silently ignored (AC-07.3)
```

**An invalid `status` must be rejected, not ignored.** Silently returning an unfiltered list would
show the person every task while their filter says otherwise — worse than an error.

---

## 2. `GET /api/tasks/{id}` — get one

**Story**: US-03

| Code | When | Body |
|---|---|---|
| `200` | Task exists | `TaskResponse` |
| `404` | No task with that id, including a deleted one | `ProblemDetails` |
| `400` | `{id}` is not an integer | `ProblemDetails` |

```http
GET /api/tasks/3     → 200  { "id": 3, ... }
GET /api/tasks/999   → 404                      # AC-03.3
GET /api/tasks/abc   → 400                      # AC-03.6
```

**Why `/abc` is `400` and not `404`.** The route declares **no** `:int` constraint. With one, the
route would simply not match and ASP.NET Core would return `404`; without one, model binding fails
and `[ApiController]` produces `400`. AC-03.6 requires `400`, so the constraint is deliberately
omitted (plan.md §6.4). If you ever see `404` for `/abc`, someone added `{id:int}`.

**A `404` changes nothing and creates nothing** (AC-03.4).

---

## 3. `POST /api/tasks` — create

**Story**: US-01

**Request**: `TaskRequest`

| Code | When | Body |
|---|---|---|
| `201` | Created | `TaskResponse` with the new `id` and `createdAt` |
| `400` | Any validation rule fails | `ProblemDetails` with an `errors` map |

**`201` carries a `Location` header** pointing at the new task — `Location: /api/tasks/4`
(AC-01.3). Produced by returning `CreatedAtAction` naming the `GetById` action.

### Success

```http
POST /api/tasks
{ "title": "Write report" }

→ 201  Location: /api/tasks/4
{
  "id": 4,
  "title": "Write report",
  "description": null,
  "priority": "Medium",          ← default (AC-01.11)
  "status": "Todo",              ← default (AC-01.11)
  "dueDate": null,
  "createdAt": "2026-09-15T09:14:22.1234567Z"   ← server-assigned (AC-01.5)
}
```

### Rejections — every one must be exercised

| Body | Code | Criterion |
|---|---|---|
| `{}` — no title | `400` | AC-01.6 |
| `{"title": ""}` | `400` | AC-01.6 |
| `{"title": "   "}` | `400` | AC-01.6 — whitespace-only is not a title |
| `{"title": "<201 chars>"}` | `400`, message states the limit | AC-01.8 |
| `{"title": "x", "description": "<1001 chars>"}` | `400`, message states the limit | AC-01.9 |
| `{"title": "x", "priority": "Urgent"}` | `400` | AC-01.12 |
| `{"title": "x", "status": "Archived"}` | `400` | AC-01.12 |

**Nothing is stored when a request is rejected** (AC-01.15). Verify by listing afterwards.

### Accepted, and worth proving

| Body | Result | Criterion |
|---|---|---|
| `{"title": "  Write report  "}` | Stored as `"Write report"` | AC-01.7 — trimmed |
| `{"title": "PR"}` | Accepted | AC-01.7 — no minimum length |
| `{"title": "x", "dueDate": "2020-01-01"}` | Accepted, no warning | AC-01.13 — past dates fine |
| `{"title": "x", "id": 99, "createdAt": "1999-01-01"}` | Accepted; `id` and `createdAt` **ignored** | AC-01.4, AC-01.5 |

**That last row is the over-posting test.** The response must show a server-generated `id` and a
`createdAt` of now — not `99` and not 1999. It passes because the DTO has no such properties, not
because anything checks for them.

---

## 4. `PUT /api/tasks/{id}` — update

**Story**: US-04

**Request**: `TaskRequest` — same shape and same rules as `POST` (AC-04.7)

| Code | When | Body |
|---|---|---|
| `200` | Updated | `TaskResponse` with the new values |
| `400` | Any validation rule fails | `ProblemDetails` |
| `404` | No task with that id | `ProblemDetails` |

```http
PUT /api/tasks/3
{ "title": "Write report", "status": "Done" }
→ 200  { "id": 3, "status": "Done", "createdAt": "<unchanged>" }
```

**Three guarantees to verify explicitly:**

1. **`404` creates nothing.** `PUT` to a missing id must **not** create a task at that id
   (AC-04.9). Some APIs treat `PUT` as upsert; this one does not. List afterwards to confirm.
2. **`createdAt` is unchanged** by every update (AC-04.4). Compare before and after.
3. **A rejected update leaves the stored task completely untouched** (AC-04.8) — not partially
   applied. Send an invalid update, then `GET` the task and confirm it is exactly as it was.

**Clearing optional fields** (AC-04.6, FR-013): send `"description": null` and `"dueDate": null`
explicitly. Both persist as absent and come back as `null`.

**The id in the body is ignored.** The route id is authoritative (AC-04.3); the DTO has no `id`
property, so there is no conflict to resolve.

---

## 5. `DELETE /api/tasks/{id}` — delete

**Story**: US-05

| Code | When | Body |
|---|---|---|
| `204` | Deleted | *(empty)* |
| `404` | No task with that id | `ProblemDetails` |

```http
DELETE /api/tasks/3   → 204
DELETE /api/tasks/3   → 404      # second attempt (AC-05.5)
GET    /api/tasks/3   → 404      # AC-05.3
```

**Deletion is permanent and irreversible** (FR-030, AC-05.6, clarification Q3). The row is removed.
There is no `isDeleted` flag, no archive, no restore endpoint. The confirmation dialog in US-11 is
the only safeguard the person has, which is why AC-11.4 requires it to say so.

**`204` returns no body** — there is nothing left to describe.

---

## Cross-cutting

### CORS

The API must allow the Angular dev origin `http://localhost:4200` (plan.md §5.14).

**If every endpoint works in Swagger but every one fails from Angular**, the API is fine — CORS is
missing, misordered, or naming the wrong origin. Swagger is same-origin with the API, so it never
exercises this. Expect this exact failure if `app.UseCors()` is absent or placed after the
endpoints.

### Status codes used

| Code | Meaning here |
|---|---|
| `200 OK` | Read or update succeeded |
| `201 Created` | Create succeeded — with `Location` |
| `204 No Content` | Delete succeeded |
| `400 Bad Request` | Validation failed, or a parameter would not bind |
| `404 Not Found` | No task with that id |

No other status code is produced deliberately. An unhandled failure would surface as `500`, which
always indicates a defect rather than an expected outcome.

### Contract-to-criteria coverage

| Operation | Story | Acceptance criteria covered |
|---|---|---|
| `GET /api/tasks` | US-02, US-06, US-07 | AC-02.1..8, AC-06.1..10, AC-07.1..9 |
| `GET /api/tasks/{id}` | US-03 | AC-03.1..7 |
| `POST /api/tasks` | US-01 | AC-01.1..15 |
| `PUT /api/tasks/{id}` | US-04 | AC-04.1..11 |
| `DELETE /api/tasks/{id}` | US-05 | AC-05.1..8 |

All 68 backend acceptance criteria are reachable through these five operations. The Phase 8 manual
checklist walks them in this order.

# Manual Test Checklist: Task Management System

**Feature**: `001-task-management` | **Phase 8 — Testing** | **Date**: 2026-09-15
**Stories**: [index](../../../stories/README.md) · **Contract**: [tasks-api.md](../contracts/tasks-api.md) · **Spec**: [spec.md](../spec.md)

Covers **all 132 acceptance criteria** (68 backend, 64 frontend) and **all 9 success criteria**.

---

## How coverage is classified

Not every criterion can be proved the same way, and pretending otherwise would make this checklist
dishonest. Three categories are used:

| Mark | Meaning |
|---|---|
| **[A]** | **Automated** — asserted by a live HTTP request in the Phase 7 gate run. Strongest evidence. |
| **[I]** | **Inspection** — a property of the code that no black-box request can distinguish (e.g. "the query filters in SQL, not in memory"). Verified by reading the named file. |
| **[M]** | **Manual** — needs a human in a browser. **These are the ones that need you.** |

**Current state: 48 backend [A] · 20 backend [I] · 64 frontend [M].**

---

## Preconditions

```powershell
# Terminal 1
cd C:\Users\moham\OneDrive\Desktop\project\backend\TaskManagement.Api
dotnet run
```
```powershell
# Terminal 2
cd C:\Users\moham\OneDrive\Desktop\project\frontend\task-management-app
npm start
```

- API: **http://localhost:5178** · Swagger: **http://localhost:5178/swagger** · App: **http://localhost:4200**
- Do **not** pass `--urls` (plan.md §2.1).
- Six demo tasks are seeded: 3 Todo, 1 InProgress, 2 Done; two have no due date.

---

# Part A — Backend (68 criteria)

## A.1 Automated — re-runnable evidence

**Last run: 2026-09-15 against http://localhost:5178 — 65 assertions, 65 passed, 0 failed.**

Every id is written out in full so this file is greppable and auditable.

- [x] **US-01 Create (14)** — AC-01.1, AC-01.2, AC-01.3, AC-01.4, AC-01.5, AC-01.6, AC-01.7,
      AC-01.8, AC-01.9, AC-01.10, AC-01.11, AC-01.12, AC-01.13, AC-01.15 **[A]**
  - includes the **over-posting** test (`{"id":99,"createdAt":"1999-01-01"}` → both ignored)
  - includes the **200/201 character boundary** on title
  - includes **"nothing was stored"** re-checked after every rejection
- [x] **US-02 List (4)** — AC-02.1, AC-02.2, AC-02.3, AC-02.5 **[A]**
- [x] **US-03 Get by id (4)** — AC-03.1, AC-03.2, AC-03.3, AC-03.6 **[A]**
      — including `/api/tasks/abc` → **400, not 404**
- [x] **US-04 Update (9)** — AC-04.1, AC-04.2, AC-04.3, AC-04.4, AC-04.5, AC-04.6, AC-04.8,
      AC-04.9, AC-04.10 **[A]**
  - includes `createdAt` unchanged, optional fields cleared to `null`, a rejected update leaving the
    task **completely** untouched, and `PUT` to a missing id **creating nothing**
- [x] **US-05 Delete (5)** — AC-05.1, AC-05.2, AC-05.3, AC-05.5, AC-05.7 **[A]**
- [x] **US-06 Search (6)** — AC-06.2, AC-06.3, AC-06.4, AC-06.5, AC-06.6, AC-06.7 **[A]**
- [x] **US-07 Filter (6)** — AC-07.1, AC-07.2, AC-07.3, AC-07.4, AC-07.5, AC-07.6 **[A]**
      — **all four** search/status combinations
- [x] **Contract** — enums serialise as strings; `404` carries a `ProblemDetails` body **[A]**

## A.2 Inspection — 20 criteria no request can distinguish

These are genuine properties of the implementation, not things a client can observe. Each names the
file and the line of reasoning.

- [x] **AC-01.14** server-side validation cannot be bypassed — `[ApiController]` on `TasksController.cs` returns `400` from the Data Annotations on `CreateTaskDto.cs` before the action body runs. Proven indirectly by A.1's seven rejection cases. **[I]**
- [x] **AC-02.4 / AC-06.5 / AC-07.4** empty result is `200 []` not `404` — asserted in A.1; listed here because the *reason* is structural: `GetAll` always returns `Ok(...)`. **[I]**
- [x] **AC-02.6** nothing is hidden by default — `GetAllAsync` applies no filter when both parameters are null (`TaskService.cs`). **[I]**
- [x] **AC-02.7** deleted tasks never appear — there is no `IsDeleted` column; the row is removed. **[I]**
- [x] **AC-02.8 / AC-03.7 / AC-04.11 / AC-05.8 / AC-06.10 / AC-07.9** every DB call is async — grep for `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` across `backend\` returns **zero hits**; every `_context` call uses its `Async` twin and is awaited. **[I]**
- [x] **AC-03.4** a `404` changes nothing — `GetByIdAsync` is a read returning `null`; no write path exists. **[I]**
- [x] **AC-03.5** deleted task returns `404` — asserted in A.1 (AC-05.3), same code path. **[I]**
- [x] **AC-04.7** update rules identical to create — `UpdateTaskDto.cs` carries character-for-character the same annotations and the same two default initializers as `CreateTaskDto.cs`. **[I]**
- [x] **AC-05.4** delete of a missing id returns `false` → `404` — asserted in A.1 (AC-05.5). **[I]**
- [x] **AC-05.6** no flag, archive or hidden retention — the migration creates no such column; `DeleteAsync` calls `Remove`. **[I]**
- [x] **AC-06.1 / AC-06.8 / AC-07.7** search/filter results stay newest-first — `OrderByDescending(CreatedAt)` is applied after both optional `Where` clauses, so ordering holds in all four combinations. **[I]**
- [x] **AC-06.9 / AC-07.8** narrowing happens in SQL, not in memory — `TaskService.GetAllAsync` builds one `IQueryable` and materialises **once** with a single `ToListAsync()` at the end. **This is the one worth re-checking if anyone edits that method**: moving `ToListAsync()` earlier would still pass every A.1 test while silently loading the whole table. **[I]**

---

# Part B — Frontend (64 criteria) — **needs you, in a browser**

You reported all seven interactions working. This walks the individual criteria behind them, so
anything missed is found now rather than later. Tick as you go.

## B.1 — US-08 View the task list (11)

- [ ] **AC-08.1** Opening http://localhost:4200 loads and displays every task
- [ ] **AC-08.2** Each row shows title, priority, status and due date
- [ ] **AC-08.3** *"Refactor the reporting query"* and *"PR review for the auth branch"* have no due date — they must read **"No due date"**, never blank and never the word "null"
- [ ] **AC-08.4** Newest first — *"PR review for the auth branch"* (seeded last) is at the top
- [ ] **AC-08.5** Status reads **"In Progress"**, not "InProgress"; priority and status are visually distinguishable
- [ ] **AC-08.6** A loading indication appears while fetching (throttle the network in DevTools to see it)
- [ ] **AC-08.7** Delete all six tasks → an empty state invites creating the first task. **Not** an error, **not** a blank page
- [ ] **AC-08.8** Stop the API (Ctrl+C in Terminal 1) and reload → readable error, app still usable. **Restart the API afterwards**
- [ ] **AC-08.9** Every row offers Edit and Delete
- [ ] **AC-08.10** After a create, an edit **and** a delete, the list updates **without a manual page reload**
- [ ] **AC-08.11** Narrow the window to phone width — the layout stays usable and nothing scrolls sideways

## B.2 — US-09 Create a task (13)

- [ ] **AC-09.1** The form offers title, description, priority, status and due date
- [ ] **AC-09.2** Priority and status are dropdowns — an invalid value cannot be typed
- [ ] **AC-09.3** A fresh form defaults to **Medium** / **To Do**
- [ ] **AC-09.4** Saving with only a title works; description and due date stay empty
- [ ] **AC-09.5** After saving you land on the list and the new task is visible
- [ ] **AC-09.6** The submit button disables while saving — one click cannot create two tasks
- [ ] **AC-09.7** An empty title blocks submission with an inline error. **Also try a title of only spaces** — it must be rejected too
- [ ] **AC-09.8** Paste 201+ characters into the title → inline error naming the 200 limit
- [ ] **AC-09.9** Paste 1001+ characters into the description → inline error naming the 1000 limit
- [ ] **AC-09.10** Errors appear **next to their field**, not only in a summary at the top
- [ ] **AC-09.11** Opening a fresh form shows **no** errors until you touch a field or submit
- [ ] **AC-09.12** A due date in the past is accepted with no warning
- [ ] **AC-09.13** *(Server rejection)* — see B.6 below

## B.3 — US-10 Edit a task (12)

- [ ] **AC-10.1** Opening a task pre-fills every field with its stored values
- [ ] **AC-10.2** Edit *"Refactor the reporting query"* (no description, no due date) — both render genuinely empty, not "null"
- [ ] **AC-10.3** **The due-date check.** Edit *"Write Q3 board report"*: the date picker must show **20 Sep 2026** — not the 19th. Save without changing it and confirm the list still shows 20 Sep. *(This is the timezone day-shift bug `DateOnly` exists to prevent.)*
- [ ] **AC-10.4** Change a task to **Done** and save → the list shows it as Done immediately
- [ ] **AC-10.5** A successful save returns you to the list with the updated values
- [ ] **AC-10.6** Open a task, change fields, press **Cancel** → nothing changed
- [ ] **AC-10.7** Clear the description and due date, save → both persist as empty
- [ ] **AC-10.8** Open http://localhost:4200/edit/99999 → a "no longer exists" message and a usable way back. **Not** a broken empty form
- [ ] **AC-10.9** Id and createdAt are not editable
- [ ] **AC-10.10** The same three validation rules from B.2 apply identically here
- [ ] **AC-10.11** Emptying the title blocks submission with an inline error
- [ ] **AC-10.12** *(Server rejection)* — see B.6 below

## B.4 — US-11 Delete with confirmation (8)

- [ ] **AC-11.1** Each row offers a delete action
- [ ] **AC-11.2** Choosing delete asks for confirmation **before** anything is sent (Network tab stays quiet)
- [ ] **AC-11.3** The confirmation **names the task** being deleted
- [ ] **AC-11.4** The confirmation states the removal is permanent and cannot be undone
- [ ] **AC-11.5** Cancelling leaves the task untouched and sends nothing
- [ ] **AC-11.6** Confirming removes it from the list without a manual reload
- [ ] **AC-11.7** Delete the same task from Swagger first, then confirm deletion in the UI → a readable message, not an unhandled failure
- [ ] **AC-11.8** Stop the API, then confirm a delete → the task **stays** in the list with an error shown. *(The display must never disagree with what is stored.)* Restart the API afterwards

## B.5 — US-12 Search (10) · US-13 Filter (10)

- [ ] **AC-12.1** A search box sits above the list
- [ ] **AC-12.2** Typing `rep` narrows to titles containing it
- [ ] **AC-12.3** Typing `REPORT` still finds *"Write Q3 board report"*
- [ ] **AC-12.4** Network tab shows the request going to the **server** with `?search=` — the browser is not filtering a full list
- [ ] **AC-12.5** Search `zzzz` → empty state that **names the term**, distinct from the "no tasks at all" state
- [ ] **AC-12.6** Clearing the box restores the full list
- [ ] **AC-12.7** A visible Clear control does it in one action
- [ ] **AC-12.8** Typing `report` fires **one** request after you stop, not one per keystroke (Network tab)
- [ ] **AC-12.9** With a search active, create a task → the narrowed view is reapplied, not silently reset
- [ ] **AC-12.10** Stop the API, then type in the search box → readable error, **previous results stay visible**. Restart afterwards
- [ ] **AC-13.1** Filter offers To Do / In Progress / Done plus **All**
- [ ] **AC-13.2** **All** is selected initially and shows every state
- [ ] **AC-13.3** Choosing **To Do** narrows to the three Todo tasks
- [ ] **AC-13.4** Network tab shows `?status=` going to the server
- [ ] **AC-13.5** **The combination test.** Type `re` **and** select **To Do** → only tasks satisfying **both**. Expect **3**: *Refactor the reporting query*, *Renew domain certificate*, *Book flights for the Berlin conference*
- [ ] **AC-13.6** Changing the filter does **not** clear the search box, and vice versa
- [ ] **AC-13.7** Filter to a state with no matches → empty state naming that status
- [ ] **AC-13.8** Returning to **All** restores the unfiltered list
- [ ] **AC-13.9** Labels read "In Progress", not "InProgress"
- [ ] **AC-13.10** With **To Do** active, edit one of those tasks to **Done** → it disappears from the current view

## B.6 — Server rejection reaching the UI (AC-09.13, AC-10.12)

Client rules mirror the server's, so the client normally blocks bad input first. To prove the
**server's** message still surfaces, bypass the client rule:

1. Open the create form and type any valid title.
2. In DevTools console, run:
   `document.querySelector('#title').value = 'x'.repeat(250);`
   then type a space in the field so Angular registers the change.
3. Submit.

- [ ] **AC-09.13** The server's `400` message appears against the **Title** field
- [ ] **AC-10.12** Everything you typed is **still there** — nothing was retyped
- [ ] Repeat on the edit form

*(The `maxlength="250"` on the input is deliberately above the 200 rule so this path is reachable.)*

---

# Part C — Success criteria (9)

| | Criterion | How to check | Status |
|---|---|---|---|
| [ ] | **SC-001** Capture a task with a title alone in under 15s | Time yourself from clicking "New task" | **[M]** |
| [ ] | **SC-002** Todo → Done in 3 interactions or fewer | Edit → select Done → Save = 3 | **[M]** |
| [x] | **SC-003** 100% of invalid submissions rejected with a message naming the value | A.1 covers all 7 rejection classes server-side | **[A]** |
| [x] | **SC-004** 100% of removed tasks absent from every later list/search/filter | A.1 AC-05.2, AC-05.3 | **[A]** |
| [ ] | **SC-005** Search of 100 tasks returns exactly the right matches | A.1 proves correctness at 6 tasks; volume is **[M]** | partial |
| [x] | **SC-006** Search + filter returns exactly the intersection | A.1 combination 4/4 | **[A]** |
| [ ] | **SC-007** List/search/filter render within 2s for up to 100 tasks | Needs ~100 tasks — see below | **[M]** |
| [ ] | **SC-008** A new user completes capture/find/complete/remove unaided, first try | Needs someone who has not seen it | **[M]** |
| [x] | **SC-009** No action on a missing task crashes or silently creates | A.1: `404` on GET/PUT/DELETE, `PUT` creates nothing | **[A]** |

**To test SC-005 and SC-007 at volume**, paste into PowerShell:

```powershell
$api = 'http://localhost:5178/api/tasks'
$words = 'report','review','deploy','invoice','meeting','refactor','migrate','audit'
1..100 | ForEach-Object {
  $t = "{0} task {1}" -f $words[$_ % $words.Count], $_
  $s = @('Todo','InProgress','Done')[$_ % 3]
  $body = "{`"title`":`"$t`",`"status`":`"$s`"}"
  Invoke-WebRequest $api -Method POST -Body ([System.Text.Encoding]::UTF8.GetBytes($body)) -ContentType 'application/json' -UseBasicParsing | Out-Null
}
"seeded: " + ((Invoke-WebRequest $api -UseBasicParsing).Content | ConvertFrom-Json).Count
```

Then reload the list and time it, search `report`, and combine with a status filter.

---

## Results

| Part | Criteria | Verified | Method |
|---|---|---|---|
| A.1 Backend automated | 48 | ✅ 48 | 65 live HTTP assertions, 0 failures |
| A.2 Backend inspection | 20 | ✅ 20 | named file + reasoning |
| B Frontend | 64 | ☐ 0 / 64 | **you, in a browser** |
| C Success criteria | 9 | ✅ 4 · ☐ 5 | mixed |

**Backend: 68/68.** **Frontend: awaiting your walkthrough.**

### Anything that fails

Record it here rather than fixing it silently — Phase 9 audits against this file.

| AC | What happened | Expected |
|---|---|---|
| | | |

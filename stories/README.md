# User Stories — Task Management System

**Feature**: `001-task-management`
**Specification**: [spec.md](../specs/001-task-management/spec.md)
**Clarifications**: [clarifications.md](../specs/001-task-management/clarifications.md)
**Created**: 2026-09-15

This tree is the bridge between the business specification and the implementation tasks, required
by Article VII (traceability) and Article VIII (layout) of the project constitution. Every task in
`tasks.md` must reference exactly one story from this index.

**13 stories · 7 backend (68 ACs) · 6 frontend (64 ACs) · 132 acceptance criteria**

---

## Index

### Backend — `stories\backend\`

| ID | Title | Endpoint | Priority | ACs |
|---|---|---|---|---|
| [US-01](./backend/US-01.md) | Create a task | `POST /api/tasks` | P1 | 15 |
| [US-02](./backend/US-02.md) | Retrieve all tasks | `GET /api/tasks` | P1 | 8 |
| [US-03](./backend/US-03.md) | Retrieve a single task by identifier | `GET /api/tasks/{id}` | P2 | 7 |
| [US-04](./backend/US-04.md) | Update an existing task | `PUT /api/tasks/{id}` | P2 | 11 |
| [US-05](./backend/US-05.md) | Delete a task | `DELETE /api/tasks/{id}` | P2 | 8 |
| [US-06](./backend/US-06.md) | Search tasks by title | `GET /api/tasks?search=` | P3 | 10 |
| [US-07](./backend/US-07.md) | Filter tasks by status | `GET /api/tasks?status=` | P3 | 9 |

### Frontend — `stories\frontend\`

| ID | Title | Consumes | Priority | ACs |
|---|---|---|---|---|
| [US-08](./frontend/US-08.md) | View the task list | US-02 | P1 | 11 |
| [US-09](./frontend/US-09.md) | Create a task through a form | US-01 | P1 | 13 |
| [US-10](./frontend/US-10.md) | Edit a task through a form | US-03, US-04 | P2 | 12 |
| [US-11](./frontend/US-11.md) | Delete a task with confirmation | US-05 | P2 | 8 |
| [US-12](./frontend/US-12.md) | Search tasks by title from the list | US-06 | P3 | 10 |
| [US-13](./frontend/US-13.md) | Filter tasks by status from the list | US-07 | P3 | 10 |

---

## Traceability matrix — story to requirement

| Story | Spec story | Satisfies (FR) | Success criteria | Depends on |
|---|---|---|---|---|
| US-01 | Story 1 | FR-001, FR-002, FR-003, FR-004, FR-005, FR-022, FR-023, FR-024, FR-025, FR-028 | SC-001, SC-003 | — |
| US-02 | Story 2 | FR-006, FR-007, FR-008, FR-027, FR-029 | SC-007 | — |
| US-03 | Story 3 | FR-009, FR-026 | SC-009 | — |
| US-04 | Story 4 | FR-010, FR-011, FR-012, FR-013, FR-022, FR-023, FR-024, FR-025, FR-026, FR-028 | SC-002, SC-003, SC-009 | — |
| US-05 | Story 5 | FR-014, FR-016, FR-026, FR-030 | SC-004, SC-009 | — |
| US-06 | Story 6 | FR-017, FR-018, FR-020, FR-021, FR-027 | SC-005 | US-02 |
| US-07 | Story 7 | FR-019, FR-020, FR-021, FR-027 | SC-006 | US-02 |
| US-08 | Story 2 | FR-006, FR-007, FR-008, FR-027, FR-029 | SC-007, SC-008 | US-02 |
| US-09 | Story 1 | FR-001, FR-002, FR-004, FR-023, FR-025 | SC-001, SC-003, SC-008 | US-01 |
| US-10 | Story 4 | FR-010, FR-011, FR-012, FR-013, FR-023, FR-025 | SC-002, SC-003, SC-009 | US-03, US-04 |
| US-11 | Story 5 | FR-014, FR-015, FR-016 | SC-004, SC-008, SC-009 | US-05 |
| US-12 | Story 6 | FR-017, FR-018, FR-020, FR-021, FR-027 | SC-005, SC-008 | US-06, US-08 |
| US-13 | Story 7 | FR-019, FR-020, FR-021, FR-027 | SC-006, SC-008 | US-07, US-08 |

## Reverse matrix — requirement to story

Proves no requirement was dropped. **All 30 functional requirements are covered.**

| FR | Requirement (abbreviated) | Covered by |
|---|---|---|
| FR-001 | Capture a task with its fields | US-01, US-09 |
| FR-002 | Title required, trimmed, no minimum | US-01, US-09 |
| FR-003 | Capture moment recorded automatically | US-01 |
| FR-004 | Defaults: Medium importance, Todo state | US-01, US-09 |
| FR-005 | Unique, never-reused identifier | US-01 |
| FR-006 | See all tasks in one list | US-02, US-08 |
| FR-007 | List shows title, importance, state, due date | US-02, US-08 |
| FR-008 | Newest captured first | US-02, US-08 |
| FR-009 | Complete detail of a single task | US-03 |
| FR-010 | Change every editable field | US-04, US-10 |
| FR-011 | Identifier and capture moment preserved | US-04, US-10 |
| FR-012 | Same checks on change as on capture | US-04, US-10 |
| FR-013 | Description and due date can be cleared | US-04, US-10 |
| FR-014 | Remove a task | US-05, US-11 |
| FR-015 | Confirm before removal | US-11 |
| FR-016 | Removed task absent everywhere | US-05, US-11 |
| FR-017 | Narrow by title fragment, anywhere in title | US-06, US-12 |
| FR-018 | Matching ignores case | US-06, US-12 |
| FR-019 | Narrow by state | US-07, US-13 |
| FR-020 | Title and state narrowing combine | US-06, US-07, US-12, US-13 |
| FR-021 | Narrowing can be cleared | US-06, US-07, US-12, US-13 |
| FR-022 | Reject importance or state outside the set | US-01, US-04 |
| FR-023 | Title ≤ 200, description ≤ 1000 | US-01, US-04, US-09, US-10 |
| FR-024 | Server validates independently of the screen | US-01, US-04 |
| FR-025 | Rejection names the value; data unchanged | US-01, US-04, US-09, US-10 |
| FR-026 | Acting on a missing task reports not found | US-03, US-04, US-05 |
| FR-027 | Empty results are normal, with a message | US-02, US-06, US-07, US-08, US-12, US-13 |
| FR-028 | Past due dates accepted, no date validation | US-01, US-04 |
| FR-029 | All matches in one response, no paging | US-02, US-08 |
| FR-030 | Removal permanent and irreversible | US-05 |

### Required features to stories

| Required feature | Story |
|---|---|
| Backend — create | US-01 |
| Backend — get all | US-02 |
| Backend — get by id | US-03 |
| Backend — update | US-04 |
| Backend — delete | US-05 |
| Backend — search by title | US-06 |
| Backend — filter by status | US-07 |
| Frontend — list | US-08 |
| Frontend — create form | US-09 |
| Frontend — edit form | US-10 |
| Frontend — delete | US-11 |
| Frontend — search | US-12 |
| Frontend — filter by status | US-13 |
| Frontend — client validation | **US-09 + US-10** (see note below) |

**Note on client validation.** The brief lists seven frontend features but allocates six story IDs
(US-08..US-13). Client-side validation is not a screen a person visits — it is behaviour belonging
to the two forms. It is therefore carried explicitly by **US-09** (creation: AC-09.7 to AC-09.13)
and **US-10** (editing: AC-10.10 to AC-10.12) rather than being invented as a fourteenth story.
Both story files label those criteria as carrying the client-validation requirement, so the
Phase 9 audit can find them.

---

## Build order

Article VII requires the backend to be complete and verified in Swagger before frontend work
begins. Within each layer, priority order applies.

```
Backend   US-01 → US-02 → US-03 → US-04 → US-05 → US-06 → US-07
                                   ↓
                        [Swagger gate — all endpoints proven]
                                   ↓
Frontend  US-08 → US-09 → US-10 → US-11 → US-12 → US-13
```

US-01 and US-02 together are the smallest demonstrable product. US-08 and US-09 make it usable by
a person. Everything after that is additive, so if the day runs short the cut line falls after
US-11 — leaving search and filter (US-06, US-07, US-12, US-13) as the only casualties.

## Conventions

- Story IDs are permanent. A dropped story keeps its ID and is marked withdrawn; IDs are never
  reused or renumbered.
- Acceptance criteria are numbered `AC-<story>.<n>` and are referenced directly by the Phase 8
  manual test checklist.
- Every task in `tasks.md` is written `T-xx [US-yy] description` and must name a story in this
  index.

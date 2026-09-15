# Feature Specification: Task Management System

**Feature Branch**: `001-task-management`

**Created**: 2026-09-15

**Status**: Draft

**Input**: User description: "A task management system where a user can capture, organise, find and complete their work items. Each task has a title, an optional description, a priority (Low/Medium/High), a status (Todo/InProgress/Done), an optional due date, and a creation timestamp. The user can create a task, view all tasks, view a single task's details, edit a task, delete a task, search tasks by title, and filter tasks by status."

## Clarifications

Full rationale, rejected alternatives and consequences for each decision are recorded in
[clarifications.md](./clarifications.md), which is the canonical record.

### Session 2026-09-15

- Q: Must every task have a due date, or is it optional? → A: Optional — a task may have none.
- Q: Should the system accept a due date that has already passed? → A: Allowed, with no special
  treatment; no overdue concept and no date-range validation.
- Q: When a task is deleted, is it permanently removed or kept but hidden? → A: Hard delete —
  permanently removed, with no archive, restore or undo.
- Q: Should a title search match anywhere in the title, or only at the start? → A: Anywhere
  (contains), ignoring upper/lower case; the description is not searched.
- Q: Must a title search and a status filter work at the same time? → A: Yes, combinable; results
  satisfy both conditions.
- Q: Should the list return every task at once, or be split into pages? → A: All at once — no
  pagination, no total count, no page controls.
- Q: What maximum lengths apply to the title and description? → A: Title 200 characters,
  description 1000 characters.
- Q: Should the title have a minimum length? → A: No — any non-blank title after trimming is
  accepted.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Capture a task (Priority: P1)

A person has something they need to do and wants it recorded before they forget it. They enter a
short title, optionally add a longer description, choose how important it is, optionally set the
date it is due, and save it. The task is now part of their list and is marked as not yet started.

**Why this priority**: Nothing else in the product has any value until tasks can be captured. This
is the single entry point through which all data enters the system.

**Independent Test**: Can be fully tested by entering a new task and confirming it is retained and
appears in the person's list with the values entered and a recorded creation moment.

**Acceptance Scenarios**:

1. **Given** an empty list, **When** the person saves a task with the title "Write report",
   **Then** the task is stored, appears in the list, is marked as not started, and carries the
   date and time it was captured.
2. **Given** the capture form is open, **When** the person leaves the title empty and tries to
   save, **Then** the task is not stored and the person is told the title is required.
3. **Given** the capture form is open, **When** the person supplies only a title, **Then** the task
   is stored with no description, no due date, a default importance, and a not-started state.
4. **Given** the capture form is open, **When** the person supplies a description, an importance
   and a due date, **Then** all of those values are retained exactly as entered.

---

### User Story 2 - See everything on the list (Priority: P1)

A person wants an overview of their outstanding work. They open the list and see every task they
have captured, each showing enough information — title, importance, state and due date — to decide
what to do next.

**Why this priority**: Capture is worthless without recall. Together with Story 1 this forms the
smallest useful product.

**Independent Test**: Can be fully tested by capturing several tasks and confirming all of them
appear with the correct summary information.

**Acceptance Scenarios**:

1. **Given** several stored tasks, **When** the person opens the list, **Then** every task is
   shown with its title, importance, state and due date.
2. **Given** no stored tasks, **When** the person opens the list, **Then** an empty list is shown
   with a message explaining there is nothing yet, rather than an error.
3. **Given** several stored tasks, **When** the person opens the list, **Then** the most recently
   captured tasks appear first.

---

### User Story 3 - Inspect one task in full (Priority: P2)

A person has spotted a task in the list and wants the full picture, including the complete
description that the summary view shortens or omits.

**Why this priority**: Needed before a person can confidently edit or complete a task, but the list
view alone already delivers value.

**Independent Test**: Can be fully tested by selecting a known task and confirming every stored
value is displayed.

**Acceptance Scenarios**:

1. **Given** a stored task, **When** the person opens it, **Then** its title, full description,
   importance, state, due date and capture moment are all displayed.
2. **Given** a task that no longer exists, **When** the person tries to open it, **Then** they are
   told the task could not be found and are returned to a usable state.

---

### User Story 4 - Keep a task up to date (Priority: P2)

Work changes. A person needs to correct a title, expand a description, raise or lower importance,
move a due date, or advance a task from not started to in progress to done.

**Why this priority**: This is how a task is actually completed — marking work done is the core
satisfaction of the product — but the product is demonstrable without it.

**Independent Test**: Can be fully tested by changing each field of a stored task and confirming
the change is retained and reflected in the list.

**Acceptance Scenarios**:

1. **Given** a stored task, **When** the person changes its title and saves, **Then** the new
   title is retained and shown everywhere the task appears.
2. **Given** a task that is not started, **When** the person marks it done, **Then** its state
   becomes done and it is shown as done in the list.
3. **Given** a stored task, **When** the person clears its due date and saves, **Then** the task
   is retained with no due date.
4. **Given** the edit form is open, **When** the person empties the title and tries to save,
   **Then** the change is rejected with the same message as at capture time and the stored task is
   left untouched.
5. **Given** a stored task, **When** any edit is saved, **Then** the moment the task was originally
   captured is unchanged.

---

### User Story 5 - Remove a task (Priority: P2)

A task was entered by mistake, or is no longer relevant. The person removes it so it stops
cluttering the list.

**Why this priority**: Lists degrade quickly without removal, but this is not needed to prove the
core journey.

**Independent Test**: Can be fully tested by removing a known task and confirming it no longer
appears in the list or in any search result.

**Acceptance Scenarios**:

1. **Given** a stored task, **When** the person confirms its removal, **Then** it disappears from
   the list and from all searches and filters.
2. **Given** the person has asked to remove a task, **When** they are asked to confirm and decline,
   **Then** the task is left untouched.
3. **Given** a task that no longer exists, **When** the person tries to remove it, **Then** they
   are told it could not be found and nothing else is affected.

---

### User Story 6 - Find a task by name (Priority: P3)

Once the list grows, scanning it is slow. The person types part of a task's name and sees only the
tasks whose names contain what they typed.

**Why this priority**: A convenience that becomes important with volume; the list is still usable
without it at small scale.

**Independent Test**: Can be fully tested by capturing tasks with distinct names, searching for a
fragment of one name, and confirming only matching tasks are returned.

**Acceptance Scenarios**:

1. **Given** tasks named "Write report" and "Book flights", **When** the person searches for
   "report", **Then** only "Write report" is returned.
2. **Given** tasks named "Write report" and "Book flights", **When** the person searches for
   "REPORT", **Then** "Write report" is still returned.
3. **Given** any set of tasks, **When** the person searches for text that matches nothing, **Then**
   an empty result is shown with a message, not an error.
4. **Given** an active search, **When** the person clears the search text, **Then** the full list
   is shown again.

---

### User Story 7 - Focus on one state of work (Priority: P3)

The person wants to see only what is not started, only what is in progress, or only what is
finished — for example to run through outstanding work at the start of the day.

**Why this priority**: Same class of convenience as search; valuable but not foundational.

**Independent Test**: Can be fully tested by capturing tasks in each state, selecting one state,
and confirming only tasks in that state are returned.

**Acceptance Scenarios**:

1. **Given** tasks in all three states, **When** the person filters to in progress, **Then** only
   in-progress tasks are returned.
2. **Given** an active filter, **When** the person clears it, **Then** the full list is shown again.
3. **Given** an active filter on a state, **When** the person also searches for a name fragment,
   **Then** only tasks that satisfy both the state and the name fragment are returned.
4. **Given** a filter on a state with no tasks in it, **When** it is applied, **Then** an empty
   result is shown with a message, not an error.

---

### Edge Cases

- **Empty or whitespace-only title**: rejected at both capture and edit, with a message naming the
  title as the problem.
- **Over-long text**: a title or description beyond the permitted length is rejected with a message
  stating the limit, rather than being silently shortened.
- **Unrecognised importance or state**: a value outside the permitted set is rejected; the system
  never stores an importance or state it cannot display.
- **Acting on a task that no longer exists**: viewing, editing or removing an already-removed task
  produces a clear "not found" outcome, never a crash or a silently created task.
- **Two people acting at once**: if the same task is removed and then edited, the edit reports
  "not found" rather than restoring the task.
- **Nothing to show**: an empty list, an empty search result and an empty filter result are all
  normal outcomes presented with an explanatory message.
- **Search text that is only spaces**: treated as no search at all; the full list is returned.
- **A due date in the past**: permitted, because tasks are routinely captured after they were due.
- **Changes submitted from outside the supplied forms**: every submitted value is checked by the
  system itself, so invalid data cannot be stored by bypassing the on-screen checks.

## Requirements *(mandatory)*

### Functional Requirements

**Capturing**

- **FR-001**: The system MUST allow a person to capture a task consisting of a title, an optional
  description, an importance, a state, and an optional due date.
- **FR-002**: The system MUST trim surrounding whitespace from the title before checking it, MUST
  reject a title that is empty or contains only spaces, and MUST accept any trimmed title of one
  or more characters — there is no minimum length.
- **FR-003**: The system MUST record the date and time each task was captured, automatically and
  without the person entering it.
- **FR-004**: The system MUST treat a newly captured task as not started, and as medium importance,
  when the person does not choose otherwise.
- **FR-005**: The system MUST assign every task an identifier that is unique and never reused.

**Viewing**

- **FR-006**: The system MUST allow a person to see all captured tasks in a single list.
- **FR-007**: The list MUST show, for each task, its title, importance, state and due date.
- **FR-008**: The system MUST present tasks with the most recently captured first.
- **FR-009**: The system MUST allow a person to see the complete detail of a single chosen task,
  including its full description and the moment it was captured.

**Changing**

- **FR-010**: The system MUST allow a person to change a task's title, description, importance,
  state and due date.
- **FR-011**: The system MUST preserve a task's identifier and its capture moment across every
  change.
- **FR-012**: The system MUST apply exactly the same checks when changing a task as when capturing
  one.
- **FR-013**: The system MUST allow a person to clear a task's description or due date, returning
  it to having none.

**Removing**

- **FR-014**: The system MUST allow a person to remove a task.
- **FR-015**: The system MUST ask the person to confirm before a removal takes effect.
- **FR-016**: A removed task MUST no longer appear in any list, search result or filter result.

**Finding**

- **FR-017**: The system MUST allow a person to narrow the list to tasks whose title contains a
  given fragment of text anywhere within it, not only at the beginning. The description MUST NOT
  be searched.
- **FR-018**: Title matching MUST ignore differences of upper and lower case. A search term
  consisting only of whitespace MUST be treated as no search at all.
- **FR-019**: The system MUST allow a person to narrow the list to tasks in a chosen state.
- **FR-020**: The system MUST allow narrowing by title fragment and by state at the same time,
  returning only tasks that satisfy both.
- **FR-021**: The system MUST allow a person to clear any narrowing and return to the full list.

**Validity and feedback**

- **FR-022**: The system MUST reject any importance or state outside the permitted sets.
- **FR-023**: The system MUST reject a title longer than 200 characters and a description longer
  than 1000 characters, stating the limit rather than silently shortening the text. These two
  numbers MUST be identical wherever they appear.
- **FR-024**: The system MUST check every submitted value itself, independently of any check
  performed by the screen the person used, so that invalid data cannot be stored by bypassing the
  on-screen checks.
- **FR-025**: When a submission is rejected, the system MUST state which value was wrong and why,
  and MUST leave stored data unchanged.
- **FR-026**: When a person acts on a task that does not exist, the system MUST report that it was
  not found and MUST make no other change.
- **FR-027**: The system MUST present an empty list, an empty search result and an empty filter
  result as normal outcomes with an explanatory message.

**Confirmed boundaries** *(settled in the 2026-09-15 clarification session)*

- **FR-028**: The system MUST accept a due date that is in the past, both when capturing and when
  changing a task, and MUST apply no date-range validation of any kind. There is no overdue
  concept.
- **FR-029**: The system MUST return every task matching the current narrowing in a single
  response, with no paging, no page size and no total count.
- **FR-030**: Removal MUST be permanent and irreversible. The system MUST NOT retain removed tasks
  in any hidden, archived or restorable form.

### Key Entities

- **Task**: A single unit of work a person intends to complete. Carries a unique identifier, a
  required short title of at most 200 characters, an optional longer description of at most 1000
  characters, an importance, a state, an optional due date that may fall in the past, and the
  moment it was captured. Tasks are independent of one another — no task contains, depends on, or
  blocks another.
- **Importance**: The fixed set of levels a task can be assigned — Low, Medium, High — used to
  signal which work matters most.
- **State**: The fixed set of stages a task moves through — Not Started, In Progress, Done —
  describing how far the work has got.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A person can capture a task with a title alone in under 15 seconds from opening the
  capture form.
- **SC-002**: A person can move a task from not started to done in 3 interactions or fewer,
  starting from the list.
- **SC-003**: 100% of submissions with an empty title, an over-long title or description, or an
  unrecognised importance or state are rejected and produce a message naming the offending value.
- **SC-004**: 100% of removed tasks are absent from every subsequent list, search and filter result.
- **SC-005**: Searching a list of 100 tasks by title fragment returns the correct set of matches
  and no others, on every attempt.
- **SC-006**: Combining a title search with a state filter returns exactly the tasks satisfying
  both conditions, on every attempt.
- **SC-007**: The list, a search result and a filter result are each presented to the person within
  2 seconds for a list of up to 100 tasks.
- **SC-008**: A person new to the product can capture, find, complete and remove a task without
  written instructions on their first attempt.
- **SC-009**: No action on a task that does not exist produces a crash, a blank screen, or a
  silently created task — it always produces a "not found" message.

## Assumptions

Each item below was a default chosen where the description was silent. Every one has now been
**confirmed by the project owner** in the 2026-09-15 clarification session; none remains a guess.
Rationale and rejected alternatives are in [clarifications.md](./clarifications.md).

- **Single user, no accounts**: One person uses the system and sees all tasks. There is no sign-in,
  no ownership, no sharing and no permissions. This matches the one-day scope ceiling set by the
  project constitution.
- **Description and due date are optional**; title, importance and state are always present.
- **Default importance is Medium** and **default state is Not Started** when the person does not
  choose.
- **Due dates in the past are permitted**, since work is often recorded after it was due.
- **Title matching is partial and case-insensitive** — searching "rep" finds "Write report".
- **Removal is permanent**; there is no archive, recycle bin or undo.
- **The whole list is returned at once**; there is no paging, because the expected volume is tens
  of tasks, not thousands.
- **Ordering is newest captured first**, with no person-controlled sorting.
- **Title is limited to 200 characters and description to 1000 characters** — enough for real use,
  bounded enough to protect the system.
- **Tasks are independent** — no sub-tasks, dependencies, recurrence, reminders, attachments,
  comments, tags or categories.
- **Data persists between sessions** and survives restarts.

### Explicitly out of scope

Considered during clarification and declined. Implementing any of these requires a constitution
amendment approved by the project owner:

- Overdue detection or overdue styling of tasks
- Any archive, recycle bin, restore or undo capability
- Pagination, page size controls, or a total-count field
- Searching the description text
- A minimum title length
- Due-date range validation of any kind

**No open questions remain.** This specification is ready for technical planning.

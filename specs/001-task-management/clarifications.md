# Clarifications: Task Management System

**Feature**: `001-task-management`
**Specification**: [spec.md](./spec.md)
**Session date**: 2026-09-15
**Decided by**: Project owner
**Status**: All seven deferred decisions resolved. No open questions remain.

This file is the canonical record of clarification decisions, required by Article VIII of the
project constitution. Each decision is mirrored into `spec.md` as a functional requirement, an
edge case, or a confirmed assumption.

---

## Summary

| # | Decision point | Answer | Spec impact |
|---|---|---|---|
| Q1 | Due date optionality | **Optional** | FR-001, FR-013, Key Entities |
| Q2 | Past due dates | **Allowed, no special treatment** | FR-028, Edge Cases |
| Q3 | Delete semantics | **Hard delete — permanent** | FR-030, FR-016 |
| Q4 | Title search matching | **Contains, case-insensitive** | FR-017, FR-018 |
| Q5 | Search + status filter | **Combinable** | FR-020 |
| Q6 | Pagination | **None — return all matches** | FR-029, SC-007 |
| Q7 | Length limits | **Title ≤ 200, description ≤ 1000** | FR-023 |
| Q7b | Title minimum length | **Any non-blank title, trimmed** | FR-002 |

Every answer matched the provisional assumption already recorded in `spec.md`, so the
specification required confirmation and the addition of explicit numbers rather than
rework. See "Consequences" below for what this locks in.

---

## Q1 — Due date optionality

**Question**: Must every task have a due date, or is it optional?

**Answer**: **A — Optional.** A task may have no due date at all.

**Rationale**: Most captured work genuinely has no deadline. Forcing a due date trains the person
to enter meaningless placeholder dates, which destroys the value of the field for the tasks that
do have real deadlines.

**Consequences**: The due date is nullable in storage and may be absent on both create and edit.
The forms must accept submission with the field left blank, and the person must be able to clear
an existing due date back to none (FR-013). No validation rule may require a due date.

**Rejected**: Mandatory due date; optional-at-capture-but-required-before-In-Progress. The latter
introduces a state-transition rule, which is disproportionate for a one-day build.

---

## Q2 — Past due dates

**Question**: Should the system accept a due date that has already passed?

**Answer**: **A — Allowed, with no special treatment.**

**Rationale**: Work is routinely recorded after it was due. Rejecting past dates would make
editing any older task infuriating, because an untouched past due date would block an unrelated
change such as a title correction.

**Consequences**: No date-range validation exists anywhere — not on create, not on edit, not on
the forms. There is no "overdue" concept, no overdue styling, and no acceptance criterion about
overdue tasks.

**Rejected**: Option B (allow, plus visual overdue marking in the list) was explicitly considered
and declined as extra scope. It remains the cheapest future enhancement if time allows, but it is
**out of scope** and must not be implemented without an amendment.

---

## Q3 — Delete semantics

**Question**: When a task is deleted, is it permanently removed, or kept but hidden?

**Answer**: **A — Hard delete.** The record is permanently removed.

**Rationale**: Soft delete adds a flag that *every* read operation must then filter on. Missing
that filter in a single place makes deleted tasks reappear — a defect that is easy to create and
hard to notice. That risk is not worth taking on a day that also involves learning EF Core.

**Consequences**: Deletion is irreversible. There is no archive, no recycle bin, no restore, and
no undo. No "deleted" flag exists on the entity. A deleted task is absent from every subsequent
read. The UI must therefore confirm before deleting (FR-015), because confirmation is the only
safeguard the person has.

**Rejected**: Soft delete without restore; soft delete with restore.

---

## Q4 — Title search matching

**Question**: When searching by title, should it match text anywhere in the title, or only at the
start?

**Answer**: **A — Contains, case-insensitive.** Searching "rep" finds "Write report".

**Rationale**: This is what people expect from a search box. Prefix-only matching almost never
fires on real task titles, because the distinguishing word is rarely the first one.

**Consequences**: Matching is a case-insensitive substring test against the title only. The
description is **not** searched. A search term consisting only of whitespace is treated as no
search at all and returns the full list.

**Rejected**: Case-sensitive contains; starts-with; exact match.

---

## Q5 — Search combined with status filter

**Question**: Must a title search and a status filter work at the same time?

**Answer**: **A — Combinable.** Results satisfy both conditions simultaneously.

**Rationale**: Both narrowing rules already travel with the same list request, so applying them
together is nearly free. Making them mutually exclusive would require extra logic to clear one
when the other is set — more work for a worse result.

**Consequences**: Supplying neither returns everything; supplying either narrows by that one;
supplying both returns only tasks satisfying both. All four combinations must be exercised in the
Phase 8 manual test checklist.

**Rejected**: Mutually exclusive search and filter.

---

## Q6 — Pagination

**Question**: Should the list return every task at once, or be split into pages?

**Answer**: **A — No pagination.** All matching tasks are returned.

**Rationale**: Pagination adds page and size parameters, a total-count concept, page state in the
UI, and interaction rules with search and filter. At the tens-of-tasks scale this product targets,
it is cost with no benefit.

**Consequences**: The list operation returns a plain collection with no envelope, no total count,
and no page metadata. There is no upper bound on the number returned. SC-007's performance target
is stated against a list of up to 100 tasks, which is the working assumption for volume.

**Rejected**: Page number and size parameters; a fixed safety cap with no page controls.

---

## Q7 — Validation limits

**Question**: What maximum lengths should apply to the title and the description?

**Answer**: **A — Title maximum 200 characters, description maximum 1000 characters.**

**Rationale**: Comfortably beyond what real titles and descriptions need, while bounded enough to
keep storage predictable and to prevent an unbounded submission.

**Consequences**: The numbers 200 and 1000 must agree in four places: the database column
definitions, the server-side validation rules, the client-side form rules, and the acceptance
tests. Exceeding either limit is rejected with a message stating the limit — never silently
truncated.

**Rejected**: Title 100 / description 500 (too tight for real descriptions); title 200 /
description unlimited (leaves an unbounded field).

---

## Q7b — Title minimum length

**Question**: Should the title have a minimum length of 3 characters, or is any non-blank title
acceptable?

**Answer**: **Any non-blank title**, evaluated after trimming surrounding whitespace.

**Rationale**: Short titles such as "PR", "CV" or "1:1" are legitimate. A minimum length would
reject real input to guard against a problem that trimming already solves.

**Consequences**: The title is trimmed before validation. A title that is empty or consists only
of whitespace is rejected. A trimmed title of one or more characters is accepted. There is no
minimum-length rule. The description has no minimum and may be absent entirely.

---

## Decisions explicitly ruled OUT of scope by this session

The following were considered and declined. Implementing any of them requires a constitution
amendment approved by the project owner:

- Overdue detection or overdue styling of tasks
- Any archive, recycle bin, restore, or undo capability
- Pagination, page size controls, or a total-count field
- Searching the description text
- A minimum title length
- Due-date range validation of any kind

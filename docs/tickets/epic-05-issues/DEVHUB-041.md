# DEVHUB-041 — Update issue endpoint

|  |  |
|---|---|
| **Epic** | EPIC 5 — Issues |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-040 |
| **Specs** | [`api-conventions.md`](../../tech-specs/api-conventions.md) §7 |

## Context

The general-purpose PATCH for title, description and due date. The single-field endpoints
(DEVHUB-042) exist for the high-frequency, optimistic operations.

## Scope

**In:** `PATCH /api/issues/{issueId}` with true partial semantics.
**Out:** status/priority/assignee/labels (DEVHUB-042, 049).

## Tasks

- [ ] `UpdateIssueCommand` with optional `title`, `description`, `dueDate` using a wrapper that
      distinguishes absent from null.
- [ ] Apply only supplied fields; each change raises its own domain event and activity row.
- [ ] Validation: title non-empty and ≤200 when supplied.
- [ ] No-op when the value is unchanged (no event, no `updatedAt` bump).
- [ ] Tests: patch one field leaves others untouched; `{"dueDate": null}` clears it; `{}` is a
      no-op returning `200`; unchanged value produces no activity.

## Acceptance criteria

- [ ] Omitted fields are never modified.
- [ ] `null` clears a nullable field; absence does not.
- [ ] An unchanged value creates no activity entry.

## Technical notes

This is where the "absent vs null" helper from DEVHUB-019 pays off. Build it once, reuse it in
every PATCH that follows.

## Learning goals

Partial update semantics in JSON APIs, idempotent no-ops, keeping an audit trail meaningful.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

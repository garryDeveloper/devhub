# DEVHUB-038 — Create issue endpoint

|  |  |
|---|---|
| **Epic** | EPIC 5 — Issues |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-037, DEVHUB-026 |
| **Specs** | [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §4 |

## Context

The first write in the product's core loop. It ties together scoping, sequence generation,
validation and activity recording in one transaction.

## Scope

**In:** `POST /api/projects/{projectId}/issues` with optional assignee and labels.
**Out:** attachments (EPIC 15), templates.

## Tasks

- [ ] `CreateIssueCommand` + validator: title required, ≤200 chars, trimmed; description
      ≤ 50 000; `dueDate` not in the past (warning-level rule → allow but note it, or reject —
      decide and document).
- [ ] Handler: authorize via `IWorkspaceAccessService.ForProject`, take the next number, create
      the aggregate, attach labels, save.
- [ ] Reject an `assigneeId` who is not a project member with `422`.
- [ ] Default status `Backlog`, default priority `NoPriority`, reporter = caller.
- [ ] Raise `IssueCreated` → activity row in the same transaction.
- [ ] Return `201` with the full `IssueDto` and a `Location` header.
- [ ] Tests: happy path, missing title, invalid assignee, non-member → 404, labels from another
      project rejected.

## Acceptance criteria

- [ ] A created issue has a server-assigned key and an activity entry of type `Created`.
- [ ] Labels belonging to a different project are rejected.
- [ ] Everything happens in one transaction: a failure leaves no partial issue and no consumed
      sequence number visible to users.

## Technical notes

Validate label ownership explicitly. Accepting arbitrary label ids is a quiet cross-tenant data
leak, and it is exactly the kind of check a UI-driven implementation forgets.

## Learning goals

Transactional command handling, cross-aggregate validation, deliberate defaults.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

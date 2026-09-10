# DEVHUB-032 — Project membership endpoints

|  |  |
|---|---|
| **Epic** | EPIC 4 — Projects |
| **Phase** | 1 — Local foundation |
| **Priority** | P1 |
| **Size** | S |
| **Depends on** | DEVHUB-031 |
| **Specs** | [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §3 |

## Context

Project membership narrows who can be assigned issues. In the MVP it does not restrict *reading*
— every workspace member can see every project in the workspace. Keep that simple rule explicit.

## Scope

**In:** list, add, remove project members; assignability rule.
**Out:** private projects, per-project roles (both post-MVP).

## Tasks

- [ ] `GET /api/projects/{id}/members`.
- [ ] `POST /api/projects/{id}/members { userId }` — owner only; `422` if the user is not a
      workspace member; `409` if already a project member.
- [ ] `DELETE /api/projects/{id}/members/{memberId}` — owner only; unassign that user from the
      project's issues in the same transaction.
- [ ] Document the visibility rule in the endpoint summary so the API doc states it plainly.
- [ ] Tests: non-workspace-member rejected, removal unassigns issues, duplicate add rejected.

## Acceptance criteria

- [ ] Only workspace members can become project members.
- [ ] Removing a project member leaves their issues unassigned, not orphaned to a missing user.
- [ ] Issue assignment (DEVHUB-042) rejects a non-project-member assignee.

## Technical notes

The "unassign on removal" step is easy to forget and produces a confusing UI (an assignee who is
no longer in the project). Cover it with an integration test.

## Learning goals

Referential rules across aggregates, transactional side effects, keeping a permission model
simple on purpose.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

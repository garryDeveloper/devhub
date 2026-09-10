# DEVHUB-053 — Comment entity and endpoints

|  |  |
|---|---|
| **Epic** | EPIC 7 — Comments & activity feed |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-040 |
| **Specs** | [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §6 |

## Context

Comments are where the collaboration happens, and the first place user-authored markdown is
stored and later rendered — so input handling matters.

## Scope

**In:** `Comment` entity, CRUD endpoints, authorship rules, mention parsing.
**Out:** attachments on comments (EPIC 15), reactions, threading.

## Tasks

- [ ] `Comment` entity as a child of the `Issue` aggregate; body ≤10 000 characters, non-empty
      after trimming.
- [ ] `GET /api/issues/{id}/comments` — paged, oldest first.
- [ ] `POST` — any project member; raises `CommentCreated` → activity row.
- [ ] `PATCH` — author only (`403` otherwise); sets `editedAt`.
- [ ] `DELETE` — author or workspace owner; hard delete plus an activity entry.
- [ ] Parse `@mentions` server-side, resolving only to project members; return the resolved
      mentions in the DTO so the client can render them as links.
- [ ] Tests: authorship rules, empty body rejected, mention resolution ignores non-members.

## Acceptance criteria

- [ ] A non-author cannot edit a comment.
- [ ] An edited comment exposes `editedAt` so the UI can show "(edited)".
- [ ] Mentions of users outside the project are stored as plain text, not links.

## Technical notes

Parse mentions on the server, not the client: notifications (EPIC 14) depend on the same parse,
and two implementations will disagree.

## Learning goals

Child entities and authorship rules, server-side text parsing, designing for a later feature
(notifications) without building it yet.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

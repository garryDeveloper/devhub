# DEVHUB-086 — Notification endpoints

|  |  |
|---|---|
| **Epic** | EPIC 14 — Notifications |
| **Phase** | 5 — Async & polish |
| **Priority** | P1 |
| **Size** | XS |
| **Depends on** | DEVHUB-085 |
| **Specs** | [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §13 |

## Context

Reading and clearing notifications. The unread count is polled frequently, so it gets its own
cheap endpoint.

## Scope

**In:** list, unread count, mark one read, mark all read.
**Out:** notification preferences, per-type muting.

## Tasks

- [ ] `GET /api/notifications?unreadOnly=&page=` — paged, newest first, own notifications only.
- [ ] `GET /api/notifications/unread-count` — a single count query using the partial index.
- [ ] `PATCH /api/notifications/{id}/read` — idempotent; `404` if it belongs to someone else.
- [ ] `POST /api/notifications/read-all` — returns the number marked.
- [ ] Retention: delete read notifications older than 90 days (scheduled cleanup).
- [ ] Tests: isolation between users, idempotent mark-read, count accuracy.

## Acceptance criteria

- [ ] A user can only see and modify their own notifications.
- [ ] The unread count is a single indexed query.
- [ ] Marking an already-read notification returns `200`, not an error.

## Technical notes

The badge polls this endpoint every 30–60 s per open tab. Keep it to one indexed count query and
never join anything into it.

## Learning goals

Designing for polling, partial indexes, per-user data isolation.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

# DEVHUB-024 — Workspace CRUD endpoints

|  |  |
|---|---|
| **Epic** | EPIC 3 — Workspaces & membership |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-023, DEVHUB-018 |
| **Specs** | [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §2 |

## Context

Create, list, read and update workspaces — and the first place the scoping rule ("only what you
are a member of") is applied.

## Scope

**In:** `POST /api/workspaces`, `GET /api/workspaces`, `GET`/`PATCH /api/workspaces/{id}`.
**Out:** delete/transfer ownership (post-MVP), members (DEVHUB-025).

## Tasks

- [ ] `POST` creates the workspace with the caller as owner; derive the slug when not supplied
      and return `409` on a slug collision.
- [ ] `GET /api/workspaces` returns only workspaces the caller belongs to, with their role and
      member count. Not paged — a user has few.
- [ ] `GET /{id}` returns `404` when the caller is not a member (never `403`).
- [ ] `PATCH /{id}` (name only) requires the `Owner` role → `403` for a member.
- [ ] Integration tests: list isolation between two users, 404 for a non-member, 403 for a
      member calling PATCH, 409 on duplicate slug.

## Acceptance criteria

- [ ] User A never sees user B's workspaces in any response.
- [ ] A non-member gets `404`, a member-without-role gets `403` — and there is a test for each.
- [ ] Slug is unique across the system and immutable.

## Technical notes

This is the ticket where the `404`-not-`403` rule becomes a habit. Write the test first; it is
the pattern every later resource copies.

## Learning goals

Multi-tenant scoping, information disclosure through status codes, role checks vs scope checks.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

# DEVHUB-019 — Current user endpoints (`GET`/`PATCH /api/me`)

|  |  |
|---|---|
| **Epic** | EPIC 2 — Authentication & user account |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-018 |
| **Specs** | [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §1 |

## Context

`GET /api/me` is how both clients bootstrap: it turns "I have a token" into "I know who I am and
what I can see".

## Scope

**In:** `GET /api/me`, `PATCH /api/me` (display name, avatar), workspace list summary in the
response.
**Out:** password change by email, avatar upload itself (DEVHUB-090 provides the attachment).

## Tasks

- [ ] `GET /api/me` returning the user DTO plus their workspaces (id, name, slug, role) — the
      clients need both to render the shell.
- [ ] `PATCH /api/me` accepting `displayName` and `avatarAttachmentId`, with partial-update
      semantics (omitted ≠ null).
- [ ] Validation: display name 1–100 characters, trimmed.
- [ ] Resolve `avatarUrl` as a short-lived presigned URL when an avatar key exists (skip until
      EPIC 15 lands; return `null` before then).
- [ ] Tests: unauthenticated → 401; patch updates only the provided field; a too-long name → 400.

## Acceptance criteria

- [ ] `GET /api/me` returns the caller's profile and their workspace memberships.
- [ ] `PATCH` with only `displayName` does not clear the avatar.
- [ ] The response never includes `passwordHash` or token values.

## Technical notes

Distinguishing "absent" from "null" in a PATCH body needs an optional wrapper or `JsonElement`
inspection — a plain nullable DTO cannot tell `{"avatar": null}` from `{}`. Solve it once here;
every later PATCH reuses the pattern.

## Learning goals

PATCH semantics, bootstrapping a client session, shaping a response around a screen's needs.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

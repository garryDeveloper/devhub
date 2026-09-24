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

- [x] `GET /api/me` returning the user DTO plus their workspaces (id, name, slug, role) — the
      clients need both to render the shell.
      *`MeDto`. **Decided:** `workspaces` ships now as an always-empty array (workspaces do not
      exist yet); DEVHUB-024 has the task to fill it. A token whose user was deleted → `401`.*
- [x] `PATCH /api/me` accepting `displayName` and `avatarAttachmentId`, with partial-update
      semantics (omitted ≠ null).
      *Returns `UserDto`. **Decided:** `avatarAttachmentId: null` removes the avatar; an id →
      `404 attachments.not_found` until DEVHUB-090, which has the task to wire it.*
- [x] Validation: display name 1–100 characters, trimmed.
      *`DisplayNameRules.ValidDisplayName()`, now shared with registration.*
- [x] Resolve `avatarUrl` as a short-lived presigned URL when an avatar key exists (skip until
      EPIC 15 lands; return `null` before then).
- [x] Tests: unauthenticated → 401; patch updates only the provided field; a too-long name → 400.
      *`MeTests` + `OptionalJsonConverterTests`.*

## Acceptance criteria

- [x] `GET /api/me` returns the caller's profile and their workspace memberships.
- [x] `PATCH` with only `displayName` does not clear the avatar.
      *Verified against a seeded `avatar_key`, since no API can set one yet.*
- [x] The response never includes `passwordHash` or token values.

## Technical notes

Distinguishing "absent" from "null" in a PATCH body needs an optional wrapper or `JsonElement`
inspection — a plain nullable DTO cannot tell `{"avatar": null}` from `{}`. Solve it once here;
every later PATCH reuses the pattern.
*Solved with `Optional<T>` + `OptionalJsonConverterFactory` (api-conventions.md §7).*

## Learning goals

PATCH semantics, bootstrapping a client session, shaping a response around a screen's needs.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

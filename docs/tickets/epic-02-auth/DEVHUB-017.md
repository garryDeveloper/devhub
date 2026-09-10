# DEVHUB-017 — Logout and session revocation

|  |  |
|---|---|
| **Epic** | EPIC 2 — Authentication & user account |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | XS |
| **Depends on** | DEVHUB-016 |
| **Specs** | [`auth-spec.md`](../../tech-specs/auth-spec.md) §4 |

## Context

Logout has to be honest: it revokes what it can (the refresh token) and the access token remains
valid for at most 15 minutes. Document that rather than pretending otherwise.

## Scope

**In:** `POST /api/auth/logout`, chain revocation, client cleanup contract.
**Out:** "log out all devices" (post-MVP), access-token denylist.

## Tasks

- [ ] `POST /api/auth/logout` accepting the refresh token; revoke it and its chain.
- [ ] Return `204` even if the token is already invalid (logout is idempotent, and it must not
      reveal token validity).
- [ ] Document the client contract: clear the in-memory access token, clear stored refresh
      token, clear the query cache, redirect to login.
- [ ] Tests: after logout the refresh token returns `401`; logout twice still returns `204`.

## Acceptance criteria

- [ ] Logout revokes the refresh session.
- [ ] Calling logout with garbage returns `204`, not `400`.
- [ ] The residual access-token window is written down in the ticket and in the spec.

## Technical notes

An access-token denylist would close the 15-minute window at the cost of a store lookup on every
request. Not worth it here — but be able to explain the trade-off, because it is a common
interview question.

## Learning goals

Idempotent endpoints, the limits of stateless auth, honest security trade-offs.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

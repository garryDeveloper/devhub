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

- [x] `POST /api/auth/logout` accepting the refresh token; revoke it and its chain.
      *`LogoutHandler` revokes the token's **family** (the DEVHUB-016 "chain"), so a stale rotated
      token also ends the session. **Decided: anonymous** (🔓, the refresh token is the credential)
      and outside the auth rate-limit bucket, like `/refresh` (auth-spec.md §4, §6).*
- [x] Return `204` even if the token is already invalid (logout is idempotent, and it must not
      reveal token validity).
      *Also for an empty/missing token and an empty body: no validator, nullable body parameter.*
- [x] Document the client contract: clear the in-memory access token, clear stored refresh
      token, clear the query cache, redirect to login. *auth-spec.md §4 "Client contract".*
- [x] Tests: after logout the refresh token returns `401`; logout twice still returns `204`.
      *`LogoutTests`, plus: garbage/empty → 204, a stale token ends the session, other devices
      survive, no access token needed, and the residual access-token window is pinned by a test.*

## Acceptance criteria

- [x] Logout revokes the refresh session.
- [x] Calling logout with garbage returns `204`, not `400`.
- [x] The residual access-token window is written down in the ticket and in the spec.
      *An access token issued before logout stays valid until its `exp`: **at most 15 minutes**
      after logout. This is an accepted trade-off. A `jti` denylist would close the window, but
      only by adding a store lookup to every request (auth-spec.md §4).*

## Technical notes

An access-token denylist would close the 15-minute window at the cost of a store lookup on every
request. Not worth it here — but be able to explain the trade-off, because it is a common
interview question.

## Learning goals

Idempotent endpoints, the limits of stateless auth, honest security trade-offs.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

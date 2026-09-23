# DEVHUB-016 — Refresh token flow with rotation

|  |  |
|---|---|
| **Epic** | EPIC 2 — Authentication & user account |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | M |
| **Depends on** | DEVHUB-015 |
| **Specs** | [`auth-spec.md`](../../tech-specs/auth-spec.md) §4 |

## Context

Short access tokens are only usable if refreshing is invisible. Rotation plus reuse detection is
what makes a stolen token a forced logout instead of a silent second session.

## Scope

**In:** `RefreshToken` entity, `POST /api/auth/refresh`, rotation, reuse detection, cleanup.
**Out:** client-side single-flight logic (DEVHUB-021 / 022).

## Tasks

- [x] `RefreshToken` entity + migration per [`database-schema.md`](../../tech-specs/database-schema.md).
      *Done early in DEVHUB-014/015 (`DEVHUB015_RefreshTokens`), because register and login must
      return a real refresh token. `created_by_ip` and `user_agent` were not mapped — decide here
      whether to capture them.* **Decided: not captured** (database-schema.md §2).
- [x] Generate a 256-bit random opaque token; store **only** its SHA-256 hash.
      *`TokenService.CreateRefreshToken` / `TokenService.Hash` — reused for the lookup via `ITokenService.HashRefreshToken`.*
- [x] Issue a refresh token on register and login.
- [x] `POST /api/auth/refresh`: validate → issue new pair → revoke the old row and set
      `replaced_by_token_id`.
- [x] Reuse detection: presenting an already-revoked token revokes the entire chain and logs a
      security warning.
      *"Chain" is implemented as a **family** (`family_id`, migration `DEVHUB016_RefreshTokenFamilies`):
      one indexed UPDATE instead of walking ~2,900 links. See auth-spec.md §4.*
- [x] Background (or startup) cleanup of tokens expired more than 60 days ago.
      *`RefreshTokenCleanupService`: 1 min after startup, then every 24 h.*
- [x] Tests: rotation works; the old token fails on second use; reuse revokes descendants;
      expired token → 401; token of a deleted user → 401.

## Acceptance criteria

- [x] A refresh returns a new access **and** a new refresh token.
- [x] The previous refresh token is unusable immediately afterwards.
- [x] Reusing a revoked token invalidates the whole chain (verified by a test).
- [x] The database stores no raw token value.

## Technical notes

- Hash comparison uses a fixed-time comparison, and the lookup is by hash (indexed), so no
  enumeration is possible.
  *Implemented without a separate `FixedTimeEquals`: the lookup compares SHA-256 digests, whose
  bytes an attacker cannot choose, so the timing reveals nothing (auth-spec.md §4).*
- Concurrency: two parallel refreshes with the same token will race. One wins, the other must
  get `401` — and the client must therefore serialize refreshes (single-flight), which is why
  DEVHUB-021 exists.
  *Implemented with optimistic concurrency on PostgreSQL `xmin`; covered by
  `RefreshTests.Two_parallel_refreshes_with_the_same_token_issue_exactly_one_successor`.*
- `/refresh` is excluded from the register+login IP rate limit (auth-spec.md §6).

## Learning goals

Refresh token rotation, reuse detection, why opaque tokens are hashed like passwords, revocation
in a stateless auth scheme.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

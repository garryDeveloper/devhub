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

- [ ] `RefreshToken` entity + migration per [`database-schema.md`](../../tech-specs/database-schema.md).
- [ ] Generate a 256-bit random opaque token; store **only** its SHA-256 hash.
- [ ] Issue a refresh token on register and login.
- [ ] `POST /api/auth/refresh`: validate → issue new pair → revoke the old row and set
      `replaced_by_token_id`.
- [ ] Reuse detection: presenting an already-revoked token revokes the entire chain and logs a
      security warning.
- [ ] Background (or startup) cleanup of tokens expired more than 60 days ago.
- [ ] Tests: rotation works; the old token fails on second use; reuse revokes descendants;
      expired token → 401; token of a deleted user → 401.

## Acceptance criteria

- [ ] A refresh returns a new access **and** a new refresh token.
- [ ] The previous refresh token is unusable immediately afterwards.
- [ ] Reusing a revoked token invalidates the whole chain (verified by a test).
- [ ] The database stores no raw token value.

## Technical notes

- Hash comparison uses a fixed-time comparison, and the lookup is by hash (indexed), so no
  enumeration is possible.
- Concurrency: two parallel refreshes with the same token will race. One wins, the other must
  get `401` — and the client must therefore serialize refreshes (single-flight), which is why
  DEVHUB-021 exists.

## Learning goals

Refresh token rotation, reuse detection, why opaque tokens are hashed like passwords, revocation
in a stateless auth scheme.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

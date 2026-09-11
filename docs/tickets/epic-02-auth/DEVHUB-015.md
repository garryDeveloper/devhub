# DEVHUB-015 — Login and JWT access tokens

|  |  |
|---|---|
| **Epic** | EPIC 2 — Authentication & user account |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | M |
| **Depends on** | DEVHUB-013 |
| **Specs** | [`auth-spec.md`](../../tech-specs/auth-spec.md) §3 |

## Context

The access token is what every other endpoint trusts. Short-lived and stateless, carrying
identity only — never roles.

## Scope

**In:** `POST /api/auth/login`, `ITokenService`, JWT configuration and validation, timing-safe
failure behaviour, lockout.
**Out:** refresh (DEVHUB-016), authorization policies (DEVHUB-018).

## Tasks

- [ ] `ITokenService.CreateAccessToken(User)` producing the claim set from the spec.
- [ ] Configure `AddAuthentication().AddJwtBearer(...)` with issuer, audience, lifetime and
      `ClockSkew = TimeSpan.Zero`.
- [ ] `LoginCommand` handler: find by email, verify hash, issue tokens.
- [ ] On an unknown email, still run a dummy verification so response time does not leak
      account existence.
- [ ] Return a single generic `401` message for any credential failure.
- [ ] Account lockout: 5 failures in 15 minutes → `429` with `Retry-After`.
- [ ] Tests: valid login, wrong password, unknown email (same message + comparable timing),
      expired token rejected, token with a tampered signature rejected.

## Acceptance criteria

- [ ] A valid login returns `200` with a JWT whose `exp` is 15 minutes out.
- [ ] The token is accepted by a protected endpoint and rejected once expired.
- [ ] Wrong password and unknown email are indistinguishable from the client's perspective.
- [ ] The JWT contains no role or workspace claim.

## Technical notes

- `ClockSkew = TimeSpan.Zero` matters: the 5-minute default silently turns a 15-minute token
  into a 20-minute one and makes expiry tests flaky.
- Roles are deliberately not in the token: a membership change must take effect on the next
  request, not in 15 minutes.

## Learning goals

JWT structure and validation, stateless authentication trade-offs, timing attacks, why claims
should be minimal.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

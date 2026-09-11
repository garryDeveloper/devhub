# DEVHUB-021 — Web token refresh and expiry handling

|  |  |
|---|---|
| **Epic** | EPIC 2 — Authentication & user account |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-020 |
| **Specs** | [`frontend-web-architecture.md`](../../tech-specs/frontend-web-architecture.md) §3, [`auth-spec.md`](../../tech-specs/auth-spec.md) §4 |

## Context

With 15-minute access tokens, the refresh path runs constantly. If it is not single-flight, two
parallel requests will rotate each other's tokens and log the user out at random — a bug that is
miserable to diagnose later.

## Scope

**In:** 401 interception, single-flight refresh, one retry, failure handling.
**Out:** proactive refresh before expiry (optional; note it if you add it).

## Tasks

- [ ] In `apiFetch`, intercept `401` (except on the refresh call itself).
- [ ] Implement single-flight: the first 401 starts the refresh, concurrent 401s await the same
      promise.
- [ ] Retry the original request exactly once after a successful refresh.
- [ ] On refresh failure: clear auth state, clear the cache, redirect to `/login?expired=1`.
- [ ] Show a subtle "session expired" message on the login screen when redirected that way.
- [ ] Tests (MSW): a single 401 refreshes and retries; five concurrent 401s trigger exactly one
      refresh call; a failed refresh logs out.

## Acceptance criteria

- [ ] Working past the access-token lifetime never interrupts the user.
- [ ] Five simultaneous requests after expiry produce exactly one `POST /api/auth/refresh`.
- [ ] A revoked refresh token logs the user out cleanly, with no infinite retry loop.

## Technical notes

Guard against recursion: the refresh request itself must bypass the interceptor, or a failing
refresh will call itself forever.

## Learning goals

Request interception, single-flight/deduplication patterns, promise sharing, avoiding retry
storms.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3.

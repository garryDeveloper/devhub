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

- [x] In `apiFetch`, intercept `401` (except on the refresh call itself).
      *`shared/api/client.ts`. Every `/api/auth/*` path is excluded, not just `/refresh`: a 401
      from `/login` means "wrong password", and treating it as expiry would send a failed login
      to `/login?expired=1`.*
- [x] Implement single-flight: the first 401 starts the refresh, concurrent 401s await the same
      promise.
      *`refreshTokenOnce()` keeps a module-scoped `refreshInFlight` promise, created with `??=`
      and cleared in `finally`. `authApi.refreshRequest` was removed and the `AuthProvider`
      bootstrap now calls `refreshTokenOnce()` too. That also fixes a latent DEVHUB-020 bug:
      under React StrictMode the bootstrap effect runs twice in dev, which sent two parallel
      refreshes with the same token and tripped reuse detection.*
      *Extra guard: if the access token changed while a request was on the wire, another request
      already refreshed, so the request retries with the new token and starts no new rotation.*
- [x] Retry the original request exactly once after a successful refresh.
      *An internal `isRetry` flag. A 401 on the retry is thrown as `ApiError`.*
- [x] On refresh failure: clear auth state, clear the cache, redirect to `/login?expired=1`.
      *DECISION (DEVHUB-021): only a **401** from `/refresh` counts as a failed session
      (`auth-spec.md` §4). A network error or 5xx keeps the tokens and just fails the original
      request, so a Wi-Fi blip does not log the user out.*
      *`shared/` cannot import React, so the client clears both tokens and calls the handler
      registered with `setSessionExpiredHandler`. `AuthProvider` runs `queryClient.clear()`,
      `setUser(null)` and sets `sessionExpired`. It does not call `navigate()`, because it sits
      outside the router. `RequireAuth` redirects to `/login?expired=1&returnTo=…`, keeping
      `returnTo` from `frontend-web-architecture.md` §3. There is no `POST /logout`, because the
      family is already dead.*
- [x] Show a subtle "session expired" message on the login screen when redirected that way.
      *`LoginPage`: a neutral slate `role="status"` notice (not a red alert), hidden while a
      form error is shown.*
- [x] Tests (MSW): a single 401 refreshes and retries; five concurrent 401s trigger exactly one
      refresh call; a failed refresh logs out.
      *`shared/api/client.test.ts` (adds `msw` as a devDependency), plus: five concurrent
      requests against a revoked session log out once; a 401 on the retry does not refresh
      again; a 401 from `/api/auth/login` does not refresh; a network error on refresh keeps
      the session; a token already rotated by another request retries without refreshing.
      `LoginPage.test.tsx` covers the notice. The concurrency tests were checked by disabling
      single-flight: they then fail with 5 refresh calls instead of 1.*

## Acceptance criteria

- [x] Working past the access-token lifetime never interrupts the user.
      *Covered by the "refreshes once on a 401 and retries" test: the caller only sees the
      successful response. Manual check past the 15-minute lifetime in a browser is still the
      owner's to confirm.*
- [x] Five simultaneous requests after expiry produce exactly one `POST /api/auth/refresh`.
- [x] A revoked refresh token logs the user out cleanly, with no infinite retry loop.
      *The refresh call bypasses the interceptor, and `isRetry` caps each request at one
      retry.*

## Technical notes

Guard against recursion: the refresh request itself must bypass the interceptor, or a failing
refresh will call itself forever.

Proactive refresh before expiry was **not** added (out of scope).

## Learning goals

Request interception, single-flight/deduplication patterns, promise sharing, avoiding retry
storms.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3.

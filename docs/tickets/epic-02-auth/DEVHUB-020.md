# DEVHUB-020 — Web authentication flow

|  |  |
|---|---|
| **Epic** | EPIC 2 — Authentication & user account |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | M |
| **Depends on** | DEVHUB-008, DEVHUB-019 |
| **Specs** | [`frontend-web-architecture.md`](../../tech-specs/frontend-web-architecture.md) §7 |

## Context

Login, register and the protected route tree. This is where the shell becomes an application.

## Scope

**In:** login/register screens, `AuthProvider`, protected routes, session bootstrap, logout.
**Out:** token refresh interception (DEVHUB-021), forgot-password (post-MVP).

## Tasks

- [x] Build `LoginPage` and `RegisterPage` with React Hook Form + Zod, inline validation, and
      server field errors mapped from `ProblemDetails.errors`.
      *`features/auth/pages/{Login,Register}Page.tsx`, `features/auth/schemas.ts`,
      `features/auth/lib/applyServerErrors.ts` maps `errors` onto fields and falls back to a
      form banner (`detail`) for non-field failures (401, 409, 429).*
- [x] `AuthProvider`: holds the access token in memory, exposes `user`, `login`, `register`,
      `logout`, `isBootstrapping`.
      *`features/auth/context/AuthProvider.tsx`. The access token itself lives in a
      module-scoped variable (`shared/api/authToken.ts`), not React state — `client.ts` reads it
      directly so every `apiFetch` call can attach `Authorization` without threading the token
      through props.*
- [x] Bootstrap on load: read the stored refresh token → refresh → `GET /api/me`; render a
      splash while pending so protected routes never flash the login screen.
      *`AuthProvider` gates rendering on `isBootstrapping` itself (`AuthSplash`), so nothing
      behind it — including the login screen — renders before the bootstrap resolves.*
- [x] `<RequireAuth>` wrapper redirecting to `/login?returnTo=…` and returning there after login.
      *`features/auth/components/RequireAuth.tsx` wraps the `AppShell` route tree in
      `app/router.tsx`; `LoginPage` reads `returnTo` and navigates there on success.*
- [x] Logout clears the token, calls the endpoint, resets the query cache, redirects.
      *Triggered from the `AppShell` top bar. `AuthProvider.logout()` reads the stored refresh
      token, fires `POST /api/auth/logout` without awaiting it, then clears the in-memory access
      token, the stored refresh token and the query cache (`auth-spec.md` §4 step order); the
      caller then navigates to `/login` with `replace: true`.*
- [x] Render the `/forgot-password` route as an honest "coming soon" placeholder — do not ship a
      form that silently does nothing.
      *`features/auth/pages/ForgotPasswordPage.tsx`.*
- [x] Tests: form validation, error mapping, redirect after login, logout clears state.
      *Vitest + Testing Library, added in this ticket (no test runner existed yet):
      `schemas.test.ts`, `lib/applyServerErrors.test.ts`, `pages/LoginPage.test.tsx`,
      `context/AuthProvider.test.tsx`.*

## Acceptance criteria

- [x] Register and login work end-to-end against the local API.
      *Verified with `curl` against the running API (register → refresh → `GET /api/me` →
      logout → reuse-after-logout correctly `401`s) — see commit for the exact requests/responses
      checked. Manual click-through in a browser is still owner's to confirm; no browser
      automation was available in this session.*
- [x] A hard refresh on a protected page keeps the user signed in.
      *`AuthProvider`'s bootstrap effect re-runs on every mount (i.e. every hard refresh),
      re-establishing `user` from the stored refresh token before the router renders.*
- [x] An invalid login shows the server message without clearing the entered email.
      *`LoginPage` never calls `reset()` on failure, so React Hook Form keeps the typed values;
      covered by `LoginPage.test.tsx`.*
- [x] Logout returns to `/login` and back-navigation does not show protected content.
      *The `AppShell` logout handler navigates with `replace: true`, and `RequireAuth` would
      redirect again regardless if back-navigation reached a protected URL with no `user`.*

## Technical notes

- Access token in memory only (an XSS-readable `localStorage` access token is the thing to avoid).
  *Done: `shared/api/authToken.ts` is a module-scoped variable, never persisted.*
- Refresh token storage: prefer an `httpOnly` cookie set by the API; if that is not implemented,
  use `localStorage` and **write the trade-off down in this ticket** rather than leaving it
  implicit.
  *As of this ticket, the API does not set an `httpOnly` cookie — `POST /api/auth/register`,
  `/login` and `/refresh` all return `refreshToken` as a plain JSON field, so there is nothing
  for the browser to store except JS-reachable storage. We use `localStorage`
  (`shared/api/refreshTokenStorage.ts`, key `devhub.refreshToken`), not `sessionStorage`, so a
  closed tab doesn't silently log the user out.*
  *DECISION (DEVHUB-020): accept this trade-off for now. **Risk:** an XSS vulnerability anywhere
  on this origin can read the refresh token and mint new sessions until the family is revoked.
  **Mitigations already in place, not added by this ticket:** the 15-minute access token
  lifetime bounds how long a stolen access token alone is useful; single-use rotation with
  family-wide reuse revocation (`auth-spec.md` §4) turns a stolen refresh token into a forced
  logout the moment either party's copy is used after the other's. **Context:** this is a
  learning/portfolio project, not a production service holding real user data, which is why the
  trade-off is acceptable here and not a shortcut we'd take on a real system. **The actual fix**
  is an `httpOnly; Secure; SameSite=Strict` cookie issued by the API on register/login/refresh —
  that is backend work the client cannot opt into, so it is left as a follow-up ticket against
  the API rather than solved here.*
- Client-side guards are UX, not security. The API is the authority.
  *`RequireAuth` only redirects; every actual authorization check still happens server-side per
  `auth-spec.md` §5.*

## Learning goals

Auth state in React, session bootstrap without flashes, mapping server validation to forms,
storage trade-offs for tokens.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3.

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

- [ ] Build `LoginPage` and `RegisterPage` with React Hook Form + Zod, inline validation, and
      server field errors mapped from `ProblemDetails.errors`.
- [ ] `AuthProvider`: holds the access token in memory, exposes `user`, `login`, `register`,
      `logout`, `isBootstrapping`.
- [ ] Bootstrap on load: read the stored refresh token → refresh → `GET /api/me`; render a
      splash while pending so protected routes never flash the login screen.
- [ ] `<RequireAuth>` wrapper redirecting to `/login?returnTo=…` and returning there after login.
- [ ] Logout clears the token, calls the endpoint, resets the query cache, redirects.
- [ ] Render the `/forgot-password` route as an honest "coming soon" placeholder — do not ship a
      form that silently does nothing.
- [ ] Tests: form validation, error mapping, redirect after login, logout clears state.

## Acceptance criteria

- [ ] Register and login work end-to-end against the local API.
- [ ] A hard refresh on a protected page keeps the user signed in.
- [ ] An invalid login shows the server message without clearing the entered email.
- [ ] Logout returns to `/login` and back-navigation does not show protected content.

## Technical notes

- Access token in memory only (an XSS-readable `localStorage` access token is the thing to avoid).
- Refresh token storage: prefer an `httpOnly` cookie set by the API; if that is not implemented,
  use `localStorage` and **write the trade-off down in this ticket** rather than leaving it
  implicit.
- Client-side guards are UX, not security. The API is the authority.

## Learning goals

Auth state in React, session bootstrap without flashes, mapping server validation to forms,
storage trade-offs for tokens.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3.

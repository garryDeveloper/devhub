# DEVHUB-018 — Authorization middleware and endpoint protection

|  |  |
|---|---|
| **Epic** | EPIC 2 — Authentication & user account |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-015 |
| **Specs** | [`auth-spec.md`](../../tech-specs/auth-spec.md) §5, §6 |

## Context

The default must be "protected". Every endpoint that is public should have to say so out loud —
that way a forgotten attribute fails closed, not open.

## Scope

**In:** fallback authorization policy, `ICurrentUser`, `[AllowAnonymous]` on the public
endpoints, 401/403 problem responses.
**Out:** workspace scoping (DEVHUB-026), role policies (DEVHUB-026).

## Tasks

- [x] Configure a fallback policy requiring an authenticated user.
      *Kept alongside the `/api` group's `RequireAuthorization()`: the group marks, the fallback
      catches what is mapped outside it. It also answers `401` to anonymous unknown routes
      (auth-spec.md §6).*
- [x] Implement `ICurrentUser` (`UserId`, `IsAuthenticated`) reading claims from
      `IHttpContextAccessor`; register it scoped.
      *`Infrastructure/Identity/CurrentUser.cs`. A non-Guid `sub` counts as anonymous, so the two
      properties never disagree.*
- [x] Mark `/api/auth/*`, `/health*` and webhook endpoints `[AllowAnonymous]`.
      *`/api/auth/*` already was (group-level, DEVHUB-014); `/health` now is. Webhooks do not exist
      yet — their tickets must add them to the allow-list.*
- [x] Ensure 401 and 403 return `ProblemDetails`, not the default empty body.
      *They already had a body (`UseStatusCodePages`); now they also carry DevHub types
      `auth.unauthenticated` / `auth.forbidden` (api-conventions.md §4).*
- [x] Add an integration test that enumerates all mapped endpoints and asserts each is either
      `[AllowAnonymous]` or requires auth — this test is what stops a future accidental leak.
      *`EndpointProtectionTests`. **Decided:** it also pins the anonymous endpoints to an explicit
      allow-list, because under a fallback policy the real leak is an inherited `AllowAnonymous()`.*

## Acceptance criteria

- [x] Any protected endpoint without a token returns `401` with a ProblemDetails body.
- [x] `/health` and `/api/auth/login` work anonymously.
- [x] `ICurrentUser.UserId` is populated inside handlers.
      *Verified through a test endpoint that resolves `ICurrentUser` from the request scope, as a
      handler does.*
- [x] The endpoint-enumeration test fails if a new endpoint is added with neither marking.
      *Covered by `An_endpoint_with_neither_marking_is_detected`, and checked by hand: removing
      `.AllowAnonymous()` from `/health` fails three tests.*

## Technical notes

Prefer the fallback policy over decorating every controller with `[Authorize]`. Opt-out is safer
than opt-in: the failure mode of forgetting is a broken public endpoint, not an open private one.

## Learning goals

ASP.NET authentication vs authorization, fallback policies, claims principals, fail-closed design.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

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

- [ ] Configure a fallback policy requiring an authenticated user.
- [ ] Implement `ICurrentUser` (`UserId`, `IsAuthenticated`) reading claims from
      `IHttpContextAccessor`; register it scoped.
- [ ] Mark `/api/auth/*`, `/health*` and webhook endpoints `[AllowAnonymous]`.
- [ ] Ensure 401 and 403 return `ProblemDetails`, not the default empty body.
- [ ] Add an integration test that enumerates all mapped endpoints and asserts each is either
      `[AllowAnonymous]` or requires auth — this test is what stops a future accidental leak.

## Acceptance criteria

- [ ] Any protected endpoint without a token returns `401` with a ProblemDetails body.
- [ ] `/health` and `/api/auth/login` work anonymously.
- [ ] `ICurrentUser.UserId` is populated inside handlers.
- [ ] The endpoint-enumeration test fails if a new endpoint is added with neither marking.

## Technical notes

Prefer the fallback policy over decorating every controller with `[Authorize]`. Opt-out is safer
than opt-in: the failure mode of forgetting is a broken public endpoint, not an open private one.

## Learning goals

ASP.NET authentication vs authorization, fallback policies, claims principals, fail-closed design.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

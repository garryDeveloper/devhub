# DEVHUB-014 — Registration endpoint

|  |  |
|---|---|
| **Epic** | EPIC 2 — Authentication & user account |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-013 |
| **Specs** | [`auth-spec.md`](../../tech-specs/auth-spec.md), [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §1 |

## Context

First real endpoint, and the template for every command that follows: controller → command →
validator → handler → domain → DTO.

## Scope

**In:** `POST /api/auth/register`, validation, duplicate handling, token issuing reuse.
**Out:** email verification (post-MVP), invitations.

## Tasks

- [ ] `RegisterCommand(Email, Password, DisplayName)` + FluentValidation validator.
- [ ] Handler: check uniqueness, hash, create the user, persist, issue tokens
      (reuse the token service from DEVHUB-015 — sequence the two tickets together).
- [ ] Controller returns `201` with the auth response DTO.
- [ ] Map a unique-violation to `409` even under a race (catch the DB exception, do not rely on
      a pre-check alone).
- [ ] Rate limit `10/min` per IP.
- [ ] Integration tests: happy path, duplicate email → 409, weak password → 400 with field
      errors, invalid email → 400.

## Acceptance criteria

- [ ] A new email returns `201` with `accessToken`, `refreshToken` and the user DTO.
- [ ] A duplicate email returns `409` and creates nothing.
- [ ] Password shorter than 10 characters returns `400` with an `errors.password` entry.
- [ ] The response never contains `passwordHash`.

## Technical notes

- Two concurrent registrations with the same email must produce exactly one user. The unique
  index is the enforcement; the pre-check is only for a nicer error message.
- Do not auto-create a workspace here. The client drives that in the onboarding flow
  (see [`user-flows.md`](../../user-flows.md) Flow 1), so the two concerns stay separable.

## Learning goals

Command/validator/handler pipeline, race conditions and database constraints, why `409` is not
`400`.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

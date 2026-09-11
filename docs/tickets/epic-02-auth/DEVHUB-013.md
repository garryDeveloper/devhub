# DEVHUB-013 — User aggregate and password hashing

|  |  |
|---|---|
| **Epic** | EPIC 2 — Authentication & user account |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-006 |
| **Specs** | [`auth-spec.md`](../../tech-specs/auth-spec.md) §2, [`domain-model.md`](../../domain-model.md) |

## Context

Everything in DevHub hangs off a user. This ticket creates the aggregate and the password
handling — the one place where getting it wrong has real consequences.

## Scope

**In:** `User` aggregate, EF configuration, migration, `IPasswordHasher` port and implementation.
**Out:** endpoints (DEVHUB-014/015), profile editing (DEVHUB-019).

## Tasks

- [ ] Create `User` in `DevHub.Domain/Users` with private setters and a static
      `User.Create(email, displayName, passwordHash)` factory that normalizes the email.
- [ ] Add domain methods: `ChangeDisplayName`, `SetAvatar`, `ChangePassword`.
- [ ] Define `IPasswordHasher` in `DevHub.Application/Common` (`Hash`, `Verify`).
- [ ] Implement it in `Infrastructure/Identity` using ASP.NET Core `PasswordHasher<User>`.
- [ ] EF configuration: `citext` unique email, max lengths, `password_hash` never projected
      into a DTO.
- [ ] Migration `users`.
- [ ] Unit tests: email normalization, hash verification succeeds/fails, a rehash of the same
      password produces a different hash (per-user salt).

## Acceptance criteria

- [ ] `dev@x.com` and `DEV@X.com` are the same user (unique index enforces it).
- [ ] `Verify` returns false for a wrong password and true for the right one.
- [ ] No DTO or log line can expose `PasswordHash` (checked by a test asserting the DTO shape).

## Technical notes

- Do not roll your own hashing. `PasswordHasher<T>` is PBKDF2 with sane defaults; Argon2id is the
  only upgrade worth considering, and it is a decision to record here if taken.
- Minimum length 10, checked in the validator not in the entity — the entity receives an
  already-hashed value and must never see plaintext.

## Learning goals

Aggregate design with invariants, ports and adapters (hashing as a port), why salts and slow
hashes exist.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

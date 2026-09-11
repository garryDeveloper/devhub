# DEVHUB-011 — Set up the test projects

|  |  |
|---|---|
| **Epic** | EPIC 1 — Foundation & project setup |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | M |
| **Depends on** | DEVHUB-006 |
| **Specs** | [`testing-strategy.md`](../../tech-specs/testing-strategy.md) |

## Context

The integration harness (real PostgreSQL via Testcontainers, real migrations, real HTTP) is the
single highest-value piece of infrastructure in this project. Build it before there is anything
to test, so every later ticket inherits it.

## Scope

**In:** four test projects, the integration base class, data reset strategy, auth helper,
builders, and the first meaningful tests.
**Out:** Playwright E2E (added in Phase 2), frontend test setup beyond the runner.

## Tasks

- [ ] Create `DevHub.Domain.UnitTests`, `DevHub.Application.UnitTests`,
      `DevHub.Api.IntegrationTests`, `DevHub.ArchitectureTests` (xUnit + FluentAssertions).
- [ ] Integration project: Testcontainers PostgreSQL, custom `WebApplicationFactory` overriding
      the connection string, migrations applied on start.
- [ ] Reset data between tests with Respawn (truncate) — one container per test class.
- [ ] Add `Seed` helpers and entity builders with sensible defaults.
- [ ] Add an `Auth` helper that logs in through the real endpoint and sets the bearer token
      (stub until EPIC 2 lands, then wire it for real).
- [ ] Write the first tests: `/health` returns 200; migrations apply to an empty database.
- [ ] Configure Vitest in `web/` and Jest + RNTL in `mobile/` with one example test each.

## Acceptance criteria

- [ ] `dotnet test` runs all four projects green from a clean clone (with Docker running).
- [ ] Integration tests do not depend on the developer's local database.
- [ ] Tests are order-independent: running a single test in isolation passes.
- [ ] The whole suite takes under two minutes at this stage.

## Technical notes

- One container per test **class**, not per test. Per-test containers turn a two-minute suite
  into twenty.
- Never mock `DbContext`. If a test needs a database, it gets a real one.
- `ITimeProvider` must be injectable so tests can freeze time instead of sleeping.

## Learning goals

Testcontainers, `WebApplicationFactory`, test isolation strategies, why integration tests are
the right bias for this codebase.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

# DEVHUB-110 — Health checks: live and ready

|  |  |
|---|---|
| **Epic** | EPIC 18 — Observability |
| **Phase** | 5 — Async & polish |
| **Priority** | P0 |
| **Size** | XS |
| **Depends on** | DEVHUB-003, DEVHUB-089 |
| **Specs** | [`observability-spec.md`](../../tech-specs/observability-spec.md) §5 |

## Context

Two endpoints with genuinely different jobs. Conflating them is how a brief database blip takes
down every healthy instance.

## Scope

**In:** `/health/live`, `/health/ready` with database and storage checks, timeouts, the JSON
payload.
**Out:** dependency checks for services that do not exist yet.

## Tasks

- [ ] `/health/live`: process-only, no dependencies, always fast.
- [ ] `/health/ready`: PostgreSQL check (`SELECT 1`) and S3 check (`HeadBucket`), each with a
      3-second timeout.
- [ ] Response payload with `status`, per-check results, `version` and `durationMs`;
      `503` when unhealthy.
- [ ] Point the Beanstalk health check at `/health/live` and the CI deploy gate at
      `/health/ready`.
- [ ] Do not expose connection strings, host names or versions of dependencies.
- [ ] Tests: ready returns 503 with the database stopped while live still returns 200.

## Acceptance criteria

- [ ] Stopping PostgreSQL makes `ready` unhealthy and leaves `live` healthy.
- [ ] Neither endpoint requires authentication or leaks infrastructure detail.
- [ ] A hung dependency cannot hang the endpoint beyond its timeout.

## Technical notes

The load balancer polls `live` constantly — it must never touch a dependency, or every dependency
blip becomes an instance replacement storm.

## Learning goals

Liveness vs readiness, timeouts on health checks, how orchestrators use these endpoints.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

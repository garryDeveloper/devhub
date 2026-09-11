# DEVHUB-003 — Health endpoint and Swagger/OpenAPI

|  |  |
|---|---|
| **Epic** | EPIC 1 — Foundation & project setup |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-002 |
| **Specs** | [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §15, [`observability-spec.md`](../../tech-specs/observability-spec.md) §5 |

## Context

`/health` is the first thing a load balancer, a CI health gate and you will call. Swagger is how
every other ticket gets verified by hand before a UI exists.

## Scope

**In:** basic health endpoint, Swagger UI, API versionless base path, CORS for local dev.
**Out:** database/storage health checks (DEVHUB-110), production hardening.

## Tasks

- [ ] Add `AddHealthChecks()` and map `GET /health` returning `{ status, version }`.
- [ ] Add Swashbuckle; enable Swagger UI **only** in Development and Staging.
- [ ] Set the base path convention `/api` for controllers.
- [ ] Configure CORS with an allow-list from configuration (`http://localhost:5173` locally).
- [ ] Return the assembly informational version in the health payload.
- [ ] Add `[ProducesResponseType]` conventions so generated docs are accurate from the start.

## Acceptance criteria

- [ ] `GET /health` returns `200` with `{"status":"Healthy","version":"..."}`.
- [ ] `GET /swagger` renders in Development and returns `404` when the environment is Production.
- [ ] A request from `http://localhost:5173` passes CORS; one from another origin does not.

## Technical notes

- `/health` must not require authentication.
- Do not use `AllowAnyOrigin()` together with credentials — it is silently ignored by browsers
  and hides the real configuration error.

## Learning goals

Health checks vs liveness vs readiness, why Swagger is disabled in production, how CORS
preflight actually works.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

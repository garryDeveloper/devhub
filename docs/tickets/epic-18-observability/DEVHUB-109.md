# DEVHUB-109 — Global exception handling and ProblemDetails

|  |  |
|---|---|
| **Epic** | EPIC 18 — Observability |
| **Phase** | 5 — Async & polish |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-108 |
| **Specs** | [`api-conventions.md`](../../tech-specs/api-conventions.md) §4, [`observability-spec.md`](../../tech-specs/observability-spec.md) §4 |

## Context

One place that turns exceptions into correct, safe HTTP responses. Ideally implemented early
(right after DEVHUB-018) and hardened here.

## Scope

**In:** exception→status mapping, RFC 7807 responses, `traceId`, safe 500s, validation error
shape.
**Out:** client-side handling (DEVHUB-112).

## Tasks

- [ ] Implement `IExceptionHandler` (or middleware) mapping the exception table from the spec.
- [ ] Define the exception types: `ValidationException`, `NotFoundException`,
      `ForbiddenException`, `ConflictException`, `DomainException`.
- [ ] Stable `type` URIs per error kind (e.g. `…/errors/invalid-status-transition`).
- [ ] Field-level `errors` for validation failures.
- [ ] `500` responses expose only `title`, `status` and `traceId`; the exception goes to logs.
- [ ] Log `4xx` at `Warning` and `5xx` at `Error`.
- [ ] Make ASP.NET's own 401/403/404 responses use the same body shape.
- [ ] Tests: one per exception type, plus an unhandled exception asserting no stack trace leaks.

## Acceptance criteria

- [ ] Every error response in the API has the same shape.
- [ ] A 500 never leaks type names, SQL or stack traces.
- [ ] `traceId` in a response matches the log line for that request.
- [ ] Validation errors carry per-field messages the clients can map to inputs.

## Technical notes

Consistency is the feature: two clients consume this API, and one exceptional response shape
means one error-handling code path in each of them.

## Learning goals

Centralized error handling, RFC 7807, information disclosure through error responses.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

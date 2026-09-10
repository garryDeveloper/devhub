# DEVHUB-108 — Structured logging and correlation ids

|  |  |
|---|---|
| **Epic** | EPIC 18 — Observability |
| **Phase** | 5 — Async & polish |
| **Priority** | P1 |
| **Size** | M |
| **Depends on** | DEVHUB-007 |
| **Specs** | [`observability-spec.md`](../../tech-specs/observability-spec.md) §1–3 |

## Context

Being able to answer "what happened to *that* request?" is the difference between debugging and
guessing. Do this before there is production traffic to debug.

## Scope

**In:** Serilog with JSON output, enrichers, correlation-id middleware, request summary logging,
log-level policy.
**Out:** metrics (DEVHUB-111), CloudWatch wiring (DEVHUB-100).

## Tasks

- [ ] Configure Serilog with the compact JSON formatter and configuration-driven levels.
- [ ] Correlation-id middleware: read or generate `X-Correlation-Id`, push to `LogContext`, echo
      on the response, expose it for `ProblemDetails.traceId`.
- [ ] Enrich with `UserId`, `Environment`, `Version`, `RequestPath` (route template, not the URL).
- [ ] Use Serilog request logging for one summary line per request; log health checks at `Debug`.
- [ ] Set `Microsoft.*` and `Microsoft.EntityFrameworkCore.*` to `Warning` in non-development.
- [ ] Replace every ad-hoc log call with a constant message template and structured properties.
- [ ] Add a test that runs a representative request flow and asserts no log line contains a
      token, password or presigned URL.

## Acceptance criteria

- [ ] Every log line is JSON with a correlation id.
- [ ] The correlation id returned in a response finds exactly that request's lines.
- [ ] EF Core does not log SQL at `Information` outside development.
- [ ] The "no secrets in logs" test passes.

## Technical notes

Message templates must be constants (`"Issue {IssueKey} changed"`), not interpolated strings.
Interpolation produces a unique message per event and destroys grouping, which is the entire
point of structured logging.

## Learning goals

Structured logging, log context and enrichment, correlation across layers, log-level discipline.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

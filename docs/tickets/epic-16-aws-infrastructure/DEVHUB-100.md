# DEVHUB-100 — CloudWatch logs and metrics wiring

|  |  |
|---|---|
| **Epic** | EPIC 16 — AWS infrastructure |
| **Phase** | 3 — AWS |
| **Priority** | P1 |
| **Size** | S |
| **Depends on** | DEVHUB-096 |
| **Specs** | [`observability-spec.md`](../../tech-specs/observability-spec.md) §7 |

## Context

Getting the structured logs the application already emits into a place where they can be queried,
and setting retention before a log bill appears.

## Scope

**In:** log groups with retention, JSON log ingestion, Logs Insights queries, a first dashboard.
**Out:** alarms (DEVHUB-111), the application-side logging work (DEVHUB-108).

## Tasks

- [ ] Create `/devhub/api/staging` and `/devhub/api/production` log groups with 7/30-day
      retention (retention is the cost control — set it before anything else).
- [ ] Confirm Beanstalk streams container logs there and that JSON lines are parsed as fields.
- [ ] Save the Logs Insights queries from the spec (by correlation id; 5xx by path).
- [ ] Create the `devhub-staging` dashboard with request rate, error rate, latency and RDS
      metrics.
- [ ] Verify a request's correlation id can be traced from an API response to a log line.
- [ ] Document the log group names and query snippets.

## Acceptance criteria

- [ ] Logs appear within seconds and are queryable by structured field, not just text.
- [ ] Retention is set on every log group (none is "Never expire").
- [ ] The correlation id from an error response finds the exact request in CloudWatch.

## Technical notes

"Never expire" is the default and the most common surprise line on an AWS bill for a small
project. Set retention on day one, on every group, including Lambda groups created later.

## Learning goals

Log groups and streams, structured log ingestion, Logs Insights, dashboards, cost control through
retention.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §5.

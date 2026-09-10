# DEVHUB-111 — Metrics, alarms and dashboard

|  |  |
|---|---|
| **Epic** | EPIC 18 — Observability |
| **Phase** | 5 — Async & polish |
| **Priority** | P1 |
| **Size** | M |
| **Depends on** | DEVHUB-100, DEVHUB-108 |
| **Specs** | [`observability-spec.md`](../../tech-specs/observability-spec.md) §6–8 |

## Context

Knowing something is wrong before a user tells you. Every alarm here must be one you would act
on — the rest are noise that trains you to ignore alarms.

## Scope

**In:** application metrics, CloudWatch alarms with SNS email, the operational dashboard,
alarm verification.
**Out:** paging/on-call, anomaly detection.

## Tasks

- [ ] Emit the metrics from the spec (request duration/count, db query duration, auth failures,
      webhooks received, deployments completed) with **low-cardinality** dimensions only.
- [ ] Create an SNS topic with an email subscription and confirm it.
- [ ] Create the six alarms from the spec table.
- [ ] Build the `devhub-{env}` dashboard with the four rows from the spec.
- [ ] **Trigger each alarm deliberately** (stop the database, generate 5xx, force a failed
      deployment) and record what you did and what the alert looked like.
- [ ] Document what to do when each alarm fires — an alarm without a response is decoration.

## Acceptance criteria

- [ ] Every alarm has been observed firing at least once, on purpose.
- [ ] No metric dimension contains a user id, issue id or full URL.
- [ ] The dashboard answers "is the system healthy?" without opening anything else.
- [ ] Each alarm has a written response action.

## Technical notes

High-cardinality dimensions are the expensive mistake: a `userId` dimension creates one metric
stream per user, and CloudWatch bills per stream. Route templates and environment names only.

## Learning goals

Metric design and cardinality, alarms and SNS, dashboards, testing alerting rather than assuming
it works.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §5.

# DEVHUB-066 — Deployment and DeploymentEvent entities

|  |  |
|---|---|
| **Epic** | EPIC 10 — Deployments |
| **Phase** | 2 — Product differentiation |
| **Priority** | P0 |
| **Size** | M |
| **Depends on** | DEVHUB-062 |
| **Specs** | [`domain-model.md`](../../domain-model.md), [`database-schema.md`](../../tech-specs/database-schema.md) |

## Context

The entity that makes DevHub a delivery tool. Its status lifecycle must be strict, because the
data arrives from an external system that can retry, duplicate and reorder its callbacks.

## Scope

**In:** `Deployment` aggregate, `DeploymentEvent` child, status transitions, duration, numbering,
migration.
**Out:** endpoints (DEVHUB-067), the webhook (DEVHUB-068).

## Tasks

- [ ] `Deployment` aggregate with per-project sequential `Number` (same technique as issue keys).
- [ ] Status transitions: `Queued → Running → (Succeeded|Failed|Canceled)`; terminal states are
      final.
- [ ] `AppendEvent(name, status, occurredAt, message)` — append-only, ordered by `occurredAt`.
- [ ] On reaching a terminal state: set `CompletedAt` and compute `DurationMs`; raise
      `DeploymentStatusChanged`.
- [ ] Optional links: `ReleaseId`, `CicdRunId`, `ExternalId` for idempotency.
- [ ] Migration with the partial unique index on `(environment_id, external_id)`.
- [ ] Unit tests: legal transitions, backwards transition ignored, duplicate terminal callback
      is a no-op, duration computed correctly.

## Acceptance criteria

- [ ] A `Running` update arriving after `Succeeded` does not change the deployment.
- [ ] A second `Succeeded` callback is a no-op and raises no second event.
- [ ] Duration matches `completedAt - startedAt`.

## Technical notes

"Ignore, do not throw" is the right behaviour for late and duplicate callbacks: throwing would
turn GitHub's retry into an error loop, and the information is genuinely redundant.

## Learning goals

Modelling externally-driven state machines, idempotency at the domain level, tolerating
out-of-order events.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

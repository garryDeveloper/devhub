# DEVHUB-069 — Environment health derivation and deployment metrics

|  |  |
|---|---|
| **Epic** | EPIC 10 — Deployments |
| **Phase** | 2 — Product differentiation |
| **Priority** | P1 |
| **Size** | S |
| **Depends on** | DEVHUB-068 |
| **Specs** | [`domain-model.md`](../../domain-model.md), [`user-flows.md`](../../user-flows.md) Flow 4–5 |

## Context

Turning a stream of deployment events into the one thing users look at: a health status they can
trust.

## Scope

**In:** health derivation rules, duration metric, deployment-frequency data for the dashboard.
**Out:** alerting (EPIC 18), DORA-style analytics (post-MVP).

## Tasks

- [ ] Implement the derivation: last deployment `Succeeded` → `Healthy`; `Failed` → `Unhealthy`;
      `Canceled` → unchanged; no deployments → `Unknown`.
- [ ] Add `Degraded`: a `Succeeded` deployment that follows a failure within a short window, or
      leave it unused — decide and document rather than leaving a dead enum value.
- [ ] Compute and store `DurationMs` on completion.
- [ ] Add a projection for the dashboard: deployments per environment in the last 7/30 days and
      the success rate.
- [ ] Backfill health for existing rows in the migration or a one-off command.
- [ ] Tests: each derivation case, including a failed deployment after a successful one.

## Acceptance criteria

- [ ] Environment health always matches the latest terminal deployment.
- [ ] A canceled deployment does not change health.
- [ ] Duration is present on every completed deployment.

## Technical notes

Health is derived, never stored independently. If you ever find code setting `HealthStatus`
outside `RecordDeploymentResult`, that is the bug — the domain method is the only door.

## Learning goals

Derived state, backfilling existing data, keeping an enum honest.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

# DEVHUB-082 — Dashboard aggregate endpoint

|  |  |
|---|---|
| **Epic** | EPIC 13 — Project dashboard |
| **Phase** | 2 — Product differentiation |
| **Priority** | P0 |
| **Size** | M |
| **Depends on** | DEVHUB-069, DEVHUB-079, DEVHUB-073 |
| **Specs** | [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §12 |

## Context

The dashboard is the product's thesis in one screen. It gets one endpoint on purpose: the
frontend must not assemble it from ten requests.

## Scope

**In:** `GET /api/projects/{projectId}/dashboard` returning the complete read model.
**Out:** caching layers, historical charts (post-MVP).

## Tasks

- [ ] Build the read model exactly as specified: project, issue summary, environments, recent
      deployments, recent CI runs, recent releases, recent activity.
- [ ] Implement as a handful of parallel projected queries (`Task.WhenAll`), not one giant join.
- [ ] Issue summary: open, in progress, in review, done in the last 7 days, unassigned.
- [ ] Recent lists capped at 5 items each.
- [ ] Authorize once with `IWorkspaceAccessService.ForProject`.
- [ ] Add a benchmark/integration test asserting the query count and a p95 under 300 ms on
      seeded data (~5 000 issues).
- [ ] Tests: shape correctness, empty project (no environments, no deployments), scoping.

## Acceptance criteria

- [ ] One request returns everything the dashboard screen needs.
- [ ] Six or fewer database round-trips.
- [ ] A brand-new project returns a valid, empty-but-complete payload.
- [ ] Response time stays under 300 ms with realistic data.

## Technical notes

This is a **read model**, not an aggregate: project it directly, do not load entities. It is the
one place the module boundary is deliberately crossed, and that is documented in
[`backend-architecture.md`](../../tech-specs/backend-architecture.md) §10 so it does not become a
precedent.

## Learning goals

Read models and CQRS-lite, parallel queries, why endpoint design follows screen design, measuring
before optimizing.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

# DEVHUB-079 — CI/CD run query endpoints

|  |  |
|---|---|
| **Epic** | EPIC 12 — CI/CD integration |
| **Phase** | 2 — Product differentiation |
| **Priority** | P1 |
| **Size** | XS |
| **Depends on** | DEVHUB-077 |
| **Specs** | [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §11 |

## Context

Read endpoints for the CI/CD screen and the dashboard panel.

## Scope

**In:** list runs (filterable), get a run.
**Out:** cancelling or re-running from DevHub (out of scope for the product).

## Tasks

- [ ] `GET /api/projects/{id}/cicd/runs?status=&branch=&workflow=&page=` — newest first.
- [ ] `GET /api/cicd/runs/{id}` including the linked deployment id when present.
- [ ] Add a summary projection (last N runs, success rate over 7 days) for the dashboard.
- [ ] Ensure the `(project_id, created_at DESC)` index is used.
- [ ] Tests: filters, paging, scoping.

## Acceptance criteria

- [ ] Filters work individually and combined.
- [ ] Runs from another workspace return `404`.
- [ ] The list query is index-supported.

## Technical notes

Keep this endpoint boring — its whole job is feeding two read-only panels. Resist adding
aggregation here; the dashboard has its own aggregate endpoint (DEVHUB-082).

## Learning goals

Read-only query endpoints, projections for panels, index verification.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

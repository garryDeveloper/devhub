# DEVHUB-067 — Deployment endpoints

|  |  |
|---|---|
| **Epic** | EPIC 10 — Deployments |
| **Phase** | 2 — Product differentiation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-066, DEVHUB-063 |
| **Specs** | [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §9 |

## Context

Read endpoints for the UI, plus authenticated create/update for manual deployments and for local
testing before the webhook exists.

## Scope

**In:** list, detail with the event timeline, create, status update.
**Out:** the webhook path (DEVHUB-068).

## Tasks

- [ ] `GET /api/environments/{id}/deployments` — paged, newest first, `?status=` filter.
- [ ] `GET /api/deployments/{id}` — full DTO including the ordered event timeline.
- [ ] `POST /api/environments/{id}/deployments` — create a deployment (project member).
- [ ] `PATCH /api/deployments/{id}/status` — advance the status, appending an event.
- [ ] Accept a deployment number in the route for user-friendly links
      (`/api/projects/{id}/deployments/182`).
- [ ] Tests: paging, filtering, transition rules through the API, scoping.

## Acceptance criteria

- [ ] The detail response contains the events in chronological order.
- [ ] An illegal transition through the API returns `422`.
- [ ] Deployments from another workspace return `404`.

## Technical notes

Having a manual create/update path is what lets you build and demo the entire environments and
dashboard UI before wiring GitHub Actions — build the UI against this, then swap in the webhook.

## Learning goals

Designing read models for a timeline UI, keeping a manual path for testability.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

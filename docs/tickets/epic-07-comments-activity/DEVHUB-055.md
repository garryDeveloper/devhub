# DEVHUB-055 — Activity feed endpoint

|  |  |
|---|---|
| **Epic** | EPIC 7 — Comments & activity feed |
| **Phase** | 1 — Local foundation |
| **Priority** | P1 |
| **Size** | XS |
| **Depends on** | DEVHUB-054 |
| **Specs** | [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §6 |

## Context

Reading the audit trail, per issue and (optionally) per project for the dashboard.

## Scope

**In:** `GET /api/issues/{id}/activities`, paged, newest first, with actor summaries.
**Out:** merging comments into the same stream (the client interleaves them).

## Tasks

- [ ] Paged query projecting actor summary, type, old/new values, metadata and timestamp.
- [ ] `?type=` filter for narrowing to one activity kind.
- [ ] A project-scoped variant (`GET /api/projects/{id}/activities?limit=`) for the dashboard's
      recent-activity panel.
- [ ] Ensure the query uses the `(issue_id, created_at DESC)` index.
- [ ] Tests: ordering, pagination, cross-workspace access → 404.

## Acceptance criteria

- [ ] Activities return newest first with correct paging metadata.
- [ ] The actor is included so the client needs no second lookup.
- [ ] Access is scoped like every other issue endpoint.

## Technical notes

Comments and activities are separate endpoints on purpose: they page differently and have
different lifetimes. The client merges them by timestamp for display — a server-side union would
make pagination incoherent.

## Learning goals

Read-only projections, index-supported ordering, deciding what to merge on the client.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

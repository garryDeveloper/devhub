# DEVHUB-039 — List issues with filtering, sorting and pagination

|  |  |
|---|---|
| **Epic** | EPIC 5 — Issues |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | M |
| **Depends on** | DEVHUB-038 |
| **Specs** | [`api-conventions.md`](../../tech-specs/api-conventions.md) §5–6, [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §4 |

## Context

The most-used endpoint in the product, and the one most likely to become slow. Build it as a
projection query with whitelisted sorting.

## Scope

**In:** filters, sorting, pagination, the list DTO.
**Out:** full-text search ranking (EPIC 8), saved presets (DEVHUB-050).

## Tasks

- [ ] `GetIssuesQuery` with: `status[]`, `priority[]`, `assigneeId` (accepts `me`), `labelId[]`,
      `search`, `createdAfter/Before`, `dueBefore`, `includeArchived`, `sort`, `page`, `pageSize`.
- [ ] Same-field values OR together; different fields AND together.
- [ ] Whitelist sort fields; reject anything else with `400`; default `-updatedAt`.
- [ ] `pageSize` default 50, clamped to 100 (clamp, do not error).
- [ ] Project directly into `IssueListItemDto` with `AsNoTracking()`; include assignee summary
      and labels without N+1.
- [ ] `search` here is a simple `ILIKE` on title/key — full-text arrives in EPIC 8.
- [ ] Tests: each filter individually, two filters combined, pagination totals, sort direction,
      cross-project isolation, and an assertion that the query count is constant regardless of
      page size.

## Acceptance criteria

- [ ] Filters combine as specified and return correct `totalCount`.
- [ ] An unknown `sort` value returns `400`; an unknown filter key is ignored.
- [ ] Loading 100 issues with assignees and labels issues a bounded number of SQL statements.
- [ ] Archived issues are excluded by default.

## Technical notes

Inspect the generated SQL once with EF's logging turned up. Labels are a collection — projecting
them naively per row is the N+1 this ticket exists to avoid.

## Learning goals

Composable query building, projection vs entity loading, N+1 detection, pagination contracts.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

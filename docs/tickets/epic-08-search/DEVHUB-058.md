# DEVHUB-058 — PostgreSQL full-text search setup

|  |  |
|---|---|
| **Epic** | EPIC 8 — Search |
| **Phase** | 2 — Product differentiation |
| **Priority** | P1 |
| **Size** | M |
| **Depends on** | DEVHUB-039 |
| **Specs** | [`search-spec.md`](../../tech-specs/search-spec.md) |

## Context

Search without new infrastructure. PostgreSQL's full-text search is more than enough at this
scale, and setting it up teaches how indexed text search actually works.

## Scope

**In:** generated `tsvector` columns, GIN indexes, `pg_trgm` fallback, EF Core mapping.
**Out:** the endpoints (DEVHUB-059) and clients (DEVHUB-060/061).

## Tasks

- [ ] Add generated `search_vector` columns to `issues`, `projects` and `releases` with the
      weights from the spec.
- [ ] Create GIN indexes on each; add a `pg_trgm` index on `issues.title` for the fallback.
- [ ] Map the columns in EF Core (`HasGeneratedTsVectorColumn` or raw SQL in the migration).
- [ ] Verify the index is used with `EXPLAIN (ANALYZE, BUFFERS)` and record the plan in the PR.
- [ ] Seed a few thousand rows locally and measure before/after.
- [ ] Test that updating a title updates the vector automatically (that is the point of
      `GENERATED … STORED`).

## Acceptance criteria

- [ ] The vector updates itself on insert and update, with no application code involved.
- [ ] `EXPLAIN` shows a bitmap index scan on the GIN index, not a sequential scan.
- [ ] Migration applies cleanly to an empty database and to one with existing rows.

## Technical notes

`GENERATED ALWAYS AS … STORED` needs an `IMMUTABLE` expression — `to_tsvector('english', …)` with
an explicit configuration qualifies; the single-argument form does not. This is the error you
will hit, and knowing why is the lesson.

## Learning goals

`tsvector`/`tsquery`, GIN vs B-tree, generated columns, reading a query plan.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

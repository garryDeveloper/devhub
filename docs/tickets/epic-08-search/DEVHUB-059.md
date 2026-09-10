# DEVHUB-059 — Search endpoints

|  |  |
|---|---|
| **Epic** | EPIC 8 — Search |
| **Phase** | 2 — Product differentiation |
| **Priority** | P1 |
| **Size** | S |
| **Depends on** | DEVHUB-058 |
| **Specs** | [`search-spec.md`](../../tech-specs/search-spec.md) §3–5 |

## Context

One grouped endpoint for the command palette, plus per-entity endpoints for full result pages.

## Scope

**In:** `/api/search`, `/api/search/issues`, `/api/search/projects`, ranking, scoping.
**Out:** highlighting via `ts_headline`, comment search (both post-MVP).

## Tasks

- [ ] `GET /api/search?q=&limit=` returning grouped issues, projects and releases.
- [ ] Special case: a term matching `^[A-Z]+-\d+$` resolves the issue key directly and returns
      it first.
- [ ] Prefix matching by appending `:*` to the final lexeme; `pg_trgm` fallback when FTS returns
      nothing.
- [ ] Scope every query by joining `workspace_members` for the caller — this join *is* the
      authorization.
- [ ] `q` shorter than 2 characters returns empty groups without querying.
- [ ] Per-entity endpoints with normal pagination and an optional `projectId` filter.
- [ ] Tests: exact key wins; title outranks description; another workspace's data never appears;
      punctuation and quotes do not throw.

## Acceptance criteria

- [ ] Searching `DEV-42` returns that issue as the first result.
- [ ] Results are strictly limited to the caller's workspaces (the critical test).
- [ ] A malformed query string cannot produce a 500.

## Technical notes

Use `websearch_to_tsquery`, never `to_tsquery` — the latter throws on ordinary human input like
`deploy failed?`, turning a search box into a 500 generator.

## Learning goals

Ranking with weights, safe query parsing, embedding authorization into the query itself.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

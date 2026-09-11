# Search specification

PostgreSQL full-text search. **Do not introduce Elasticsearch or OpenSearch.** The dataset is
small, the queries are simple, and running a search cluster teaches operations, not search.

---

## 1. Scope

Searchable in the MVP: issues (key, title, description), projects (key, name, description),
releases (version, name, notes). Comments and activity are post-MVP.

Results are always scoped to workspaces the caller belongs to. Search must never be the hole
through which authorization leaks.

---

## 2. Index design

Generated `tsvector` column with weights, plus a GIN index:

```sql
ALTER TABLE issues ADD COLUMN search_vector tsvector
  GENERATED ALWAYS AS (
      setweight(to_tsvector('english', coalesce(key, '')),         'A') ||
      setweight(to_tsvector('english', coalesce(title, '')),       'B') ||
      setweight(to_tsvector('english', coalesce(description, '')), 'C')
  ) STORED;

CREATE INDEX ix_issues_search ON issues USING GIN (search_vector);
```

Weights: `A` key (an exact `DEV-42` must win), `B` title, `C` description. The default ranking
`ts_rank_cd` respects them.

Same pattern for `projects` (`key` A, `name` B, `description` C) and `releases`
(`version` A, `name` B, `notes` C).

The `english` configuration is fine for a portfolio project. If content is bilingual, use
`simple` (no stemming) rather than guessing the language per row.

---

## 3. Query construction

```sql
SELECT i.id, i.key, i.title, i.status, p.key AS project_key,
       ts_rank_cd(i.search_vector, q) AS rank
  FROM issues i
  JOIN projects p           ON p.id = i.project_id
  JOIN workspace_members wm ON wm.workspace_id = p.workspace_id AND wm.user_id = @userId
     , websearch_to_tsquery('english', @q) AS q
 WHERE i.search_vector @@ q
   AND i.archived_at IS NULL
 ORDER BY rank DESC, i.updated_at DESC
 LIMIT @limit;
```

- Use `websearch_to_tsquery` — it accepts human input (`deployment -failed`, `"exact phrase"`)
  and never throws on syntax, unlike `to_tsquery`.
- Always parameterize. Never concatenate the query string.
- The `workspace_members` join **is** the authorization check; it cannot be forgotten the way a
  separate `WHERE` clause can.

In EF Core:

```csharp
var q = EF.Functions.WebSearchToTsQuery("english", term);
var results = await db.Issues
    .Where(i => i.SearchVector.Matches(q) && i.ArchivedAt == null)
    .Where(i => db.WorkspaceMembers.Any(wm =>
        wm.UserId == userId && wm.WorkspaceId == i.Project.WorkspaceId))
    .OrderByDescending(i => i.SearchVector.RankCoverDensity(q))
    .Take(limit)
    .Select(i => new IssueSearchResultDto(...))
    .ToListAsync(ct);
```

---

## 4. Prefix / "as you type" behaviour

Full-text search does not match prefixes by default: typing `deplo` finds nothing. Two
mitigations, in order of preference:

1. Append `:*` to the last lexeme for a prefix query when the term does not end in whitespace.
2. Fall back to `pg_trgm` similarity on `title` when FTS returns zero rows:

```sql
CREATE INDEX ix_issues_title_trgm ON issues USING GIN (title gin_trgm_ops);
-- WHERE title % @q ORDER BY similarity(title, @q) DESC
```

Special case handled before either: if the term matches `^[A-Z]+-\d+$`, look up the issue key
directly and return it first. Searching `DEV-42` must always find `DEV-42`.

---

## 5. API behaviour

```http
GET /api/search?q=deployment&limit=5
GET /api/search/issues?q=deployment&projectId=…&page=1&pageSize=20
```

- `q` shorter than 2 characters → `200` with empty groups, no query executed.
- Global search returns at most `limit` (default 5, max 10) per group — it feeds a palette, not
  a results page.
- Per-entity endpoints are paged normally.
- Response includes `totalCount` only for the per-entity endpoints; the palette does not need it.

---

## 6. Client behaviour

- Debounce 250 ms; abort the in-flight request when the term changes (`AbortController`).
- Empty input shows recent searches from local storage — no request.
- Highlight matched terms client-side (simple case-insensitive segment match); server-side
  `ts_headline` is a post-MVP nicety and costs a query per row.
- Web: ⌘/Ctrl+K opens the palette, ↑/↓ navigate, ↵ opens, Esc closes, focus returns to the
  trigger. Mobile: a search screen with recents and grouped results.

---

## 7. Performance targets

| Scale | Expectation |
|---|---|
| < 10k issues | < 30 ms, GIN index only |
| < 100k issues | < 80 ms; add `(project_id)` to the index if project-scoped search dominates |
| > 100k issues | revisit — that is far beyond this project's needs |

Measure with `EXPLAIN (ANALYZE, BUFFERS)` before optimizing. A sequential scan on a small table
is not a bug.

---

## 8. Testing

- [ ] `DEV-42` returns that exact issue first.
- [ ] Matching by title ranks above matching by description only.
- [ ] Results from another user's workspace never appear (the critical test).
- [ ] Archived issues are excluded.
- [ ] A term with punctuation or quotes does not throw.
- [ ] A 1-character query returns empty groups without hitting the database.
- [ ] The GIN index is actually used (assert on the query plan in a dedicated test, or verify
      manually once and record the plan in the ticket).

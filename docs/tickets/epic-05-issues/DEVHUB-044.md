# DEVHUB-044 — Web issue list view

|  |  |
|---|---|
| **Epic** | EPIC 5 — Issues |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | M |
| **Depends on** | DEVHUB-039, DEVHUB-033 |
| **Specs** | [`frontend-web-architecture.md`](../../tech-specs/frontend-web-architecture.md), [`screens-and-navigation.md`](../../screens-and-navigation.md) |

## Context

The default working view: a dense, scannable table with the filters in the URL.

## Scope

**In:** table, sorting, pagination, row navigation, create-issue modal, URL-driven query state.
**Out:** the filter bar UI (DEVHUB-051), the board (DEVHUB-045), the detail drawer (DEVHUB-046).

## Tasks

- [ ] `/p/:projectKey/issues`: columns key, title, status, priority, assignee, labels, updated.
- [ ] Read filters, sort and page from the URL via a typed `useIssueFilters()` hook (Zod-parsed).
- [ ] Sortable column headers that update the URL.
- [ ] Pagination control showing the total count.
- [ ] Create-issue modal with optimistic insert and rollback on failure.
- [ ] Row click opens the detail (a route change; the drawer arrives in DEVHUB-046).
- [ ] Skeleton, empty and error states; keyboard-navigable rows.

## Acceptance criteria

- [ ] Copying the URL into a new tab reproduces the exact filtered, sorted, paged view.
- [ ] Browser back/forward moves through view states correctly.
- [ ] Creating an issue shows it immediately and reconciles with the server response.
- [ ] The table is readable at 768px (columns collapse rather than overflow the page).

## Technical notes

Include the parsed filter object in the query key so each view state caches independently —
otherwise switching filters shows the previous result for a frame.

## Learning goals

URL as the single source of truth for view state, typed query-param parsing, optimistic list
insertion.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3.

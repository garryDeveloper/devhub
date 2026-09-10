# DEVHUB-047 — Mobile issue list, detail and editing

|  |  |
|---|---|
| **Epic** | EPIC 5 — Issues |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | L |
| **Depends on** | DEVHUB-035, DEVHUB-042 |
| **Specs** | [`mobile-architecture.md`](../../tech-specs/mobile-architecture.md) |

## Context

The main reason the mobile app exists: check what is assigned to you and move it along without
opening a laptop.

## Scope

**In:** My Issues, project issue list, issue detail, create and edit, status/priority bottom
sheets.
**Out:** filters UI (DEVHUB-052), comments (DEVHUB-057), board editing (read-only on mobile).

## Tasks

- [ ] `MyIssues` (Issues tab): issues assigned to the caller across projects, grouped by project.
- [ ] Project issue list with infinite scroll (`useInfiniteQuery`) and pull-to-refresh.
- [ ] `IssueDetail`: key, title, description (markdown), status, priority, assignee, labels,
      due date, and metadata.
- [ ] Status and priority changes via a bottom sheet with an optimistic update and rollback.
- [ ] Create issue screen (title, description, priority, assignee, labels).
- [ ] Edit title/description screen with a dirty-state guard.
- [ ] Loading, empty and error states everywhere; deep link `devhub://issues/DEV-42`.

## Acceptance criteria

- [ ] An issue can be found, opened and moved to a new status in under three taps from the tab.
- [ ] Optimistic changes revert visibly when the request fails.
- [ ] Infinite scroll does not duplicate or drop items when data changes between pages.
- [ ] The deep link opens the right issue from a cold start.

## Technical notes

Offset pagination plus live data can duplicate rows across pages. Sort by a stable key
(`-updatedAt, id`) and de-duplicate by id when flattening pages.

## Learning goals

Infinite queries, bottom sheets as a mobile-native control, optimistic UI on flaky networks,
pagination pitfalls.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §4.

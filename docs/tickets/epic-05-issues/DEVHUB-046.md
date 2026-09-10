# DEVHUB-046 — Web issue detail drawer

|  |  |
|---|---|
| **Epic** | EPIC 5 — Issues |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | M |
| **Depends on** | DEVHUB-040, DEVHUB-044 |
| **Specs** | [`screens-and-navigation.md`](../../screens-and-navigation.md) §2, §4 |

## Context

Where an issue is actually worked. A drawer keeps the list context visible; the same URL must
also work as a full page for direct links and narrow screens.

## Scope

**In:** drawer layout, inline editing of every field, markdown rendering, responsive full-page
mode.
**Out:** comments and activity feed (DEVHUB-056), attachments (DEVHUB-092).

## Tasks

- [ ] Route `/p/:projectKey/issues/:issueKey` rendering a drawer over the list on ≥1024px and a
      full page below that.
- [ ] Header: key, title (inline editable), close button; Esc closes and focus returns to the row.
- [ ] Sidebar fields: status, priority, assignee, labels, due date — each an inline control
      calling its endpoint with an optimistic update.
- [ ] Description: markdown rendered read-only, editable in place with save/cancel.
- [ ] Sanitize rendered markdown (no raw HTML/script) — treat issue text as untrusted input.
- [ ] Archive action with confirmation.
- [ ] Skeleton while loading, error state with retry, "not found" state for a bad key.

## Acceptance criteria

- [ ] Opening an issue does not lose the list's scroll position or filters.
- [ ] The URL is shareable and opens the same issue directly.
- [ ] Every field edit persists and reflects in the list behind the drawer.
- [ ] Focus is trapped in the drawer and restored on close.

## Technical notes

Markdown sanitization is not optional: `dangerouslySetInnerHTML` over user text is a stored XSS.
Use a renderer that escapes HTML by default, and test with `<img src=x onerror=alert(1)>`.

## Learning goals

Drawer vs page routing on one URL, focus management, inline editing patterns, safe markdown
rendering.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3.

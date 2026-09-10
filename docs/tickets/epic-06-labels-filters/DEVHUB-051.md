# DEVHUB-051 — Web filter bar, label management and URL persistence

|  |  |
|---|---|
| **Epic** | EPIC 6 — Labels, priorities & filtering |
| **Phase** | 1 — Local foundation |
| **Priority** | P1 |
| **Size** | M |
| **Depends on** | DEVHUB-044, DEVHUB-048, DEVHUB-050 |
| **Specs** | [`screens-and-navigation.md`](../../screens-and-navigation.md) §4 |

## Context

The filter bar is what makes the list and the board usable past fifty issues, and it is shared by
both views.

## Scope

**In:** filter bar component, active-filter chips, saved presets, labels tab in project settings.
**Out:** mobile filters (DEVHUB-052).

## Tasks

- [ ] Filter bar above the list and board: status, priority, assignee, label, date range,
      plus a search input.
- [ ] Multi-select dropdowns with counts; every change writes to the URL, never to component
      state.
- [ ] Active filters shown as removable chips, with "Clear all".
- [ ] Saved presets dropdown: apply, save current, delete.
- [ ] Labels tab in project settings: create, rename, recolor, delete (with a warning showing how
      many issues use the label).
- [ ] Debounce the search input by 250 ms; other filters apply immediately.
- [ ] Result count reflects the active filters; empty state explains which filters to relax.

## Acceptance criteria

- [ ] The bar reads its state from the URL and survives a refresh and a share.
- [ ] Applying a preset updates the URL and the results.
- [ ] Deleting a label in use warns first and updates the affected issues in the UI.
- [ ] Filters apply identically to the list and to the board.

## Technical notes

Keep the filter state in exactly one place — the URL. A local copy "for performance" will drift
from it, and the resulting bugs are subtle and constant.

## Learning goals

Complex form state driven by the URL, debouncing, sharing view state across two screens.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3.

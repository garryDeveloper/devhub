# DEVHUB-052 — Mobile issue filters

|  |  |
|---|---|
| **Epic** | EPIC 6 — Labels, priorities & filtering |
| **Phase** | 1 — Local foundation |
| **Priority** | P2 |
| **Size** | S |
| **Depends on** | DEVHUB-047, DEVHUB-048 |
| **Specs** | [`mobile-architecture.md`](../../tech-specs/mobile-architecture.md) §7 |

## Context

Mobile filtering is about a few quick choices, not a full filter builder. A modal with the
common options is enough.

## Scope

**In:** filter modal (status, priority, assignee, label), quick chips, applied-filter indicator.
**Out:** saved presets and date ranges (web-only), label management.

## Tasks

- [ ] `IssueFilters` modal screen with grouped selectable chips and Apply / Reset actions.
- [ ] Quick chips above the list: "Mine", "In progress", "Urgent".
- [ ] Filter state held in the screen's navigation params so back/forward behaves sensibly.
- [ ] A badge on the filter icon showing how many filters are active.
- [ ] Empty state that names the active filters and offers to clear them.

## Acceptance criteria

- [ ] Applying filters updates the list and shows the active count.
- [ ] Navigating away and back preserves the filters for that screen.
- [ ] Reset returns to the unfiltered list in one tap.

## Technical notes

Include the filter object in the query key exactly as on web, so switching filters does not show
stale results for a frame.

## Learning goals

Mobile filter UX, navigation params as view state, keeping parity with web where it matters and
diverging where it does not.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §4.

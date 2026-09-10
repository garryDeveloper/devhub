# DEVHUB-087 — Web notification center

|  |  |
|---|---|
| **Epic** | EPIC 14 — Notifications |
| **Phase** | 5 — Async & polish |
| **Priority** | P1 |
| **Size** | S |
| **Depends on** | DEVHUB-086, DEVHUB-083 |
| **Specs** | [`screens-and-navigation.md`](../../screens-and-navigation.md) §4 |

## Context

A badge, a dropdown and a full page. The value is in the deep links: every notification must land
the user exactly where the event happened.

## Scope

**In:** sidebar badge, dropdown panel, `/notifications` page, mark-read behaviour.
**Out:** browser push notifications, real-time delivery (polling is fine).

## Tasks

- [ ] Badge showing the unread count, polling `unread-count` every 60 s and on window focus.
- [ ] Dropdown with the 10 most recent, unread visually distinct, "Mark all read" and "See all".
- [ ] `/notifications` page: paged list with an unread-only filter.
- [ ] Clicking a notification marks it read (optimistically) and navigates to its target.
- [ ] Target routing per type: issue → issue drawer, deployment → deployment detail, CI → run.
- [ ] Empty state ("You're all caught up"), loading and error states.

## Acceptance criteria

- [ ] The badge reflects reality within a minute of an event.
- [ ] Every notification type navigates to a correct, existing screen.
- [ ] Marking all read updates the badge immediately and does not resurrect on refetch.

## Technical notes

Polling every 60 s per open tab is acceptable at this scale and avoids a WebSocket layer. If
several tabs make it feel wasteful, `BroadcastChannel` can share one poller — a nice optional
exercise, not a requirement.

## Learning goals

Polling strategies, optimistic read-state, type-driven navigation.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3.

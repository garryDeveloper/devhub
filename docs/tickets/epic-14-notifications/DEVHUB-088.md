# DEVHUB-088 — Mobile notifications screen

|  |  |
|---|---|
| **Epic** | EPIC 14 — Notifications |
| **Phase** | 5 — Async & polish |
| **Priority** | P1 |
| **Size** | S |
| **Depends on** | DEVHUB-086, DEVHUB-084 |
| **Specs** | [`mobile-architecture.md`](../../tech-specs/mobile-architecture.md) §3 |

## Context

In-app notifications on mobile, with the deep-link plumbing that a later push-notification ticket
will reuse unchanged.

## Scope

**In:** notifications screen, unread badge on the Home tab, deep-link navigation.
**Out:** push notifications (post-MVP, but this ticket prepares the routing).

## Tasks

- [ ] `NotificationsScreen` under the Home tab: list with unread styling, pull-to-refresh,
      infinite scroll.
- [ ] Tab badge with the unread count, refreshed on focus.
- [ ] Tap marks read optimistically and navigates via the same deep-link resolver used for
      `devhub://` links.
- [ ] Swipe-to-mark-read and a "Mark all read" header action.
- [ ] Empty, loading and error states.

## Acceptance criteria

- [ ] Every notification type navigates correctly, including from a cold start.
- [ ] The badge clears when notifications are read.
- [ ] The deep-link resolver is shared with DEVHUB-022's link handling, not duplicated.

## Technical notes

Routing every navigation through one resolver is what makes the future push-notification ticket
small: the payload will carry the same `devhub://` URL and reuse this path.

## Learning goals

Centralized navigation resolution, swipe actions, designing today for a known future feature.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §4.

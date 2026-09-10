# DEVHUB-084 — Mobile home dashboard

|  |  |
|---|---|
| **Epic** | EPIC 13 — Project dashboard |
| **Phase** | 2 — Product differentiation |
| **Priority** | P1 |
| **Size** | M |
| **Depends on** | DEVHUB-082, DEVHUB-065 |
| **Specs** | [`mobile-architecture.md`](../../tech-specs/mobile-architecture.md) §7 |

## Context

Mobile's home tab: a vertical, prioritized version of the same answer. Not a shrunken desktop
dashboard.

## Scope

**In:** Home tab with environment health across projects, assigned issues, recent deployments.
**Out:** the full panel set from web (CI runs and releases stay in their project sections).

## Tasks

- [ ] Home screen sections, in priority order: (1) environment health for the user's projects,
      (2) issues assigned to the user, (3) recent deployments.
- [ ] A per-project dashboard screen reusing the same endpoint, laid out vertically.
- [ ] Pull-to-refresh; refetch on focus.
- [ ] Every row navigates to the relevant detail screen.
- [ ] Loading, empty and error states per section, so one failing section does not blank the
      screen.

## Acceptance criteria

- [ ] Opening the app answers "is anything broken?" without scrolling.
- [ ] Each section fails independently.
- [ ] Data refreshes when returning to the app after a while.

## Technical notes

Ordering by *what a phone user needs first* — health, then their own work — is the whole design
decision here. Copying the web order would bury the alert under counters.

## Learning goals

Prioritizing content for a small screen, resilient sectioned screens, focus-based refresh.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §4.

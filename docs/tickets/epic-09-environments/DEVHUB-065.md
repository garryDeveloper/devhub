# DEVHUB-065 — Mobile environments screens

|  |  |
|---|---|
| **Epic** | EPIC 9 — Environments |
| **Phase** | 2 — Product differentiation |
| **Priority** | P1 |
| **Size** | S |
| **Depends on** | DEVHUB-063, DEVHUB-035 |
| **Specs** | [`mobile-architecture.md`](../../tech-specs/mobile-architecture.md) §7 |

## Context

"Is production up?" is the single most valuable question the mobile app can answer, and it should
be answerable from the home screen without navigating.

## Scope

**In:** environment list under a project, environment detail with recent deployments.
**Out:** environment management (web-only), deployment detail (DEVHUB-071).

## Tasks

- [ ] `Environments` list: name, type badge, health glyph, version, relative last-deploy time.
- [ ] `EnvironmentDetail`: large health status, version, last deployment, and the ten most
      recent deployments.
- [ ] Tapping the environment URL opens it in the system browser.
- [ ] Pull-to-refresh; refetch when the screen regains focus.
- [ ] Loading, empty and error states.

## Acceptance criteria

- [ ] Health is legible at a glance on a phone screen, in both light and dark mode.
- [ ] Returning to the screen shows fresh data without a manual refresh.
- [ ] Every state (including Unknown) renders correctly.

## Technical notes

Use `useFocusEffect` to refetch on screen focus — a stale "Healthy" from ten minutes ago is worse
than no answer at all on a screen whose whole job is current status.

## Learning goals

Focus-driven refetching in React Navigation, compact status design, dark mode considerations.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §4.

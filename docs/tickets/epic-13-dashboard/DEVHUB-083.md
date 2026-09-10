# DEVHUB-083 — Web project dashboard

|  |  |
|---|---|
| **Epic** | EPIC 13 — Project dashboard |
| **Phase** | 2 — Product differentiation |
| **Priority** | P0 |
| **Size** | M |
| **Depends on** | DEVHUB-082, DEVHUB-070 |
| **Specs** | [`screens-and-navigation.md`](../../screens-and-navigation.md) §4 |

## Context

The screen that answers "what is being worked on, what shipped, is production healthy?" — and the
one to screenshot for the portfolio.

## Scope

**In:** the project overview screen with all panels.
**Out:** customizable layout, charts over time.

## Tasks

- [ ] `/p/:projectKey` layout: production health banner, counter row, then three panels
      (recent deployments, recent CI runs, recent activity) plus recent releases.
- [ ] Health banner: environment name, health, version, relative last-deploy time — large and
      unmissable.
- [ ] Counters (open issues, in progress, releases) link to the correspondingly filtered views.
- [ ] Deployment and CI rows link to their detail screens.
- [ ] Refetch on window focus and every 60 s.
- [ ] Skeletons matching the final layout; per-panel empty states; a single error state with
      retry.
- [ ] Responsive: three columns at ≥1280px, stacked at 768px.

## Acceptance criteria

- [ ] Everything renders from one API call.
- [ ] Production health is the first thing the eye lands on.
- [ ] Every counter and row is a link to somewhere useful — no dead ends.
- [ ] A new project shows a helpful onboarding-ish empty dashboard, not a broken grid.

## Technical notes

Skeletons should match the real layout's dimensions, otherwise the page jumps when data arrives —
that visible reflow is what makes an otherwise fast app feel slow.

## Learning goals

Dashboard information hierarchy, layout stability, making every element navigable.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3.

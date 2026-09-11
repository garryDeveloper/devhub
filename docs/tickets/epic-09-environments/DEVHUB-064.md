# DEVHUB-064 — Web environments screen

|  |  |
|---|---|
| **Epic** | EPIC 9 — Environments |
| **Phase** | 2 — Product differentiation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-063, DEVHUB-034 |
| **Specs** | [`screens-and-navigation.md`](../../screens-and-navigation.md) §4, §8 |

## Context

The "is production healthy?" answer, at a glance. This screen is judged on how fast a human can
read it.

## Scope

**In:** environment cards with health, version and last deploy; environment management for
owners.
**Out:** deployment history list (DEVHUB-070).

## Tasks

- [ ] `/p/:projectKey/environments`: one card per environment showing health glyph + label,
      current version, relative last-deploy time, and a link to the environment URL.
- [ ] Health colors paired with glyphs (never color alone); "Unknown" handled explicitly.
- [ ] Card links to the environment detail (deployment history).
- [ ] Owner-only: add, edit and reorder environments; delete with the `409` case explained.
- [ ] Auto-refresh every 30 s while the tab is focused (`refetchOnWindowFocus` + interval).
- [ ] Loading skeletons, empty state, error state.

## Acceptance criteria

- [ ] Production health is identifiable in under a second of looking.
- [ ] The screen refreshes without a manual reload after a deployment completes.
- [ ] Meaning survives with color vision deficiency (verify with a simulator).
- [ ] Members see the cards but no management controls.

## Technical notes

Relative times ("12 minutes ago") need a ticking clock or they silently go stale on a screen
someone leaves open — recompute on an interval, and show the absolute UTC time in the tooltip.

## Learning goals

Status-at-a-glance design, accessible status encoding, polling vs focus-based refetching.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3.

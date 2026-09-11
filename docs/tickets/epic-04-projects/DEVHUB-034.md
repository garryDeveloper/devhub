# DEVHUB-034 — Web project settings screen

|  |  |
|---|---|
| **Epic** | EPIC 4 — Projects |
| **Phase** | 1 — Local foundation |
| **Priority** | P1 |
| **Size** | S |
| **Depends on** | DEVHUB-032, DEVHUB-033 |
| **Specs** | [`screens-and-navigation.md`](../../screens-and-navigation.md) §2 |

## Context

One settings page with tabs, so labels (EPIC 6) and future sections have an obvious home.

## Scope

**In:** general tab (name, description, color, icon, archive), members tab.
**Out:** labels tab (added by DEVHUB-051), integrations tab (EPIC 12).

## Tasks

- [ ] `/p/:projectKey/settings` with a tabbed layout: General · Members (· Labels later).
- [ ] General: name, description, color, icon; read-only key with an explanatory tooltip.
- [ ] Archive with a confirmation dialog explaining what archiving does and does not do.
- [ ] Members tab: list, add (workspace-member picker), remove with confirmation.
- [ ] Owner-only controls hidden for members, with a read-only view instead.
- [ ] Dirty-state guard: warn before navigating away with unsaved changes.

## Acceptance criteria

- [ ] Saving general settings updates the sidebar and project list immediately.
- [ ] Archiving redirects to the project list and shows the project as archived.
- [ ] Members can view settings but cannot modify them.
- [ ] Navigating away with unsaved edits prompts first.

## Technical notes

Invalidate both the project detail and the project list query keys after a save; a stale sidebar
name is the classic symptom of forgetting one.

## Learning goals

Tabbed settings layout, cache invalidation across related queries, unsaved-changes guards.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3.

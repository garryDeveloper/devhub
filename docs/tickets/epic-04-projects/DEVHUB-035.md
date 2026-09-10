# DEVHUB-035 — Mobile project list and project shell

|  |  |
|---|---|
| **Epic** | EPIC 4 — Projects |
| **Phase** | 1 — Local foundation |
| **Priority** | P1 |
| **Size** | S |
| **Depends on** | DEVHUB-029, DEVHUB-031 |
| **Specs** | [`mobile-architecture.md`](../../tech-specs/mobile-architecture.md) §3 |

## Context

The Projects tab and the per-project section list that later tickets (issues, environments,
deployments) plug their screens into.

## Scope

**In:** project list, project detail shell with section navigation, placeholders for the
sections.
**Out:** the section screens themselves, project creation and settings (web-only).

## Tasks

- [ ] `ProjectList`: `FlatList` of projects in the active workspace with color, key and name.
- [ ] Pull-to-refresh, loading, empty and error states.
- [ ] `ProjectDetail`: header with project identity plus a section list (Overview, Issues,
      Board, Releases, Environments, Deployments, CI/CD) navigating to placeholder screens.
- [ ] Typed navigation params (`projectId`, `projectKey`).
- [ ] Deep link `devhub://projects/{id}` opens the project detail.

## Acceptance criteria

- [ ] The list reflects the selected workspace and refreshes on pull.
- [ ] Every section navigates without a typing error at compile time.
- [ ] The deep link works from a cold start.

## Technical notes

Seed the detail screen's cache from the list item so the header renders immediately instead of
showing a spinner for data the app already has.

## Learning goals

FlatList patterns, nested navigators, cache seeding for instant detail screens.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §4.

# DEVHUB-029 — Mobile workspace selector and members view

|  |  |
|---|---|
| **Epic** | EPIC 3 — Workspaces & membership |
| **Phase** | 1 — Local foundation |
| **Priority** | P1 |
| **Size** | S |
| **Depends on** | DEVHUB-022, DEVHUB-025 |
| **Specs** | [`mobile-architecture.md`](../../tech-specs/mobile-architecture.md) §7 |

## Context

Mobile needs to know which workspace it is in, and to show who is in it — but workspace
administration deliberately stays on web.

## Scope

**In:** workspace selector modal, active-workspace context, read-only members list under the
Me tab.
**Out:** creating workspaces, inviting, role changes, renaming (all web-only by design).

## Tasks

- [ ] Workspace selector as a modal from the Projects tab header; persist the selection in
      SecureStore/AsyncStorage (non-sensitive → AsyncStorage is fine here).
- [ ] `WorkspaceContext` providing the active workspace to queries; changing it resets
      workspace-scoped queries.
- [ ] `WorkspaceMembers` read-only list with avatar, name and role.
- [ ] Empty state when the user has no workspace, pointing them to the web app to create one.
- [ ] Pull-to-refresh, loading and error states.

## Acceptance criteria

- [ ] The selected workspace survives an app restart.
- [ ] Switching workspaces refreshes the project list.
- [ ] No mutation controls appear anywhere on these screens.

## Technical notes

Be explicit in the UI that management happens on the web — an unexplained missing button reads
as a bug, a one-line note reads as a decision.

## Learning goals

Cross-screen context in React Native, deciding what *not* to build on mobile.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §4.

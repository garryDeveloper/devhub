# DEVHUB-027 — Web workspace switcher

|  |  |
|---|---|
| **Epic** | EPIC 3 — Workspaces & membership |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-020, DEVHUB-024 |
| **Specs** | [`screens-and-navigation.md`](../../screens-and-navigation.md) §3 |

## Context

The workspace is in the URL, so the switcher is really navigation plus a memory of where the
user was last.

## Scope

**In:** switcher dropdown, create-workspace modal, last-workspace memory, routing.
**Out:** settings and member screens (DEVHUB-028).

## Tasks

- [ ] Dropdown in the sidebar header listing the user's workspaces with their role.
- [ ] Switching navigates to `/w/{slug}` and invalidates workspace-scoped queries.
- [ ] "Create workspace" modal with name (slug preview), calling `POST /api/workspaces` and
      navigating into the new workspace on success.
- [ ] Remember the last workspace slug in `localStorage`; `/` redirects there, or to the first
      workspace, or to an onboarding empty state when the user has none.
- [ ] Empty state for a brand-new account that guides straight into creating a workspace.
- [ ] Loading and error states for the workspace list.

## Acceptance criteria

- [ ] A user with several workspaces can switch without a full page reload.
- [ ] After creating a workspace, the app is already inside it.
- [ ] A returning user lands in the workspace they last used.
- [ ] A user with zero workspaces sees the onboarding empty state, not a broken shell.

## Technical notes

Invalidate the query cache on switch, or a stale project list from workspace A will flash inside
workspace B. Prefer scoping the query keys by workspace id so this is structural rather than a
manual invalidation.

## Learning goals

URL-driven tenant context, query cache scoping, onboarding empty states.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3.

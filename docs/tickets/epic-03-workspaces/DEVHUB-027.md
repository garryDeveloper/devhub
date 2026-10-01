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

- [x] Dropdown in the sidebar header listing the user's workspaces with their role.
      *Disclosure pattern (button + links), not `role="menu"`. The list comes from
      `GET /api/workspaces` through TanStack Query, not from `/me`.*
- [x] Switching navigates to `/w/{slug}` and invalidates workspace-scoped queries.
      *Structural, not manual: workspace-scoped keys live under the workspace id
      (`qk.projects(workspaceId)`), so a switch changes the key and nothing needs invalidating.*
- [x] "Create workspace" modal with name (slug preview), calling `POST /api/workspaces` and
      navigating into the new workspace on success.
      *Read-only preview from `slugify()`, a mirror of `WorkspaceSlug.FromName`; only `name` is
      sent. A 409, or a 400 on `slug`, is shown on the name field. The modal lives in the shell
      so the mobile drawer can close without unmounting it.*
- [x] Remember the last workspace slug in `localStorage`; `/` redirects there, or to the first
      workspace, or to an onboarding empty state when the user has none.
      *Key is per user (`devhub:lastWorkspace:{userId}`) and only written once `WorkspaceLayout`
      has confirmed the slug. Routes without a slug (`/p/...`) show the last workspace.*
- [x] Empty state for a brand-new account that guides straight into creating a workspace.
- [x] Loading and error states for the workspace list.
      *Also a "Workspace not found" state for a slug the user does not belong to.*

## Acceptance criteria

- [x] A user with several workspaces can switch without a full page reload.
- [x] After creating a workspace, the app is already inside it.
- [x] A returning user lands in the workspace they last used.
- [x] A user with zero workspaces sees the onboarding empty state, not a broken shell.

## Technical notes

Invalidate the query cache on switch, or a stale project list from workspace A will flash inside
workspace B. Prefer scoping the query keys by workspace id so this is structural rather than a
manual invalidation.

## Learning goals

URL-driven tenant context, query cache scoping, onboarding empty states.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3.

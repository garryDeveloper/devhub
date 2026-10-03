# DEVHUB-028 — Web workspace settings and members screens

|  |  |
|---|---|
| **Epic** | EPIC 3 — Workspaces & membership |
| **Phase** | 1 — Local foundation |
| **Priority** | P1 |
| **Size** | M |
| **Depends on** | DEVHUB-025, DEVHUB-027 |
| **Specs** | [`screens-and-navigation.md`](../../screens-and-navigation.md) §4 |

## Context

The first screens with role-dependent UI and destructive actions — a good place to establish how
those are presented everywhere else.

## Scope

**In:** settings (rename), members table, invite modal, role change, remove/leave.
**Out:** billing, workspace deletion, audit log.

## Tasks

- [x] `/w/:slug/settings`: name form, read-only slug with an explanation of why it is fixed.
      *Not route-guarded by role (see technical notes below): a member gets the same screen,
      rendered read-only, never a disabled form.*
- [x] `/w/:slug/members`: table with avatar, name, email, role, joined date.
- [x] Invite modal (email + role) with server error mapping (`404` user not found →
      "No DevHub account with that email").
- [x] Role dropdown with an optimistic update and rollback on failure.
- [x] Remove member with a confirmation naming the person; "Leave workspace" for non-owners.
      *Also applies to an owner's own row — removing yourself is "Leave", regardless of role.*
- [x] Hide owner-only controls for members, and show a read-only view instead of a broken form.
- [x] Loading, empty and error states; disable controls while a mutation is in flight.

## Acceptance criteria

- [x] An owner can invite, change roles and remove members from the UI.
- [x] A member sees the list but no mutation controls.
- [x] Attempting to demote the last owner shows the server's explanation, not a generic error.
- [x] Every destructive action is confirmed and names its target.

## Technical notes

Hiding a control is UX, not authorization — the API still enforces the rule. Say this out loud
in review so the habit sticks.

screens-and-navigation.md §2 originally marked both routes "owner"-only. That conflicts with "A
member sees the list but no mutation controls" — a route guard would block a member from a screen
they're explicitly supposed to see, read-only. Fixed the doc instead: both routes are reachable by
any member; only the mutation controls are role-conditional, inside the page. Updated
frontend-web-architecture.md §7 to match (it had described a `<RequireWorkspaceRole>` route guard
that was never built and would have been wrong for this ticket).

## Learning goals

Role-conditional UI, optimistic mutations with rollback, humane error messages, destructive
action design.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3.

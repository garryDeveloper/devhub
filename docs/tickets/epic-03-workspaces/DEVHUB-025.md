# DEVHUB-025 — Workspace membership endpoints

|  |  |
|---|---|
| **Epic** | EPIC 3 — Workspaces & membership |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-024 |
| **Specs** | [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §2 |

## Context

Collaboration needs members. In the MVP, adding a member means adding an existing DevHub user by
email — email invitations for people without accounts are post-MVP.

## Scope

**In:** list, add, change role, remove members.
**Out:** email invitations, pending-invite state, seat limits.

## Tasks

- [x] `GET .../members` — any member may read the list (id, user summary, role, joinedAt).
      *Scoped entirely by `IWorkspaceQueries.ListMembersAsync`: no `IWorkspaceAccessService` call
      needed, since membership is itself the only check a read needs.*
- [x] `POST .../members { email, role }` — owner only; `404` if no such user, `409` if already a
      member.
- [x] `PATCH .../members/{memberId} { role }` — owner only; `422` when demoting the last owner.
- [x] `DELETE .../members/{memberId}` — owner only; `422` when removing the last owner; a member
      may remove themselves (leave).
      *Authorized with `RequireMember`, not `RequireOwner`: the handler itself allows a non-owner
      only when the target member is the caller.*
- [ ] Removing a member also removes their project memberships in the same transaction.
      *Deferred: `Project`/`ProjectMember` don't exist yet (EPIC 4 is unbuilt). Revisit in
      DEVHUB-031/032.*
- [x] Integration tests for each rule above, including "member leaves their own workspace".

## Acceptance criteria

- [x] An owner can add, promote, demote and remove members.
- [x] The last owner cannot be demoted or removed by any path.
- [ ] A removed member immediately loses access to the workspace's projects and issues.
      *Not yet meaningfully testable — there are no projects to lose access to until EPIC 4.*
- [x] A non-owner attempting any mutation gets `403`.

## Technical notes

"Add by email" leaks whether an email is registered. Acceptable within an authenticated,
owner-only endpoint, but note it — a public equivalent would not be.

## Learning goals

Role-based rules in a domain aggregate, cascading membership removal, careful design of
destructive operations.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

# DEVHUB-023 — Workspace and WorkspaceMember entities

|  |  |
|---|---|
| **Epic** | EPIC 3 — Workspaces & membership |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-013 |
| **Specs** | [`domain-model.md`](../../domain-model.md), [`database-schema.md`](../../tech-specs/database-schema.md) |

## Context

The workspace is the security boundary of the whole application. Every authorization check in
every later ticket resolves to "is this user a member of this workspace".

## Scope

**In:** `Workspace` aggregate with its member collection, roles, slug rules, migration.
**Out:** endpoints (DEVHUB-024/025), invitations by email (post-MVP).

## Tasks

- [ ] `Workspace` aggregate in `Domain/Workspaces` with `Members` as a private collection
      exposed read-only.
- [ ] `Workspace.Create(name, ownerId)` adds the owner as a member in the same operation.
- [ ] Domain methods: `AddMember`, `RemoveMember`, `ChangeMemberRole`, `Rename`.
- [ ] Invariant: the workspace always keeps at least one `Owner`; removing or demoting the last
      owner throws `DomainException`.
- [ ] Slug: lowercase kebab, derived from the name, unique, 3–50 chars, immutable after creation.
- [ ] EF configuration + migration for `workspaces` and `workspace_members`
      (unique `(workspace_id, user_id)`, index on `user_id`).
- [ ] Unit tests for every invariant, including the last-owner rules.

## Acceptance criteria

- [ ] Creating a workspace yields exactly one `Owner` member.
- [ ] Removing the last owner throws; removing a second owner does not.
- [ ] Adding the same user twice is rejected by the domain and by the unique index.

## Technical notes

The index on `workspace_members(user_id)` is not optional — it is on the hot path of every
authorization query in the system.

## Learning goals

Aggregate boundaries with child collections, invariant enforcement in the domain, designing a
tenancy boundary.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

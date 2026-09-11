# DEVHUB-030 — Project and ProjectMember entities

|  |  |
|---|---|
| **Epic** | EPIC 4 — Projects |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-023 |
| **Specs** | [`domain-model.md`](../../domain-model.md), [`database-schema.md`](../../tech-specs/database-schema.md) |

## Context

Projects hold everything interesting: issues, environments, releases, CI runs. The project key is
the piece users see constantly (`DEV-42`), so its rules must be strict from the start.

## Scope

**In:** `Project` aggregate, `ProjectMember`, key rules, issue sequence counter, archive
semantics, migration.
**Out:** endpoints (DEVHUB-031/032), default environment seeding (DEVHUB-062).

## Tasks

- [ ] `Project` aggregate with `Create(workspaceId, name, key, creatorId)`.
- [ ] Key validation `^[A-Z][A-Z0-9]{1,9}$`, unique per workspace, **immutable** after creation.
- [ ] `IssueSequence` counter starting at 0, incremented only through the domain.
- [ ] Domain methods: `Rename`, `UpdateDescription`, `SetAppearance`, `Archive`, `Unarchive`,
      `AddMember`, `RemoveMember`.
- [ ] Invariant: a project member must be a workspace member.
- [ ] EF configuration + migration with the unique index and the key CHECK constraint.
- [ ] Unit tests: key validation, immutability, archive/unarchive, member rules.

## Acceptance criteria

- [ ] Two projects in the same workspace cannot share a key; two workspaces can.
- [ ] Attempting to change the key throws.
- [ ] An archived project keeps its data and can be restored.

## Technical notes

Enforce the key format in the database as well as the domain. Database constraints are the only
rules that survive a bad migration or a manual fix.

## Learning goals

Immutable fields, database CHECK constraints, per-tenant uniqueness, soft-delete semantics.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

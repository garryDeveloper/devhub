# DEVHUB-031 — Project CRUD endpoints

|  |  |
|---|---|
| **Epic** | EPIC 4 — Projects |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-030, DEVHUB-026 |
| **Specs** | [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §3 |

## Context

Standard CRUD, but with two decisions worth making deliberately: the key cannot change, and
"delete" means archive.

## Scope

**In:** create, list, get, update, archive.
**Out:** members (DEVHUB-032), permanent deletion.

## Tasks

- [ ] `POST /api/workspaces/{workspaceId}/projects` — any workspace member; `409` on a duplicate
      key; seeds the three default environments once EPIC 9 exists.
- [ ] `GET /api/workspaces/{workspaceId}/projects?includeArchived=false` returning summaries
      with open-issue counts.
- [ ] `GET /api/projects/{projectId}` — `404` for non-members.
- [ ] `PATCH /api/projects/{projectId}` — owner only; `422` if `key` is present in the body.
- [ ] `DELETE /api/projects/{projectId}` — owner only; sets `archivedAt`, returns `204`.
- [ ] Tests: duplicate key, key change attempt, archived project excluded by default, non-member
      404, member 403 on PATCH.

## Acceptance criteria

- [ ] Creating a project returns `201` with a `Location` header.
- [ ] Archived projects are hidden unless explicitly requested.
- [ ] Attempting to change the key returns `422` with a clear message.

## Technical notes

The open-issue count in the list is a subquery, not a loaded collection. Check the generated SQL
once — this is exactly the shape that becomes an N+1 if written naively.

## Learning goals

Nested vs flat routes, archive vs delete, avoiding N+1 in list projections.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

# DEVHUB-026 — Workspace authorization and scoping helper

|  |  |
|---|---|
| **Epic** | EPIC 3 — Workspaces & membership |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-023 |
| **Specs** | [`auth-spec.md`](../../tech-specs/auth-spec.md) §5, [`backend-architecture.md`](../../tech-specs/backend-architecture.md) §8 |

## Context

Every subsequent endpoint needs the same question answered: "can this user touch this resource,
and in what role?" Answer it once, in one place, or it will be answered inconsistently forty
times.

## Scope

**In:** `IWorkspaceAccessService` with resolvers per resource type, role requirement helpers,
and the convention that "no access" produces `404`.
**Out:** per-project roles (post-MVP).

## Tasks

- [ ] Define `WorkspaceAccess(WorkspaceId, ProjectId?, Role)` and
      `IWorkspaceAccessService` with `ForWorkspace`, `ForProject`, `ForIssue`,
      `ForEnvironment`, `ForRelease`, `ForDeployment`, `ForCicdRun` — each returning `null`
      when the caller has no access.
- [ ] Implement each as a single projected query (no aggregate loading).
- [ ] Add `RequireMember()` / `RequireOwner()` helpers throwing `NotFoundException` /
      `ForbiddenException`.
- [ ] Retrofit DEVHUB-024/025 to use it.
- [ ] Add an integration test per resolver proving cross-workspace access returns `404`.

## Acceptance criteria

- [ ] One helper call is all a handler needs to authorize.
- [ ] Each resolver costs a single database round-trip.
- [ ] Cross-workspace access returns `404` for every resource type.

## Technical notes

Resolve scope **from the resource id**, never from a workspace id in the request body — a body
value is attacker-controlled. Consider caching the result per request (scoped service) since a
handler may ask twice.

## Learning goals

Centralizing authorization, projection queries, designing an API that is hard to misuse.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

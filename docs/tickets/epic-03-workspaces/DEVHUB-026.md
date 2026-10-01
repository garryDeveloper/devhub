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

- [x] Define `WorkspaceAccess(WorkspaceId, ProjectId?, Role)` and
      `IWorkspaceAccessService` with `ForWorkspace`, `ForProject`, `ForIssue`,
      `ForEnvironment`, `ForRelease`, `ForDeployment`, `ForCicdRun` — each returning `null`
      when the caller has no access.
      *Only `ForWorkspaceAsync` is implemented: the other entities do not exist yet. The rest
      are commented-out reminders in the interface and in `WorkspaceAccessService`, each tagged
      with the ticket that creates its entity and must implement it: `ForProject` DEVHUB-030,
      `ForIssue` 036, `ForEnvironment` 062, `ForDeployment` 066, `ForRelease` 072,
      `ForCicdRun` 077. `Role` is the `WorkspaceRole` enum; the caller comes from
      `ICurrentUser`, so no resolver takes a user id.*
- [x] Implement each as a single projected query (no aggregate loading).
      *Results are cached per request (scoped service), "no access" included.*
- [x] Add `RequireMember()` / `RequireOwner()` helpers throwing `NotFoundException` /
      `ForbiddenException`.
      *DECISION: they return `Result<WorkspaceAccess>` instead of throwing — "no access" is an
      expected outcome, and expected outcomes are returned in this codebase (`Result.cs`). It
      also avoids a global exception translator. Each takes the resource's own not-found error;
      the 403 is `workspaces.owner_required`.*
- [x] Retrofit DEVHUB-024/025 to use it.
      *024: `UpdateWorkspaceHandler`. `GET`/list keep `IWorkspaceQueries`, which already filters
      by membership inside its single query. 025 is not implemented yet — it uses the service
      from the start.*
- [x] Add an integration test per resolver proving cross-workspace access returns `404`.
      *`WorkspaceAccessServiceTests` covers `ForWorkspace`; each later resolver adds its own.*

## Acceptance criteria

- [x] One helper call is all a handler needs to authorize.
      *One resolver call plus `RequireX`, then one `if (access.IsFailure)`.*
- [x] Each resolver costs a single database round-trip.
- [ ] Cross-workspace access returns `404` for every resource type.
      *Met for workspaces. Stays open until the last resolver (DEVHUB-077) lands with its test.*

## Technical notes

Resolve scope **from the resource id**, never from a workspace id in the request body — a body
value is attacker-controlled. Consider caching the result per request (scoped service) since a
handler may ask twice.

## Learning goals

Centralizing authorization, projection queries, designing an API that is hard to misuse.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

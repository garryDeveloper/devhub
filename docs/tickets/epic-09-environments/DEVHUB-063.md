# DEVHUB-063 — Environment endpoints

|  |  |
|---|---|
| **Epic** | EPIC 9 — Environments |
| **Phase** | 2 — Product differentiation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-062, DEVHUB-026 |
| **Specs** | [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §8 |

## Context

CRUD, with one rule that must be enforced at the API edge: clients cannot write derived fields.

## Scope

**In:** list, create, get, update, delete.
**Out:** deployment history within the environment response (DEVHUB-067 adds the summary).

## Tasks

- [ ] `GET /api/projects/{id}/environments` ordered by `sortOrder`, including the last deployment
      summary.
- [ ] `POST` — owner only; `409` on duplicate name.
- [ ] `PATCH` — owner only; accepts `name`, `url`, `type`, `sortOrder` only; `422` if the body
      contains `currentVersion`, `healthStatus` or `lastDeployedAt`.
- [ ] `DELETE` — owner only; `409` if the environment has deployments.
- [ ] `GET /api/environments/{id}` with the last deployment embedded.
- [ ] Tests: derived-field write rejected, delete blocked by deployments, member gets 403 on
      mutations, non-member gets 404.

## Acceptance criteria

- [ ] A client attempting to set health receives `422` and nothing changes.
- [ ] An environment with history cannot be deleted.
- [ ] The list is ordered deterministically.

## Technical notes

Rejecting unknown/derived properties with `422` instead of silently ignoring them turns a
client bug into an immediate, obvious error — which is what you want while building two clients.

## Learning goals

Protecting derived state through the API, meaningful conflict responses, ordering guarantees.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

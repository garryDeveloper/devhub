# DEVHUB-073 — Release endpoints and publishing

|  |  |
|---|---|
| **Epic** | EPIC 11 — Releases |
| **Phase** | 2 — Product differentiation |
| **Priority** | P1 |
| **Size** | S |
| **Depends on** | DEVHUB-072, DEVHUB-026 |
| **Specs** | [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §10 |

## Context

CRUD plus one action endpoint. `POST /publish` is a good example of when REST should model an
action rather than a field update.

## Scope

**In:** list, create, get, patch, publish.
**Out:** issue linking (DEVHUB-074).

## Tasks

- [ ] `GET /api/projects/{id}/releases?status=` — newest first, with linked-issue counts.
- [ ] `POST` — `409` on duplicate version.
- [ ] `GET /api/releases/{id}` — includes linked issues and the deployments carrying that version.
- [ ] `PATCH` — full edit while `Draft`; name/notes only once `Released` (`422` otherwise).
- [ ] `POST /api/releases/{id}/publish` — `409` if already published; returns the updated DTO.
- [ ] Tests: publish flow, double publish, editing a published release, duplicate version.

## Acceptance criteria

- [ ] Publishing sets the status and timestamp and creates activity on every linked issue.
- [ ] A published release rejects version and issue-set changes.
- [ ] The detail response shows where the release is deployed.

## Technical notes

`POST /publish` rather than `PATCH {status:"Released"}` because publishing has side effects
(timestamps, activity, notifications) and is not reversible — an action endpoint says that,
a field update does not.

## Learning goals

Action endpoints in REST, state-dependent validation, connecting aggregates in a read model.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

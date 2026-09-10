# DEVHUB-077 — CICDRun entity

|  |  |
|---|---|
| **Epic** | EPIC 12 — CI/CD integration |
| **Phase** | 2 — Product differentiation |
| **Priority** | P1 |
| **Size** | S |
| **Depends on** | DEVHUB-030 |
| **Specs** | [`domain-model.md`](../../domain-model.md), [`webhooks-spec.md`](../../tech-specs/webhooks-spec.md) §5 |

## Context

DevHub mirrors pipeline runs; it does not execute them. The entity stores metadata and links out
to the provider for anything detailed.

## Scope

**In:** `CICDRun` aggregate, provider mapping, idempotency key, migration, repository mapping
config on the project.
**Out:** the webhook (DEVHUB-078), the UI (DEVHUB-081).

## Tasks

- [ ] `CICDRun` aggregate: provider, externalId, workflow, runNumber, branch, commitSha,
      commitUrl, runUrl, status, startedAt, completedAt, durationMs.
- [ ] `(projectId, provider, externalId)` unique — the idempotency key.
- [ ] `UpdateFromProvider(status, completedAt, …)` applying the same "never move backwards" rule
      as deployments.
- [ ] Add `Project.GithubRepository` (e.g. `dario/devhub`) so a webhook can resolve the project.
- [ ] Raise `CicdRunCompleted` on terminal status.
- [ ] Migration + index `(project_id, created_at DESC)`.
- [ ] Unit tests: status mapping from GitHub's status/conclusion pairs, idempotent updates.

## Acceptance criteria

- [ ] The same provider run id never creates two rows.
- [ ] Every GitHub status/conclusion combination maps to a defined DevHub status.
- [ ] No log or column stores CI output — only links.

## Technical notes

Storing logs would mean unbounded storage growth and a worse log viewer than GitHub's. Store the
`runUrl` and get out of the way.

## Learning goals

Mirroring an external system's state, idempotency keys, scoping a feature by deciding what not
to store.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

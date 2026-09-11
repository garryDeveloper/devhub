# DEVHUB-062 — Environment entity and default seeding

|  |  |
|---|---|
| **Epic** | EPIC 9 — Environments |
| **Phase** | 2 — Product differentiation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-030 |
| **Specs** | [`domain-model.md`](../../domain-model.md), [`database-schema.md`](../../tech-specs/database-schema.md) |

## Context

Environments are where DevHub stops being a task manager. They are also the first entity whose
state is **derived** rather than edited — a distinction worth enforcing in the domain.

## Scope

**In:** `Environment` aggregate, health/version as derived state, default seeding on project
creation, migration.
**Out:** endpoints (DEVHUB-063), deployments (EPIC 10).

## Tasks

- [ ] `Environment` aggregate with `Name`, `Type`, `Url`, `SortOrder` as editable fields.
- [ ] `CurrentVersion`, `HealthStatus`, `LastDeployedAt` settable **only** through
      `RecordDeploymentResult(status, version, at)` — no public setters.
- [ ] Unique name per project (case-insensitive); type from the three-value enum.
- [ ] Seed `Development`, `Staging`, `Production` when a project is created (through the domain,
      not a migration).
- [ ] Migration + index `(project_id, sort_order)`.
- [ ] Unit tests: seeding, derived-state transitions, duplicate name rejected.

## Acceptance criteria

- [ ] A new project has exactly three environments in the documented order.
- [ ] Health and version cannot be set except by recording a deployment result.
- [ ] Two environments in one project cannot share a name.

## Technical notes

Modelling derived state as a domain method rather than a property is the whole lesson here: it
makes the invariant "health reflects reality" impossible to break from a controller.

## Learning goals

Derived vs stored state, seeding through the domain, encapsulation as an invariant tool.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

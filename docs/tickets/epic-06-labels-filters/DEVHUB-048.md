# DEVHUB-048 — Label entity and CRUD endpoints

|  |  |
|---|---|
| **Epic** | EPIC 6 — Labels, priorities & filtering |
| **Phase** | 1 — Local foundation |
| **Priority** | P1 |
| **Size** | S |
| **Depends on** | DEVHUB-031 |
| **Specs** | [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §5 |

## Context

Labels are the flexible dimension of issue organization: project-scoped, colored, many-to-many.

## Scope

**In:** `Label` entity, migration, CRUD endpoints, deletion cascade.
**Out:** assigning labels to issues (DEVHUB-049), label UI (DEVHUB-051).

## Tasks

- [ ] `Label` entity in `Domain/Projects` (a child of the project aggregate).
- [ ] Unique name per project, case-insensitive; ≤40 characters; hex color validated `^#[0-9A-Fa-f]{6}$`.
- [ ] Endpoints: list, create, patch (name/color), delete.
- [ ] Deleting a label removes it from every issue and records an activity entry on each.
- [ ] Optionally seed a small default set (`bug`, `feature`, `docs`) on project creation — decide
      and document.
- [ ] Tests: duplicate name (case-insensitive) → 409, invalid color → 400, delete detaches from
      issues, cross-project label access → 404.

## Acceptance criteria

- [ ] `Backend` and `backend` cannot both exist in one project.
- [ ] Deleting a label leaves no orphan rows in `issue_labels`.
- [ ] Labels are invisible and unusable across projects.

## Technical notes

Enforce case-insensitive uniqueness with a functional index (`lower(name)`), not by lowercasing
the stored value — users care about the casing they typed.

## Learning goals

Child entities within an aggregate, functional indexes, cascade side effects with audit trails.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

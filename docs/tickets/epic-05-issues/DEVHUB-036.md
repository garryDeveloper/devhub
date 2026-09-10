# DEVHUB-036 — Issue entity, statuses and transitions

|  |  |
|---|---|
| **Epic** | EPIC 5 — Issues |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | M |
| **Depends on** | DEVHUB-030 |
| **Specs** | [`domain-model.md`](../../domain-model.md), [`database-schema.md`](../../tech-specs/database-schema.md) |

## Context

The core aggregate of the product. Every rule about how work moves lives here, in the domain —
not in a controller and not in the UI.

## Scope

**In:** `Issue` aggregate, status/priority enums, transition rules, domain events, EF mapping,
migration, indexes.
**Out:** key generation (DEVHUB-037), endpoints (DEVHUB-038+), labels (EPIC 6).

## Tasks

- [ ] `Issue` aggregate in `Domain/Issues` with `Create(projectId, number, key, title, reporterId, …)`.
- [ ] `IssueStatus` and `IssuePriority` enums exactly as specified.
- [ ] Encode the transition table from [`domain-model.md`](../../domain-model.md) in a
      `CanTransitionTo` map; `ChangeStatus` throws `DomainException("invalid-status-transition")`
      otherwise.
- [ ] Methods: `ChangeStatus`, `ChangePriority`, `Assign`, `Unassign`, `UpdateTitle`,
      `UpdateDescription`, `SetDueDate`, `Archive`.
- [ ] Raise the matching domain event from each method, carrying old and new values.
- [ ] EF configuration + migration with the indexes from the schema spec.
- [ ] Unit tests covering every legal and illegal transition, and event emission.

## Acceptance criteria

- [ ] `Done → InReview` throws; `Done → InProgress` (reopen) succeeds.
- [ ] Each mutating method raises exactly one domain event with correct old/new values.
- [ ] Setting the same value twice does **not** raise an event (no noise in the activity feed).
- [ ] Indexes from the spec exist in the migration.

## Technical notes

The "same value is a no-op" rule matters more than it looks: without it, every save produces
activity rows and notifications for changes that did not happen.

## Learning goals

State machines in a domain model, domain events, index design driven by known query patterns.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

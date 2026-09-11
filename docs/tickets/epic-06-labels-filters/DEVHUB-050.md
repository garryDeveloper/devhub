# DEVHUB-050 — Saved filter presets

|  |  |
|---|---|
| **Epic** | EPIC 6 — Labels, priorities & filtering |
| **Phase** | 1 — Local foundation |
| **Priority** | P2 |
| **Size** | S |
| **Depends on** | DEVHUB-039 |
| **Specs** | [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §5 |

## Context

Users converge on two or three views ("my open work", "urgent bugs"). Saving them is cheap and
makes the filter bar feel finished.

## Scope

**In:** `IssueFilter` entity, list/create/delete endpoints, private-per-user semantics.
**Out:** sharing presets with the team, default preset per user.

## Tasks

- [ ] `IssueFilter` entity: project, user, name, `query` (the raw query string), created date.
- [ ] Endpoints: list (own only), create, delete.
- [ ] Validate the stored query against the same parser the list endpoint uses — reject a preset
      that would not execute.
- [ ] Unique name per (project, user), case-insensitive.
- [ ] Cap at 20 presets per user per project.
- [ ] Tests: another user's preset is invisible and undeletable; an invalid query → 400.

## Acceptance criteria

- [ ] Presets are private to their creator.
- [ ] A saved preset reproduces exactly the view it was saved from.
- [ ] An unparseable query is rejected at save time, not at use time.

## Technical notes

Storing the query string (rather than structured columns) means the preset format follows the URL
format for free — one parser, one source of truth. Validate on write so a stored preset can never
break the list screen.

## Learning goals

Storing user preferences, reusing a parser for validation, per-user data scoping.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

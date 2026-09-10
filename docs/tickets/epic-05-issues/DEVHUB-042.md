# DEVHUB-042 — Status, priority and assignee endpoints

|  |  |
|---|---|
| **Epic** | EPIC 5 — Issues |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-041, DEVHUB-032 |
| **Specs** | [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §4, [`user-flows.md`](../../user-flows.md) Flow 3 |

## Context

These three are the actions users perform dozens of times a day, from the board and from mobile.
Dedicated endpoints keep the payload tiny and the optimistic-update contract obvious.

## Scope

**In:** `PATCH /status`, `PATCH /priority`, `PATCH /assignee`.
**Out:** labels (DEVHUB-049), bulk operations (post-MVP).

## Tasks

- [ ] Three thin endpoints, each dispatching a focused command.
- [ ] `/status`: enforce the domain transition table; invalid → `422` with a message naming both
      states.
- [ ] `/assignee`: `null` unassigns; a non-project-member → `422`.
- [ ] Each returns the full updated `IssueDto` so the client can reconcile its optimistic state.
- [ ] Raise the domain events → activity rows; assignee change also notifies (EPIC 14).
- [ ] Tests: legal and illegal transitions, self-assign, unassign, invalid assignee, no-op change.

## Acceptance criteria

- [ ] An illegal transition returns `422` and changes nothing.
- [ ] Every successful change appears in the activity feed with the correct old/new values.
- [ ] The response is the complete issue, not a partial patch result.

## Technical notes

Returning the full DTO is deliberate: the board reconciles its optimistic card from this response,
and a partial body would force a second fetch.

## Learning goals

Task-based endpoints vs generic PATCH, state machine enforcement at the API edge, designing for
optimistic clients.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

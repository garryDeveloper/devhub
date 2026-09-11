# DEVHUB-112 — Client error handling and reporting

|  |  |
|---|---|
| **Epic** | EPIC 18 — Observability |
| **Phase** | 5 — Async & polish |
| **Priority** | P1 |
| **Size** | S |
| **Depends on** | DEVHUB-109, DEVHUB-021 |
| **Specs** | [`observability-spec.md`](../../tech-specs/observability-spec.md) §9 |

## Context

Server-side traceability is only half the story. When something breaks in the browser or on a
phone, the user needs a recoverable screen and a reference you can search for.

## Scope

**In:** consistent error surfaces on web and mobile, correlation id exposure, retry affordances,
an audit of silent failures.
**Out:** a third-party error tracker (optional follow-up).

## Tasks

- [ ] Web: a top-level error boundary rendering a recoverable screen with a Reload action, plus
      per-route boundaries already added in DEVHUB-008.
- [ ] Show the server `traceId` in error UI as "Reference: 4bf92f…", copyable.
- [ ] Every mutation failure produces a toast with a retry where retrying is safe.
- [ ] Distinguish network failure ("You appear to be offline") from server error ("Something went
      wrong") from validation error (inline on the field).
- [ ] Mobile: the same three-way distinction, plus an offline banner using reachability.
- [ ] Audit the codebase for empty `catch` blocks and unhandled promise rejections; fix each.
- [ ] Optional: wire Sentry with token and PII scrubbing, behind an environment flag.

## Acceptance criteria

- [ ] No user action can fail silently.
- [ ] An error screen shows a reference id that finds the request in CloudWatch.
- [ ] Offline and server errors are visibly different and suggest different actions.
- [ ] No `catch {}` remains that swallows an error without logging or surfacing it.

## Technical notes

The silent-failure audit is the valuable part of this ticket. Every swallowed error is a future
bug report you will not be able to reproduce.

## Learning goals

Error boundaries, user-facing error taxonomy, connecting client errors to server logs, auditing
for silent failures.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3, §4.

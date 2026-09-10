# DEVHUB-040 — Issue detail endpoint

|  |  |
|---|---|
| **Epic** | EPIC 5 — Issues |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | XS |
| **Depends on** | DEVHUB-038 |
| **Specs** | [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §4 |

## Context

One request that gives the detail screen everything it needs above the fold, without pulling in
the comment and activity history (those are paged separately).

## Scope

**In:** `GET /api/issues/{issueId}` returning the full DTO with assignee, reporter, labels and
counts. Also accept an issue **key** so `/p/DEV/issues/DEV-42` resolves in one call.
**Out:** comments and activities (their own endpoints).

## Tasks

- [ ] `GetIssueQuery` projecting the full DTO, including `commentCount` and `attachmentCount`.
- [ ] Accept either a GUID or a key (`DEV-42`) in the route, resolving keys case-insensitively.
- [ ] `404` for a non-member or an unknown id/key.
- [ ] Include `releaseId` and the release version when linked.
- [ ] Tests: by id, by key, wrong-workspace → 404, counts correct.

## Acceptance criteria

- [ ] `GET /api/issues/DEV-42` and `GET /api/issues/{guid}` return the same payload.
- [ ] Counts match the actual number of comments and ready attachments.
- [ ] A non-member gets `404`.

## Technical notes

Accepting the key here saves the web client a lookup round-trip when a user pastes a URL. Detect
the format with a regex and branch — do not add a second endpoint.

## Learning goals

Route-level polymorphic identifiers, projection with aggregate counts, designing for the screen.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

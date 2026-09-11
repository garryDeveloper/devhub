# DEVHUB-049 — Assign and remove labels on issues

|  |  |
|---|---|
| **Epic** | EPIC 6 — Labels, priorities & filtering |
| **Phase** | 1 — Local foundation |
| **Priority** | P1 |
| **Size** | XS |
| **Depends on** | DEVHUB-048, DEVHUB-042 |
| **Specs** | [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §4 |

## Context

A set-replacement endpoint (`PATCH /labels` with the complete desired set) is simpler for the
client than add/remove pairs, and it makes the activity diff easy to compute.

## Scope

**In:** `PATCH /api/issues/{issueId}/labels` with full set semantics and per-label activity.
**Out:** bulk labelling across issues.

## Tasks

- [ ] Command takes `labelIds[]` — the complete desired set.
- [ ] Domain computes the diff and raises `LabelAdded` / `LabelRemoved` per change only.
- [ ] Validate every label belongs to the issue's project → `422` otherwise.
- [ ] Cap the number of labels per issue (e.g. 10) with a clear message.
- [ ] Return the full `IssueDto`.
- [ ] Tests: add, remove, replace, no-op, foreign label rejected, duplicate ids in the payload
      deduplicated.

## Acceptance criteria

- [ ] Sending the same set twice produces no activity entries the second time.
- [ ] Each added and removed label produces exactly one activity row.
- [ ] A label from another project is rejected.

## Technical notes

Set replacement is idempotent, which makes it safe for an optimistic client to retry. Add/remove
endpoints are not — a duplicated retry silently double-toggles.

## Learning goals

Set-based updates, diffing for audit trails, idempotency in API design.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

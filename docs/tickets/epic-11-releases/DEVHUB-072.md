# DEVHUB-072 — Release and ReleaseIssue entities

|  |  |
|---|---|
| **Epic** | EPIC 11 — Releases |
| **Phase** | 2 — Product differentiation |
| **Priority** | P1 |
| **Size** | S |
| **Depends on** | DEVHUB-036 |
| **Specs** | [`domain-model.md`](../../domain-model.md) |

## Context

A release is the unit of delivery that connects issues to deployments — the bridge between "work
done" and "shipped".

## Scope

**In:** `Release` aggregate, `ReleaseIssue` link, publish lifecycle, migration.
**Out:** endpoints (DEVHUB-073/074), auto-generated release notes (post-MVP).

## Tasks

- [ ] `Release` aggregate: version (unique per project), name, notes, status, publishedAt.
- [ ] Lifecycle `Draft → Released → Archived`; `Publish()` sets `PublishedAt` once and is
      irreversible.
- [ ] A `Released` release allows only name/notes edits; anything else throws.
- [ ] `LinkIssue` / `UnlinkIssue`; an issue may belong to at most one **released** release.
- [ ] Raise `ReleasePublished` → activity on every linked issue.
- [ ] Migration with `(project_id, version)` unique and the release-issue constraint.
- [ ] Unit tests: publish twice → throws; edit version after publish → throws; issue linked to a
      second released release → throws.

## Acceptance criteria

- [ ] Publishing is idempotent-safe: a second attempt is rejected, not silently repeated.
- [ ] Version is unique per project.
- [ ] An issue cannot appear in two released releases.

## Technical notes

Version strings are free-form on purpose (`v1.8.0`, `2026.09.1`, `sprint-42`). Enforcing semver
would be a guess about how the user versions their software — validate uniqueness and length,
nothing more.

## Learning goals

Irreversible state transitions, cross-aggregate uniqueness rules, resisting over-validation.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

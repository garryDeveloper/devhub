# DEVHUB-074 — Link issues and deployments to releases

|  |  |
|---|---|
| **Epic** | EPIC 11 — Releases |
| **Phase** | 2 — Product differentiation |
| **Priority** | P1 |
| **Size** | S |
| **Depends on** | DEVHUB-073, DEVHUB-068 |
| **Specs** | [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §10 |

## Context

This is the connective tissue of the product: issue → release → deployment → environment. Without
it, DevHub is two unrelated features in one app.

## Scope

**In:** add/remove issues on a release, automatic deployment↔release linking by version.
**Out:** automatic issue detection from commit messages (post-MVP; noted below).

## Tasks

- [ ] `POST /api/releases/{id}/issues { issueIds[] }` — validates same project, `422` if an issue
      is already in another released release.
- [ ] `DELETE /api/releases/{id}/issues/{issueId}` — blocked once the release is published.
- [ ] When a deployment webhook carries `releaseVersion` (or a `version` matching a release),
      link them automatically.
- [ ] Expose the link on both sides: `IssueDto.releaseId`, `ReleaseDto.deployments[]`.
- [ ] Activity entry `LinkedToRelease` on each issue.
- [ ] Tests: linking, double-linking rejected, cross-project issue rejected, automatic deployment
      link, unlink blocked after publish.

## Acceptance criteria

- [ ] From an issue you can reach its release, and from the release its deployments.
- [ ] Deploying a version automatically associates the matching release, with no manual step.
- [ ] Published releases have an immutable issue set.

## Technical notes

Parsing issue keys out of commit messages (`DEV-42: fix …`) would automate the link entirely —
that is the post-MVP "automatic issue/PR linking" item, and this ticket deliberately builds the
manual path first so the data model is proven before automating it.

## Learning goals

Modelling relationships across aggregates, automatic association by natural key, sequencing
manual before automatic.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

# DEVHUB-080 — Link CI runs to deployments and commits to issues

|  |  |
|---|---|
| **Epic** | EPIC 12 — CI/CD integration |
| **Phase** | 2 — Product differentiation |
| **Priority** | P2 |
| **Size** | S |
| **Depends on** | DEVHUB-078, DEVHUB-068 |
| **Specs** | [`domain-model.md`](../../domain-model.md) |

## Context

The final connection: a commit mentions an issue, a run builds that commit, a deployment ships
it. Once linked, the whole delivery story is visible from any of its parts.

## Scope

**In:** run↔deployment linking by commit sha, best-effort commit-message → issue linking.
**Out:** full GitHub API integration, PR linking (post-MVP).

## Tasks

- [ ] When a deployment webhook arrives, link it to a `CICDRun` with the same `commitSha` in the
      same project (and vice versa when the run arrives second).
- [ ] Parse issue keys matching `[A-Z]+-[0-9]+` (with a non-letter boundary) from the commit message when the webhook payload
      carries one; record a `LinkedToCommit` activity on each matched issue with the commit URL.
- [ ] Ignore keys that do not exist or belong to another project — silently, no error.
- [ ] Surface the link in `DeploymentDto`, `CicdRunDto` and the issue activity feed.
- [ ] Tests: link in both arrival orders, multiple keys in one message, unknown key ignored,
      no commit message present.

## Acceptance criteria

- [ ] A deployment and its CI run reference each other regardless of which webhook arrives first.
- [ ] `fix: correct status transition (DEV-42)` adds an activity entry to `DEV-42`.
- [ ] Unmatched keys never produce an error or a log flood.

## Technical notes

Best-effort means best-effort: this linking must never fail a webhook. Wrap it, log at Debug on
no-match, and move on.

## Learning goals

Correlating events from independent sources, tolerant parsing, keeping optional enrichment out of
the critical path.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

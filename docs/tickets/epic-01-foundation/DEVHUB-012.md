# DEVHUB-012 — Baseline GitHub Actions CI

|  |  |
|---|---|
| **Epic** | EPIC 1 — Foundation & project setup |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | M |
| **Depends on** | DEVHUB-010, DEVHUB-011 |
| **Specs** | [`cicd-architecture.md`](../../tech-specs/cicd-architecture.md) §3 |

## Context

CI from the first week means the repository is never broken for long, and the deployment
workflows in EPIC 17 are an extension of something that already works rather than a new thing.

## Scope

**In:** three PR workflows (backend, web, mobile) with path filters and caching, required status
checks.
**Out:** anything that touches AWS (EPIC 17).

## Tasks

- [ ] `backend-ci.yml`: on PR and push to main, paths `api/**`; restore, build, architecture
      tests, unit tests, integration tests.
- [ ] `web-ci.yml`: paths `web/**`; install, typecheck, lint, test, build.
- [ ] `mobile-ci.yml`: paths `mobile/**`; install, typecheck, lint, test.
- [ ] Add NuGet and npm caching.
- [ ] Add a concurrency group per branch with `cancel-in-progress: true`.
- [ ] Mark all three as required status checks on `main`.
- [ ] Add a CI status badge to the README.

## Acceptance criteria

- [ ] Opening a PR runs only the relevant workflows (a docs-only PR runs none of them).
- [ ] A failing test fails the workflow and blocks the merge.
- [ ] Integration tests run successfully on the runner (Docker is available on `ubuntu-latest`).
- [ ] The slowest workflow finishes in under 8 minutes.

## Technical notes

- Pin action versions (`actions/checkout@v4`), not `@main`.
- Never `continue-on-error` a test step. A green check that hides failures is worse than no check.
- `permissions: contents: read` by default; add scopes only where a job needs them.

## Learning goals

Workflow triggers and path filters, caching strategies, required checks and branch protection,
keeping the feedback loop short.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §6.

# DEVHUB-101 — Backend CI workflow (hardened)

|  |  |
|---|---|
| **Epic** | EPIC 17 — CI/CD for DevHub |
| **Phase** | 4 — CI/CD |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-012 |
| **Specs** | [`cicd-architecture.md`](../../tech-specs/cicd-architecture.md) §3 |

## Context

DEVHUB-012 created a working backend workflow. This ticket makes it production-grade: caching,
artifacts, coverage visibility and a build output the deploy jobs can consume.

## Scope

**In:** improved `backend-ci.yml`, publish artifact, test reporting.
**Out:** the deploy jobs (DEVHUB-105/106).

## Tasks

- [ ] Split into jobs: `build` → `test` (unit + architecture) → `integration-test`.
- [ ] Cache NuGet by `packages.lock.json` or the csproj hash.
- [ ] Publish the API build output as a workflow artifact for reuse by deploy jobs.
- [ ] Collect test results and publish them as a check summary.
- [ ] Collect coverage and print a summary (report only — no gate; see the testing strategy).
- [ ] Fail fast on the build; still run all tests when only tests fail so you see every failure.
- [ ] Verify the workflow completes in under 8 minutes.

## Acceptance criteria

- [ ] The workflow produces a downloadable build artifact.
- [ ] Test failures are visible in the PR checks without opening the raw logs.
- [ ] A warm cache noticeably shortens the run.

## Technical notes

Building once and publishing an artifact is what makes "promote the tested artifact" possible
later — rebuilding at deploy time means production runs bytes CI never tested.

## Learning goals

Job dependencies, artifacts, caching, actionable CI output.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §6.

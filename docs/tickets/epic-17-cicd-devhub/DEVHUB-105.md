# DEVHUB-105 — Staging deployment workflow

|  |  |
|---|---|
| **Epic** | EPIC 17 — CI/CD for DevHub |
| **Phase** | 4 — CI/CD |
| **Priority** | P0 |
| **Size** | L |
| **Depends on** | DEVHUB-104, DEVHUB-096, DEVHUB-098 |
| **Specs** | [`cicd-architecture.md`](../../tech-specs/cicd-architecture.md) §4 |

## Context

Push to `main`, and staging updates itself. This is the workflow the whole pipeline story is
built on.

## Scope

**In:** `deploy-staging.yml` — version, build, deploy API and SPA, migrate, health check.
**Out:** the DevHub callback (DEVHUB-107) and production (DEVHUB-106).

## Tasks

- [ ] Trigger on push to `main` after the CI workflows succeed.
- [ ] Compute `version = 1.<run_number>.0`; tag the Docker image with the version and the sha.
- [ ] Build and push the API image; create a Beanstalk application version and deploy it.
- [ ] Run EF migrations as a separate, explicit step before the new version takes traffic.
- [ ] Build the SPA with the staging API URL, sync to `devhub-web-staging`, invalidate CloudFront.
- [ ] Health gate: poll `/health/ready` until `200`, up to 5 minutes, then fail.
- [ ] `permissions: { id-token: write, contents: read }`; assume the role via OIDC.
- [ ] Add the Playwright smoke suite against staging after the health check.
- [ ] Document the manual rollback command.

## Acceptance criteria

- [ ] A merge to `main` results in staging running the new version with no manual step.
- [ ] A failing health check fails the workflow.
- [ ] Migrations run exactly once per deploy, before traffic shifts.
- [ ] The whole deploy takes under 10 minutes.

## Technical notes

Migrations in a pipeline need care: run them from a step that can reach RDS, keep them additive
(never destructive in the same release as the code change), and make them idempotent so a retried
deploy is safe.

## Learning goals

Deployment pipelines, database migrations in CI/CD, health gating, artifact promotion, OIDC in
practice.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §5, §6.

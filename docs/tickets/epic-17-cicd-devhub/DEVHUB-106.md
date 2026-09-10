# DEVHUB-106 — Production deployment with manual approval

|  |  |
|---|---|
| **Epic** | EPIC 17 — CI/CD for DevHub |
| **Phase** | 4 — CI/CD |
| **Priority** | P1 |
| **Size** | M |
| **Depends on** | DEVHUB-105 |
| **Specs** | [`cicd-architecture.md`](../../tech-specs/cicd-architecture.md) §5 |

## Context

Production is promoted, not rebuilt, and only after a human says so. Manual approval in the MVP
is a deliberate choice, not a missing feature.

## Scope

**In:** production Beanstalk environment, `deploy-production.yml` with a GitHub environment gate,
promotion of the staging-tested artifact, rollback procedure.
**Out:** blue/green, automatic rollback on alarms (post-MVP).

## Tasks

- [ ] Create `devhub-api-production` and `devhub-web-prod` mirroring staging, with production SSM
      parameters and Swagger disabled.
- [ ] Configure the GitHub `production` environment with a required reviewer.
- [ ] `deploy-production.yml` on `workflow_dispatch` with a `version` input.
- [ ] Verify the version exists as a staging-deployed application version; **do not rebuild**.
- [ ] Run migrations, deploy, health check, and fail loudly on any step.
- [ ] Document and rehearse a rollback to the previous application version.
- [ ] Restrict the OIDC role's production permissions to the `production` environment condition.

## Acceptance criteria

- [ ] Production deploys only after an explicit approval.
- [ ] The deployed artifact is byte-identical to the one staging tested.
- [ ] A rollback has been performed at least once and is written down.
- [ ] Production has its own configuration and its own database credentials.

## Technical notes

If the SPA bakes its API URL at build time (see DEVHUB-102), byte-identical promotion is
impossible for the frontend. Either accept a rebuild for the SPA and document it, or switch to a
runtime `config.json` — this is the ticket where that decision becomes real.

## Learning goals

Environment promotion, approval gates, immutable artifacts, rehearsing rollback before needing it.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §5, §6.

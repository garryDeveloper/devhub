# DEVHUB-107 — Publish deployment status back to DevHub

|  |  |
|---|---|
| **Epic** | EPIC 17 — CI/CD for DevHub |
| **Phase** | 4 — CI/CD |
| **Priority** | P0 |
| **Size** | M |
| **Depends on** | DEVHUB-105, DEVHUB-068 |
| **Specs** | [`webhooks-spec.md`](../../tech-specs/webhooks-spec.md) §8, [`cicd-architecture.md`](../../tech-specs/cicd-architecture.md) §8 |

## Context

The ticket that closes the loop and makes the product's central claim true: DevHub shows DevHub's
own deployments.

## Scope

**In:** the signing script, `Running`/terminal callbacks in both deploy workflows, timeline
events, failure tolerance.
**Out:** SQS-based async processing of the callback (post-MVP).

## Tasks

- [ ] `.github/scripts/notify-devhub.sh`: build the JSON body, compute the HMAC over
      `{timestamp}.{body}`, POST with `--data-raw` so the signed bytes are the sent bytes.
- [ ] Call it before the deploy (`status: Running`) and after (`Succeeded`/`Failed`) with
      `if: always()`.
- [ ] Send `event` entries at key steps (build, tests, image pushed, deployed, health check) so
      the timeline is real rather than two rows.
- [ ] Store `DEVHUB_WEBHOOK_SECRET` as a repository secret; `DEVHUB_API_URL` and
      `DEVHUB_PROJECT_ID` as environment variables.
- [ ] Never fail the deployment because the callback failed — log a warning and continue.
- [ ] End-to-end verification: push to `main`, watch the deployment appear, progress and complete
      in the DevHub UI.
- [ ] Force a failure (break a test after the build gate) and verify the failed deployment and
      its notification.

## Acceptance criteria

- [ ] A real merge to `main` produces a visible deployment in DevHub, with a timeline.
- [ ] A failed deploy shows as `Failed`, sets the environment `Unhealthy` and notifies.
- [ ] A callback outage does not fail the deployment.
- [ ] Screenshots of the end-to-end flow are attached to the PR — this is the portfolio artifact.

## Technical notes

Shell quoting is the enemy here: build the JSON with `jq -nc` rather than string interpolation,
and always send with `--data-raw`. A signature mismatch caused by a stray newline is a very
frustrating hour.

## Learning goals

Signing requests from CI, `if: always()` semantics, treating observability integrations as
non-critical, closing a product loop end to end.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §6.

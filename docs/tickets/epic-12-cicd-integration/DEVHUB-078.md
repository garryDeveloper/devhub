# DEVHUB-078 — GitHub Actions webhook endpoint

|  |  |
|---|---|
| **Epic** | EPIC 12 — CI/CD integration |
| **Phase** | 2 — Product differentiation |
| **Priority** | P1 |
| **Size** | M |
| **Depends on** | DEVHUB-077, DEVHUB-068 |
| **Specs** | [`webhooks-spec.md`](../../tech-specs/webhooks-spec.md) §5 |

## Context

Consuming GitHub's `workflow_run` events so the CI/CD screen fills itself for every workflow,
not only deployments.

## Scope

**In:** `POST /api/webhooks/github/actions`, GitHub HMAC validation, payload mapping, idempotent
upsert.
**Out:** `push` / `pull_request` events, GitHub App installation flow.

## Tasks

- [ ] Validate `X-Hub-Signature-256` over the raw body with the configured GitHub secret.
- [ ] Read `X-GitHub-Event` (handle `workflow_run` only, ignore others with `200`) and
      `X-GitHub-Delivery` for audit.
- [ ] Resolve the project from `repository.full_name`; unknown repo → `404`, logged once.
- [ ] Map `status` + `conclusion` to `CICDRun.Status` per the spec table.
- [ ] Upsert by `(projectId, provider, externalId)`; ignore backwards transitions.
- [ ] Persist the delivery to `webhook_deliveries`.
- [ ] Tests: valid signature, tampered payload, unknown repo, duplicate delivery, each
      status/conclusion pair, an unhandled event type returns `200`.

## Acceptance criteria

- [ ] A real GitHub delivery (via ngrok) creates and then completes a run.
- [ ] Replaying the same delivery changes nothing.
- [ ] An unhandled event type is ignored without an error.
- [ ] An invalid signature returns `401` and stores no run.

## Technical notes

GitHub retries on `5xx`. Returning `200` for "I understood but there is nothing to do" is
correct and prevents a retry storm; reserve `4xx` for genuinely malformed or unauthorized
deliveries.

## Learning goals

Consuming a third-party webhook contract, mapping an external state model to your own, being a
good webhook citizen.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

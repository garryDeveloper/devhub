# DEVHUB-068 — Deployment webhook with signature validation

|  |  |
|---|---|
| **Epic** | EPIC 10 — Deployments |
| **Phase** | 2 — Product differentiation |
| **Priority** | P0 |
| **Size** | L |
| **Depends on** | DEVHUB-067 |
| **Specs** | [`webhooks-spec.md`](../../tech-specs/webhooks-spec.md) |

## Context

The integration point the whole product is built around, and the most security-sensitive endpoint
in the API: unauthenticated by user token, authenticated by HMAC.

## Scope

**In:** `POST /api/webhooks/deployments`, signature and timestamp validation, idempotent upsert,
delivery audit, rate limiting.
**Out:** the GitHub Actions side (DEVHUB-107), async processing via SQS (post-MVP).

## Tasks

- [ ] Endpoint reading the **raw body** (buffer before model binding) for signature computation.
- [ ] Validate `X-DevHub-Signature`, `X-DevHub-Timestamp` (±300 s) and `X-DevHub-Project`.
- [ ] Fixed-time comparison; per-project secret with support for two secrets during rotation.
- [ ] Resolve the environment by name within the project; `404` if unknown.
- [ ] Upsert by `(environmentId, externalId)`; handle the unique-violation race by falling
      through to update.
- [ ] Apply the status transition, append the optional event, and on terminal status update the
      environment through `RecordDeploymentResult`.
- [ ] Link the release by version when one matches.
- [ ] Persist every delivery to `webhook_deliveries` (payload, validity, response code).
- [ ] Rate limit 60/min per project; return `429` with `Retry-After`.
- [ ] Tests: valid payload, tampered body, wrong secret, stale timestamp, duplicate delivery,
      out-of-order delivery, unknown environment, concurrent identical deliveries.

## Acceptance criteria

- [ ] An invalid signature returns `401` and writes nothing except the audit row.
- [ ] The same payload delivered five times produces exactly one deployment.
- [ ] A terminal deployment ignores later non-terminal callbacks and still returns `200`.
- [ ] A successful production deployment updates environment health, version and timestamp.
- [ ] No business condition ever returns `5xx`.

## Technical notes

Signature over the raw bytes is non-negotiable: re-serializing the deserialized model changes
whitespace and property order, and the HMAC will never match. In ASP.NET Core, call
`Request.EnableBuffering()` and read the stream before binding.

## Learning goals

HMAC signing and verification, replay protection, idempotent upserts under concurrency, why
webhooks must never return `5xx` for business conditions.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

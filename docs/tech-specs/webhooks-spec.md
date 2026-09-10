# Webhooks specification

Inbound callbacks that let CI/CD report into DevHub. This is the mechanism behind the flagship
flow in [`../user-flows.md`](../user-flows.md#flow-4--successful-deployment-the-flagship-flow).

---

## 1. Principles

1. **DevHub never runs pipelines.** GitHub Actions does the work and reports the result.
2. **Every webhook is signed.** No signature, no processing.
3. **Every webhook is idempotent.** The same event delivered five times produces one row.
4. **Webhooks are fast.** Validate, persist, return `202`/`200`. Heavy work goes to a queue
   (post-MVP: SQS → Lambda).
5. **Webhooks are auditable.** Every delivery is recorded in `webhook_deliveries`.

---

## 2. Endpoints

| Endpoint | Source | Ticket |
|---|---|---|
| `POST /api/webhooks/deployments` | DevHub's own GitHub Actions deploy job | DEVHUB-068 |
| `POST /api/webhooks/github/actions` | GitHub `workflow_run` event | DEVHUB-078 |

Both are `[AllowAnonymous]` to ASP.NET and authenticated by HMAC.

---

## 3. Signature scheme

### DevHub deployment webhook

```text
X-DevHub-Signature: sha256=<hex>
X-DevHub-Timestamp: 1757160000        (unix seconds)
X-DevHub-Project:   <projectId>

signature = HMAC_SHA256(secret, "{timestamp}.{rawBody}")
```

Validation, in order:

1. Headers present → else `400`.
2. `|now - timestamp| <= 300s` → else `401` (replay protection).
3. Look up the project's webhook secret → unknown project → `401`.
4. Recompute the HMAC over the **raw body bytes** (buffer the body before model binding; do not
   re-serialize the deserialized object — property order and formatting will differ).
5. Compare with `CryptographicOperations.FixedTimeEquals` → mismatch → `401`.

### GitHub webhook

GitHub signs with `X-Hub-Signature-256: sha256=<hex>` = `HMAC_SHA256(secret, rawBody)` (no
timestamp). Also read `X-GitHub-Event` and `X-GitHub-Delivery`; use the delivery id for
idempotency.

On any failure: log source IP, delivery id and the failure reason — **never the body** — and
return `401` with an empty `ProblemDetails`. Do not explain which check failed.

---

## 4. Deployment webhook contract

```jsonc
POST /api/webhooks/deployments
{
  "externalId": "gha-812-deploy-production",   // stable per deployment attempt
  "environment": "Production",                  // matched by name within the project
  "version": "1.8.2",
  "status": "Running",                          // Queued|Running|Succeeded|Failed|Canceled
  "commitSha": "8a2d91f4c…",
  "commitUrl": "https://github.com/u/devhub/commit/8a2d91f",
  "branch": "main",
  "triggeredBy": "dario",
  "runUrl": "https://github.com/u/devhub/actions/runs/812",
  "releaseVersion": "v1.8.2",                   // optional, links the release
  "occurredAt": "2026-09-06T14:32:11Z",
  "event": {                                    // optional timeline entry
    "name": "Health check passed",
    "status": "Succeeded",
    "message": null
  }
}
```

Processing:

```text
validate signature
   ↓
resolve project (header) + environment (by name)  → 404 if unknown
   ↓
upsert deployment by (environmentId, externalId)
   ├─ new      → assign per-project number, status, startedAt
   └─ existing → apply the status transition if legal, ignore if already terminal
   ↓
append DeploymentEvent when "event" is present
   ↓
on terminal status:
   ├─ completedAt + durationMs
   ├─ update environment: currentVersion, lastDeployedAt, healthStatus
   │     Succeeded → Healthy · Failed → Unhealthy · Canceled → unchanged
   ├─ link release by version if it exists
   └─ raise DeploymentStatusChanged  → notifications
   ↓
200 { "deploymentId": "…", "number": 182, "status": "Succeeded" }
```

Responses: `200` processed (including "already terminal, ignored"), `400` malformed body,
`401` bad signature/timestamp, `404` unknown project or environment, `429` rate limited.

**Never `500` for a business condition.** A `5xx` makes GitHub Actions retry and floods the API.

---

## 5. GitHub Actions run webhook

```jsonc
POST /api/webhooks/github/actions       // GitHub workflow_run payload (subset used)
{
  "action": "completed",                 // requested | in_progress | completed
  "workflow_run": {
    "id": 12345678,
    "name": "backend-ci",
    "run_number": 812,
    "head_branch": "main",
    "head_sha": "8a2d91f4c…",
    "status": "completed",               // queued | in_progress | completed
    "conclusion": "success",             // success|failure|cancelled|skipped|timed_out
    "html_url": "https://github.com/…/runs/12345678",
    "run_started_at": "2026-09-06T14:28:00Z",
    "updated_at": "2026-09-06T14:32:11Z"
  },
  "repository": { "full_name": "dario/devhub" }
}
```

Mapping:

| GitHub | DevHub `CICDRun.Status` |
|---|---|
| `status=queued` | `Queued` |
| `status=in_progress` | `Running` |
| `conclusion=success` | `Succeeded` |
| `conclusion=failure` / `timed_out` | `Failed` |
| `conclusion=cancelled` / `skipped` | `Canceled` |

The project is resolved from `repository.full_name` via a configured mapping
(`project.githubRepository`). Unknown repository → `404`, logged once, not retried.

Upsert by `(projectId, provider, externalId = workflow_run.id)`. On completion, link the run to a
deployment with the same `commitSha` and, on failure, raise `CicdRunCompleted` → notification.

---

## 6. Idempotency & ordering

- Uniqueness is enforced by the database, not by an "exists?" check — two concurrent deliveries
  must not both insert. Catch the unique-violation and fall through to the update path.
- Events can arrive **out of order**. Never move a deployment backwards: a `Running` callback
  arriving after `Succeeded` is ignored (logged at Debug).
- `occurredAt` from the payload orders timeline events; server time is only a fallback.

---

## 7. Secrets

- One secret per project, generated on demand, shown once, stored hashed-at-rest is not possible
  (it must be recomputable) → store encrypted via SSM/KMS, never plaintext in the database in
  production. Local development may use a single configured shared secret.
- Rotation: two active secrets accepted during a rotation window; validation tries both.
- The secret lives in GitHub as `secrets.DEVHUB_WEBHOOK_SECRET` and is passed to the deploy job.

---

## 8. Calling DevHub from GitHub Actions

```yaml
- name: Notify DevHub
  if: always()
  env:
    SECRET:  ${{ secrets.DEVHUB_WEBHOOK_SECRET }}
    PROJECT: ${{ vars.DEVHUB_PROJECT_ID }}
  run: |
    BODY=$(jq -nc \
      --arg id  "gha-${{ github.run_id }}-deploy-production" \
      --arg st  "${{ job.status == 'success' && 'Succeeded' || 'Failed' }}" \
      --arg sha "${{ github.sha }}" \
      '{externalId:$id, environment:"Production", version:"${{ needs.build.outputs.version }}",
        status:$st, commitSha:$sha, branch:"${{ github.ref_name }}",
        triggeredBy:"${{ github.actor }}",
        runUrl:"${{ github.server_url }}/${{ github.repository }}/actions/runs/${{ github.run_id }}",
        occurredAt:(now|todate)}')
    TS=$(date +%s)
    SIG=$(printf '%s.%s' "$TS" "$BODY" | openssl dgst -sha256 -hmac "$SECRET" -hex | awk '{print $2}')
    curl -sS -X POST "$DEVHUB_API/api/webhooks/deployments" \
      -H "Content-Type: application/json" \
      -H "X-DevHub-Project: $PROJECT" \
      -H "X-DevHub-Timestamp: $TS" \
      -H "X-DevHub-Signature: sha256=$SIG" \
      --data-raw "$BODY" --fail-with-body
```

`--data-raw` matters: the bytes signed must be the bytes sent.

---

## 9. Testing

- Unit: signature validation (valid, tampered body, wrong secret, stale timestamp).
- Unit: status transition rules, including out-of-order and duplicate terminal callbacks.
- Integration: full deployment lifecycle `Queued → Running → Succeeded` updates environment
  health and creates exactly one deployment row.
- Integration: the same payload posted twice creates one row and returns `200` both times.
- Local: `ngrok http 5080` (or a saved `curl` script in `infrastructure/webhooks/`) to exercise
  the endpoint before AWS exists.

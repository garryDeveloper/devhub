# Main user flows

End-to-end flows the product must support. Each flow lists the screens, the API calls and the
failure modes that must be handled.

---

## Flow 1 — New user onboarding

```text
Landing → Register → Create workspace → Create project → Project dashboard → First issue
```

| Step | Call | Notes |
|---|---|---|
| Register | `POST /api/auth/register` | returns access + refresh token |
| Bootstrap | `GET /api/me` | |
| Create workspace | `POST /api/workspaces` | creator becomes `Owner` |
| Create project | `POST /api/workspaces/{id}/projects` | key defaults from name, editable |
| Seed environments | server-side | `Development`, `Staging`, `Production` created automatically |
| Create issue | `POST /api/projects/{id}/issues` | |

Failure modes: email already registered (`409`), weak password (`400` with field errors),
duplicate project key in workspace (`409`).

**Design rule:** a brand-new account must reach a usable project dashboard in under 60 seconds.
Workspace creation is offered inline, not as a separate onboarding wizard.

---

## Flow 2 — Create an issue

```text
Project → Issues → New issue → title / description / priority / assignee / labels → Create → Issue detail
```

- `POST /api/projects/{projectId}/issues` returns `201` with `Location` and the full issue DTO.
- The issue key (`DEV-42`) is assigned server-side inside the same transaction as the insert.
- Web: optimistic insert at the top of the list, reconciled with the server response.
- On failure the optimistic row is rolled back and the form re-opens with the entered values.

---

## Flow 3 — Work on an issue

```text
Issue → assign to self → status In Progress → comment → status In Review → link to release → Done
```

Each mutation:

```text
PATCH /api/issues/{id}/assignee     → activity: AssigneeChanged  + notification to assignee
PATCH /api/issues/{id}/status       → activity: StatusChanged
POST  /api/issues/{id}/comments     → activity: Commented + mention notifications
POST  /api/releases/{id}/issues     → activity: LinkedToRelease
```

Board drag & drop uses an optimistic status update; a rejected transition (`422`) snaps the card
back and shows a toast explaining why.

---

## Flow 4 — Successful deployment (the flagship flow)

```text
git push
  ↓
GitHub Actions: build → test → docker build → deploy
  ↓
POST /api/webhooks/deployments      (HMAC signed)
  ↓
DevHub validates signature + payload
  ↓
Deployment created (Queued → Running)
  ↓
DeploymentEvents appended as the workflow reports progress
  ↓
Final callback: status = Succeeded
  ↓
Environment.CurrentVersion + LastDeployedAt + HealthStatus updated
  ↓
Activity recorded, notification for production deploys
  ↓
Dashboard and Environments screen reflect the new state
```

Rules:

- The webhook is **idempotent**: the same `(provider, externalId)` updates the existing row.
- A callback for an already-terminal deployment is accepted and ignored (`200`), never duplicated.
- Signature validation failure returns `401` and logs the attempt without the payload body.
- Environment health is derived: last deployment `Succeeded` → `Healthy`, `Failed` → `Unhealthy`.

---

## Flow 5 — Failed deployment

```text
GitHub Actions deploy fails
  ↓
POST /api/webhooks/deployments (status = Failed, message)
  ↓
Deployment = Failed, DeploymentEvent appended with the failing step
  ↓
Environment = Unhealthy
  ↓
Notification "Deployment failed — Production v1.8.0" for project members
  ↓
User opens notification → Deployment detail → timeline shows failing step → "Open GitHub run"
```

The deployment detail must always expose `RunUrl` so the user can reach the real logs in one
click. DevHub does not try to store or render CI logs.

---

## Flow 6 — Mobile issue update

```text
Mobile → Issues → select issue → change status (bottom sheet)
  ↓
PATCH /api/issues/{id}/status
  ↓
API authorizes (project membership) → validates transition → updates → creates activity
  ↓
200 + updated issue
  ↓
Query cache updated; list and detail re-render
```

- The UI updates optimistically and reverts on error.
- Expired access token → silent refresh → retry once → only then show the error.

---

## Flow 7 — Attachment upload

```text
Compose comment / edit issue → attach file
  ↓
POST /api/attachments/upload-url   { fileName, contentType, sizeBytes, issueId|commentId }
  ↓
API validates size + MIME, creates Attachment(Pending), returns presigned PUT URL (5 min TTL)
  ↓
Client PUTs the bytes directly to S3 (progress bar)
  ↓
POST /api/attachments/{id}/complete
  ↓
API verifies the object exists (HeadObject), flips status to Ready, records metadata
  ↓
Attachment appears in the issue/comment
```

Bytes never pass through the API. Downloads use a short-lived presigned GET URL; the bucket
stays private with public access blocked.

Failure modes: client abandons after step 2 → `Pending` row is garbage-collected after 24 h;
`HeadObject` misses → `409` and the client may retry the upload.

---

## Flow 8 — Global search

```text
⌘K → type "deployment" → debounced 250 ms → GET /api/search?q=deployment
  ↓
Grouped results: Issues / Projects / Releases
  ↓
↑ ↓ to move, ↵ to open, Esc to close
```

Scoped to the workspaces the user belongs to. Empty query shows recent searches instead of
hitting the API.

---

## Flow 9 — Release a version

```text
Releases → New release (Draft) → link done issues → write notes → Publish
  ↓
POST /api/releases/{id}/publish → Status = Released, PublishedAt set
  ↓
Deployment carrying that version links itself to the release
  ↓
Release detail shows the delivered issues and where it is deployed
```

Publishing is irreversible; only the notes stay editable afterwards.

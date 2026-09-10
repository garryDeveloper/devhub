# API endpoints

Complete endpoint reference. Conventions (status codes, errors, pagination, filtering) are in
[`api-conventions.md`](api-conventions.md) and are not repeated per endpoint.

Legend: 🔓 public · 🔒 authenticated · 👑 workspace `Owner` · 🪝 webhook (signed, no user token)

---

## 1. Auth — `/api/auth`

| Method | Path | Auth | Ticket |
|---|---|---|---|
| POST | `/api/auth/register` | 🔓 | DEVHUB-014 |
| POST | `/api/auth/login` | 🔓 | DEVHUB-015 |
| POST | `/api/auth/refresh` | 🔓 | DEVHUB-016 |
| POST | `/api/auth/logout` | 🔒 | DEVHUB-017 |
| GET | `/api/me` | 🔒 | DEVHUB-019 |
| PATCH | `/api/me` | 🔒 | DEVHUB-019 |

```jsonc
// POST /api/auth/register
{ "email": "dario@example.com", "password": "…", "displayName": "Dario" }
// 201
{ "accessToken": "eyJ…", "expiresIn": 900, "refreshToken": "opaque…",
  "user": { "id": "…", "email": "…", "displayName": "Dario", "avatarUrl": null } }
// 409 email already registered · 400 weak password / invalid email
```

```jsonc
// POST /api/auth/login   { "email": "...", "password": "..." }
// 200 same shape as register · 401 invalid credentials (never say which field)

// POST /api/auth/refresh { "refreshToken": "opaque..." }
// 200 { accessToken, expiresIn, refreshToken }   ← rotated, old one revoked
// 401 expired/revoked/unknown  (reuse revokes the whole chain)

// POST /api/auth/logout  { "refreshToken": "opaque..." }  → 204

// GET /api/me → 200 UserDto
// PATCH /api/me { "displayName"?, "avatarAttachmentId"? } → 200 UserDto
```

---

## 2. Workspaces — `/api/workspaces`

| Method | Path | Auth | Ticket |
|---|---|---|---|
| POST | `/api/workspaces` | 🔒 | DEVHUB-024 |
| GET | `/api/workspaces` | 🔒 | DEVHUB-024 |
| GET | `/api/workspaces/{workspaceId}` | 🔒 | DEVHUB-024 |
| PATCH | `/api/workspaces/{workspaceId}` | 👑 | DEVHUB-024 |
| GET | `/api/workspaces/{workspaceId}/members` | 🔒 | DEVHUB-025 |
| POST | `/api/workspaces/{workspaceId}/members` | 👑 | DEVHUB-025 |
| PATCH | `/api/workspaces/{workspaceId}/members/{memberId}` | 👑 | DEVHUB-025 |
| DELETE | `/api/workspaces/{workspaceId}/members/{memberId}` | 👑 | DEVHUB-025 |

```jsonc
// POST /api/workspaces { "name": "Acme", "slug": "acme" }   slug optional → derived
// 201 { "id","name","slug","role":"Owner","memberCount":1,"createdAt" }
// 409 slug taken

// GET /api/workspaces → 200 WorkspaceSummaryDto[]  (not paged; a user has few)

// POST .../members { "email": "ana@example.com", "role": "Member" }
// 201 MemberDto · 404 user not found · 409 already a member
// PATCH .../members/{id} { "role": "Owner" } · 422 cannot demote the last owner
// DELETE .../members/{id} → 204 · 422 cannot remove the last owner
```

---

## 3. Projects

| Method | Path | Auth | Ticket |
|---|---|---|---|
| POST | `/api/workspaces/{workspaceId}/projects` | 🔒 | DEVHUB-031 |
| GET | `/api/workspaces/{workspaceId}/projects` | 🔒 | DEVHUB-031 |
| GET | `/api/projects/{projectId}` | 🔒 | DEVHUB-031 |
| PATCH | `/api/projects/{projectId}` | 👑 | DEVHUB-031 |
| DELETE | `/api/projects/{projectId}` | 👑 | DEVHUB-031 |
| GET | `/api/projects/{projectId}/members` | 🔒 | DEVHUB-032 |
| POST | `/api/projects/{projectId}/members` | 👑 | DEVHUB-032 |
| DELETE | `/api/projects/{projectId}/members/{memberId}` | 👑 | DEVHUB-032 |

```jsonc
// POST { "name":"DevHub API", "key":"DEV", "description":"…", "color":"#4F46E5", "icon":"rocket" }
// 201 ProjectDto  → also seeds Development/Staging/Production environments
// 409 key already used in this workspace · 400 key not ^[A-Z][A-Z0-9]{1,9}$
// PATCH cannot change "key" → 422
// DELETE = archive (sets archivedAt) → 204;  ?permanent=true is not supported in the MVP

// GET /api/workspaces/{id}/projects?includeArchived=false → 200 ProjectSummaryDto[]
```

---

## 4. Issues

| Method | Path | Auth | Ticket |
|---|---|---|---|
| POST | `/api/projects/{projectId}/issues` | 🔒 | DEVHUB-038 |
| GET | `/api/projects/{projectId}/issues` | 🔒 | DEVHUB-039 |
| GET | `/api/issues/{issueId}` | 🔒 | DEVHUB-040 |
| PATCH | `/api/issues/{issueId}` | 🔒 | DEVHUB-041 |
| DELETE | `/api/issues/{issueId}` | 🔒 | DEVHUB-043 |
| PATCH | `/api/issues/{issueId}/status` | 🔒 | DEVHUB-042 |
| PATCH | `/api/issues/{issueId}/priority` | 🔒 | DEVHUB-042 |
| PATCH | `/api/issues/{issueId}/assignee` | 🔒 | DEVHUB-042 |
| PATCH | `/api/issues/{issueId}/labels` | 🔒 | DEVHUB-049 |

```jsonc
// POST { "title":"Implement deployment history", "description":"…",
//        "priority":"High", "assigneeId":"…", "labelIds":["…"], "dueDate":"2026-09-20" }
// 201 IssueDto with server-assigned "key": "DEV-42"

// IssueDto
{
  "id":"…", "key":"DEV-42", "projectId":"…", "projectKey":"DEV",
  "title":"…", "description":"…",
  "status":"InProgress", "priority":"High",
  "assignee": { "id":"…","displayName":"Dario","avatarUrl":null },
  "reporter": { "id":"…","displayName":"Dario","avatarUrl":null },
  "labels": [ { "id":"…","name":"backend","color":"#22C55E" } ],
  "dueDate":"2026-09-20", "commentCount":3, "attachmentCount":1,
  "releaseId":null, "createdAt":"…", "updatedAt":"…"
}

// GET list filters: status (repeatable), priority (repeatable), assigneeId ("me" allowed),
//   labelId (repeatable), search, createdAfter, createdBefore, dueBefore, includeArchived,
//   sort ∈ { createdAt, -createdAt, updatedAt, -updatedAt, priority, -priority, status, key }
// → PagedResult<IssueListItemDto>  (list item omits description)

// PATCH /status { "status":"InReview" }  → 200 IssueDto
//   422 invalid-status-transition (see domain-model.md)
// PATCH /assignee { "assigneeId": null }        → unassign
//   422 assignee is not a member of the project
// PATCH /labels { "labelIds": ["…","…"] }       → full replacement of the label set
// DELETE → 204 (archive); ?permanent=true → 204 hard delete, owner only
```

---

## 5. Labels & saved filters

| Method | Path | Auth | Ticket |
|---|---|---|---|
| GET | `/api/projects/{projectId}/labels` | 🔒 | DEVHUB-048 |
| POST | `/api/projects/{projectId}/labels` | 🔒 | DEVHUB-048 |
| PATCH | `/api/labels/{labelId}` | 🔒 | DEVHUB-048 |
| DELETE | `/api/labels/{labelId}` | 🔒 | DEVHUB-048 |
| GET | `/api/projects/{projectId}/issue-filters` | 🔒 | DEVHUB-050 |
| POST | `/api/projects/{projectId}/issue-filters` | 🔒 | DEVHUB-050 |
| DELETE | `/api/issue-filters/{filterId}` | 🔒 | DEVHUB-050 |

```jsonc
// POST label { "name":"backend", "color":"#22C55E" }  → 201 · 409 duplicate name in project
// DELETE label → 204, removes it from every issue (and records activity)

// POST filter { "name":"My open work", "query":"status=Todo&status=InProgress&assigneeId=me" }
// 201 · a filter is private to its creator
```

---

## 6. Comments & activity

| Method | Path | Auth | Ticket |
|---|---|---|---|
| GET | `/api/issues/{issueId}/comments` | 🔒 | DEVHUB-053 |
| POST | `/api/issues/{issueId}/comments` | 🔒 | DEVHUB-053 |
| PATCH | `/api/comments/{commentId}` | 🔒 | DEVHUB-053 |
| DELETE | `/api/comments/{commentId}` | 🔒 | DEVHUB-053 |
| GET | `/api/issues/{issueId}/activities` | 🔒 | DEVHUB-055 |

```jsonc
// POST { "body":"Deployed to staging, @ana please verify" }
// 201 CommentDto { id, issueId, author{…}, body, createdAt, updatedAt, editedAt }
//   @mentions are parsed server-side → notifications for project members only
// PATCH allowed to the author only → 403 otherwise
// DELETE allowed to author or workspace owner

// GET /activities?page=1&pageSize=50 → PagedResult<ActivityDto>
{ "id":"…","type":"StatusChanged","actor":{…},
  "oldValue":"Todo","newValue":"InProgress","createdAt":"…" }
```

---

## 7. Search

| Method | Path | Auth | Ticket |
|---|---|---|---|
| GET | `/api/search?q=` | 🔒 | DEVHUB-059 |
| GET | `/api/search/issues?q=` | 🔒 | DEVHUB-059 |
| GET | `/api/search/projects?q=` | 🔒 | DEVHUB-059 |

```jsonc
// GET /api/search?q=deployment&limit=5   ← scoped to the caller's workspaces
{
  "issues":   [ { "id","key","title","projectKey","status" } ],
  "projects": [ { "id","key","name","workspaceId" } ],
  "releases": [ { "id","version","projectKey","status" } ]
}
// q shorter than 2 characters → 200 with empty groups (not an error)
```

---

## 8. Environments

| Method | Path | Auth | Ticket |
|---|---|---|---|
| GET | `/api/projects/{projectId}/environments` | 🔒 | DEVHUB-063 |
| POST | `/api/projects/{projectId}/environments` | 👑 | DEVHUB-063 |
| GET | `/api/environments/{environmentId}` | 🔒 | DEVHUB-063 |
| PATCH | `/api/environments/{environmentId}` | 👑 | DEVHUB-063 |
| DELETE | `/api/environments/{environmentId}` | 👑 | DEVHUB-063 |

```jsonc
// EnvironmentDto
{ "id":"…","projectId":"…","name":"Production","type":"Production",
  "url":"https://app.example.com","currentVersion":"1.8.2",
  "healthStatus":"Healthy","lastDeployedAt":"…","lastDeployment":{ "id","number","status","version" } }

// PATCH accepts name, url, type, sortOrder ONLY.
// currentVersion / healthStatus / lastDeployedAt are derived → 422 if a client sends them.
// DELETE → 409 if the environment has deployments (archive semantics are post-MVP)
```

---

## 9. Deployments

| Method | Path | Auth | Ticket |
|---|---|---|---|
| GET | `/api/environments/{environmentId}/deployments` | 🔒 | DEVHUB-067 |
| POST | `/api/environments/{environmentId}/deployments` | 🔒 | DEVHUB-067 |
| GET | `/api/deployments/{deploymentId}` | 🔒 | DEVHUB-067 |
| PATCH | `/api/deployments/{deploymentId}/status` | 🔒 | DEVHUB-067 |
| POST | `/api/webhooks/deployments` | 🪝 | DEVHUB-068 |

```jsonc
// POST create { "version":"1.8.2","commitSha":"8a2d91f","branch":"main",
//               "status":"Queued","triggeredBy":"github-actions","releaseId":null }
// 201 DeploymentDto

// DeploymentDto
{ "id","number":182,"environmentId","environmentName":"Production",
  "version":"1.8.2","commitSha":"8a2d91f","commitUrl","branch":"main",
  "status":"Succeeded","triggeredBy":"dario","releaseId",
  "cicdRunId","startedAt","completedAt","durationMs":214000,
  "events":[ { "name":"Build started","status":"Succeeded","occurredAt","message":null } ] }

// GET list → PagedResult<DeploymentListItemDto>, newest first, ?status= filter
// Webhook body → see webhooks-spec.md
```

---

## 10. Releases

| Method | Path | Auth | Ticket |
|---|---|---|---|
| GET | `/api/projects/{projectId}/releases` | 🔒 | DEVHUB-073 |
| POST | `/api/projects/{projectId}/releases` | 🔒 | DEVHUB-073 |
| GET | `/api/releases/{releaseId}` | 🔒 | DEVHUB-073 |
| PATCH | `/api/releases/{releaseId}` | 🔒 | DEVHUB-073 |
| POST | `/api/releases/{releaseId}/publish` | 🔒 | DEVHUB-073 |
| POST | `/api/releases/{releaseId}/issues` | 🔒 | DEVHUB-074 |
| DELETE | `/api/releases/{releaseId}/issues/{issueId}` | 🔒 | DEVHUB-074 |

```jsonc
// POST { "version":"v1.8.0", "name":"August delivery", "notes":"markdown" }
// 201 · 409 version already exists in the project
// PATCH on a Released release: only "notes" and "name" → 422 otherwise
// POST /publish → 200, sets status=Released + publishedAt · 409 already published
// POST /issues { "issueIds":["…","…"] } → 200 · 422 issue belongs to another released release
```

---

## 11. CI/CD

| Method | Path | Auth | Ticket |
|---|---|---|---|
| GET | `/api/projects/{projectId}/cicd/runs` | 🔒 | DEVHUB-079 |
| GET | `/api/cicd/runs/{runId}` | 🔒 | DEVHUB-079 |
| POST | `/api/webhooks/github/actions` | 🪝 | DEVHUB-078 |

```jsonc
// CicdRunDto
{ "id","projectId","provider":"GitHubActions","externalId":"12345678",
  "workflow":"backend-ci","runNumber":812,"branch":"main","commitSha":"8a2d91f",
  "commitUrl","runUrl","status":"Succeeded","startedAt","completedAt","durationMs",
  "linkedDeploymentId":null }
// GET list filters: status, branch, workflow
```

---

## 12. Dashboard

| Method | Path | Auth | Ticket |
|---|---|---|---|
| GET | `/api/projects/{projectId}/dashboard` | 🔒 | DEVHUB-082 |

```jsonc
{
  "project": { "id","key","name","color" },
  "issueSummary": { "open":24,"inProgress":8,"inReview":3,"doneLast7Days":11,"unassigned":5 },
  "environments": [ { "id","name","type","healthStatus","currentVersion","lastDeployedAt" } ],
  "recentDeployments": [ { "id","number","environmentName","version","status","completedAt" } ],
  "recentCicdRuns":   [ { "id","workflow","runNumber","status","branch","completedAt" } ],
  "recentReleases":   [ { "id","version","status","publishedAt" } ],
  "recentActivity":   [ { "id","type","actor","issueKey","createdAt" } ]
}
```

One call, ≤ 6 queries server-side, target < 300 ms. The frontend must never assemble this from
8–10 separate requests.

---

## 13. Notifications

| Method | Path | Auth | Ticket |
|---|---|---|---|
| GET | `/api/notifications` | 🔒 | DEVHUB-086 |
| GET | `/api/notifications/unread-count` | 🔒 | DEVHUB-086 |
| PATCH | `/api/notifications/{notificationId}/read` | 🔒 | DEVHUB-086 |
| POST | `/api/notifications/read-all` | 🔒 | DEVHUB-086 |

```jsonc
// GET ?unreadOnly=true&page=1 → PagedResult<NotificationDto>
{ "id","type":"DeploymentFailed","title":"Deployment failed — Production v1.8.0",
  "body":"…","targetType":"Deployment","targetId":"…","readAt":null,"createdAt":"…" }
```

---

## 14. Attachments

| Method | Path | Auth | Ticket |
|---|---|---|---|
| POST | `/api/attachments/upload-url` | 🔒 | DEVHUB-090 |
| POST | `/api/attachments/{attachmentId}/complete` | 🔒 | DEVHUB-090 |
| GET | `/api/attachments/{attachmentId}` | 🔒 | DEVHUB-091 |
| DELETE | `/api/attachments/{attachmentId}` | 🔒 | DEVHUB-091 |

```jsonc
// POST /upload-url { "fileName":"screenshot.png","contentType":"image/png",
//                    "sizeBytes":184320,"issueId":"…" }
// 201 { "attachmentId":"…","uploadUrl":"https://s3…","expiresIn":300,
//        "requiredHeaders":{"Content-Type":"image/png"} }
// 400 unsupported MIME type · 413 over 10 MB

// POST /{id}/complete → 200 AttachmentDto (status Ready) · 409 object not found in S3
// GET  /{id} → 200 { …, "downloadUrl":"https://s3…", "expiresIn":300 }
// DELETE → 204, deletes the S3 object too
```

---

## 15. Health & meta

| Method | Path | Auth | Ticket |
|---|---|---|---|
| GET | `/health` | 🔓 | DEVHUB-003 / 110 |
| GET | `/health/live` | 🔓 | DEVHUB-110 |
| GET | `/health/ready` | 🔓 | DEVHUB-110 |
| GET | `/swagger` | 🔓 (non-prod) | DEVHUB-003 |

```jsonc
// GET /health/ready
{ "status":"Healthy", "checks": { "database":"Healthy", "storage":"Healthy" },
  "version":"1.8.2", "durationMs": 12 }
// 200 Healthy · 503 Unhealthy
```

`/health/live` never touches dependencies — it answers "is the process alive" for the load
balancer. `/health/ready` checks the database and S3.

# API conventions

Rules every endpoint follows. Endpoint-by-endpoint contracts live in
[`api-endpoints.md`](api-endpoints.md).

---

## 1. Base

```text
Base URL:  /api
Format:    application/json, UTF-8
Casing:    camelCase in JSON, PascalCase in C#
Dates:     ISO 8601 UTC with Z — "2026-09-06T14:32:11Z"
Ids:       UUID strings
```

Resources are plural nouns. Nesting shows ownership only one level deep; once an id is globally
unique the flat route wins:

```text
POST /api/projects/{projectId}/issues     ← create needs the parent
GET  /api/issues/{issueId}                ← the id is enough
```

## 2. Methods

| Method | Use | Idempotent |
|---|---|---|
| `GET` | read | yes |
| `POST` | create, or an action that is not a plain update (`/publish`, `/read-all`) | no |
| `PATCH` | partial update — the default for updates | no |
| `PUT` | full replacement — avoid unless a resource is genuinely replaceable | yes |
| `DELETE` | remove | yes |

## 3. Status codes

| Code | When |
|---|---|
| `200 OK` | successful read or update, body returned |
| `201 Created` | resource created; `Location` header + the created resource |
| `204 No Content` | successful delete, or an update with nothing to return |
| `400 Bad Request` | malformed request or validation failure |
| `401 Unauthorized` | missing/expired/invalid access token |
| `403 Forbidden` | authenticated, resource visible, but the role is insufficient |
| `404 Not Found` | does not exist **or** the caller may not see it |
| `409 Conflict` | uniqueness or state conflict (duplicate key, already published) |
| `422 Unprocessable Entity` | semantically invalid operation (illegal status transition) |
| `429 Too Many Requests` | rate limited (webhooks, auth endpoints) |
| `500 Internal Server Error` | unhandled — never leaks details |

**404 vs 403:** if the caller is not a member of the owning workspace, return `404`. Use `403`
only when membership exists but the role is too low. This prevents probing for resource
existence.

## 4. Errors — RFC 7807 ProblemDetails

Every error response:

```json
{
  "type": "https://devhub.dev/errors/validation",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "See the errors property.",
  "instance": "/api/projects/9f1.../issues",
  "traceId": "00-4bf92f...-01",
  "errors": {
    "title": ["Title is required.", "Title must be 200 characters or fewer."],
    "priority": ["'Critical' is not a valid priority."]
  }
}
```

- `errors` is present only for field-level validation (`400`).
- `traceId` equals the correlation id, so a user-reported error maps to a log line.
- Domain rule violations use `422` with a stable `type` URI, e.g.
  `https://devhub.dev/errors/invalid-status-transition`.
- Never return exception messages, SQL, or stack traces.

## 5. Pagination

Offset pagination everywhere in the MVP (keyset is a post-MVP optimization for activity feeds):

```http
GET /api/projects/{projectId}/issues?page=1&pageSize=50
```

```json
{
  "items": [ ... ],
  "page": 1,
  "pageSize": 50,
  "totalCount": 137,
  "totalPages": 3
}
```

`page` is 1-based. `pageSize` defaults to 50, maximum 100 — a larger value is clamped, not
rejected.

## 6. Filtering & sorting

```http
GET /api/projects/{id}/issues
    ?status=InProgress&status=InReview        # repeated key = OR within the field
    &priority=High
    &assigneeId=me                            # "me" resolves to the caller
    &labelId=...
    &createdAfter=2026-01-01
    &search=deployment
    &sort=-updatedAt                          # "-" prefix = descending
```

Different fields combine with AND. Unknown query parameters are ignored, not errors. Allowed
sort fields are whitelisted per endpoint — never interpolate user input into SQL/LINQ ordering.

## 7. Requests

- `Authorization: Bearer <accessToken>` on every private endpoint.
- `Content-Type: application/json` on bodies. File bytes never go to the API (see
  [`storage-s3-spec.md`](storage-s3-spec.md)).
- PATCH bodies contain only the fields being changed. Distinguish "absent" from "null":
  `{"assigneeId": null}` unassigns; omitting `assigneeId` leaves it alone. Use
  `JsonElement`/optional wrappers, not nullable-only DTOs.
- Unknown properties in a body are rejected with `400` to catch client typos early.

## 8. Responses

- Collections are wrapped in the paged envelope; single resources are returned bare.
- DTOs never include `passwordHash`, raw tokens, internal S3 keys or `metadata` blobs.
- Related data is embedded when a screen always needs it (issue → assignee summary, labels),
  referenced by id otherwise. Optimize for the screen, not for purity.
- Enums are serialized as strings (`"InProgress"`), never integers.

## 9. Headers

| Header | Direction | Purpose |
|---|---|---|
| `Authorization` | in | bearer access token |
| `X-Correlation-Id` | in/out | request tracing; generated if absent, always echoed |
| `X-Hub-Signature-256` | in | GitHub webhook HMAC |
| `X-DevHub-Signature` | in | DevHub deployment webhook HMAC |
| `Location` | out | on `201` |
| `Retry-After` | out | on `429` |

## 10. Auth, CORS, rate limits

- Access token: JWT, 15 min. Refresh token: opaque, 30 days, rotated on use.
  See [`auth-spec.md`](auth-spec.md).
- CORS: explicit origin allow-list per environment (local web, staging, production). Never `*`
  with credentials.
- Rate limits: `POST /api/auth/*` 10/min per IP; webhooks 60/min per project; general API
  300/min per user. Exceeding returns `429` + `Retry-After`.

## 11. Versioning

No version segment in the MVP — the API and its only clients ship together. Rules until a `/v2`
is ever needed:

- Adding an optional field or endpoint is non-breaking; do it freely.
- Removing or renaming a field, tightening validation, or changing a status code is breaking:
  it needs a ticket, a client update in the same PR, and a note in the endpoint doc.

## 12. OpenAPI

Swagger is enabled in Development and Staging, disabled in Production. Every endpoint declares
`[ProducesResponseType]` for each documented status code, and DTOs carry XML doc comments —
the generated spec is part of the deliverable, not an afterthought.

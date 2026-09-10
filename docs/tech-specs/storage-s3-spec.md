# Storage & S3 specification

Private buckets, presigned URLs, least-privilege IAM. File bytes never pass through the API.

---

## 1. Why presigned URLs

Routing uploads through the API means the request pipeline holds the whole file, memory and
timeouts scale with file size, and Elastic Beanstalk's request limits become a product
constraint. A presigned URL moves the bytes client → S3 directly, while the API keeps control of
**who** may upload, **what** and **where** (it names the key and signs it).

```text
Client                    API                       S3
  │  POST /upload-url      │                         │
  │───────────────────────►│ validate size/MIME      │
  │                        │ create Attachment(Pending)
  │◄───────────────────────│ presigned PUT (5 min)   │
  │  PUT bytes ──────────────────────────────────────►│
  │  POST /{id}/complete   │                         │
  │───────────────────────►│ HeadObject ────────────►│
  │◄───────────────────────│ status = Ready          │
```

---

## 2. Buckets

| Bucket | Purpose | Public |
|---|---|---|
| `devhub-attachments-{env}` | issue/comment attachments, avatars, project icons | no |
| `devhub-web-{env}` | React SPA build, served through CloudFront (OAC) | no (CloudFront only) |

Both: Block Public Access **on** (all four settings), SSE-S3 (or SSE-KMS) enabled, versioning on
for `attachments`, TLS-only bucket policy:

```json
{
  "Sid": "DenyInsecureTransport",
  "Effect": "Deny",
  "Principal": "*",
  "Action": "s3:*",
  "Resource": ["arn:aws:s3:::devhub-attachments-prod",
               "arn:aws:s3:::devhub-attachments-prod/*"],
  "Condition": { "Bool": { "aws:SecureTransport": "false" } }
}
```

---

## 3. Key layout

```text
attachments/{workspaceId}/{projectId}/{issueId}/{attachmentId}/{sanitizedFileName}
avatars/{userId}/{attachmentId}.{ext}
project-icons/{projectId}/{attachmentId}.{ext}
releases/{releaseId}/{attachmentId}/{sanitizedFileName}
```

Rules:

- The **server** builds the key. A client-supplied key is a path-traversal and overwrite bug.
- `attachmentId` in the path guarantees uniqueness, so two files named `screenshot.png` never
  collide.
- The original file name is kept in the database and used in `Content-Disposition` on download;
  the key holds a sanitized version (`[^a-zA-Z0-9._-]` → `_`, max 100 chars).
- The prefix mirrors ownership, which makes lifecycle rules and future per-tenant policies easy.

---

## 4. Validation

| Rule | Value | Enforced |
|---|---|---|
| Max size | 10 MB (MVP) | API before signing **and** in the presigned condition |
| Allowed MIME | `image/png`, `image/jpeg`, `image/gif`, `image/webp`, `application/pdf`, `text/plain`, `text/markdown`, `application/zip`, `application/json` | API |
| Max attachments per issue | 20 | API |
| File name length | ≤ 255 | API |

The presigned PUT is generated with the exact `Content-Type` and a `content-length-range`
condition, so a client cannot sign a small file and upload a large one. The client must send
the same `Content-Type` header or S3 rejects the request.

Never trust the extension: `contentType` comes from the request, is validated against the
allow-list, and is what is stored on the object. Images are served with
`Content-Disposition: attachment` unless a ticket explicitly opts into inline rendering, so a
crafted SVG/HTML cannot execute in the app's origin.

---

## 5. Presigned URL parameters

| Operation | TTL | Notes |
|---|---|---|
| PUT (upload) | 5 min | one object, one content type, size-bounded |
| GET (download) | 5 min | generated on demand, never stored |

Download URLs are generated per request and are **never** cached in the database or in a client
store beyond their lifetime. `GET /api/attachments/{id}` returns a fresh URL plus `expiresIn`.

---

## 6. Lifecycle

```text
Pending attachments (never completed)  → deleted after 1 day  (prefix-independent, by status)
Noncurrent versions                    → expire after 30 days
Incomplete multipart uploads           → abort after 7 days
```

The `Pending` cleanup has two halves: an S3 lifecycle rule on a `pending/` prefix is possible,
but the simpler MVP approach is a scheduled job in the API that deletes `Pending` rows older
than 24 h and their objects. Whichever is chosen, document it in DEVHUB-091.

---

## 7. IAM

**Application role** (Elastic Beanstalk EC2 instance profile) — the only S3 permissions the API
ever needs:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": ["s3:PutObject", "s3:GetObject", "s3:DeleteObject", "s3:HeadObject"],
      "Resource": "arn:aws:s3:::devhub-attachments-prod/*"
    },
    {
      "Effect": "Allow",
      "Action": ["s3:ListBucket"],
      "Resource": "arn:aws:s3:::devhub-attachments-prod",
      "Condition": { "StringLike": { "s3:prefix": ["attachments/*", "avatars/*"] } }
    }
  ]
}
```

No `s3:*`. No `Resource: "*"`. No bucket-level delete. The role has no permission on the web
bucket, and the CI deploy role has no permission on the attachments bucket.

**Credentials:** the API uses the instance role via the default credential chain — no access
keys in configuration, ever. Locally, use a dedicated low-privilege IAM user with a profile in
`~/.aws/credentials`, or MinIO/LocalStack to avoid touching AWS at all during Phase 1.

---

## 8. Local development

Two options, decided in DEVHUB-089:

1. **MinIO** in `docker-compose` — S3-compatible, no AWS account needed, presigned URLs work.
   Requires `ForcePathStyle = true` and a `ServiceURL` override in the S3 client config.
2. **A real dev bucket** — closer to production, teaches real IAM, costs cents.

Both are configured through the same `IFileStorage` abstraction, so the application code is
identical.

```csharp
public interface IFileStorage
{
    Task<PresignedUpload> CreateUploadUrlAsync(string key, string contentType, long maxBytes, CancellationToken ct);
    Task<string>          CreateDownloadUrlAsync(string key, string fileName, CancellationToken ct);
    Task<bool>            ExistsAsync(string key, CancellationToken ct);
    Task                  DeleteAsync(string key, CancellationToken ct);
}
```

The interface lives in `DevHub.Application`; the S3 implementation lives in
`DevHub.Infrastructure.Storage`. The domain knows nothing about S3.

---

## 9. CORS on the attachments bucket

The browser PUTs directly to S3, so the bucket needs its own CORS configuration — the API's CORS
settings are irrelevant here:

```json
[{
  "AllowedOrigins": ["http://localhost:5173", "https://app.devhub.example"],
  "AllowedMethods": ["PUT", "GET"],
  "AllowedHeaders": ["Content-Type"],
  "ExposeHeaders": ["ETag"],
  "MaxAgeSeconds": 3000
}]
```

A failed browser upload with no useful error message is almost always this.

---

## 10. Post-MVP

- S3 event → SQS → Lambda → thumbnail generation, written back as `…/thumb.webp`.
- CloudFront in front of attachments with signed cookies for image-heavy views.
- Virus scanning on upload.
- Per-workspace storage quotas.

---

## 11. Test checklist

- [ ] Upload URL is refused for a disallowed MIME type and for > 10 MB.
- [ ] The key is server-generated; a client-supplied `key` field is ignored.
- [ ] `complete` fails with `409` when the object is not in S3.
- [ ] A download URL expires (assert the `X-Amz-Expires` parameter; optionally test a real 403).
- [ ] Deleting an attachment deletes the S3 object.
- [ ] A user from another workspace cannot fetch a download URL (`404`).

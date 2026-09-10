# DEVHUB-090 — Presigned upload URL and completion endpoints

|  |  |
|---|---|
| **Epic** | EPIC 15 — Attachments & S3 |
| **Phase** | 3 — AWS |
| **Priority** | P1 |
| **Size** | M |
| **Depends on** | DEVHUB-089 |
| **Specs** | [`storage-s3-spec.md`](../../tech-specs/storage-s3-spec.md), [`user-flows.md`](../../user-flows.md) Flow 7 |

## Context

The two-step upload: the API authorizes and names the object, the client sends the bytes
straight to S3.

## Scope

**In:** `Attachment` entity, `POST /upload-url`, `POST /{id}/complete`, validation.
**Out:** download and delete (DEVHUB-091), clients (DEVHUB-092/093).

## Tasks

- [ ] `Attachment` entity + migration, including the `issue_id XOR comment_id` check constraint.
- [ ] `POST /api/attachments/upload-url`: authorize the target issue/comment, validate MIME
      (allow-list) and size (≤10 MB), enforce the per-issue cap, create the row as `Pending`,
      return a 5-minute presigned PUT with a `content-length-range` condition.
- [ ] The **server** builds the storage key; a client-supplied key is ignored.
- [ ] Sanitize the file name for the key; keep the original in the database.
- [ ] `POST /api/attachments/{id}/complete`: `HeadObject` to verify, compare the real size,
      flip to `Ready`, record activity, return the DTO.
- [ ] `409` when the object is missing; `413`/`400` for size and type failures.
- [ ] Tests: disallowed MIME, oversize, client-supplied key ignored, complete without upload,
      cross-workspace target → 404, path traversal in the file name neutralized.

## Acceptance criteria

- [ ] A browser can complete the full three-step flow against MinIO.
- [ ] The presigned URL cannot be reused for a different content type or a larger file.
- [ ] An abandoned upload leaves only a `Pending` row (cleaned up by DEVHUB-091).

## Technical notes

Verifying with `HeadObject` at completion is what stops a client from claiming an upload it never
made. Compare the reported size to the actual object size — trusting the client's number defeats
the size limit.

## Learning goals

Presigned URLs and their conditions, two-phase resource creation, defending against
client-controlled inputs.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2, §5.

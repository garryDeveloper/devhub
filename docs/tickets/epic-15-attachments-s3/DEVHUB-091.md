# DEVHUB-091 — Attachment download, deletion and cleanup

|  |  |
|---|---|
| **Epic** | EPIC 15 — Attachments & S3 |
| **Phase** | 3 — AWS |
| **Priority** | P1 |
| **Size** | S |
| **Depends on** | DEVHUB-090 |
| **Specs** | [`storage-s3-spec.md`](../../tech-specs/storage-s3-spec.md) §5–6 |

## Context

Closing the lifecycle: fetching files safely, deleting both row and object, and making sure
abandoned uploads do not accumulate silently.

## Scope

**In:** `GET /api/attachments/{id}` (fresh presigned GET), `DELETE`, pending cleanup, lifecycle
rules.
**Out:** thumbnails, CloudFront delivery (post-MVP).

## Tasks

- [ ] `GET /api/attachments/{id}` returns metadata plus a freshly generated 5-minute download URL
      with `Content-Disposition: attachment` and the original file name.
- [ ] Never cache or persist download URLs.
- [ ] `DELETE`: uploader or workspace owner; delete the row and the S3 object; activity entry.
- [ ] A scheduled job deleting `Pending` attachments older than 24 h and their objects.
- [ ] Bucket lifecycle rules: expire noncurrent versions after 30 days, abort incomplete
      multipart uploads after 7 days.
- [ ] Cascade: deleting an issue or comment deletes its attachments' objects too.
- [ ] Tests: cross-workspace access → 404, delete removes the object, cleanup job removes only
      stale pending rows.

## Acceptance criteria

- [ ] Download URLs expire and are generated per request.
- [ ] Deleting an attachment leaves nothing behind in S3.
- [ ] Abandoned uploads disappear within 24 h.

## Technical notes

`Content-Disposition: attachment` on downloads is a security control, not a convenience: it stops
an uploaded HTML or SVG file from executing in the app's origin.

## Learning goals

Presigned GETs, lifecycle rules, background cleanup jobs, cascading deletes across a database and
an object store.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2, §5.

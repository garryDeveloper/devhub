# DEVHUB-092 — Web attachment upload

|  |  |
|---|---|
| **Epic** | EPIC 15 — Attachments & S3 |
| **Phase** | 3 — AWS |
| **Priority** | P1 |
| **Size** | M |
| **Depends on** | DEVHUB-091, DEVHUB-056 |
| **Specs** | [`user-flows.md`](../../user-flows.md) Flow 7 |

## Context

Drag a screenshot onto an issue and have it appear. The three-step flow must be invisible to the
user, including when a step fails.

## Scope

**In:** uploader component with progress, attachment list, image previews, delete, avatar upload.
**Out:** paste-from-clipboard (nice follow-up), inline markdown embedding.

## Tasks

- [ ] `<AttachmentUploader>`: drag & drop plus a file picker, multiple files, client-side type
      and size validation matching the server rules.
- [ ] Per-file progress using `XMLHttpRequest` (or `fetch` with a stream) for the S3 PUT.
- [ ] Orchestrate upload-url → PUT → complete, with per-file error handling and retry.
- [ ] Attachment list on the issue and on comments: thumbnail for images, icon otherwise, name,
      size, uploader, delete for permitted users.
- [ ] Image preview in a lightbox.
- [ ] Reuse the same component for the profile avatar.
- [ ] Cancel an in-flight upload; abandoned uploads leave no visible row.

## Acceptance criteria

- [ ] Uploading a 5 MB image shows real progress and appears when complete.
- [ ] A rejected file type is refused before any request is made.
- [ ] A failed PUT shows a retry rather than a silently missing attachment.
- [ ] Deleting an attachment removes it from the list immediately.

## Technical notes

`fetch` still has no upload progress event — use `XMLHttpRequest` for the PUT if you want a real
progress bar. That surprise is worth knowing before you build the component.

## Learning goals

Direct-to-S3 uploads from the browser, upload progress, multi-step client flows with partial
failure, CORS in practice.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3.

# DEVHUB-093 — Mobile attachment upload

|  |  |
|---|---|
| **Epic** | EPIC 15 — Attachments & S3 |
| **Phase** | 3 — AWS |
| **Priority** | P2 |
| **Size** | S |
| **Depends on** | DEVHUB-091, DEVHUB-057 |
| **Specs** | [`mobile-architecture.md`](../../tech-specs/mobile-architecture.md) |

## Context

Attaching a photo from a phone is the most natural attachment flow there is — and the one most
likely to hit size limits.

## Scope

**In:** image picker and camera capture, upload with progress, attachment viewing.
**Out:** document picker, video (both post-MVP).

## Tasks

- [ ] `expo-image-picker` for library and camera, with permission handling and a clear
      explanation when permission is denied.
- [ ] Client-side resize/compress before upload (target ≤2 MB) — phone photos routinely exceed
      the 10 MB limit.
- [ ] Upload via the same three-step flow with a progress indicator.
- [ ] Attachment thumbnails in the issue detail; tap for a full-screen viewer.
- [ ] Error handling with retry; cancel an in-flight upload.

## Acceptance criteria

- [ ] A photo taken in-app uploads and appears on the issue.
- [ ] A large photo is compressed rather than rejected.
- [ ] Denied camera permission produces an explanation, not a crash.

## Technical notes

Compressing before upload is the difference between a feature that works on cellular and one
that times out. It also keeps the S3 bill sane.

## Learning goals

Native permissions, image manipulation on device, uploads over unreliable networks.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §4.

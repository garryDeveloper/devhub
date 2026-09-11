# DEVHUB-089 — S3 bucket, local MinIO and the storage abstraction

|  |  |
|---|---|
| **Epic** | EPIC 15 — Attachments & S3 |
| **Phase** | 3 — AWS |
| **Priority** | P1 |
| **Size** | M |
| **Depends on** | DEVHUB-007 |
| **Specs** | [`storage-s3-spec.md`](../../tech-specs/storage-s3-spec.md) |

## Context

The first AWS service. Start with the bucket, the IAM policy and the abstraction — before any
feature depends on it.

## Scope

**In:** `IFileStorage` port, S3 implementation, MinIO for local development, bucket creation,
application IAM policy, CORS.
**Out:** endpoints (DEVHUB-090), UI (DEVHUB-092/093).

## Tasks

- [ ] Create `devhub-attachments-dev` with Block Public Access on, SSE enabled, versioning on.
- [ ] Add the TLS-only bucket policy from the spec.
- [ ] Configure bucket CORS for `http://localhost:5173` (PUT, GET, `Content-Type`, expose `ETag`).
- [ ] Create `DevHubApplicationRole`'s S3 policy — scoped actions, scoped ARNs, no `s3:*`.
- [ ] Define `IFileStorage` in Application; implement `S3FileStorage` in Infrastructure using
      `AWSSDK.S3`.
- [ ] Add MinIO to `docker-compose` with `ForcePathStyle` and a `ServiceURL` override, so local
      development needs no AWS account.
- [ ] Credentials from the default chain (instance role in AWS, profile or MinIO keys locally) —
      never from committed configuration.
- [ ] Integration test against MinIO: put, presign, head, delete.

## Acceptance criteria

- [ ] The same code path works against MinIO locally and S3 in AWS, switched by configuration.
- [ ] The bucket rejects unencrypted (HTTP) requests and is not publicly readable.
- [ ] The IAM policy contains no wildcard action or resource.
- [ ] Teardown steps (delete bucket contents, versions, bucket) are documented in the PR.

## Technical notes

MinIO needs path-style addressing; S3 uses virtual-host style. Getting this wrong produces DNS
errors that look like network problems — a classic first-day-with-S3 hour lost.

## Learning goals

Buckets, bucket policies vs IAM policies, Block Public Access precedence, ports and adapters for
infrastructure, local cloud emulation.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2, §5.

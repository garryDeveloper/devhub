# DEVHUB-102 — Web CI workflow

|  |  |
|---|---|
| **Epic** | EPIC 17 — CI/CD for DevHub |
| **Phase** | 4 — CI/CD |
| **Priority** | P0 |
| **Size** | XS |
| **Depends on** | DEVHUB-012 |
| **Specs** | [`cicd-architecture.md`](../../tech-specs/cicd-architecture.md) §3 |

## Context

Typecheck, lint, test and build the SPA, producing the exact bundle that will be deployed.

## Scope

**In:** `web-ci.yml` with build artifact upload.
**Out:** E2E (`main` only, added with DEVHUB-105), deploy.

## Tasks

- [ ] Jobs: install (cached) → typecheck → lint → unit tests → build.
- [ ] Build with the staging API URL and upload `dist/` as an artifact.
- [ ] Fail on TypeScript errors and lint warnings (`--max-warnings 0`).
- [ ] Report bundle size in the job summary and flag a large regression.
- [ ] Path filter `web/**` plus the workflow file itself.

## Acceptance criteria

- [ ] A type error fails the workflow.
- [ ] The built `dist/` is available as an artifact.
- [ ] The run takes under 4 minutes.

## Technical notes

The API URL is baked in at build time, so a bundle built for staging cannot be promoted to
production unchanged. Either build twice, or read the API URL from a runtime config file fetched
by the app — decide here and document which, because it shapes DEVHUB-106.

## Learning goals

Frontend CI, build-time vs runtime configuration, bundle size awareness.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §6.

# DEVHUB-098 — S3 + CloudFront for the web SPA

|  |  |
|---|---|
| **Epic** | EPIC 16 — AWS infrastructure |
| **Phase** | 3 — AWS |
| **Priority** | P1 |
| **Size** | M |
| **Depends on** | DEVHUB-089, DEVHUB-096 |
| **Specs** | [`aws-architecture.md`](../../tech-specs/aws-architecture.md) §3 |

## Context

Hosting the React app as static files behind a CDN — cheap, fast, and the standard SPA deployment
shape.

## Scope

**In:** web bucket, CloudFront distribution with OAC, SPA routing fallback, cache policy,
security headers, manual first deploy.
**Out:** custom domain and ACM certificate (optional follow-up), automated deploy (DEVHUB-105).

## Tasks

- [ ] Create `devhub-web-staging`, private, Block Public Access on.
- [ ] CloudFront distribution with **Origin Access Control**; update the bucket policy to allow
      only that distribution.
- [ ] Default root object `index.html`; custom error responses mapping `403` and `404` to
      `/index.html` with status `200` — without this, deep links break.
- [ ] Cache policy: long TTL for hashed assets, no-cache for `index.html`.
- [ ] Response headers policy: HSTS, `X-Content-Type-Options`, `X-Frame-Options`, a CSP.
- [ ] Build the SPA with the staging API URL and sync it to the bucket; invalidate `/*`.
- [ ] Add the CloudFront origin to the API's CORS allow-list.
- [ ] Document the deploy and teardown steps.

## Acceptance criteria

- [ ] The app loads over the CloudFront URL and can talk to the API.
- [ ] A hard refresh on `/p/DEV/issues` loads the app rather than an S3 error.
- [ ] The bucket is not directly accessible.
- [ ] A new deploy is visible after invalidation, with no stale `index.html`.

## Technical notes

Hashed asset filenames plus a no-cache `index.html` means you only ever need to invalidate
`index.html` — full `/*` invalidations are slow and, past the free tier, billable.

## Learning goals

CloudFront origins and behaviours, OAC, SPA routing on a static host, cache strategy, security
headers.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §5.

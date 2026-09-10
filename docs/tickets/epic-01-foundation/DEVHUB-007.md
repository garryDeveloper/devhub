# DEVHUB-007 — Configuration and secrets strategy

|  |  |
|---|---|
| **Epic** | EPIC 1 — Foundation & project setup |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-002 |
| **Specs** | [`local-development.md`](../../tech-specs/local-development.md) §4, [`aws-architecture.md`](../../tech-specs/aws-architecture.md) §4 |

## Context

Decide once where configuration lives, so no ticket ever has an excuse to commit a connection
string. The same shape must work locally, in CI and in AWS.

## Scope

**In:** strongly-typed options, configuration layering, user-secrets locally, `.env.example`
files for the clients, startup validation.
**Out:** SSM Parameter Store wiring (DEVHUB-097).

## Tasks

- [ ] Define options classes: `JwtOptions`, `StorageOptions`, `CorsOptions`, `WebhookOptions`.
- [ ] Bind with `AddOptions<T>().Bind(...).ValidateDataAnnotations().ValidateOnStart()`.
- [ ] Move every secret out of `appsettings*.json`; leave only non-sensitive defaults.
- [ ] Initialize user-secrets in `DevHub.Api` and document the `dotnet user-secrets set` commands.
- [ ] Add `web/.env.example` and `mobile/.env.example` with placeholder values.
- [ ] Confirm `.gitignore` excludes `.env`, `.env.local`, `*.user`.
- [ ] Add a startup log line listing which configuration sources loaded (names, never values).

## Acceptance criteria

- [ ] The API refuses to start with a clear message if `Jwt:Secret` is missing — it does not
      start with an insecure default.
- [ ] No secret value exists anywhere in the repository (verify with a `git grep` for the
      obvious patterns and, ideally, a `gitleaks` run).
- [ ] Environment variables override user-secrets, which override appsettings.

## Technical notes

- The `__` separator maps environment variables to nested keys:
  `ConnectionStrings__Default` → `ConnectionStrings:Default`. This is how AWS will inject them.
- `ValidateOnStart()` is the point: failing at boot is far better than failing on the first
  request that needs the value.

## Learning goals

.NET configuration layering, the options pattern, fail-fast configuration, secret management
hygiene.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

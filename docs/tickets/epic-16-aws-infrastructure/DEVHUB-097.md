# DEVHUB-097 — Configuration and secrets in AWS (SSM Parameter Store)

|  |  |
|---|---|
| **Epic** | EPIC 16 — AWS infrastructure |
| **Phase** | 3 — AWS |
| **Priority** | P1 |
| **Size** | S |
| **Depends on** | DEVHUB-096, DEVHUB-007 |
| **Specs** | [`aws-architecture.md`](../../tech-specs/aws-architecture.md) §4 |

## Context

Getting the hand-entered environment variables out of the Beanstalk console and into a managed,
encrypted, auditable store.

## Scope

**In:** SSM parameter hierarchy, KMS encryption, injection at startup, IAM scoping, rotation
procedure.
**Out:** Secrets Manager, automatic rotation.

## Tasks

- [ ] Create parameters under `/devhub/{env}/…` per the spec; secrets as `SecureString`.
- [ ] Scope the application role to `ssm:GetParametersByPath` on its own environment path only,
      plus `kms:Decrypt` on that key.
- [ ] Load parameters at container start (an entrypoint script exporting them, or the AWS SSM
      configuration provider in .NET) mapping `__` to configuration nesting.
- [ ] Remove every secret from the Beanstalk console configuration.
- [ ] Verify the app fails to start with a clear message when a required parameter is missing.
- [ ] Document how to add and rotate a parameter without a redeploy where possible.

## Acceptance criteria

- [ ] No secret is visible in the Beanstalk console, the repository or the image.
- [ ] The staging role cannot read production parameters (verified by attempting it).
- [ ] Rotating `Jwt__Secret` takes effect after a restart, with a documented procedure.

## Technical notes

Parameter Store's standard tier is free and enough here; Secrets Manager costs ~$0.40 per secret
per month and buys automatic rotation you do not yet need. Note the trade-off rather than
defaulting to the more expensive service.

## Learning goals

Parameter hierarchies, KMS-encrypted parameters, path-scoped IAM, injecting configuration into a
container without baking it in.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §5.

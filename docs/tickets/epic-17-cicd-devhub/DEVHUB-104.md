# DEVHUB-104 — GitHub OIDC and the AWS deployment role

|  |  |
|---|---|
| **Epic** | EPIC 17 — CI/CD for DevHub |
| **Phase** | 4 — CI/CD |
| **Priority** | P0 |
| **Size** | M |
| **Depends on** | DEVHUB-099 |
| **Specs** | [`cicd-architecture.md`](../../tech-specs/cicd-architecture.md) §6 |

## Context

The security centrepiece of the pipeline: GitHub deploys to AWS with **no stored credentials**,
using short-lived tokens from a federated trust.

## Scope

**In:** OIDC identity provider, `DevHubGitHubActionsRole`, trust conditions, deploy policy,
verification.
**Out:** the deploy workflows themselves (DEVHUB-105/106).

## Tasks

- [ ] Create the IAM OIDC provider for `token.actions.githubusercontent.com`.
- [ ] Create `DevHubGitHubActionsRole` with a trust policy conditioned on
      `repo:<owner>/devhub:ref:refs/heads/main` and `repo:<owner>/devhub:environment:production`.
- [ ] Attach a least-privilege deploy policy: Beanstalk create-version/update-environment,
      `s3:PutObject` on the artifact and web buckets, `cloudfront:CreateInvalidation`.
- [ ] Explicitly ensure it has **no** access to the attachments bucket, RDS or IAM.
- [ ] Add a throwaway workflow that assumes the role and runs `aws sts get-caller-identity`.
- [ ] Verify the condition works: run the same job from a feature branch and confirm it is denied.
- [ ] Remove any pre-existing AWS access keys from GitHub secrets.

## Acceptance criteria

- [ ] A job on `main` assumes the role successfully.
- [ ] The same job on a feature branch is denied (proof pasted in the PR).
- [ ] No AWS access key exists in GitHub secrets.
- [ ] The role cannot read the attachments bucket (tested and documented).

## Technical notes

The `sub` condition is the entire security boundary. Without it, any repository on GitHub could
assume your role — a trust policy that only checks the provider is effectively public.

## Learning goals

OIDC federation, trust policies and conditions, short-lived credentials, verifying a denial
rather than assuming it.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §5, §6.

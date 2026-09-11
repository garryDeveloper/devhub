# DEVHUB-099 — IAM roles and least-privilege review

|  |  |
|---|---|
| **Epic** | EPIC 16 — AWS infrastructure |
| **Phase** | 3 — AWS |
| **Priority** | P1 |
| **Size** | M |
| **Depends on** | DEVHUB-096, DEVHUB-098 |
| **Specs** | [`aws-architecture.md`](../../tech-specs/aws-architecture.md) §3 |

## Context

A dedicated pass over every permission granted so far. IAM is one of the stated learning goals of
this whole project, so it gets its own ticket rather than being a side effect of other tickets.

## Scope

**In:** review and tighten every role, remove unused permissions, add trust-policy conditions,
enable billing and root-usage alarms.
**Out:** the GitHub OIDC role (DEVHUB-104), organizations/SCPs.

## Tasks

- [ ] Enumerate every role and policy created so far; write each one's purpose in one line.
- [ ] Replace any `*` action or resource with explicit lists, or justify it in writing.
- [ ] Verify `DevHubApplicationRole` cannot touch the web bucket, and that no role can read
      another environment's SSM path.
- [ ] Use the IAM Access Analyzer / last-accessed data to find and remove unused permissions.
- [ ] Ensure your personal admin user has MFA; confirm the root account is unused and has MFA.
- [ ] Create a billing alarm (e.g. $10) and a root-usage alarm.
- [ ] Attempt one denied action per role and record the failure as proof.

## Acceptance criteria

- [ ] No workload role has `AdministratorAccess` or an unjustified wildcard.
- [ ] Each role's denied-action test is documented with its actual error.
- [ ] Billing and root-usage alarms are active.
- [ ] A written table of roles → permissions → why exists in the PR.

## Technical notes

The last-accessed data in IAM is the honest way to find over-provisioning: grant what you think
is needed, run the system for a week, then remove everything unused.

## Learning goals

Least privilege in practice, policy evaluation and explicit deny, trust policy conditions, IAM
tooling, account hygiene.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §5.

# DEVHUB-103 — Mobile CI workflow

|  |  |
|---|---|
| **Epic** | EPIC 17 — CI/CD for DevHub |
| **Phase** | 4 — CI/CD |
| **Priority** | P1 |
| **Size** | XS |
| **Depends on** | DEVHUB-012 |
| **Specs** | [`cicd-architecture.md`](../../tech-specs/cicd-architecture.md) §3 |

## Context

Keep the mobile app compiling and linted. Store builds are out of scope for the MVP, and saying
so explicitly stops it from feeling like an omission.

## Scope

**In:** `mobile-ci.yml` — install, typecheck, lint, unit tests.
**Out:** EAS builds, store submission, device testing.

## Tasks

- [ ] Jobs: install (cached) → typecheck → lint → test.
- [ ] Path filter `mobile/**`.
- [ ] `expo-doctor` (or equivalent) to catch dependency mismatches early.
- [ ] Document in the workflow file why no build job exists yet and what would be needed
      (EAS account, credentials, build minutes).

## Acceptance criteria

- [ ] A type error or lint failure in mobile fails the check.
- [ ] The workflow runs only on mobile changes.
- [ ] The build-scope decision is written down where the next person will find it.

## Technical notes

EAS builds are slow and quota-limited; adding them to every PR would dominate the feedback loop
for little benefit while the app has no release channel.

## Learning goals

Scoping CI to what is useful, dependency-drift detection in the Expo ecosystem.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §6.

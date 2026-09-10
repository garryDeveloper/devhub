# DEVHUB-071 — Mobile deployments

|  |  |
|---|---|
| **Epic** | EPIC 10 — Deployments |
| **Phase** | 2 — Product differentiation |
| **Priority** | P1 |
| **Size** | S |
| **Depends on** | DEVHUB-065, DEVHUB-067 |
| **Specs** | [`mobile-architecture.md`](../../tech-specs/mobile-architecture.md) §7 |

## Context

Checking a deployment from a phone — typically right after getting a "deployment failed"
notification.

## Scope

**In:** deployment list per environment, deployment detail with timeline, external links.
**Out:** any deployment mutation.

## Tasks

- [ ] Deployment list within `EnvironmentDetail`: status glyph, version, relative time, duration.
- [ ] `DeploymentDetail`: metadata, event timeline, "Open in GitHub" button.
- [ ] Deep link `devhub://deployments/{id}` — the target of failure notifications.
- [ ] Poll while the deployment is non-terminal and the screen is focused.
- [ ] Pull-to-refresh, loading, empty and error states.

## Acceptance criteria

- [ ] Tapping a deployment-failed notification opens the right deployment from a cold start.
- [ ] The timeline is readable on a small screen without horizontal scrolling.
- [ ] Polling stops when the screen loses focus.

## Technical notes

Cold-start deep linking with authentication is the tricky path: the link must be held until the
auth bootstrap resolves, then replayed. DEVHUB-022 set this up — verify it here with a real
notification.

## Learning goals

Deep link handling end to end, focus-aware polling, compact timeline layout.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §4.

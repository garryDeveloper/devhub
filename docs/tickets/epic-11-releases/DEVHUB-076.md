# DEVHUB-076 — Mobile releases

|  |  |
|---|---|
| **Epic** | EPIC 11 — Releases |
| **Phase** | 2 — Product differentiation |
| **Priority** | P2 |
| **Size** | S |
| **Depends on** | DEVHUB-073, DEVHUB-035 |
| **Specs** | [`mobile-architecture.md`](../../tech-specs/mobile-architecture.md) §7 |

## Context

Read-only on mobile: see what shipped. Creating and publishing releases stays on web.

## Scope

**In:** releases list and detail (read-only).
**Out:** create, edit, publish, issue linking.

## Tasks

- [ ] Releases list under a project: version, status badge, published date.
- [ ] Release detail: notes (markdown), linked issues (tappable through to issue detail),
      deployments per environment.
- [ ] Pull-to-refresh; loading, empty and error states.

## Acceptance criteria

- [ ] Notes render as markdown, not raw text.
- [ ] Tapping a linked issue opens the issue detail screen.
- [ ] No mutation controls appear.

## Technical notes

Keep the release detail a single scroll view — sections, not tabs. Tabs inside a detail screen on
mobile hide content that users then never find.

## Learning goals

Read-only screen design, cross-feature navigation on mobile.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §4.

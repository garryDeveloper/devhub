# DEVHUB-070 — Web deployment history and detail

|  |  |
|---|---|
| **Epic** | EPIC 10 — Deployments |
| **Phase** | 2 — Product differentiation |
| **Priority** | P0 |
| **Size** | M |
| **Depends on** | DEVHUB-067, DEVHUB-064 |
| **Specs** | [`screens-and-navigation.md`](../../screens-and-navigation.md) §4 |

## Context

The screens that answer "what shipped, when, and did it work?" — and, when it did not, get you
to the real logs in one click.

## Scope

**In:** deployment history under an environment, deployment detail with the event timeline.
**Out:** triggering deployments from the UI (out of scope for the whole product).

## Tasks

- [ ] Environment detail page listing deployments: status glyph, version, short commit sha,
      relative time, duration, triggered by.
- [ ] Filter by status; paginate or infinite-scroll.
- [ ] Deployment detail `/p/:projectKey/deployments/:number`: metadata block plus a vertical
      event timeline with per-step status and timestamps.
- [ ] Prominent external links: commit and CI run.
- [ ] Link to the associated release when present.
- [ ] Live-ish updates while a deployment is `Queued`/`Running` (poll every 10 s, stop on
      terminal status).
- [ ] Loading, empty ("No deployments yet — connect CI in EPIC 17") and error states.

## Acceptance criteria

- [ ] A failed deployment makes the failing step obvious and links to the GitHub run.
- [ ] A running deployment updates without a manual refresh and stops polling when it finishes.
- [ ] The commit sha links to the right commit.

## Technical notes

Stop the polling interval on a terminal status and on unmount. A forgotten interval on a
background tab is a quiet battery and quota drain.

## Learning goals

Timeline visualization, conditional polling, designing a screen whose job is to hand off to
another tool.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3.

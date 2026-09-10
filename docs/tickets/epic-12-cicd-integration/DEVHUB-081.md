# DEVHUB-081 — Web and mobile CI/CD views

|  |  |
|---|---|
| **Epic** | EPIC 12 — CI/CD integration |
| **Phase** | 2 — Product differentiation |
| **Priority** | P1 |
| **Size** | M |
| **Depends on** | DEVHUB-079, DEVHUB-070 |
| **Specs** | [`screens-and-navigation.md`](../../screens-and-navigation.md) §4, §7 |

## Context

The pipeline activity screen. Deliberately a small surface: DevHub shows *that* things ran and
links to GitHub for *how*.

## Scope

**In:** web CI/CD list and run detail, mobile CI/CD list.
**Out:** log viewing, re-running workflows.

## Tasks

- [ ] Web `/p/:projectKey/cicd`: workflow, run number, branch, short sha, status, duration,
      relative time, external link.
- [ ] Filters: status, branch, workflow.
- [ ] Run detail (a modal or panel is enough): metadata, linked deployment, "Open in GitHub".
- [ ] Poll while a run is non-terminal; stop at terminal.
- [ ] Mobile: read-only list with the same fields, condensed; tap opens the GitHub run.
- [ ] Empty state that explains how to connect CI (pointing at EPIC 17).

## Acceptance criteria

- [ ] Running workflows update live; finished ones stop polling.
- [ ] Every row reaches the corresponding GitHub run in one click/tap.
- [ ] The empty state is instructive rather than blank.

## Technical notes

Branch names can be long (`feat/DEVHUB-042-really-descriptive-name`). Truncate in the middle with
the full value in a tooltip — truncating the end hides the ticket id, which is the useful part.

## Learning goals

Read-only integration screens, live polling with cleanup, text truncation that keeps the
information.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3, §4.

# DEVHUB-075 — Web releases list and detail

|  |  |
|---|---|
| **Epic** | EPIC 11 — Releases |
| **Phase** | 2 — Product differentiation |
| **Priority** | P1 |
| **Size** | M |
| **Depends on** | DEVHUB-074, DEVHUB-044 |
| **Specs** | [`screens-and-navigation.md`](../../screens-and-navigation.md) §4 |

## Context

The changelog view: what went out, when, and where it is running.

## Scope

**In:** releases list, release detail, create/edit, issue picker, publish flow.
**Out:** exporting release notes, auto-generated notes.

## Tasks

- [ ] `/p/:projectKey/releases`: version, status badge, published date, issue count.
- [ ] `/p/:projectKey/releases/:version`: notes (markdown), linked issues grouped by status,
      and the deployments carrying this version with their environments.
- [ ] Create/edit form: version, name, markdown notes with preview.
- [ ] Issue picker: search within the project, multi-select, showing which issues are already in
      another release.
- [ ] Publish button with a confirmation that states publishing is irreversible.
- [ ] Read-only rendering for published releases, with only notes editable.
- [ ] Loading, empty and error states.

## Acceptance criteria

- [ ] A release page reads like a changelog entry someone could paste into a announcement.
- [ ] Publishing requires confirmation and updates the UI immediately.
- [ ] Issues already released elsewhere are visibly unavailable in the picker.

## Technical notes

The picker is the fiddly part: search, multi-select, and a disabled reason per row. Reuse the
issue list query with a `search` filter rather than inventing a second search path.

## Learning goals

Multi-select pickers with server-side search, irreversible action UX, composing a read model
across three entities.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3.

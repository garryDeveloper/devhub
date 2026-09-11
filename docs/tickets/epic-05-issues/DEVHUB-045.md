# DEVHUB-045 — Web board with drag & drop

|  |  |
|---|---|
| **Epic** | EPIC 5 — Issues |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | L |
| **Depends on** | DEVHUB-042, DEVHUB-044 |
| **Specs** | [`screens-and-navigation.md`](../../screens-and-navigation.md) §4 |

## Context

The signature interaction of the product, and the best place in the codebase to learn optimistic
updates properly.

## Scope

**In:** six status columns, drag & drop between them, optimistic updates with rollback, keyboard
alternative, per-column counts.
**Out:** manual ordering within a column (no rank field in the MVP), swimlanes, WIP limits.

## Tasks

- [ ] `/p/:projectKey/board` with a column per status, cards showing key, title, priority,
      assignee and labels.
- [ ] dnd-kit drag & drop; dropping calls `PATCH /api/issues/{id}/status`.
- [ ] Optimistic move with `onMutate`/`onError` rollback and a toast on failure (including the
      `422` invalid-transition message).
- [ ] Keyboard alternative: a card menu with a status selector, so the board is usable without
      a mouse.
- [ ] Reuse the same filters as the list; the board honours the URL filter state.
- [ ] Column headers show counts; columns scroll independently; cards are memoized.
- [ ] Loading skeleton per column, empty column states, error state.

## Acceptance criteria

- [ ] Dragging a card moves it instantly and persists; the server response reconciles it.
- [ ] A rejected transition snaps the card back and explains why.
- [ ] The board is fully operable with the keyboard.
- [ ] Dragging one card does not re-render other columns (verify with the profiler).

## Technical notes

Ordering within a column is deliberately not persisted — adding a rank field means fractional
indexing and rebalancing, which is a project of its own. Sort by priority then updated date and
say so in the UI.

## Learning goals

Drag & drop accessibility, optimistic mutation lifecycle, React render performance, scoping a
feature down to what is actually needed.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3.

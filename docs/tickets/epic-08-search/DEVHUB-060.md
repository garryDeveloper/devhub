# DEVHUB-060 — Web command palette (⌘K)

|  |  |
|---|---|
| **Epic** | EPIC 8 — Search |
| **Phase** | 2 — Product differentiation |
| **Priority** | P1 |
| **Size** | M |
| **Depends on** | DEVHUB-059, DEVHUB-044 |
| **Specs** | [`screens-and-navigation.md`](../../screens-and-navigation.md) §4 |

## Context

The fastest path to anything in the app, and the feature that makes DevHub feel like a developer
tool rather than a form over a database.

## Scope

**In:** global palette with debounced search, grouped results, keyboard navigation, recents.
**Out:** command actions ("create issue", "go to settings") — a natural follow-up, not this
ticket.

## Tasks

- [ ] Global ⌘K / Ctrl+K listener (ignored while typing in an input) opening a modal palette.
- [ ] Debounce 250 ms, abort in-flight requests on change.
- [ ] Grouped results (Issues, Projects, Releases) with keys, statuses and project context.
- [ ] Keyboard: ↑/↓ across groups, ↵ opens, Esc closes, focus restored to the trigger.
- [ ] Recent searches (and recently opened issues) from `localStorage` when the input is empty.
- [ ] Loading, "no results" and error states inside the palette.
- [ ] Highlight matched substrings in the result rows.

## Acceptance criteria

- [ ] ⌘K opens from anywhere in the app, including inside the issue drawer.
- [ ] Typing quickly issues one request per pause, not one per keystroke.
- [ ] The palette is fully usable without a mouse.
- [ ] Selecting a result navigates and closes the palette.

## Technical notes

The keyboard listener must ignore the shortcut while focus is in a text field, or users cannot
type a literal ⌘K sequence in a comment. Also handle the case where the palette opens over an
already-open drawer — focus restoration must go back to the right element.

## Learning goals

Global keyboard shortcuts, request cancellation, accessible modal and listbox patterns.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3.

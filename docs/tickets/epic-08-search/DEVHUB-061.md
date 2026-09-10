# DEVHUB-061 — Mobile search

|  |  |
|---|---|
| **Epic** | EPIC 8 — Search |
| **Phase** | 2 — Product differentiation |
| **Priority** | P2 |
| **Size** | S |
| **Depends on** | DEVHUB-059, DEVHUB-047 |
| **Specs** | [`mobile-architecture.md`](../../tech-specs/mobile-architecture.md) §7 |

## Context

On mobile, search is mostly "find that issue I was just talking about". Recents matter more than
ranking sophistication.

## Scope

**In:** search screen with debounced input, grouped results, recent searches.
**Out:** voice input, filters inside search.

## Tasks

- [ ] Search entry point in the Home tab header and the project screen.
- [ ] Debounced (300 ms) input with a clear button and an activity indicator.
- [ ] Grouped, sectioned results using `SectionList`.
- [ ] Recent searches stored locally, tappable, with a clear-all action.
- [ ] Tapping a result navigates to the right detail screen.
- [ ] Empty ("No results for …"), loading and error states; dismiss the keyboard on scroll.

## Acceptance criteria

- [ ] Results appear within a moment of stopping typing, without a request per keystroke.
- [ ] Recents persist across app restarts.
- [ ] Every result type navigates to a working screen.

## Technical notes

A slightly longer debounce than web is right here: mobile typing is slower and mobile networks
are worse, so 300 ms costs nothing perceptible and saves several requests.

## Learning goals

SectionList, debouncing on mobile, local persistence of small preferences.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §4.

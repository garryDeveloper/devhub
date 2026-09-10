# DEVHUB-057 — Mobile comments

|  |  |
|---|---|
| **Epic** | EPIC 7 — Comments & activity feed |
| **Phase** | 1 — Local foundation |
| **Priority** | P1 |
| **Size** | S |
| **Depends on** | DEVHUB-047, DEVHUB-053 |
| **Specs** | [`mobile-architecture.md`](../../tech-specs/mobile-architecture.md) §7 |

## Context

Commenting from a phone is one of the few things mobile genuinely does better than a laptop —
it happens in the moment.

## Scope

**In:** comment list and composer on the issue detail, activity shown compactly, edit/delete.
**Out:** mentions autocomplete (nice-to-have; plain `@name` typing is fine).

## Tasks

- [ ] Comments section on `IssueDetail`, newest last, with "Load earlier".
- [ ] Keyboard-aware composer pinned above the keyboard; send button disabled while empty or
      pending.
- [ ] Optimistic append with rollback and a retry affordance on failure.
- [ ] Long-press own comment → edit / delete action sheet.
- [ ] Activity entries rendered as compact single lines, visually distinct from comments.
- [ ] Markdown rendered safely; links open in the system browser.

## Acceptance criteria

- [ ] The composer is never hidden behind the keyboard on either platform.
- [ ] A failed comment stays visible with a retry option instead of vanishing.
- [ ] Only own comments expose edit/delete.

## Technical notes

`KeyboardAvoidingView` behaves differently on iOS and Android — test both. This is the single
most common source of "the app feels broken" on mobile.

## Learning goals

Keyboard handling in React Native, optimistic UI with retry, action sheets.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §4.

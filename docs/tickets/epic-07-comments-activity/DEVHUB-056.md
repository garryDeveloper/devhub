# DEVHUB-056 — Web comments and activity feed

|  |  |
|---|---|
| **Epic** | EPIC 7 — Comments & activity feed |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | M |
| **Depends on** | DEVHUB-046, DEVHUB-053, DEVHUB-055 |
| **Specs** | [`screens-and-navigation.md`](../../screens-and-navigation.md) §4 |

## Context

The lower half of the issue drawer: a single chronological stream that mixes what people said
with what changed.

## Scope

**In:** merged comment/activity timeline, composer, edit/delete, mention rendering, markdown.
**Out:** rich-text editor, attachments (DEVHUB-092), real-time updates.

## Tasks

- [ ] Fetch comments and activities, merge by timestamp, render one timeline.
- [ ] Activity entries render as one-line sentences ("Dario changed status Todo → In Progress").
- [ ] Comment composer: markdown textarea with a preview toggle, ⌘↵ to submit, optimistic append.
- [ ] Edit in place for own comments, with an "(edited)" marker; delete with confirmation.
- [ ] Render mentions as links; sanitize all rendered markdown.
- [ ] "Load more" for long histories; relative timestamps with absolute tooltips.
- [ ] Loading, empty ("No activity yet") and error states.

## Acceptance criteria

- [ ] Comments and activities interleave correctly by time.
- [ ] Posting a comment appears immediately and rolls back on failure.
- [ ] Only the author sees edit/delete on their comments.
- [ ] Markdown cannot inject HTML or scripts (tested explicitly).

## Technical notes

Two paginated sources in one visual list is the tricky part: page each independently and merge
the loaded windows, rather than trying to keep a single merged cursor.

## Learning goals

Merging heterogeneous paginated data, optimistic list appends, safe markdown rendering, timeline
UX.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3.

# DEVHUB-054 — Issue activity recording

|  |  |
|---|---|
| **Epic** | EPIC 7 — Comments & activity feed |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | M |
| **Depends on** | DEVHUB-042 |
| **Specs** | [`domain-model.md`](../../domain-model.md) §4, [`backend-architecture.md`](../../tech-specs/backend-architecture.md) §7 |

## Context

The activity feed is the audit trail that makes DevHub trustworthy. It is produced by domain
events, not by controllers remembering to write a row.

## Scope

**In:** `IssueActivity` entity, event handlers for every issue event, transactional guarantees.
**Out:** the activity endpoint (DEVHUB-055) and UI (DEVHUB-056), project-level feed.

## Tasks

- [ ] `IssueActivity` entity + migration; append-only by convention and by repository design.
- [ ] Handlers for `IssueCreated`, `StatusChanged`, `PriorityChanged`, `AssigneeChanged`,
      `LabelAdded/Removed`, `TitleChanged`, `DescriptionChanged`, `DueDateChanged`,
      `CommentCreated`, `Archived`, `LinkedToRelease`.
- [ ] Store human-readable `oldValue`/`newValue` (display names, not raw ids) plus ids in
      `metadata` for linking.
- [ ] Write activity rows **inside** the same transaction as the change that produced them.
- [ ] Never update or delete an activity row — assert this in a test.
- [ ] Tests: each event type produces exactly one correctly shaped row; a failed business
      operation produces none.

## Acceptance criteria

- [ ] Every user-visible change to an issue appears in its activity.
- [ ] A rolled-back operation leaves no activity row.
- [ ] Activity rows are never modified after creation.

## Technical notes

Activity is the one event consumer that must be transactional — a lost audit entry is a silent
data-integrity problem. Notifications, by contrast, are best-effort and must not roll back a
business write.

## Learning goals

Event-driven side effects, transactional boundaries, append-only audit design.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

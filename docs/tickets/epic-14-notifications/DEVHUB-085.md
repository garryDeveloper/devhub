# DEVHUB-085 — Notification entity and service

|  |  |
|---|---|
| **Epic** | EPIC 14 — Notifications |
| **Phase** | 5 — Async & polish |
| **Priority** | P1 |
| **Size** | M |
| **Depends on** | DEVHUB-054, DEVHUB-069 |
| **Specs** | [`domain-model.md`](../../domain-model.md), [`backend-architecture.md`](../../tech-specs/backend-architecture.md) §7 |

## Context

In-app notifications, driven entirely by domain events already being raised. This is where the
"best-effort side effect" rule gets exercised.

## Scope

**In:** `Notification` entity, notification service, event handlers, fan-out rules.
**Out:** email, push (post-MVP), digest batching.

## Tasks

- [ ] `Notification` entity + migration, with the partial unread index.
- [ ] `INotificationService.CreateAsync(userId, type, title, body, target)`.
- [ ] Handlers: `IssueAssigneeChanged` → assignee; `CommentCreated` → mentioned users;
      `IssueStatusChanged` → assignee and reporter; `DeploymentStatusChanged` (Failed, or
      Succeeded on Production) → project members; `CicdRunCompleted` (Failed) → project members.
- [ ] Never notify the actor about their own action.
- [ ] De-duplicate: no more than one notification per (user, type, target) within 60 seconds.
- [ ] Handlers are wrapped so a failure logs a warning and never rolls back the business write.
- [ ] Tests: each rule, self-action suppression, de-duplication, handler failure isolation.

## Acceptance criteria

- [ ] Assigning an issue to someone else notifies them; assigning to yourself does not.
- [ ] A failed production deployment notifies every project member once.
- [ ] A thrown exception inside a notification handler does not fail the request.

## Technical notes

This is the concrete case for "activity is transactional, notifications are best-effort". Making
a notification failure roll back a status change would be an availability bug caused by a
non-essential feature.

## Learning goals

Event fan-out, idempotency/de-duplication windows, isolating non-critical side effects.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

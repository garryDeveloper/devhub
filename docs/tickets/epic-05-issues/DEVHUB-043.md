# DEVHUB-043 — Archive and delete issue

|  |  |
|---|---|
| **Epic** | EPIC 5 — Issues |
| **Phase** | 1 — Local foundation |
| **Priority** | P1 |
| **Size** | XS |
| **Depends on** | DEVHUB-041 |
| **Specs** | [`api-endpoints.md`](../../tech-specs/api-endpoints.md) §4 |

## Context

Archive is the everyday action; permanent deletion exists for mistakes and is owner-only.

## Scope

**In:** `DELETE /api/issues/{id}` (archive) and `?permanent=true` (hard delete).
**Out:** trash/restore UI, bulk delete.

## Tasks

- [ ] Archive sets `archivedAt`, records activity, returns `204`.
- [ ] Add an unarchive path (`PATCH /api/issues/{id}` with `archived: false`, or a dedicated
      endpoint — pick one and document it).
- [ ] `?permanent=true`: owner only; cascade comments, activities, labels and attachments
      (including their S3 objects once EPIC 15 exists).
- [ ] Archived issues are excluded from lists, boards, search and dashboard counts by default.
- [ ] Tests: archive hides it from lists; permanent delete removes children; a member gets `403`
      on permanent delete; the issue number is not reused afterwards.

## Acceptance criteria

- [ ] Archiving is reversible; permanent deletion is not and is owner-only.
- [ ] No orphaned comments, activities or attachment rows remain after a permanent delete.
- [ ] Archived issues disappear from every default listing.

## Technical notes

Cascade deletes are configured in EF **and** in the database. If the attachment S3 objects are
not deleted alongside the row, you have created an invisible, growing storage bill.

## Learning goals

Soft vs hard delete, cascade configuration, cleaning up external side effects.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

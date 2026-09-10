# DEVHUB-037 — Issue key generation

|  |  |
|---|---|
| **Epic** | EPIC 5 — Issues |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-036 |
| **Specs** | [`database-schema.md`](../../tech-specs/database-schema.md) §4 |

## Context

`DEV-42` is the identifier humans use. It must be unique, gapless enough to look sane, never
reused, and correct under concurrent creation.

## Scope

**In:** per-project atomic sequence, key composition, uniqueness enforcement, concurrency test.
**Out:** renumbering, key change on project rename (impossible — the key is immutable).

## Tasks

- [ ] Implement the atomic increment: `UPDATE projects SET issue_sequence = issue_sequence + 1
      WHERE id = @id RETURNING issue_sequence`, executed in the issue-creation transaction.
- [ ] Compose `key = project.Key + "-" + number` and persist both `number` and `key`.
- [ ] Unique indexes `(project_id, number)` and `(key)`.
- [ ] Integration test: create 50 issues in parallel in one project → 50 distinct sequential
      keys, no duplicates, no gaps caused by the code itself.
- [ ] Test: deleting an issue does not free its number for reuse.

## Acceptance criteria

- [ ] Concurrent creation never produces a duplicate key.
- [ ] Keys are sequential from 1 within a project and independent across projects.
- [ ] The key is stored, not computed on read (search and links depend on it).

## Technical notes

`SELECT max(number)+1` is the wrong answer — it races. The `UPDATE … RETURNING` takes a row lock
for the duration of the transaction, which is exactly the serialization needed. A dedicated
PostgreSQL sequence per project would also work but creates schema objects at runtime; not worth
it here.

## Learning goals

Concurrency and row-level locking, why read-then-write is unsafe, testing for races on purpose.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

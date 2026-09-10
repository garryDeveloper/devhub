# DEVHUB-006 — Configure EF Core, DbContext and the initial migration

|  |  |
|---|---|
| **Epic** | EPIC 1 — Foundation & project setup |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | M |
| **Depends on** | DEVHUB-002, DEVHUB-005 |
| **Specs** | [`database-schema.md`](../../tech-specs/database-schema.md) |

## Context

This ticket sets the conventions every future entity inherits: snake_case naming, UTC
timestamps, enums as text, uuid keys. Getting them right once avoids a dozen corrective
migrations.

## Scope

**In:** Npgsql provider, `DevHubDbContext`, naming conventions, `IEntityTypeConfiguration`
pattern, domain event collection hook, the first migration (users table only).
**Out:** the rest of the entities (their own epics), repositories beyond the base interface.

## Tasks

- [ ] Add `Npgsql.EntityFrameworkCore.PostgreSQL` and `EFCore.NamingConventions` to
      `DevHub.Infrastructure`.
- [ ] Create `DevHubDbContext` with `UseSnakeCaseNamingConvention()`.
- [ ] Configure `citext` and enable it in the model (`HasPostgresExtension`).
- [ ] Add a convention: all `DateTime` mapped as `timestamptz`, all enums as `text` with a
      `HasConversion<string>()`.
- [ ] Override `SaveChangesAsync` to stamp `CreatedAt`/`UpdatedAt` and collect domain events.
- [ ] Add `IEntityTypeConfiguration` for `User` and apply configurations from the assembly.
- [ ] Add a design-time factory so `dotnet ef` works without running the app.
- [ ] Create the initial migration and apply it.
- [ ] Add an integration-test base that applies migrations to a Testcontainers database.

## Acceptance criteria

- [ ] `dotnet ef database update` creates a `users` table with snake_case columns.
- [ ] Columns are `timestamptz`; `email` is `citext` and uniquely indexed.
- [ ] Applying migrations to an empty database succeeds from scratch in CI.
- [ ] Saving an entity sets `created_at`/`updated_at` without the caller doing it.

## Technical notes

- Enums as `text`, not `int`: readable in psql and immune to accidental reordering of the C# enum.
- Guid v7 for ids (`Guid.CreateVersion7()` in .NET 9, or a small helper in .NET 8) keeps B-tree
  index inserts sequential; random v4 guids fragment the index.
- Do not put migrations in the Api project. They belong with the persistence code.

## Learning goals

EF Core model configuration vs data annotations, migrations as source-controlled schema history,
change tracking, why UTC-only timestamps prevent an entire class of bugs.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

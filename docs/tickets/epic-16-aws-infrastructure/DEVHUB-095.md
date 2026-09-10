# DEVHUB-095 — RDS PostgreSQL instance

|  |  |
|---|---|
| **Epic** | EPIC 16 — AWS infrastructure |
| **Phase** | 3 — AWS |
| **Priority** | P1 |
| **Size** | M |
| **Depends on** | DEVHUB-094 |
| **Specs** | [`aws-architecture.md`](../../tech-specs/aws-architecture.md) §3 |

## Context

Moving the database to a managed service. The one setting that matters most is the one that is
easiest to get wrong: public accessibility.

## Scope

**In:** subnet group, parameter group, RDS instance, backups, connection verification, migration
run.
**Out:** Multi-AZ, read replicas, proxy (documented as later).

## Tasks

- [ ] DB subnet group over the two **private** subnets.
- [ ] Create `devhub-db`: PostgreSQL 16, `db.t4g.micro`, 20 GB gp3, encryption on,
      **Public access: No**, `sg-db`, 7-day automated backups.
- [ ] Parameter group with `log_min_duration_statement = 500`.
- [ ] Store the connection string in SSM (DEVHUB-097 formalizes this; create the parameter now).
- [ ] Connect from a bastion or SSM session to run `dotnet ef database update` — never by
      temporarily making the instance public.
- [ ] Verify the app connects from the application security group and nothing else can.
- [ ] Document the restore-from-snapshot procedure and test it once.
- [ ] Set an Npgsql `Maximum Pool Size` consistent with the instance's connection limit.

## Acceptance criteria

- [ ] The instance is not reachable from the public internet (verified by attempting it).
- [ ] Migrations apply successfully to the RDS database.
- [ ] Automated backups are on with a documented restore procedure you have actually run.
- [ ] Connection details exist only in SSM, never in the repository.

## Technical notes

`db.t4g.micro` has a low `max_connections`. A default .NET pool per instance can exhaust it
quickly — set the pool size explicitly and know the number.

## Learning goals

RDS networking, subnet and parameter groups, backup and restore, connection pooling limits, why
"just make it public for a minute" is the most dangerous shortcut in cloud work.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §5.

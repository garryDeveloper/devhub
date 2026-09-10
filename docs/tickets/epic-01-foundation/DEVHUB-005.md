# DEVHUB-005 — Local PostgreSQL with Docker Compose

|  |  |
|---|---|
| **Epic** | EPIC 1 — Foundation & project setup |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-001 |
| **Specs** | [`local-development.md`](../../tech-specs/local-development.md) §3 |

## Context

A reproducible database is the difference between "it works today" and "it works on a fresh
clone". Compose also gives Adminer for looking at rows while learning EF Core.

## Scope

**In:** `infrastructure/docker-compose.yml` with PostgreSQL 16 and Adminer, named volume,
healthcheck.
**Out:** MinIO (added in DEVHUB-089), RDS (DEVHUB-095).

## Tasks

- [ ] Write `infrastructure/docker-compose.yml` per the spec (postgres:16-alpine + adminer).
- [ ] Named volume `pgdata` so data survives `down`.
- [ ] `pg_isready` healthcheck so dependent services wait correctly.
- [ ] Document up/down/logs/wipe commands in the README.
- [ ] Confirm the connection string used by the API matches the compose credentials.

## Acceptance criteria

- [ ] `docker compose up -d` gives a healthy PostgreSQL on 5432.
- [ ] Adminer on `http://localhost:5050` connects to the database.
- [ ] `docker compose down` then `up` preserves data; `down -v` wipes it.
- [ ] A teammate (or a fresh clone) can start the database with one command.

## Technical notes

- Pin the major version (`16-alpine`). `latest` will change under you and break a migration one
  morning.
- Local credentials are deliberately trivial (`devhub`/`devhub`) — they are not secrets, and
  nothing outside your machine can reach the port. Never reuse them anywhere else.

## Learning goals

Compose services, volumes vs bind mounts, healthchecks, why pinning image versions matters.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1.

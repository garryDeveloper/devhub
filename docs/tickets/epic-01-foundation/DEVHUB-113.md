# DEVHUB-113 — Discoverable local secrets, one source for the API and `dotnet ef`

|  |  |
|---|---|
| **Epic** | EPIC 1 — Foundation & project setup |
| **Phase** | 1 — Local foundation |
| **Priority** | P1 |
| **Size** | S |
| **Depends on** | DEVHUB-006, DEVHUB-007 |
| **Specs** | [`local-development.md`](../../tech-specs/local-development.md) §4 |

## Context

Two problems with the local configuration set up in DEVHUB-007:

1. **The API and `dotnet ef` read the connection string from different places.** The API reads
   user-secrets; `DevHubDbContextFactory` read only the `ConnectionStrings__Default` environment
   variable and otherwise fell back to `localhost:5432`. On a machine whose compose stack was
   moved to 5433 (another project already on 5432), `dotnet ef database update` silently targeted
   the wrong server, the `DEVHUB023_Workspaces` migration never reached the DevHub database, and
   the API failed at runtime with `relation "workspaces" does not exist`.
2. **The required settings are only discoverable by reading the docs.** This is a public
   repository: nothing real may be committed, yet anyone cloning it must be able to see at a
   glance which settings the API needs to run locally.

## Decision

**Keep user-secrets as the only place local secrets live, add a committed example, and make the
design-time factory read the same user-secrets as the API.**

Alternatives considered:

- *A git-ignored `appsettings.Local.json` plus a committed `appsettings.Local.example.json`.*
  Rejected: the secrets would live inside the working tree, one `.gitignore` mistake away from a
  public commit, and the factory (in Infrastructure) would still need a fragile relative path to a
  file in DevHub.Api. User-secrets are stored outside the repository, so they cannot be committed.
- *Delete the factory and run `dotnet ef` with DevHub.Api as the startup project.* Rejected for
  now: it reverses DEVHUB-006's decision that schema work must not depend on the application
  booting.

Consequences:

- `api/secrets.example.json` lists every secret the API needs with **placeholder values that fail
  startup validation**, so a forgotten placeholder stops the app with a message naming the key
  instead of running with a publicly known secret. One command loads it into user-secrets; the
  random secrets are then generated locally.
- The example lives in `api/`, not in `src/DevHub.Api/`, so the Web SDK does not publish it into
  the container image as content.
- DevHub.Infrastructure declares the **same `UserSecretsId`** as DevHub.Api, so the factory reads
  the same store. A test fails if the two ids ever differ.
- Precedence in the factory: environment variable → user-secrets → the compose default
  (`localhost:5432`). The environment variable still wins, as before.
- New package in DevHub.Infrastructure: `Microsoft.Extensions.Configuration.UserSecrets` (plus
  `…Configuration.EnvironmentVariables`). Used only by the design-time factory.

## Scope

**In:** `api/secrets.example.json`, factory configuration, `UserSecretsId` in Infrastructure, the
id-equality test, `local-development.md` §4 and CLAUDE.md §6.
**Out:** deployed environments (SSM, DEVHUB-097), web/mobile `.env.example` (already exist).

## Tasks

- [x] Add `api/secrets.example.json` with every user-secret the API requires, placeholder values
      only, and no real data.
      *Verified that `cat secrets.example.json | dotnet user-secrets set` loads all 5 keys.*
- [x] Add the shared `UserSecretsId` to DevHub.Infrastructure and read it in
      `DevHubDbContextFactory` (env var → user-secrets → compose default).
- [x] Test that DevHub.Api and DevHub.Infrastructure declare the same `UserSecretsId`.
      *`DevHub.ArchitectureTests/UserSecretsIdTests`.*
- [x] Document the setup in `local-development.md` §4: a table of every setting (required, what it
      is, where it comes from) and the bash/PowerShell commands to load the example and generate
      the secrets.
      *Also three new §9 troubleshooting rows.*
- [x] Update CLAUDE.md §6 commands.

## Acceptance criteria

- [x] No real credential, token or personal data is committed; every secret value in the example
      is a placeholder.
      *The connection string and MinIO values are the throwaway local defaults already public in
      `docker-compose.yml` and this doc; the two real secrets are `REPLACE_ME`.*
- [x] Starting the API with an unreplaced placeholder fails at startup naming the setting.
      *`OptionsValidationException … JwtOptions … 'Secret' … minimum length of '32'`.*
- [x] With the connection string only in user-secrets (no environment variable),
      `dotnet ef migrations list` reaches the same database as the API.
- [ ] A fresh clone can go from zero to a running API by following §4 alone.
      *Each step was verified on its own; a full run on a clean clone is still to do.*

## Learning goals

Configuration providers and their precedence, design-time vs runtime configuration, keeping
secrets out of a public repository without hiding what is required.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

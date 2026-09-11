# CLAUDE.md — DevHub

Guidance for Claude Code (and any AI assistant) working in this repository.

---

## 1. What this project is

**DevHub** is a developer-oriented project management and deployment workspace. It combines
lightweight issue tracking with deployment visibility: environments, releases, deployment
history and CI/CD activity.

The differentiator is the link between **work items and software delivery**. The dashboard must
answer: *"What is being worked on, what was shipped, and is production healthy?"*

This is a **learning project first, portfolio project second**. The owner is deliberately using it
to learn AWS, React, React Native and ASP.NET Core. That changes how you should help — see
§7 "How to collaborate with the repo owner".

Full product context: [`docs/product-spec.md`](docs/product-spec.md).

---

## 2. Repository layout

```text
devhub/
├── CLAUDE.md                 # this file
├── api/                      # ASP.NET Core backend (DevHub.sln)
├── web/                      # React + TypeScript web app
├── mobile/                   # React Native + TypeScript app
├── infrastructure/           # docker-compose, IaC (added in EPIC 16)
├── .github/workflows/        # GitHub Actions (added in EPIC 17)
└── docs/
    ├── product-spec.md
    ├── domain-model.md
    ├── screens-and-navigation.md
    ├── user-flows.md
    ├── roadmap.md
    ├── definition-of-done.md
    ├── glossary.md
    ├── tech-specs/
    │   ├── backend-architecture.md
    │   ├── frontend-web-architecture.md
    │   ├── mobile-architecture.md
    │   ├── api-conventions.md
    │   ├── api-endpoints.md
    │   ├── database-schema.md
    │   ├── auth-spec.md
    │   ├── search-spec.md
    │   ├── webhooks-spec.md
    │   ├── storage-s3-spec.md
    │   ├── observability-spec.md
    │   ├── testing-strategy.md
    │   ├── local-development.md
    │   ├── aws-architecture.md
    │   └── cicd-architecture.md
    └── tickets/
        ├── README.md         # full backlog index
        └── epic-XX-*/DEVHUB-XXX.md
```

`api/`, `web/` and `mobile/` are intentionally empty at bootstrap. They are created by
`DEVHUB-002`, `DEVHUB-008` and `DEVHUB-009`.

---

## 3. Target stack

| Area | Technology |
|---|---|
| Web | React 18 + TypeScript + Vite |
| Mobile | React Native + TypeScript (Expo) |
| Backend | ASP.NET Core 10 Web API (modular monolith) |
| ORM | EF Core |
| Database | PostgreSQL 16 (local: Docker; AWS: RDS) |
| Object storage | AWS S3 |
| Compute | AWS Elastic Beanstalk (ECS later, optional) |
| CDN | CloudFront |
| Async | AWS Lambda + SQS |
| Observability | CloudWatch |
| CI/CD | GitHub Actions + OIDC |
| Containers | Docker |

Do not introduce new infrastructure (Redis, Elasticsearch, Kubernetes, microservices,
message brokers other than SQS) without an explicit decision recorded in a ticket.

---

## 4. Architecture rules (non-negotiable)

### Backend

- **Modular monolith**, not microservices.
- Layers and dependency direction:

  ```text
  DevHub.Api  →  DevHub.Application  →  DevHub.Domain
  DevHub.Infrastructure  →  DevHub.Application + DevHub.Domain
  ```

- `DevHub.Domain` must **not** reference EF Core, ASP.NET, AWS SDK or any external package.
  Entities enforce their own invariants; no public setters on aggregate state.
- Controllers are thin: validate route/model binding, dispatch a command/query, map to DTO.
- Business rules live in the domain; orchestration lives in Application handlers.
- Every write that changes user-visible state must raise the domain event that produces the
  `issue_activities` row (see [`docs/tech-specs/backend-architecture.md`](docs/tech-specs/backend-architecture.md)).
- Errors are returned as RFC 7807 `ProblemDetails`. Never leak stack traces.

### Frontend (web and mobile)

- Feature-first folder structure (`features/issues/…`), not layer-first.
- Server state goes through TanStack Query. Client state stays local or in a small store.
  Do not put server data in a global store.
- Every list/detail view implements **loading, empty and error** states. This is part of the
  Definition of Done, not a nice-to-have.
- API types are shared conventions, not guesses: mirror the DTOs in
  [`docs/tech-specs/api-endpoints.md`](docs/tech-specs/api-endpoints.md).

### Data

- All schema changes go through an EF Core migration. Never edit the database by hand.
- All ids are `uuid` (v7 preferred for index locality). Timestamps are `timestamptz`, stored UTC.
- Soft-delete only where a ticket says so (`archived_at`); otherwise delete rows.

---

## 5. Conventions

### Naming

- C#: PascalCase types/members, `_camelCase` private fields, `Async` suffix on async methods.
- TypeScript: PascalCase components/types, camelCase functions/variables, `use*` for hooks.
- Database: `snake_case` tables and columns, plural table names.
- API routes: kebab/lowercase plural nouns under `/api`, e.g. `/api/projects/{projectId}/issues`.
- Branches: `feat/DEVHUB-042-issue-board`, `fix/…`, `chore/…`.
- Commits: Conventional Commits, with the ticket id — `feat(issues): add status patch endpoint (DEVHUB-042)`.

### Testing

- Unit tests for domain behaviour and application handlers with real business rules.
- Integration tests for endpoints against a real PostgreSQL (Testcontainers), not mocks.
- Do not write tests that only assert the mock was called.
- See [`docs/tech-specs/testing-strategy.md`](docs/tech-specs/testing-strategy.md).

### Security

- Never commit credentials, connection strings, `.env` files or AWS keys.
- Local secrets go in .NET user-secrets / `.env.local` (git-ignored).
- AWS access from CI uses **OIDC + assumed role**, never long-lived access keys.
- Every endpoint declares its authorization requirement explicitly. Workspace/project scoping
  is checked in the Application layer, not only in the controller.

---

## 6. Commands

Fill these in as the projects are scaffolded; keep this table accurate.

```bash
# Backend
cd api && dotnet restore
dotnet build DevHub.sln
dotnet test
dotnet run --project src/DevHub.Api
dotnet ef migrations add <Name> -p src/DevHub.Infrastructure -s src/DevHub.Api
dotnet ef database update -p src/DevHub.Infrastructure -s src/DevHub.Api

# Local infrastructure
docker compose -f infrastructure/docker-compose.yml up -d

# Web
cd web && npm install && npm run dev && npm run lint && npm run build

# Mobile
cd mobile && npm install && npm start
```

---

## 7. How to collaborate with the repo owner

The owner is learning. **The goal is not to finish fast — it is to understand.**

**Do:**

- Explain the trade-offs of an approach before writing code.
- Work **one ticket at a time**. Ask which ticket before starting.
- Propose the design (domain behaviour, authorization, DTOs, persistence, error cases) and let
  the owner decide before implementing.
- Point out what the ticket teaches (e.g. "this is where you'll see how EF change tracking works").
- Review code the owner wrote and be direct about problems.
- Generate tests, docs, acceptance criteria, edge cases and alternatives freely.

**Do not:**

- Generate a whole epic in one shot.
- Silently scaffold large amounts of code the owner has not asked for.
- Add dependencies, abstractions or patterns that no ticket requires.
- Skip the "why". A working implementation the owner cannot explain is a failed ticket.

Workflow for every ticket: **Understand → Design → Implement → Test → Review → Document.**

---

## 8. Working a ticket

1. Read the ticket in `docs/tickets/epic-XX-*/DEVHUB-XXX.md`.
2. Check its **Depends on** list is done.
3. Read the tech-specs it references.
4. Restate the plan (files to touch, contracts, tests) and confirm with the owner.
5. Implement in small commits referencing the ticket id.
6. Verify every acceptance criterion, then check it against
   [`docs/definition-of-done.md`](docs/definition-of-done.md).
7. Update docs if the implementation changed a contract. **Docs drift is a bug.**

If reality diverges from a spec, update the spec in the same PR — the docs in `docs/` are the
source of truth for this repo, and they must stay true.

---

## 9. Order of work

Do **not** implement epics in numeric order. Follow
[`docs/roadmap.md`](docs/roadmap.md):

1. **Phase 1 — Local foundation:** foundation, auth, workspaces, projects, issues, labels, comments.
2. **Phase 2 — Product differentiation:** search, releases, environments, deployments, CI/CD runs, dashboard.
3. **Phase 3 — AWS:** S3 attachments, deployment, IAM, RDS, CloudWatch, VPC.
4. **Phase 4 — CI/CD:** GitHub Actions, OIDC, staging automation, production approval, callbacks.
5. **Phase 5 — Async & cloud features:** SQS, Lambda, S3 events, notifications, background jobs.

The first portfolio-quality milestone is the end-to-end flow: create issue → work on it →
release it → push code → GitHub Actions deploys → DevHub receives the deployment webhook →
environment and dashboard update.

# Roadmap by phases

Epics are **not** implemented in numeric order. This file is the execution order.

Priority convention: `P0` = required for MVP · `P1` = important · `P2` = post-MVP.

---

## Overview

| Phase | Theme | Tickets | Outcome |
|---|---|---|---|
| 1 | Local foundation | DEVHUB-001 → 057 | A working issue tracker running locally |
| 2 | Product differentiation | DEVHUB-058 → 084 | An engineering workspace, not a task manager |
| 3 | AWS | DEVHUB-089 → 100 | Running on AWS with S3, RDS, IAM, CloudWatch |
| 4 | CI/CD | DEVHUB-101 → 107 | Automated pipeline that reports back into DevHub |
| 5 | Async & polish | DEVHUB-085 → 088, 108 → 112 | Notifications, observability, background processing |

---

## Phase 1 — Local foundation

**Goal:** React → .NET → PostgreSQL working end-to-end on your machine. At the end of this phase
DevHub is already useful as a personal issue tracker.

| Order | Epic | Tickets |
|---|---|---|
| 1 | EPIC 1 — Foundation & project setup | DEVHUB-001 … 012 |
| 2 | EPIC 2 — Authentication & user account | DEVHUB-013 … 022 |
| 3 | EPIC 3 — Workspaces & membership | DEVHUB-023 … 029 |
| 4 | EPIC 4 — Projects | DEVHUB-030 … 035 |
| 5 | EPIC 5 — Issues | DEVHUB-036 … 047 |
| 6 | EPIC 6 — Labels, priorities & filtering | DEVHUB-048 … 052 |
| 7 | EPIC 7 — Comments & activity feed | DEVHUB-053 … 057 |

**Exit criteria**

- Register, log in, create a workspace, create a project, create and work issues on a board.
- Refresh-token flow works on web and mobile.
- Integration tests run against a real PostgreSQL in CI.
- No AWS account required yet.

**What you should be able to explain:** layered architecture and dependency direction, EF Core
change tracking and migrations, JWT vs refresh tokens, why authorization is checked in the
application layer, how TanStack Query caches and invalidates server state.

---

## Phase 2 — Product differentiation

**Goal:** the features that make DevHub *DevHub*. Still local, but the domain becomes interesting.

| Order | Epic | Tickets |
|---|---|---|
| 8 | EPIC 8 — Search | DEVHUB-058 … 061 |
| 9 | EPIC 9 — Environments | DEVHUB-062 … 065 |
| 10 | EPIC 10 — Deployments | DEVHUB-066 … 071 |
| 11 | EPIC 11 — Releases | DEVHUB-072 … 076 |
| 12 | EPIC 12 — CI/CD integration | DEVHUB-077 … 081 |
| 13 | EPIC 13 — Project dashboard | DEVHUB-082 … 084 |

**Exit criteria**

- Deployment webhook accepts a signed payload and updates environment health.
- Releases group delivered issues; the dashboard aggregates in a single call.
- Full-text search over issues, projects and releases without extra infrastructure.

**What you should be able to explain:** webhook signature validation and idempotency, derived
read models vs stored state, why the dashboard is one aggregate endpoint instead of ten calls,
PostgreSQL `tsvector` indexing.

---

## Phase 3 — AWS

**Goal:** move from localhost to a real cloud deployment, learning one service at a time.

| Order | Epic | Tickets |
|---|---|---|
| 14 | EPIC 15 — Attachments & S3 | DEVHUB-089 … 093 |
| 15 | EPIC 16 — AWS infrastructure | DEVHUB-094 … 100 |

Recommended sub-order inside EPIC 16: VPC (094) → RDS (095) → Elastic Beanstalk (096) →
configuration/SSM (097) → S3 + CloudFront for the SPA (098) → IAM hardening (099) →
CloudWatch (100).

**Exit criteria**

```text
React → S3 + CloudFront
.NET  → Elastic Beanstalk
DB    → RDS PostgreSQL (private subnet)
Files → S3 (private, presigned URLs)
Logs  → CloudWatch
```

**What you should be able to explain:** IAM roles vs users vs policies, trust policies, why
presigned URLs exist, security groups vs NACLs, public/private subnets, RDS networking, why the
application role must not be `AdministratorAccess`.

---

## Phase 4 — CI/CD

**Goal:** every push is tested and deployed, and DevHub sees its own deployments.

| Order | Epic | Tickets |
|---|---|---|
| 16 | EPIC 17 — CI/CD for DevHub | DEVHUB-101 … 107 |

**Exit criteria**

- PR workflow: lint, unit tests, integration tests, build, mobile checks.
- `main` → staging automatically; production behind a manual approval environment.
- GitHub authenticates to AWS via OIDC — **zero long-lived AWS keys in GitHub**.
- The deploy job posts a signed callback to `POST /api/webhooks/deployments`, so the flagship
  flow in [`user-flows.md`](user-flows.md#flow-4--successful-deployment-the-flagship-flow) is live.

**What you should be able to explain:** how OIDC federation replaces static credentials,
GitHub environments and approvals, artifact promotion, health-check gating.

---

## Phase 5 — Async, notifications & operations

**Goal:** the advanced-feature layer, plus the operational polish that makes it portfolio-ready.

| Order | Epic | Tickets |
|---|---|---|
| 17 | EPIC 14 — Notifications | DEVHUB-085 … 088 |
| 18 | EPIC 18 — Observability | DEVHUB-108 … 112 |
| 19 | Post-MVP async (SQS → Lambda, S3 events → thumbnails) | backlog |

**Exit criteria**

- In-app notifications for assignment, mentions, failed deployments and production deploys.
- Structured logs with a correlation id visible in CloudWatch.
- Error-rate and latency alarms that actually fire.

---

## MVP milestone

The first portfolio-quality milestone is Phases 1 + 2 plus enough of Phase 3/4 to demonstrate:

```text
Create project → create issue → work the issue → create release → push code
   → GitHub Actions runs → app deploys to AWS → DevHub receives the deployment event
   → production environment updates → dashboard shows it
```

That single flow demonstrates far more than a CRUD app. Everything after it is depth.

---

## Explicitly out of scope for the MVP

GitHub OAuth login, automatic PR/issue linking, real-time updates (SignalR/WebSockets), push
notifications, rich markdown editor, image processing, teams and granular RBAC, ECS/Fargate,
Terraform/CDK, Elasticsearch. Each becomes a ticket only after the MVP flow is green.

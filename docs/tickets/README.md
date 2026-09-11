# DevHub backlog

112 tickets across 18 epics. Work them in the order given by [`../roadmap.md`](../roadmap.md),
**not** in numeric order. Each ticket carries its own context, scope, tasks, acceptance criteria,
technical notes and learning goals, and defers the checklist to
[`../definition-of-done.md`](../definition-of-done.md).

Priority: `P0` required for MVP · `P1` important · `P2` post-MVP.
Size: XS (< 2 h) · S (half a day) · M (a day) · L (two days or more).

---

## Phase 1 — Local foundation

### EPIC 1 — Foundation & project setup · [`epic-01-foundation/`](epic-01-foundation/)

| Ticket | Title | P | Size |
|---|---|---|---|
| [DEVHUB-001](epic-01-foundation/DEVHUB-001.md) | Initialize monorepo structure | P0 | S |
| [DEVHUB-002](epic-01-foundation/DEVHUB-002.md) | Create the ASP.NET Core solution with layered projects | P0 | M |
| [DEVHUB-003](epic-01-foundation/DEVHUB-003.md) | Health endpoint and Swagger/OpenAPI | P0 | S |
| [DEVHUB-004](epic-01-foundation/DEVHUB-004.md) | Dockerize the API | P0 | S |
| [DEVHUB-005](epic-01-foundation/DEVHUB-005.md) | Local PostgreSQL with Docker Compose | P0 | S |
| [DEVHUB-006](epic-01-foundation/DEVHUB-006.md) | Configure EF Core, DbContext and the initial migration | P0 | M |
| [DEVHUB-007](epic-01-foundation/DEVHUB-007.md) | Configuration and secrets strategy | P0 | S |
| [DEVHUB-008](epic-01-foundation/DEVHUB-008.md) | Create the React web application shell | P0 | M |
| [DEVHUB-009](epic-01-foundation/DEVHUB-009.md) | Create the React Native application shell | P0 | M |
| [DEVHUB-010](epic-01-foundation/DEVHUB-010.md) | Linting, formatting and code style | P1 | S |
| [DEVHUB-011](epic-01-foundation/DEVHUB-011.md) | Set up the test projects | P0 | M |
| [DEVHUB-012](epic-01-foundation/DEVHUB-012.md) | Baseline GitHub Actions CI | P0 | M |

### EPIC 2 — Authentication & user account · [`epic-02-auth/`](epic-02-auth/)

| Ticket | Title | P | Size |
|---|---|---|---|
| [DEVHUB-013](epic-02-auth/DEVHUB-013.md) | User aggregate and password hashing | P0 | S |
| [DEVHUB-014](epic-02-auth/DEVHUB-014.md) | Registration endpoint | P0 | S |
| [DEVHUB-015](epic-02-auth/DEVHUB-015.md) | Login and JWT access tokens | P0 | M |
| [DEVHUB-016](epic-02-auth/DEVHUB-016.md) | Refresh token flow with rotation | P0 | M |
| [DEVHUB-017](epic-02-auth/DEVHUB-017.md) | Logout and session revocation | P0 | XS |
| [DEVHUB-018](epic-02-auth/DEVHUB-018.md) | Authorization middleware and endpoint protection | P0 | S |
| [DEVHUB-019](epic-02-auth/DEVHUB-019.md) | Current user endpoints (`/api/me`) | P0 | S |
| [DEVHUB-020](epic-02-auth/DEVHUB-020.md) | Web authentication flow | P0 | M |
| [DEVHUB-021](epic-02-auth/DEVHUB-021.md) | Web token refresh and expiry handling | P0 | S |
| [DEVHUB-022](epic-02-auth/DEVHUB-022.md) | Mobile authentication with secure storage | P0 | M |

### EPIC 3 — Workspaces & membership · [`epic-03-workspaces/`](epic-03-workspaces/)

| Ticket | Title | P | Size |
|---|---|---|---|
| [DEVHUB-023](epic-03-workspaces/DEVHUB-023.md) | Workspace and WorkspaceMember entities | P0 | S |
| [DEVHUB-024](epic-03-workspaces/DEVHUB-024.md) | Workspace CRUD endpoints | P0 | S |
| [DEVHUB-025](epic-03-workspaces/DEVHUB-025.md) | Workspace membership endpoints | P0 | S |
| [DEVHUB-026](epic-03-workspaces/DEVHUB-026.md) | Workspace authorization and scoping helper | P0 | S |
| [DEVHUB-027](epic-03-workspaces/DEVHUB-027.md) | Web workspace switcher | P0 | S |
| [DEVHUB-028](epic-03-workspaces/DEVHUB-028.md) | Web workspace settings and members screens | P1 | M |
| [DEVHUB-029](epic-03-workspaces/DEVHUB-029.md) | Mobile workspace selector and members view | P1 | S |

### EPIC 4 — Projects · [`epic-04-projects/`](epic-04-projects/)

| Ticket | Title | P | Size |
|---|---|---|---|
| [DEVHUB-030](epic-04-projects/DEVHUB-030.md) | Project and ProjectMember entities | P0 | S |
| [DEVHUB-031](epic-04-projects/DEVHUB-031.md) | Project CRUD endpoints | P0 | S |
| [DEVHUB-032](epic-04-projects/DEVHUB-032.md) | Project membership endpoints | P1 | S |
| [DEVHUB-033](epic-04-projects/DEVHUB-033.md) | Web project list and creation | P0 | S |
| [DEVHUB-034](epic-04-projects/DEVHUB-034.md) | Web project settings screen | P1 | S |
| [DEVHUB-035](epic-04-projects/DEVHUB-035.md) | Mobile project list and project shell | P1 | S |

### EPIC 5 — Issues · [`epic-05-issues/`](epic-05-issues/)

| Ticket | Title | P | Size |
|---|---|---|---|
| [DEVHUB-036](epic-05-issues/DEVHUB-036.md) | Issue entity, statuses and transitions | P0 | M |
| [DEVHUB-037](epic-05-issues/DEVHUB-037.md) | Issue key generation | P0 | S |
| [DEVHUB-038](epic-05-issues/DEVHUB-038.md) | Create issue endpoint | P0 | S |
| [DEVHUB-039](epic-05-issues/DEVHUB-039.md) | List issues with filtering, sorting and pagination | P0 | M |
| [DEVHUB-040](epic-05-issues/DEVHUB-040.md) | Issue detail endpoint | P0 | XS |
| [DEVHUB-041](epic-05-issues/DEVHUB-041.md) | Update issue endpoint | P0 | S |
| [DEVHUB-042](epic-05-issues/DEVHUB-042.md) | Status, priority and assignee endpoints | P0 | S |
| [DEVHUB-043](epic-05-issues/DEVHUB-043.md) | Archive and delete issue | P1 | XS |
| [DEVHUB-044](epic-05-issues/DEVHUB-044.md) | Web issue list view | P0 | M |
| [DEVHUB-045](epic-05-issues/DEVHUB-045.md) | Web board with drag & drop | P0 | L |
| [DEVHUB-046](epic-05-issues/DEVHUB-046.md) | Web issue detail drawer | P0 | M |
| [DEVHUB-047](epic-05-issues/DEVHUB-047.md) | Mobile issue list, detail and editing | P0 | L |

### EPIC 6 — Labels, priorities & filtering · [`epic-06-labels-filters/`](epic-06-labels-filters/)

| Ticket | Title | P | Size |
|---|---|---|---|
| [DEVHUB-048](epic-06-labels-filters/DEVHUB-048.md) | Label entity and CRUD endpoints | P1 | S |
| [DEVHUB-049](epic-06-labels-filters/DEVHUB-049.md) | Assign and remove labels on issues | P1 | XS |
| [DEVHUB-050](epic-06-labels-filters/DEVHUB-050.md) | Saved filter presets | P2 | S |
| [DEVHUB-051](epic-06-labels-filters/DEVHUB-051.md) | Web filter bar, label management and URL persistence | P1 | M |
| [DEVHUB-052](epic-06-labels-filters/DEVHUB-052.md) | Mobile issue filters | P2 | S |

### EPIC 7 — Comments & activity feed · [`epic-07-comments-activity/`](epic-07-comments-activity/)

| Ticket | Title | P | Size |
|---|---|---|---|
| [DEVHUB-053](epic-07-comments-activity/DEVHUB-053.md) | Comment entity and endpoints | P0 | S |
| [DEVHUB-054](epic-07-comments-activity/DEVHUB-054.md) | Issue activity recording | P0 | M |
| [DEVHUB-055](epic-07-comments-activity/DEVHUB-055.md) | Activity feed endpoint | P1 | XS |
| [DEVHUB-056](epic-07-comments-activity/DEVHUB-056.md) | Web comments and activity feed | P0 | M |
| [DEVHUB-057](epic-07-comments-activity/DEVHUB-057.md) | Mobile comments | P1 | S |

---

## Phase 2 — Product differentiation

### EPIC 8 — Search · [`epic-08-search/`](epic-08-search/)

| Ticket | Title | P | Size |
|---|---|---|---|
| [DEVHUB-058](epic-08-search/DEVHUB-058.md) | PostgreSQL full-text search setup | P1 | M |
| [DEVHUB-059](epic-08-search/DEVHUB-059.md) | Search endpoints | P1 | S |
| [DEVHUB-060](epic-08-search/DEVHUB-060.md) | Web command palette (⌘K) | P1 | M |
| [DEVHUB-061](epic-08-search/DEVHUB-061.md) | Mobile search | P2 | S |

### EPIC 9 — Environments · [`epic-09-environments/`](epic-09-environments/)

| Ticket | Title | P | Size |
|---|---|---|---|
| [DEVHUB-062](epic-09-environments/DEVHUB-062.md) | Environment entity and default seeding | P0 | S |
| [DEVHUB-063](epic-09-environments/DEVHUB-063.md) | Environment endpoints | P0 | S |
| [DEVHUB-064](epic-09-environments/DEVHUB-064.md) | Web environments screen | P0 | S |
| [DEVHUB-065](epic-09-environments/DEVHUB-065.md) | Mobile environments screens | P1 | S |

### EPIC 10 — Deployments · [`epic-10-deployments/`](epic-10-deployments/)

| Ticket | Title | P | Size |
|---|---|---|---|
| [DEVHUB-066](epic-10-deployments/DEVHUB-066.md) | Deployment and DeploymentEvent entities | P0 | M |
| [DEVHUB-067](epic-10-deployments/DEVHUB-067.md) | Deployment endpoints | P0 | S |
| [DEVHUB-068](epic-10-deployments/DEVHUB-068.md) | Deployment webhook with signature validation | P0 | L |
| [DEVHUB-069](epic-10-deployments/DEVHUB-069.md) | Environment health derivation and deployment metrics | P1 | S |
| [DEVHUB-070](epic-10-deployments/DEVHUB-070.md) | Web deployment history and detail | P0 | M |
| [DEVHUB-071](epic-10-deployments/DEVHUB-071.md) | Mobile deployments | P1 | S |

### EPIC 11 — Releases · [`epic-11-releases/`](epic-11-releases/)

| Ticket | Title | P | Size |
|---|---|---|---|
| [DEVHUB-072](epic-11-releases/DEVHUB-072.md) | Release and ReleaseIssue entities | P1 | S |
| [DEVHUB-073](epic-11-releases/DEVHUB-073.md) | Release endpoints and publishing | P1 | S |
| [DEVHUB-074](epic-11-releases/DEVHUB-074.md) | Link issues and deployments to releases | P1 | S |
| [DEVHUB-075](epic-11-releases/DEVHUB-075.md) | Web releases list and detail | P1 | M |
| [DEVHUB-076](epic-11-releases/DEVHUB-076.md) | Mobile releases | P2 | S |

### EPIC 12 — CI/CD integration · [`epic-12-cicd-integration/`](epic-12-cicd-integration/)

| Ticket | Title | P | Size |
|---|---|---|---|
| [DEVHUB-077](epic-12-cicd-integration/DEVHUB-077.md) | CICDRun entity | P1 | S |
| [DEVHUB-078](epic-12-cicd-integration/DEVHUB-078.md) | GitHub Actions webhook endpoint | P1 | M |
| [DEVHUB-079](epic-12-cicd-integration/DEVHUB-079.md) | CI/CD run query endpoints | P1 | XS |
| [DEVHUB-080](epic-12-cicd-integration/DEVHUB-080.md) | Link CI runs to deployments and commits to issues | P2 | S |
| [DEVHUB-081](epic-12-cicd-integration/DEVHUB-081.md) | Web and mobile CI/CD views | P1 | M |

### EPIC 13 — Project dashboard · [`epic-13-dashboard/`](epic-13-dashboard/)

| Ticket | Title | P | Size |
|---|---|---|---|
| [DEVHUB-082](epic-13-dashboard/DEVHUB-082.md) | Dashboard aggregate endpoint | P0 | M |
| [DEVHUB-083](epic-13-dashboard/DEVHUB-083.md) | Web project dashboard | P0 | M |
| [DEVHUB-084](epic-13-dashboard/DEVHUB-084.md) | Mobile home dashboard | P1 | M |

---

## Phase 3 — AWS

### EPIC 15 — Attachments & S3 · [`epic-15-attachments-s3/`](epic-15-attachments-s3/)

| Ticket | Title | P | Size |
|---|---|---|---|
| [DEVHUB-089](epic-15-attachments-s3/DEVHUB-089.md) | S3 bucket, local MinIO and the storage abstraction | P1 | M |
| [DEVHUB-090](epic-15-attachments-s3/DEVHUB-090.md) | Presigned upload URL and completion endpoints | P1 | M |
| [DEVHUB-091](epic-15-attachments-s3/DEVHUB-091.md) | Attachment download, deletion and cleanup | P1 | S |
| [DEVHUB-092](epic-15-attachments-s3/DEVHUB-092.md) | Web attachment upload | P1 | M |
| [DEVHUB-093](epic-15-attachments-s3/DEVHUB-093.md) | Mobile attachment upload | P2 | S |

### EPIC 16 — AWS infrastructure · [`epic-16-aws-infrastructure/`](epic-16-aws-infrastructure/)

| Ticket | Title | P | Size |
|---|---|---|---|
| [DEVHUB-094](epic-16-aws-infrastructure/DEVHUB-094.md) | VPC, subnets and security groups | P1 | M |
| [DEVHUB-095](epic-16-aws-infrastructure/DEVHUB-095.md) | RDS PostgreSQL instance | P1 | M |
| [DEVHUB-096](epic-16-aws-infrastructure/DEVHUB-096.md) | Elastic Beanstalk environment for the API | P1 | L |
| [DEVHUB-097](epic-16-aws-infrastructure/DEVHUB-097.md) | Configuration and secrets in AWS (SSM) | P1 | S |
| [DEVHUB-098](epic-16-aws-infrastructure/DEVHUB-098.md) | S3 + CloudFront for the web SPA | P1 | M |
| [DEVHUB-099](epic-16-aws-infrastructure/DEVHUB-099.md) | IAM roles and least-privilege review | P1 | M |
| [DEVHUB-100](epic-16-aws-infrastructure/DEVHUB-100.md) | CloudWatch logs and metrics wiring | P1 | S |

---

## Phase 4 — CI/CD

### EPIC 17 — CI/CD for DevHub · [`epic-17-cicd-devhub/`](epic-17-cicd-devhub/)

| Ticket | Title | P | Size |
|---|---|---|---|
| [DEVHUB-101](epic-17-cicd-devhub/DEVHUB-101.md) | Backend CI workflow (hardened) | P0 | S |
| [DEVHUB-102](epic-17-cicd-devhub/DEVHUB-102.md) | Web CI workflow | P0 | XS |
| [DEVHUB-103](epic-17-cicd-devhub/DEVHUB-103.md) | Mobile CI workflow | P1 | XS |
| [DEVHUB-104](epic-17-cicd-devhub/DEVHUB-104.md) | GitHub OIDC and the AWS deployment role | P0 | M |
| [DEVHUB-105](epic-17-cicd-devhub/DEVHUB-105.md) | Staging deployment workflow | P0 | L |
| [DEVHUB-106](epic-17-cicd-devhub/DEVHUB-106.md) | Production deployment with manual approval | P1 | M |
| [DEVHUB-107](epic-17-cicd-devhub/DEVHUB-107.md) | Publish deployment status back to DevHub | P0 | M |

---

## Phase 5 — Async, notifications & operations

### EPIC 14 — Notifications · [`epic-14-notifications/`](epic-14-notifications/)

| Ticket | Title | P | Size |
|---|---|---|---|
| [DEVHUB-085](epic-14-notifications/DEVHUB-085.md) | Notification entity and service | P1 | M |
| [DEVHUB-086](epic-14-notifications/DEVHUB-086.md) | Notification endpoints | P1 | XS |
| [DEVHUB-087](epic-14-notifications/DEVHUB-087.md) | Web notification center | P1 | S |
| [DEVHUB-088](epic-14-notifications/DEVHUB-088.md) | Mobile notifications screen | P1 | S |

### EPIC 18 — Observability · [`epic-18-observability/`](epic-18-observability/)

| Ticket | Title | P | Size |
|---|---|---|---|
| [DEVHUB-108](epic-18-observability/DEVHUB-108.md) | Structured logging and correlation ids | P1 | M |
| [DEVHUB-109](epic-18-observability/DEVHUB-109.md) | Global exception handling and ProblemDetails | P0 | S |
| [DEVHUB-110](epic-18-observability/DEVHUB-110.md) | Health checks: live and ready | P0 | XS |
| [DEVHUB-111](epic-18-observability/DEVHUB-111.md) | Metrics, alarms and dashboard | P1 | M |
| [DEVHUB-112](epic-18-observability/DEVHUB-112.md) | Client error handling and reporting | P1 | S |

---

## Suggested first sprint

Everything else depends on these. Do them in order:

```text
DEVHUB-001  monorepo
DEVHUB-002  solution + layers
DEVHUB-005  PostgreSQL compose
DEVHUB-003  health + swagger
DEVHUB-006  EF Core + first migration
DEVHUB-007  configuration & secrets
DEVHUB-011  test harness
DEVHUB-012  CI
DEVHUB-008  web shell
DEVHUB-009  mobile shell
```

Then EPIC 2 end to end (013 → 022) before touching workspaces. At that point you have a real,
authenticated, tested full-stack application and the rest is domain work.

## Two exceptions to the ordering

- **DEVHUB-109** (ProblemDetails) and **DEVHUB-110** (health checks) are listed under EPIC 18 but
  are worth implementing early — right after DEVHUB-018 — because every later ticket's error
  handling and every deploy's health gate depend on them. Their tickets are written to be done
  early and hardened later.
- **DEVHUB-089** (S3 + MinIO) can be pulled forward into Phase 1 if you want attachments while
  building the issue UI. It has no AWS-account dependency thanks to MinIO.

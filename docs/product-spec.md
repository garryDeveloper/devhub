# DevHub — Product & Technical Specification

## 1. Vision

DevHub is a developer-oriented project management and deployment workspace. It combines lightweight issue/project management with deployment visibility, environments, releases, and CI/CD activity.

The goal is not to recreate Trello or Linear. The differentiator is the connection between **work items and software delivery**:

- Projects contain issues.
- Issues can be linked to branches and pull requests.
- Pull requests and commits can be associated with issues.
- Deployments belong to environments.
- Releases summarize delivered work.
- CI/CD runs are visible from the project.
- The dashboard answers: **"What is being worked on, what was shipped, and is production healthy?"**

### Target stack

| Area | Technology |
|---|---|
| Web | React + TypeScript |
| Mobile | React Native + TypeScript |
| Backend | ASP.NET Core Web API |
| Database | PostgreSQL |
| Object storage | AWS S3 |
| Compute | AWS Elastic Beanstalk initially; ECS later as an optional evolution |
| CDN | CloudFront |
| Async processing | AWS Lambda + SQS |
| Observability | CloudWatch |
| Identity / permissions | AWS IAM for infrastructure; application authentication/authorization in the API |
| CI/CD | GitHub Actions |
| Containers | Docker |
| Source control | GitHub |

---

# 2. Product scope

## MVP

The MVP should include:

1. Authentication and user profile
2. Workspaces
3. Projects
4. Issues
5. Labels
6. Priorities
7. Comments
8. Issue activity
9. Search and filtering
10. Environments
11. Deployments
12. Releases
13. CI/CD run visibility
14. Project dashboard
15. Web application
16. Mobile application
17. Basic CI/CD for the DevHub itself

## Post-MVP

- GitHub OAuth integration
- GitHub webhooks
- Automatic issue/PR linking
- Deployment webhooks
- Notifications
- Push notifications
- Real-time updates
- Markdown editor
- Attachments
- S3 image processing
- Background jobs
- Advanced analytics
- Teams and granular RBAC
- ECS/Fargate migration
- Infrastructure as Code with Terraform/CDK

---

# 3. Domain model

Core entities:

```text
User
 └── WorkspaceMember
        └── Workspace

Workspace
 ├── Project
 │    ├── Issue
 │    │    ├── Comment
 │    │    ├── Label
 │    │    ├── Activity
 │    │    └── Attachment
 │    │
 │    ├── Environment
 │    │    ├── Deployment
 │    │    └── DeploymentEvent
 │    │
 │    ├── Release
 │    │    └── ReleaseIssue
 │    │
 │    └── CICDRun
 │
 └── WorkspaceMember
```

Suggested database tables:

```text
users
workspaces
workspace_members
projects
project_members
issues
issue_labels
labels
comments
issue_activities
attachments
environments
deployments
deployment_events
releases
release_issues
cicd_runs
notifications
refresh_tokens
```

---

# 4. Epics and task breakdown

Priority convention:

- P0 = required for MVP
- P1 = important
- P2 = post-MVP

---

## EPIC 1 — Foundation & project setup

### Goal

Create the repositories, development environment, architectural boundaries, database setup and local infrastructure.

### Tasks

- [ ] Create Git repository structure.
- [ ] Create ASP.NET Core API.
- [ ] Create React web application.
- [ ] Create React Native application.
- [ ] Add Docker support for backend.
- [ ] Add Docker Compose for local PostgreSQL.
- [ ] Add environment configuration strategy.
- [ ] Add API health endpoint.
- [ ] Add Swagger/OpenAPI.
- [ ] Configure EF Core and PostgreSQL.
- [ ] Configure database migrations.
- [ ] Establish backend layers:
  - API
  - Application
  - Domain
  - Infrastructure
- [ ] Establish frontend feature/module structure.
- [ ] Configure linting and formatting.
- [ ] Configure unit test projects.
- [ ] Configure integration test project.
- [ ] Add basic README and architecture documentation.

### Acceptance criteria

- Web, mobile and API projects run locally.
- PostgreSQL starts with Docker Compose.
- API exposes `/health`.
- Swagger works.
- Database migrations execute successfully.
- CI can restore, build and test the backend.

---

# EPIC 2 — Authentication & user account

### Goal

Allow users to create an account, sign in and maintain a session.

### Web screens

- Login
- Register
- Forgot password
- Profile
- Account settings

### Mobile screens

- Welcome
- Login
- Register
- Profile
- Settings

### Tasks

- [ ] Create User aggregate.
- [ ] Implement registration endpoint.
- [ ] Implement login endpoint.
- [ ] Implement refresh-token flow.
- [ ] Implement logout.
- [ ] Implement current-user endpoint.
- [ ] Add password hashing.
- [ ] Add validation.
- [ ] Add authorization middleware.
- [ ] Protect private endpoints.
- [ ] Persist authentication state on web.
- [ ] Persist authentication state securely on mobile.
- [ ] Handle expired access tokens.
- [ ] Add profile editing.

### API

```http
POST   /api/auth/register
POST   /api/auth/login
POST   /api/auth/refresh
POST   /api/auth/logout
GET    /api/me
PATCH  /api/me
```

### Acceptance criteria

- A user can register.
- A user can authenticate.
- Access tokens expire.
- Refresh tokens can obtain a new access token.
- Private endpoints reject unauthenticated requests.
- Logout invalidates the refresh session.

---

# EPIC 3 — Workspaces & membership

### Goal

Provide a container for projects and collaboration.

### Web screens

- Workspace switcher
- Workspace settings
- Members
- Invite member modal

### Mobile screens

- Workspace selector
- Workspace members
- Workspace settings

### Tasks

- [ ] Create Workspace entity.
- [ ] Create WorkspaceMember entity.
- [ ] Create workspace.
- [ ] List user's workspaces.
- [ ] Get workspace details.
- [ ] Update workspace.
- [ ] Invite member.
- [ ] Remove member.
- [ ] Change member role.
- [ ] Implement workspace authorization.
- [ ] Add workspace switcher.

### Roles

MVP:

```text
Owner
Member
```

Later:

```text
Owner
Admin
Member
Viewer
```

### API

```http
POST   /api/workspaces
GET    /api/workspaces
GET    /api/workspaces/{workspaceId}
PATCH  /api/workspaces/{workspaceId}

GET    /api/workspaces/{workspaceId}/members
POST   /api/workspaces/{workspaceId}/members
PATCH  /api/workspaces/{workspaceId}/members/{memberId}
DELETE /api/workspaces/{workspaceId}/members/{memberId}
```

---

# EPIC 4 — Projects

### Goal

Create and manage software projects.

### Web screens

- Project list
- Project overview
- Project settings
- Project members

### Mobile screens

- Project list
- Project overview
- Project settings

### Tasks

- [ ] Create project.
- [ ] Update project.
- [ ] Archive project.
- [ ] List projects.
- [ ] Add project members.
- [ ] Remove project members.
- [ ] Configure project key, e.g. `DEV`.
- [ ] Configure project description.
- [ ] Configure project icon/color.
- [ ] Create project dashboard shell.

### API

```http
POST   /api/workspaces/{workspaceId}/projects
GET    /api/workspaces/{workspaceId}/projects
GET    /api/projects/{projectId}
PATCH  /api/projects/{projectId}
DELETE /api/projects/{projectId}

GET    /api/projects/{projectId}/members
POST   /api/projects/{projectId}/members
DELETE /api/projects/{projectId}/members/{memberId}
```

---

# EPIC 5 — Issues

### Goal

Provide the main work-management experience.

### Issue fields

```text
id
projectId
key
title
description
status
priority
assigneeId
reporterId
createdAt
updatedAt
dueDate
```

Statuses:

```text
Backlog
Todo
In Progress
In Review
Done
Canceled
```

Priorities:

```text
No Priority
Low
Medium
High
Urgent
```

### Web screens

#### Project issue board

```text
Backlog       Todo          In Progress      Review       Done
────────      ──────        ───────────      ──────       ─────
DEV-12        DEV-19        DEV-20           DEV-17       DEV-10
DEV-15        DEV-21        DEV-22           DEV-18       DEV-11
```

#### Issue detail

```text
DEV-20
Implement deployment history

Status       In Progress
Priority     High
Assignee     Dario
Labels       backend, aws

Description
────────────────────────

Activity
────────────────────────
Dario changed status
Dario added label
Ana commented
```

### Mobile screens

- Project issue list
- Issue detail
- Create issue
- Edit issue
- Filter issues

### Tasks

- [ ] Create issue.
- [ ] Update issue.
- [ ] Delete issue.
- [ ] Archive issue.
- [ ] Assign issue.
- [ ] Change status.
- [ ] Change priority.
- [ ] Add/remove labels.
- [ ] Set due date.
- [ ] Generate human-readable issue key.
- [ ] Add optimistic status updates in web.
- [ ] Implement pagination.
- [ ] Implement sorting.
- [ ] Implement filtering.
- [ ] Implement issue detail drawer on web.
- [ ] Implement issue detail screen on mobile.

### API

```http
POST   /api/projects/{projectId}/issues
GET    /api/projects/{projectId}/issues
GET    /api/issues/{issueId}
PATCH  /api/issues/{issueId}
DELETE /api/issues/{issueId}

PATCH  /api/issues/{issueId}/status
PATCH  /api/issues/{issueId}/priority
PATCH  /api/issues/{issueId}/assignee
PATCH  /api/issues/{issueId}/labels
```

Query example:

```http
GET /api/projects/{projectId}/issues?
    status=InProgress&
    priority=High&
    assigneeId=...&
    labelId=...&
    search=deployment&
    page=1&
    pageSize=50
```

---

# EPIC 6 — Labels, priorities and filtering

### Goal

Make issue organization useful at scale.

### Tasks

- [ ] CRUD labels.
- [ ] Configure label name.
- [ ] Configure label color.
- [ ] Assign labels.
- [ ] Remove labels.
- [ ] Filter by status.
- [ ] Filter by priority.
- [ ] Filter by assignee.
- [ ] Filter by label.
- [ ] Filter by date.
- [ ] Combine filters.
- [ ] Persist filters in URL on web.
- [ ] Save filter presets.

### API

```http
GET    /api/projects/{projectId}/labels
POST   /api/projects/{projectId}/labels
PATCH  /api/labels/{labelId}
DELETE /api/labels/{labelId}

GET /api/projects/{projectId}/issue-filters
POST /api/projects/{projectId}/issue-filters
DELETE /api/issue-filters/{filterId}
```

---

# EPIC 7 — Comments & activity feed

### Goal

Provide collaboration and an auditable history.

### Tasks

- [ ] Create comment.
- [ ] Edit comment.
- [ ] Delete comment.
- [ ] List comments.
- [ ] Record issue activity.
- [ ] Record status changes.
- [ ] Record assignment changes.
- [ ] Record label changes.
- [ ] Record priority changes.
- [ ] Display chronological activity feed.

### API

```http
GET    /api/issues/{issueId}/comments
POST   /api/issues/{issueId}/comments
PATCH  /api/comments/{commentId}
DELETE /api/comments/{commentId}

GET /api/issues/{issueId}/activities
```

---

# EPIC 8 — Search

### Goal

Allow users to quickly find projects, issues and releases.

### Web

Global search shortcut:

```text
Ctrl/Cmd + K
```

Search modal:

```text
Search DevHub...

Issues
  DEV-123 Fix deployment failure
  DEV-119 Add AWS integration

Projects
  DevHub API

Releases
  v1.4.0
```

### Mobile

- Search tab/action.
- Search screen.
- Recent searches.

### Tasks

- [ ] Search issues.
- [ ] Search projects.
- [ ] Search releases.
- [ ] Add search endpoint.
- [ ] Add debounced search in clients.
- [ ] Add keyboard shortcut on web.
- [ ] Add recent searches.
- [ ] Add search filters.

### API

```http
GET /api/search?q=deployment
GET /api/search/issues?q=deployment
GET /api/search/projects?q=devhub
```

MVP can use PostgreSQL full-text search. Do not introduce Elasticsearch/OpenSearch initially.

---

# EPIC 9 — Environments

### Goal

Represent where an application is deployed.

Default environments:

```text
Development
Staging
Production
```

### Web screens

Project → Environments

```text
Production
🟢 Healthy
Version: 1.8.2
Last deploy: 12 minutes ago

Staging
🟢 Healthy
Version: 1.9.0-rc1
Last deploy: 2 hours ago
```

### Mobile

Project → Environments

Environment detail:

```text
Production

Status
Healthy

Version
1.8.2

Last deployment
12 min ago

Recent deployments
...
```

### Tasks

- [ ] Create environment.
- [ ] Update environment.
- [ ] Delete environment.
- [ ] Configure environment URL.
- [ ] Configure environment type.
- [ ] Configure health status.
- [ ] Link deployment history.
- [ ] Show current version.

### API

```http
GET    /api/projects/{projectId}/environments
POST   /api/projects/{projectId}/environments
GET    /api/environments/{environmentId}
PATCH  /api/environments/{environmentId}
DELETE /api/environments/{environmentId}
```

---

# EPIC 10 — Deployments

### Goal

Track deployments to environments.

### Deployment model

```text
Deployment
├── Environment
├── Version
├── Commit SHA
├── Branch
├── Status
├── StartedAt
├── CompletedAt
├── TriggeredBy
└── Metadata
```

Statuses:

```text
Queued
Running
Succeeded
Failed
Canceled
```

### Web screens

Environment → Deployment history

```text
Production

✓ v1.8.2   8a2d91f   12 min ago
✓ v1.8.1   29f4b10   yesterday
✕ v1.8.0   19aa82c   yesterday
✓ v1.7.9   82c92aa   3 days ago
```

Deployment detail:

```text
Deployment #182

Production
v1.8.2
Commit 8a2d91f

Status: Succeeded

Timeline
────────────────────────
Build started
Tests passed
Docker image created
Deployment started
Health check passed
Deployment completed
```

### Tasks

- [ ] Create deployment.
- [ ] Update deployment status.
- [ ] List deployment history.
- [ ] Get deployment detail.
- [ ] Record deployment events.
- [ ] Calculate deployment duration.
- [ ] Link deployment to release.
- [ ] Link deployment to commit.
- [ ] Expose deployment webhook endpoint.
- [ ] Validate webhook signature.
- [ ] Store raw webhook payload when needed.

### API

```http
GET  /api/environments/{environmentId}/deployments
POST /api/environments/{environmentId}/deployments
GET  /api/deployments/{deploymentId}

POST /api/webhooks/deployments
```

---

# EPIC 11 — Releases

### Goal

Group delivered issues into versions.

### Web screens

- Releases list
- Release detail
- Create release
- Edit release

### Release detail

```text
v1.8.0

Status: Released
Published: Sep 5, 2026

Changes
────────────────────
DEV-120 Fix authentication
DEV-121 Improve dashboard
DEV-124 Add deployment history

Deployment
Production ✓
```

### Tasks

- [ ] Create release.
- [ ] Update release.
- [ ] Publish release.
- [ ] Archive release.
- [ ] Link issues.
- [ ] List release issues.
- [ ] Link deployment.
- [ ] Display release timeline.
- [ ] Generate release notes manually.
- [ ] Later: generate release notes automatically.

### API

```http
GET    /api/projects/{projectId}/releases
POST   /api/projects/{projectId}/releases
GET    /api/releases/{releaseId}
PATCH  /api/releases/{releaseId}
POST   /api/releases/{releaseId}/publish
POST   /api/releases/{releaseId}/issues
DELETE /api/releases/{releaseId}/issues/{issueId}
```

---

# EPIC 12 — CI/CD integration

### Goal

Connect DevHub to GitHub Actions and expose pipeline activity.

## MVP approach

Do not build a full CI/CD engine.

Instead:

1. GitHub Actions runs the actual pipeline.
2. GitHub Actions calls DevHub.
3. DevHub stores and displays the run.

Example:

```text
Git push
   ↓
GitHub Actions
   ↓
build
   ↓
test
   ↓
docker build
   ↓
deploy
   ↓
POST /api/webhooks/cicd
   ↓
DevHub
```

### CI/CD run model

```text
CICDRun
├── ProjectId
├── Provider
├── Workflow
├── Branch
├── CommitSha
├── Status
├── StartedAt
├── CompletedAt
├── CommitUrl
└── RunUrl
```

### Tasks

- [ ] Create CICDRun entity.
- [ ] Create CI/CD webhook endpoint.
- [ ] Authenticate webhook requests.
- [ ] Validate payload.
- [ ] Store workflow name.
- [ ] Store branch.
- [ ] Store commit SHA.
- [ ] Store status.
- [ ] Store timestamps.
- [ ] Display recent runs.
- [ ] Display run detail.
- [ ] Link run to deployment.
- [ ] Link commit to issues where possible.

### API

```http
GET /api/projects/{projectId}/cicd/runs
GET /api/cicd/runs/{runId}

POST /api/webhooks/github/actions
```

---

# EPIC 13 — Project dashboard

### Goal

Create the primary "engineering health" screen.

### Web

```text
DevHub API

┌──────────────────────────────────────────────┐
│ Production        🟢 Healthy                 │
│ v1.8.2             deployed 12m ago          │
├──────────────────────────────────────────────┤
│ Open Issues       In Progress       Releases │
│ 24                8                  2        │
├──────────────────────────────────────────────┤
│ Recent Deployments                           │
│ ✓ v1.8.2                                     │
│ ✓ v1.8.1                                     │
│ ✕ v1.8.0                                     │
├──────────────────────────────────────────────┤
│ Recent CI/CD                                  │
│ ✓ build #812                                  │
│ ✓ build #811                                  │
│ ✕ build #810                                  │
└──────────────────────────────────────────────┘
```

### Tasks

- [ ] Project overview endpoint.
- [ ] Issue summary.
- [ ] Environment summary.
- [ ] Deployment summary.
- [ ] CI/CD summary.
- [ ] Recent activity.
- [ ] Recent releases.
- [ ] Mobile dashboard optimized for vertical layout.

### API

```http
GET /api/projects/{projectId}/dashboard
```

The dashboard endpoint should aggregate read models rather than force the frontend to make 8-10 independent calls.

---

# EPIC 14 — Notifications

### MVP

In-app notifications only.

### Events

- Assigned issue.
- Mentioned in comment.
- Issue status changed.
- Deployment failed.
- Production deployment succeeded.
- CI/CD failed.

### Tasks

- [ ] Notification entity.
- [ ] Create notification service.
- [ ] List notifications.
- [ ] Mark as read.
- [ ] Mark all as read.
- [ ] Notification badge.
- [ ] Mobile notification screen.

### API

```http
GET   /api/notifications
PATCH /api/notifications/{notificationId}/read
POST  /api/notifications/read-all
```

Post-MVP:

```text
API
 ↓
SNS / mobile push provider
 ↓
React Native push notification
```

---

# EPIC 15 — Attachments & S3

### Goal

Learn and use S3 naturally.

Use S3 for:

- issue attachments
- avatars
- project icons
- release assets

### Recommended upload architecture

Do NOT upload large files through the API.

Instead:

```text
Client
  │
  │ request upload URL
  ↓
.NET API
  │
  │ generates presigned URL
  ↓
Client
  │
  │ PUT file directly
  ↓
S3
```

Then:

```text
Client
  ↓
POST /attachments/complete
  ↓
.NET API
  ↓
DB
```

### Tasks

- [ ] Create S3 bucket.
- [ ] Configure private bucket.
- [ ] Configure IAM role.
- [ ] Generate presigned upload URL.
- [ ] Generate presigned download URL.
- [ ] Store attachment metadata.
- [ ] Implement upload progress.
- [ ] Validate file size.
- [ ] Validate MIME type.
- [ ] Delete S3 object when attachment is deleted.
- [ ] Add lifecycle rules.
- [ ] Later: S3 event → Lambda → thumbnail generation.

### API

```http
POST   /api/attachments/upload-url
POST   /api/attachments/{attachmentId}/complete
GET    /api/attachments/{attachmentId}
DELETE /api/attachments/{attachmentId}
```

---

# EPIC 16 — AWS infrastructure

### Goal

Move the application from local development to AWS.

## Initial architecture

```text
                    Internet
                       │
              ┌────────┴────────┐
              │                 │
          CloudFront          API
              │                 │
              ↓                 ↓
             S3          Elastic Beanstalk
         React Web              │
                                ↓
                              RDS
                           PostgreSQL

                              S3
                         Attachments

                         CloudWatch
                            Logs

                            IAM
```

### AWS services to learn

#### IAM

Learn:

- Users
- Roles
- Policies
- Least privilege
- Trust policies
- Service roles
- GitHub Actions OIDC

#### S3

Learn:

- Buckets
- Object keys
- Bucket policies
- IAM policies
- Presigned URLs
- Versioning
- Lifecycle rules

#### RDS

Learn:

- PostgreSQL
- Security groups
- Backups
- Connection configuration
- Private networking

#### Elastic Beanstalk

Learn:

- Application
- Environment
- Deployment
- Environment variables
- Logs
- Health monitoring

#### CloudWatch

Learn:

- Logs
- Log groups
- Metrics
- Alarms
- Application monitoring

#### VPC

Learn:

- VPC
- Subnets
- Internet gateway
- Route tables
- Security groups
- Public/private architecture

---

# EPIC 17 — CI/CD for DevHub

### Goal

Automatically test and deploy the application.

## Pipeline

```text
Developer
   │
   │ git push
   ↓
GitHub
   │
   ↓
GitHub Actions
   │
   ├── Restore
   ├── Lint
   ├── Unit tests
   ├── Integration tests
   ├── Build
   └── Docker build
          │
          ↓
      Deploy AWS
          │
          ↓
     Health check
          │
          ↓
      Deployment
          │
          ↓
       DevHub
```

### Pull request workflow

```text
PR opened
   ↓
Lint
   ↓
Unit tests
   ↓
Integration tests
   ↓
Build
   ↓
PR status
```

### Main branch workflow

```text
Push main
   ↓
Tests
   ↓
Build
   ↓
Deploy staging
   ↓
Health check
```

### Production workflow

For the MVP, make production deployment manual:

```text
GitHub Actions
      ↓
Build
      ↓
Deploy staging
      ↓
Manual approval
      ↓
Deploy production
```

### Tasks

- [ ] Create GitHub Actions PR workflow.
- [ ] Run backend unit tests.
- [ ] Run integration tests.
- [ ] Run frontend lint/build.
- [ ] Build Docker image.
- [ ] Configure GitHub OIDC.
- [ ] Create AWS IAM deployment role.
- [ ] Configure staging deployment.
- [ ] Add health check.
- [ ] Add production deployment approval.
- [ ] Publish deployment status to DevHub.
- [ ] Store deployment result.
- [ ] Display deployment in DevHub.

---

# EPIC 18 — Observability

### Goal

Learn basic production observability.

### Tasks

- [ ] Structured logging in .NET.
- [ ] Correlation/request ID.
- [ ] CloudWatch log group.
- [ ] Error logging.
- [ ] Basic metrics.
- [ ] API latency metric.
- [ ] Error-rate alarm.
- [ ] Database health monitoring.
- [ ] Deployment failure logging.
- [ ] Health endpoint checks.

### Health endpoints

```http
GET /health
GET /health/live
GET /health/ready
```

Example:

```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "storage": "Healthy"
  }
}
```

---

# 5. Web application information architecture

```text
App
├── Workspace switcher
├── Dashboard
├── Projects
│   └── Project
│       ├── Overview
│       ├── Issues
│       ├── Board
│       ├── Releases
│       ├── Environments
│       ├── Deployments
│       ├── CI/CD
│       ├── Activity
│       └── Settings
├── Search
├── Notifications
└── Account
    ├── Profile
    └── Settings
```

## Primary navigation

Desktop sidebar:

```text
DEVHUB

⌂ Overview
▣ Projects
  └── DevHub
      ├── Issues
      ├── Board
      ├── Releases
      ├── Environments
      ├── Deployments
      └── CI/CD

⌕ Search

────────────

Notifications
Settings
Profile
```

---

# 6. Mobile information architecture

Mobile should not simply reproduce the desktop UI.

Bottom navigation:

```text
┌─────────────────────────────┐
│                             │
│        Application          │
│                             │
├─────────────────────────────┤
│ Home  Issues  Projects  Me  │
└─────────────────────────────┘
```

Project screen:

```text
Project
────────────────
Overview
Issues
Board
Releases
Environments
Deployments
CI/CD
```

Mobile prioritizes:

1. Checking status
2. Viewing issues
3. Updating issues
4. Commenting
5. Checking deployments
6. Receiving notifications

Complex configuration remains primarily on web.

---

# 7. User flows

## Flow 1 — New user

```text
Landing
  ↓
Register
  ↓
Create account
  ↓
Create workspace
  ↓
Create project
  ↓
Project dashboard
  ↓
Create first issue
```

## Flow 2 — Create issue

```text
Project
  ↓
Issues
  ↓
Create issue
  ↓
Title
Description
Priority
Assignee
Labels
  ↓
Create
  ↓
Issue detail
```

## Flow 3 — Work on issue

```text
Issue
  ↓
Assign to self
  ↓
Status → In Progress
  ↓
Comment
  ↓
Change status → In Review
  ↓
Release
  ↓
Done
```

## Flow 4 — Deployment

```text
Git push
  ↓
GitHub Actions
  ↓
Build
  ↓
Tests
  ↓
Deploy
  ↓
Webhook
  ↓
DevHub
  ↓
Deployment created
  ↓
Environment updated
  ↓
Activity recorded
```

## Flow 5 — Failed deployment

```text
GitHub Actions
      ↓
Deployment fails
      ↓
POST webhook
      ↓
DevHub
      ↓
Deployment = Failed
      ↓
Environment = Unhealthy
      ↓
Notification created
      ↓
User opens notification
      ↓
Deployment detail
      ↓
Open GitHub run
```

## Flow 6 — Mobile issue update

```text
Mobile
  ↓
Issues
  ↓
Select issue
  ↓
Change status
  ↓
PATCH API
  ↓
API validates authorization
  ↓
Database update
  ↓
Activity created
  ↓
Response
  ↓
Mobile updates UI
```

## Flow 7 — Attachment upload

```text
Create comment/issue
       ↓
Attach file
       ↓
POST /attachments/upload-url
       ↓
API returns presigned S3 URL
       ↓
Mobile/Web uploads directly to S3
       ↓
POST /attachments/complete
       ↓
API persists metadata
       ↓
Attachment visible
```

---

# 8. Backend architecture

Use a modular monolith rather than microservices.

```text
┌─────────────────────────────────────────────┐
│                ASP.NET Core API             │
├─────────────────────────────────────────────┤
│ API / Controllers                           │
├─────────────────────────────────────────────┤
│ Application                                 │
│                                             │
│ Auth       Projects       Issues             │
│ Releases   Deployments    CI/CD              │
│ Search     Notifications  Attachments        │
├─────────────────────────────────────────────┤
│ Domain                                      │
│                                             │
│ Entities / Aggregates / Value Objects       │
│ Domain Events / Business Rules              │
├─────────────────────────────────────────────┤
│ Infrastructure                              │
│                                             │
│ EF Core / PostgreSQL                        │
│ AWS S3                                      │
│ AWS integrations                            │
│ External providers                          │
└─────────────────────────────────────────────┘
```

Recommended dependency direction:

```text
API
 ↓
Application
 ↓
Domain

Infrastructure
 ↓
Application + Domain
```

Domain must not depend on AWS, EF Core or ASP.NET.

---

# 9. Backend modules

Suggested structure:

```text
src/
├── DevHub.Api/
│   ├── Controllers/
│   ├── Middleware/
│   ├── Filters/
│   └── Configuration/
│
├── DevHub.Application/
│   ├── Auth/
│   ├── Workspaces/
│   ├── Projects/
│   ├── Issues/
│   ├── Comments/
│   ├── Releases/
│   ├── Environments/
│   ├── Deployments/
│   ├── CICD/
│   ├── Search/
│   ├── Notifications/
│   └── Attachments/
│
├── DevHub.Domain/
│   ├── Users/
│   ├── Workspaces/
│   ├── Projects/
│   ├── Issues/
│   ├── Releases/
│   ├── Deployments/
│   └── Shared/
│
└── DevHub.Infrastructure/
    ├── Persistence/
    ├── Identity/
    ├── Storage/
    ├── Aws/
    └── Integrations/
```

---

# 10. API conventions

Base URL:

```text
/api
```

Use REST semantics.

Successful creation:

```http
201 Created
```

Successful update:

```http
200 OK
```

Successful deletion:

```http
204 No Content
```

Validation:

```http
400 Bad Request
```

Authentication:

```http
401 Unauthorized
```

Authorization:

```http
403 Forbidden
```

Not found:

```http
404 Not Found
```

Conflict:

```http
409 Conflict
```

Server error:

```http
500 Internal Server Error
```

Use RFC 7807 / ASP.NET `ProblemDetails` for errors.

---

# 11. API overview

```text
/auth
  POST /register
  POST /login
  POST /refresh
  POST /logout

/workspaces
  POST /
  GET /
  GET /{id}
  PATCH /{id}
  GET /{id}/members
  POST /{id}/members
  PATCH /{id}/members/{memberId}
  DELETE /{id}/members/{memberId}

/projects
  POST /
  GET /
  GET /{id}
  PATCH /{id}
  DELETE /{id}

/issues
  POST /
  GET /
  GET /{id}
  PATCH /{id}
  DELETE /{id}
  PATCH /{id}/status
  PATCH /{id}/priority
  PATCH /{id}/assignee

/comments
  GET /issues/{issueId}
  POST /issues/{issueId}
  PATCH /{id}
  DELETE /{id}

/releases
  GET /projects/{projectId}
  POST /projects/{projectId}
  GET /{id}
  PATCH /{id}
  POST /{id}/publish

/environments
  GET /projects/{projectId}
  POST /projects/{projectId}
  GET /{id}
  PATCH /{id}
  DELETE /{id}

/deployments
  GET /environments/{environmentId}
  POST /environments/{environmentId}
  GET /{id}

/cicd
  GET /projects/{projectId}/runs
  GET /runs/{id}

/attachments
  POST /upload-url
  POST /{id}/complete
  GET /{id}
  DELETE /{id}

/search
  GET /?q=

/notifications
  GET /
  PATCH /{id}/read
  POST /read-all

/webhooks
  POST /github/actions
  POST /deployments
```

---

# 12. Backend request flow

Normal command:

```text
HTTP Request
    ↓
Controller
    ↓
Application Command
    ↓
Handler
    ↓
Domain
    ↓
Repository
    ↓
PostgreSQL
    ↓
Domain/Application events
    ↓
Response DTO
    ↓
HTTP Response
```

Example:

```text
PATCH /api/issues/123/status

Controller
    ↓
ChangeIssueStatusCommand
    ↓
ChangeIssueStatusHandler
    ↓
Issue.ChangeStatus()
    ↓
Repository.Update()
    ↓
SaveChanges()
    ↓
IssueStatusChanged event
    ↓
Activity created
    ↓
Notification created
    ↓
IssueResponseDto
```

---

# 13. Asynchronous architecture

Do not introduce asynchronous infrastructure everywhere.

Use it where it provides a clear benefit.

Initial async flow:

```text
GitHub webhook
      ↓
API
      ↓
Persist event
      ↓
SQS
      ↓
Lambda
      ↓
Process event
      ↓
Update deployment
      ↓
Create notification
```

Later:

```text
S3 upload
   ↓
S3 event
   ↓
Lambda
   ↓
Image processing
   ↓
S3 thumbnail
```

---

# 14. AWS architecture — initial

```text
                         Internet
                            │
                 ┌──────────┴──────────┐
                 │                     │
                 ▼                     ▼
          CloudFront                 API
                 │                     │
                 ▼                     ▼
             S3 bucket          Elastic Beanstalk
             React SPA                 │
                                      │
                              ┌───────┴────────┐
                              │                │
                              ▼                ▼
                            RDS              S3
                         PostgreSQL       Attachments
                              │
                              │
                              ▼
                         CloudWatch
                            Logs

                            IAM
                             │
              ┌──────────────┼───────────────┐
              ▼              ▼               ▼
          Beanstalk       GitHub         Lambda/SQS
            Role          Actions
```

---

# 15. AWS networking evolution

## Learning architecture

Start simple:

```text
VPC
├── Public subnet
│   └── Elastic Beanstalk
│
└── Private subnet
    └── RDS
```

Then evolve toward:

```text
Internet
   │
CloudFront
   │
S3

Internet
   │
Load Balancer
   │
Private application
   │
Private RDS
```

The second architecture is more representative of a production design, but the first is easier to understand and operate as a learning project.

---

# 16. IAM learning plan

Create separate roles for separate responsibilities.

```text
GitHubActionsRole
 ├── deploy application
 └── read required artifacts

ApplicationRole
 ├── read/write specific S3 bucket
 └── no broad AWS permissions

LambdaRole
 ├── consume SQS
 ├── write logs
 └── access required S3 resources
```

Avoid:

```text
AdministratorAccess
```

for application workloads.

The purpose of this project is partly to learn **least privilege**, so IAM should be treated as a feature of the learning plan, not an afterthought.

---

# 17. CI/CD architecture

## Repository

A monorepo is recommended initially:

```text
devhub/
├── apps/
│   ├── web/
│   └── mobile/
│
├── backend/
│   └── DevHub.sln
│
├── infrastructure/
│   └── docker-compose.yml
│
├── .github/
│   └── workflows/
│
└── docs/
```

## GitHub Actions

```text
.github/workflows/
├── web-ci.yml
├── backend-ci.yml
├── mobile-ci.yml
├── deploy-staging.yml
└── deploy-production.yml
```

### PR

```text
Pull Request
    │
    ├── Web lint
    ├── Web build
    ├── Backend build
    ├── Backend unit tests
    ├── Integration tests
    └── Mobile checks
```

### Main

```text
main
 │
 ├── CI
 │
 ├── Build artifact
 │
 ├── Deploy staging
 │
 ├── Health check
 │
 └── Update DevHub deployment
```

---

# 18. Definition of Done

A task is complete when:

- Code is implemented.
- Unit tests exist where business logic warrants them.
- API validation is implemented.
- Authorization is verified.
- Error cases are handled.
- Swagger/OpenAPI is updated automatically or manually where necessary.
- Frontend loading/error/empty states exist.
- Mobile behavior is implemented if the feature is mobile-scoped.
- Database migration exists when schema changes.
- Logs are meaningful.
- CI passes.

For infrastructure tasks:

- AWS resource is configured.
- IAM permissions are least-privilege where practical.
- Configuration is documented.
- No credentials are committed.
- The change is reproducible.

---

# 19. Recommended implementation order

Do not implement the epics strictly in numerical order.

## Phase 1 — Local foundation

1. Foundation
2. Authentication
3. Workspaces
4. Projects
5. Issues
6. Labels
7. Comments

Result:

```text
React
 ↓
.NET
 ↓
PostgreSQL
```

At this point DevHub is already useful.

## Phase 2 — Product differentiation

8. Search
9. Releases
10. Environments
11. Deployments
12. CI/CD runs
13. Dashboard

Result:

```text
DevHub becomes an engineering workspace
rather than another task manager.
```

## Phase 3 — AWS

14. S3 attachments
15. AWS deployment
16. IAM
17. RDS
18. CloudWatch
19. VPC

Result:

```text
React → S3/CloudFront
.NET → AWS
DB → RDS
Files → S3
Logs → CloudWatch
```

## Phase 4 — CI/CD

20. GitHub Actions
21. OIDC
22. Automated staging
23. Production approval
24. Deployment callbacks

## Phase 5 — Async/cloud features

25. SQS
26. Lambda
27. S3 events
28. Notifications
29. Background processing

---

# 20. Suggested MVP milestone

The first portfolio-quality milestone should be:

```text
Authentication
        +
Workspace
        +
Projects
        +
Issues
        +
Comments
        +
Releases
        +
Environments
        +
Deployments
        +
CI/CD
```

A user should be able to:

```text
Create project
      ↓
Create issue
      ↓
Work on issue
      ↓
Create release
      ↓
Push code to GitHub
      ↓
GitHub Actions runs
      ↓
Application deploys
      ↓
DevHub receives deployment event
      ↓
Production environment updates
      ↓
Dashboard shows deployment
```

That single flow demonstrates a much stronger engineering skill set than a generic CRUD application.

---

# 21. Portfolio story

The project should be presented as:

> **DevHub — Developer Project & Deployment Workspace**

Key portfolio bullets:

- Built a full-stack developer platform with React, React Native and ASP.NET Core.
- Designed a modular monolith with domain/application/infrastructure boundaries.
- Implemented issue tracking, releases, environments and deployment history.
- Deployed the web application and backend to AWS.
- Used S3 for private file storage and presigned uploads.
- Used PostgreSQL/RDS for persistence.
- Implemented GitHub Actions CI/CD.
- Configured AWS IAM roles and GitHub OIDC instead of long-lived deployment credentials.
- Added CloudWatch logging and health monitoring.
- Implemented webhook-based deployment tracking.
- Added asynchronous processing with SQS/Lambda as an advanced feature.

The important point is that every bullet corresponds to something actually implemented in the repository.

---

# 22. Recommended development philosophy

Use AI for:

- UX exploration
- UI alternatives
- user stories
- acceptance criteria
- test case generation
- documentation
- code review
- architectural discussion

Write the core implementation yourself when the purpose is learning.

For each ticket, follow:

```text
Understand
   ↓
Design
   ↓
Implement
   ↓
Test
   ↓
Review
   ↓
Document
```

Avoid asking AI to generate an entire epic at once.

Instead, take one task such as:

> Implement `PATCH /api/issues/{issueId}/status`.

Then independently decide:

- domain behavior
- authorization
- request DTO
- response DTO
- persistence
- tests
- error cases

This keeps DevHub useful both as a portfolio project and as a genuine learning exercise.

---

# 23. First tickets to implement

The first practical sprint should be:

### DEVHUB-1
Initialize repository and solution structure.

### DEVHUB-2
Create ASP.NET Core API with health endpoint.

### DEVHUB-3
Create PostgreSQL Docker Compose environment.

### DEVHUB-4
Configure EF Core and initial migration.

### DEVHUB-5
Create React web application shell.

### DEVHUB-6
Create React Native application shell.

### DEVHUB-7
Configure backend unit tests.

### DEVHUB-8
Configure GitHub Actions CI.

### DEVHUB-9
Implement User entity.

### DEVHUB-10
Implement registration.

### DEVHUB-11
Implement login.

### DEVHUB-12
Implement refresh-token authentication.

### DEVHUB-13
Implement Workspace entity.

### DEVHUB-14
Implement workspace creation.

### DEVHUB-15
Implement workspace/project navigation.

### DEVHUB-16
Implement Project entity.

### DEVHUB-17
Implement Issue entity.

### DEVHUB-18
Implement create issue.

### DEVHUB-19
Implement issue list.

### DEVHUB-20
Implement issue detail.

At this point, stop and build the basic UI experience before adding more domain functionality.

---

# 24. Success criteria

DevHub is successful as a learning project if, by the end, you can confidently explain:

### .NET

- Why the backend is a modular monolith.
- Why domain logic is separated from infrastructure.
- How authentication works.
- How authorization works.
- How database transactions work.
- How background processing works.
- How webhooks are validated.

### AWS

- What IAM roles are.
- How IAM policies work.
- Why applications should not use root credentials.
- How S3 permissions work.
- Why presigned URLs are useful.
- How RDS networking works.
- What a VPC/subnet/security group does.
- How CloudWatch is used.
- How Lambda and SQS fit into an architecture.

### CI/CD

- How GitHub Actions builds and tests the application.
- How OIDC authenticates GitHub Actions to AWS.
- How deployment environments work.
- How a deployment becomes visible in DevHub.
- How failed deployments are represented.

### Frontend

- How React communicates with the API.
- How state/server-state is handled.
- How optimistic updates work.
- How responsive web and mobile UX differ.

The final project should therefore demonstrate not just "I know AWS", but:

> **I can design, build, test, deploy, observe and operate a full-stack application.**

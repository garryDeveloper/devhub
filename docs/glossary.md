# Glossary

Shared vocabulary. Use these exact terms in code, docs, UI copy and commit messages.

| Term | Meaning in DevHub |
|---|---|
| **Workspace** | Top-level container for people and projects. Billing/ownership boundary. |
| **Project** | A software product or service inside a workspace. Owns issues, environments, releases and CI/CD runs. |
| **Project key** | Short uppercase code (`DEV`) unique per workspace, used to build issue keys. Immutable. |
| **Issue** | A unit of work. Identified by a human key like `DEV-42`. |
| **Issue key** | `{ProjectKey}-{Number}`, assigned once, never reused, even if the issue is deleted. |
| **Status** | Workflow position: Backlog, Todo, In Progress, In Review, Done, Canceled. |
| **Priority** | No Priority, Low, Medium, High, Urgent. Independent of status. |
| **Label** | Free-form project-scoped tag with a color. Many-to-many with issues. |
| **Activity** | Append-only audit record of what changed on an issue and who changed it. |
| **Comment** | User-written markdown message on an issue. |
| **Attachment** | File stored in S3 and referenced from an issue or comment. Bytes never pass through the API. |
| **Environment** | A place a project is deployed to: Development, Staging, Production. |
| **Environment health** | Derived status (Healthy / Degraded / Unhealthy / Unknown) computed from the latest deployment. Never set directly by a client. |
| **Deployment** | One attempt to ship a version to an environment. Has a status lifecycle and a timeline of events. |
| **Deployment event** | A step within a deployment ("Tests passed", "Health check passed"). Append-only. |
| **Release** | A named version (`v1.8.0`) grouping the issues delivered in it. Draft → Released → Archived. |
| **CI/CD run** | A GitHub Actions workflow execution mirrored into DevHub. DevHub stores metadata and links out; it never stores logs. |
| **Webhook** | Signed HTTP callback from GitHub Actions into DevHub reporting a deployment or a run. |
| **Aggregate root** | Entity loaded and saved as a unit by a repository. Children are only reached through it. |
| **Domain event** | Fact raised by an aggregate after a state change, dispatched after the transaction commits. |
| **Modular monolith** | One deployable process with enforced internal module boundaries. DevHub's chosen backend shape — not microservices. |
| **Read model** | A query-shaped projection (e.g. the dashboard response) assembled for a screen instead of exposing entities. |
| **Presigned URL** | Time-limited S3 URL that lets a client upload or download directly without AWS credentials. |
| **OIDC (GitHub → AWS)** | Federation that lets a GitHub Actions job assume an AWS role with a short-lived token instead of stored access keys. |
| **ProblemDetails** | RFC 7807 error body format used by every DevHub error response. |
| **Correlation id** | Per-request identifier propagated through logs and returned in the `X-Correlation-Id` header. |
| **P0 / P1 / P2** | Required for MVP / important / post-MVP. |
| **DoD** | Definition of Done — see [`definition-of-done.md`](definition-of-done.md). |

## Naming pitfalls to avoid

- "Ticket" refers to a **backlog item in `docs/tickets/`**. Work items inside the product are
  called **issues**. Don't mix them.
- "Deploy" is the verb, "deployment" is the entity.
- "Release" is a version grouping issues; it is *not* a deployment. A release can be deployed to
  several environments.
- "Run" always means a CI/CD run, never an application process.

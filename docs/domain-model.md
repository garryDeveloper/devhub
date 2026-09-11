# Domain model & entities

Source of truth for entities, invariants and relationships. The relational mapping lives in
[`tech-specs/database-schema.md`](tech-specs/database-schema.md).

---

## 1. Aggregate map

```text
User
 └── RefreshToken

Workspace  (aggregate root)
 ├── WorkspaceMember
 └── Project  (aggregate root)
      ├── ProjectMember
      ├── Label
      ├── IssueFilter (saved preset)
      ├── Issue  (aggregate root)
      │    ├── IssueLabel
      │    ├── Comment
      │    ├── IssueActivity
      │    └── Attachment
      ├── Environment  (aggregate root)
      │    └── Deployment
      │         └── DeploymentEvent
      ├── Release  (aggregate root)
      │    └── ReleaseIssue
      └── CICDRun

Notification  (standalone, references user + target)
```

**Aggregate roots** are the only entities loaded and saved directly by a repository:
`User`, `Workspace`, `Project`, `Issue`, `Environment`, `Release`, `CICDRun`, `Notification`.
Children (`Comment`, `DeploymentEvent`, `ReleaseIssue`, …) are reached through their root.

Cross-aggregate references are by **id only** — never by navigation property. `Issue` holds
`AssigneeId`, not a `User` object.

---

## 2. Entities

### User

| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| Email | string(320) | unique, lowercased, immutable after creation |
| DisplayName | string(100) | |
| AvatarUrl | string? | S3 key resolved to a presigned URL |
| PasswordHash | string | ASP.NET Core `PasswordHasher` (PBKDF2) |
| CreatedAt / UpdatedAt | timestamptz | UTC |

Invariants: email is unique and normalized; `PasswordHash` is never returned by any DTO.

### RefreshToken

| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| UserId | Guid | FK |
| TokenHash | string | SHA-256 of the opaque token; raw value never stored |
| ExpiresAt | timestamptz | |
| RevokedAt | timestamptz? | |
| ReplacedByTokenId | Guid? | rotation chain |
| CreatedByIp / UserAgent | string? | audit |

Invariants: a token is usable only if `RevokedAt is null && ExpiresAt > now`. Reuse of a revoked
token revokes the entire chain (see [`tech-specs/auth-spec.md`](tech-specs/auth-spec.md)).

### Workspace

| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| Name | string(80) | |
| Slug | string(50) | unique, lowercase kebab |
| OwnerId | Guid | FK → User |
| CreatedAt / UpdatedAt | timestamptz | |

Invariants: always has ≥ 1 member with role `Owner`; the owner cannot be removed or demoted
while they are the last owner.

### WorkspaceMember

`WorkspaceId`, `UserId`, `Role`, `JoinedAt`. Unique on (`WorkspaceId`, `UserId`).

Roles (MVP): `Owner`, `Member`. Post-MVP: `Owner`, `Admin`, `Member`, `Viewer`.

### Project

| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| WorkspaceId | Guid | FK |
| Name | string(80) | |
| Key | string(10) | uppercase A–Z, unique **per workspace**, immutable after creation |
| Description | string(2000)? | |
| Color | string(7)? | hex |
| Icon | string(40)? | |
| IssueSequence | int | last issued issue number, starts at 0 |
| ArchivedAt | timestamptz? | archived projects are hidden but not deleted |

Invariants: `Key` matches `^[A-Z][A-Z0-9]{1,9}$`; issue keys are `{Key}-{n}` and never reused.

### ProjectMember

`ProjectId`, `UserId`, `Role`, `AddedAt`. A project member must already be a workspace member.

### Issue

| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| ProjectId | Guid | FK |
| Number | int | per-project sequential |
| Key | string | computed `{Project.Key}-{Number}`, persisted for search |
| Title | string(200) | required, trimmed |
| Description | text? | markdown |
| Status | enum | see below |
| Priority | enum | see below |
| AssigneeId | Guid? | must be a project member |
| ReporterId | Guid | immutable |
| DueDate | date? | |
| ArchivedAt | timestamptz? | |
| CreatedAt / UpdatedAt | timestamptz | |

```text
Status:    Backlog | Todo | InProgress | InReview | Done | Canceled
Priority:  NoPriority | Low | Medium | High | Urgent
```

Allowed transitions (MVP is permissive but explicit):

```text
Backlog    → Todo, InProgress, Canceled
Todo       → Backlog, InProgress, Canceled
InProgress → Todo, InReview, Done, Canceled
InReview   → InProgress, Done, Canceled
Done       → InProgress            (reopen)
Canceled   → Backlog, Todo
```

Invariants: title non-empty; `Number` assigned once, atomically; changing status/priority/
assignee/labels **always** produces an `IssueActivity` row.

### Label

`Id`, `ProjectId`, `Name` (unique per project, ≤ 40 chars), `Color` (hex). Join table
`IssueLabel(IssueId, LabelId)`.

### Comment

`Id`, `IssueId`, `AuthorId`, `Body` (markdown, ≤ 10 000), `CreatedAt`, `UpdatedAt`, `EditedAt?`.
Only the author may edit; author or workspace owner may delete. Deleting is a hard delete plus
an activity entry.

### IssueActivity

`Id`, `IssueId`, `ActorId`, `Type`, `OldValue?`, `NewValue?`, `Metadata jsonb?`, `CreatedAt`.

```text
Type: Created | StatusChanged | PriorityChanged | AssigneeChanged |
      LabelAdded | LabelRemoved | TitleChanged | DescriptionChanged |
      DueDateChanged | Commented | AttachmentAdded | LinkedToRelease | Archived
```

Activity rows are **append-only**. Never update or delete them.

### Attachment

`Id`, `IssueId?`, `CommentId?`, `UploadedById`, `FileName`, `ContentType`, `SizeBytes`,
`StorageKey`, `Status` (`Pending | Ready | Failed`), `CreatedAt`.

Invariants: exactly one owner (`IssueId` xor `CommentId`); rows are created as `Pending` when the
presigned URL is issued and flipped to `Ready` on `POST /attachments/{id}/complete`; `Pending`
rows older than 24 h are garbage-collected.

### Environment

`Id`, `ProjectId`, `Name` (unique per project), `Type` (`Development | Staging | Production`),
`Url?`, `CurrentVersion?`, `HealthStatus` (`Unknown | Healthy | Degraded | Unhealthy`),
`LastDeployedAt?`, `SortOrder`.

Invariants: `CurrentVersion`, `HealthStatus` and `LastDeployedAt` are **derived** — only a
successful/failed deployment updates them, never a direct client write.

### Deployment

| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| EnvironmentId | Guid | FK |
| Number | int | per-project sequential, display as `#182` |
| Version | string(50) | e.g. `v1.8.2` |
| CommitSha | string(40)? | |
| CommitUrl | string? | |
| Branch | string(200)? | |
| Status | enum | `Queued | Running | Succeeded | Failed | Canceled` |
| TriggeredBy | string(100)? | GitHub actor or DevHub user |
| ReleaseId | Guid? | FK |
| CicdRunId | Guid? | FK |
| StartedAt / CompletedAt | timestamptz? | |
| DurationMs | int? | computed on completion |
| Metadata | jsonb? | raw provider payload subset |

Transitions: `Queued → Running → (Succeeded | Failed | Canceled)`. Terminal states are final;
a repeated webhook for a terminal deployment is ignored idempotently.

### DeploymentEvent

`Id`, `DeploymentId`, `Name` (e.g. `Build started`), `Status`, `OccurredAt`, `Message?`.
Append-only, ordered by `OccurredAt` — this is the timeline in the deployment detail screen.

### Release

`Id`, `ProjectId`, `Version` (unique per project), `Name?`, `Notes?` (markdown),
`Status` (`Draft | Released | Archived`), `PublishedAt?`, `CreatedById`.

Invariants: version unique per project; a `Draft` can be edited freely, a `Released` release can
only have notes edited; publishing sets `PublishedAt` once and is not reversible.

### ReleaseIssue

`ReleaseId`, `IssueId`, `AddedAt`. An issue belongs to at most one **released** release.

### CICDRun

`Id`, `ProjectId`, `Provider` (`GitHubActions`), `ExternalId` (unique per provider+project),
`Workflow`, `RunNumber`, `Branch`, `CommitSha`, `CommitUrl`, `RunUrl`,
`Status` (`Queued | Running | Succeeded | Failed | Canceled`), `StartedAt`, `CompletedAt`.

Invariant: `(Provider, ExternalId)` is unique — webhooks are idempotent upserts.

### Notification

`Id`, `UserId`, `Type`, `Title`, `Body?`, `TargetType`, `TargetId`, `ReadAt?`, `CreatedAt`.

```text
Type: IssueAssigned | MentionedInComment | IssueStatusChanged |
      DeploymentFailed | ProductionDeploymentSucceeded | CicdFailed
```

A user never receives a notification for their own action.

---

## 3. Ownership & scoping rule

Every entity resolves to exactly one workspace:

```text
Issue → Project → Workspace
Deployment → Environment → Project → Workspace
Release/CICDRun → Project → Workspace
```

**Every query and command must be scoped by the caller's workspace membership.** A missing scope
check is a security bug, not a missing feature. Resources the caller cannot see return `404`,
not `403`, to avoid leaking existence.

---

## 4. Domain events

Raised by aggregates, dispatched after `SaveChanges` succeeds:

| Event | Consumers |
|---|---|
| `IssueCreated` | activity |
| `IssueStatusChanged` | activity, notification |
| `IssueAssigneeChanged` | activity, notification |
| `IssuePriorityChanged` / `IssueLabelsChanged` | activity |
| `CommentCreated` | activity, mention notifications |
| `DeploymentStatusChanged` | environment health update, notification |
| `ReleasePublished` | activity on linked issues |
| `CicdRunCompleted` | notification on failure |

Handlers must be idempotent and must not throw into the request path — a failed notification
must not roll back the business write.

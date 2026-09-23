# Backend architecture

ASP.NET Core 10, modular monolith, four projects, one deployable.

---

## 1. Why a modular monolith

Microservices would add network boundaries, distributed transactions, service discovery and
deployment complexity to a single-developer learning project — cost with no benefit. A modular
monolith gives the same boundary discipline (module isolation, dependency rules, domain purity)
with one process, one database and one deploy. If a module ever needs to be extracted, the
boundaries are already there.

---

## 2. Layers

```text
┌──────────────────────────────────────────────┐
│ DevHub.Api                                   │
│ Endpoints · Middleware · Filters · Auth      │
├──────────────────────────────────────────────┤
│ DevHub.Application                           │
│ Commands · Queries · Handlers · Validators   │
│ DTOs · Ports (interfaces) · Event handlers   │
├──────────────────────────────────────────────┤
│ DevHub.Domain                                │
│ Entities · Aggregates · Value objects        │
│ Domain events · Business rules · Enums       │
├──────────────────────────────────────────────┤
│ DevHub.Infrastructure                        │
│ EF Core · Repositories · Identity · S3 · AWS │
└──────────────────────────────────────────────┘
```

Dependency direction:

```text
Api            → Application → Domain
Infrastructure → Application + Domain
```

`Domain` references **nothing** but the BCL. If you find yourself adding a NuGet package to
`DevHub.Domain`, the design is wrong.

`Api` references `Infrastructure` **only** in `Program.cs` for dependency-injection wiring.
Endpoints never reference `DbContext`.

Enforce it with a test (`ArchitectureTests`) using NetArchTest or a simple reflection assertion,
so the rule is checked by CI, not by discipline.

---

## 3. Project structure

```text
api/
├── DevHub.sln
├── Dockerfile
├── src/
│   ├── DevHub.Api/
│   │   ├── Endpoints/           # Minimal API modules, one folder per feature (§5.1)
│   │   ├── Middleware/          # correlation id, exception handling
│   │   ├── Filters/
│   │   ├── Extensions/          # API-only helpers (ProblemDetails mapping, results)
│   │   ├── Program.cs           # composition root: AddApplication() + AddInfrastructure()
│   │   └── appsettings*.json
│   ├── DevHub.Application/
│   │   ├── DependencyInjection.cs   # AddApplication()
│   │   ├── Common/              # Result, PagedResult, ICurrentUser, interfaces
│   │   ├── Auth/
│   │   ├── Workspaces/
│   │   ├── Projects/
│   │   ├── Issues/
│   │   ├── Comments/
│   │   ├── Labels/
│   │   ├── Releases/
│   │   ├── Environments/
│   │   ├── Deployments/
│   │   ├── CICD/
│   │   ├── Search/
│   │   ├── Dashboard/
│   │   ├── Notifications/
│   │   └── Attachments/
│   ├── DevHub.Domain/
│   │   ├── Common/              # Entity, AggregateRoot, IDomainEvent
│   │   ├── Users/
│   │   ├── Workspaces/
│   │   ├── Projects/
│   │   ├── Issues/
│   │   ├── Environments/
│   │   ├── Deployments/
│   │   ├── Releases/
│   │   ├── CICD/
│   │   └── Notifications/
│   └── DevHub.Infrastructure/
│       ├── DependencyInjection.cs   # AddInfrastructure(IConfiguration)
│       ├── Persistence/
│       │   ├── DevHubDbContext.cs
│       │   ├── Configurations/  # IEntityTypeConfiguration per entity
│       │   ├── Migrations/
│       │   └── Repositories/
│       ├── Identity/            # password hashing, JWT issuing
│       ├── Storage/             # S3 client wrapper
│       ├── Aws/
│       └── Integrations/        # GitHub webhook parsing
└── tests/
    ├── DevHub.Domain.UnitTests/
    ├── DevHub.Application.UnitTests/
    ├── DevHub.Api.IntegrationTests/
    └── DevHub.ArchitectureTests/
```

Inside `Application`, one folder per module, and inside it one file per use case:

```text
Issues/
├── ChangeIssueStatus/
│   ├── ChangeIssueStatusCommand.cs
│   ├── ChangeIssueStatusHandler.cs
│   └── ChangeIssueStatusValidator.cs
├── GetIssues/
│   ├── GetIssuesQuery.cs
│   └── GetIssuesHandler.cs
└── Contracts/IssueDto.cs
```

---

## 4. CQRS-lite

Not full CQRS — one database, no event sourcing. But commands and queries are separated:

- **Commands** load an aggregate through a repository, call a domain method, save. They return
  a DTO or `Result`.
- **Queries** bypass repositories and project directly from `DbContext` with `AsNoTracking()`
  and `.Select(...)` into a DTO. Never load a whole aggregate just to read one field.

**Decision (DEVHUB-002): plain handler classes, no MediatR.** Endpoint methods inject the exact
handler interface they need and call it:

```csharp
public interface ICommandHandler<in TCommand, TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface IQueryHandler<in TQuery, TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
```

`AddApplication()` registers every implementation of these in the assembly as scoped. The
dependency is visible in the constructor and "go to definition" lands on the handler rather
than on `IMediator.Send`. The cost is that pipeline behaviours are not free — they are added
as decorators over these two interfaces when a ticket first needs them.

Pipeline behaviours, in order:

```text
Request → Logging → Validation → Authorization → Transaction → Handler
```

---

## 5. Request flow

```text
HTTP request
   ↓ model binding + route constraints
Endpoint (Minimal API)
   ↓ dispatch
Command / Query
   ↓ validator (FluentValidation) → 400 ProblemDetails on failure
Handler
   ↓ authorization check (workspace/project membership) → 404 if not visible
Domain method (enforces invariants) → raises domain events
   ↓
Repository / DbContext
   ↓ SaveChangesAsync (one transaction)
Domain event dispatch (after commit)
   ↓ activity rows, notifications
DTO
   ↓
HTTP response
```

Worked example — `PATCH /api/issues/{id}/status`:

```text
IssueEndpoints.ChangeStatusAsync
   ↓
ChangeIssueStatusCommand(IssueId, NewStatus)
   ↓
ChangeIssueStatusHandler
   ├─ load Issue via IIssueRepository.GetByIdAsync
   ├─ assert current user is a member of issue's project  → else 404
   ├─ issue.ChangeStatus(newStatus, currentUserId)        → throws DomainException on bad transition
   ├─ SaveChangesAsync
   └─ dispatch IssueStatusChanged
          ├─ IssueActivityHandler   → append issue_activities row
          └─ NotificationHandler    → notify assignee (not the actor)
   ↓
IssueDto
```

### 5.1 Organizing endpoints (Minimal APIs)

**Decision: Minimal APIs, not MVC controllers, organized as one static module per feature
and registered explicitly.** Controllers were used until the first auth endpoints; they were
replaced before the API grew.

```text
DevHub.Api/Endpoints/
├── ApiEndpoints.cs              # MapApiEndpoints(): the /api group + one line per module
├── Auth/AuthEndpoints.cs        # MapAuthEndpoints(this IEndpointRouteBuilder api)
└── Issues/IssueEndpoints.cs     # MapIssueEndpoints(...)
```

Rules:

- **`ApiEndpoints.MapApiEndpoints()` is the index of the API.** It creates the `/api` group and
  calls each module's `Map<Feature>Endpoints()`. A new module is one new line there. No assembly
  scanning: an unlisted module fails visibly (its routes 404) instead of silently.
- **Group-level defaults live on the `/api` group:** `RequireAuthorization()` (secure by default;
  anonymous endpoints opt out with `AllowAnonymous()`; a global fallback policy backs it up for
  anything mapped outside the group, and `EndpointProtectionTests` holds the allow-list of
  anonymous routes — auth-spec.md §6) and the ProblemDetails responses every
  endpoint can return (`ProducesProblem(400/401/404/500)`).
- **Each module creates its own sub-group** (`api.MapGroup("/projects/{projectId:guid}/issues")`)
  and applies what is specific to it: `WithTags`, rate-limit policy, extra `ProducesProblem`.
- **Handlers are named private static methods, not inline lambdas**, returning
  `Results<Ok<TDto>, ProblemHttpResult>` (or `Created<…>`, `NoContent`, …). The typed result is
  what OpenAPI reads the success schema from, so no `[ProducesResponseType]` is needed.
- **Endpoints stay thin:** bind, call the handler interface, map `Result` with
  `Error.ToProblem(httpContext)` (`Extensions/ErrorResultExtensions.cs`). Workspace/project
  scoping is checked in the Application handler; an endpoint filter may add a check, never
  replace it.
- **Input validation is FluentValidation only.** Minimal APIs do not run implicit model
  validation, so nothing competes with the validators. An unreadable body is rejected by the
  framework with a bare 400, which `UseStatusCodePages` turns into ProblemDetails.

Test-only endpoints (e.g. `GET /api/test/whoami`) are added by the integration-test harness
through an `IStartupFilter`, never in `DevHub.Api`.

---

## 6. Domain model conventions

```csharp
public abstract class Entity
{
    public Guid Id { get; protected set; }
}

public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _events = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _events;
    protected void Raise(IDomainEvent e) => _events.Add(e);
    public void ClearDomainEvents() => _events.Clear();
}
```

Rules:

- Private setters. State changes only through intention-revealing methods
  (`issue.Assign(userId, actorId)`, not `issue.AssigneeId = ...`).
- Private parameterless constructor for EF Core; a real constructor or static factory for code.
- Invariant violations throw `DomainException`, translated to `409`/`422` by the exception
  middleware.
- Enums are C# enums persisted as `text` (readable in the database, safe to reorder).

---

## 7. Domain event dispatch

Collect events from tracked aggregates in `SaveChangesAsync`, clear them, commit, then dispatch:

```csharp
var events = ChangeTracker.Entries<AggregateRoot>()
    .SelectMany(e => e.Entity.DomainEvents).ToList();
foreach (var e in ChangeTracker.Entries<AggregateRoot>()) e.Entity.ClearDomainEvents();
var result = await base.SaveChangesAsync(ct);
foreach (var e in events) await _dispatcher.Dispatch(e, ct);
return result;
```

Handlers must be idempotent and must never throw into the request path: wrap each dispatch in
try/catch, log failures, and let the business write stand. A missing notification is an
annoyance; a rolled-back status change is a bug.

Activity rows are the exception — they are part of the audit contract, so write them inside the
same transaction as the change that produced them.

---

## 8. Authorization

Three levels, all enforced in the Application layer:

1. **Authenticated** — valid access token; `ICurrentUser` exposes `UserId`.
2. **Workspace scope** — the target resource resolves to a workspace the user is a member of.
3. **Role** — `Owner` for destructive/settings operations; `Member` for everyday work.

A helper resolves scope in one query:

```csharp
Task<WorkspaceAccess?> GetAccessForProject(Guid projectId, Guid userId, CancellationToken ct);
// null → the caller gets 404, never 403
```

Never trust a `workspaceId` from the request body — always derive it from the resource.

---

## 9. Cross-cutting

| Concern | Approach |
|---|---|
| Validation | FluentValidation, one validator per command, returns field-level errors |
| Errors | `ProblemDetails` middleware, see [`api-conventions.md`](api-conventions.md) |
| Logging | Serilog, structured, correlation id enriched, see [`observability-spec.md`](observability-spec.md) |
| Time | `ITimeProvider` abstraction — never `DateTime.UtcNow` in domain or handlers, so tests can freeze time |
| Ids | `Guid` v7 via a `IIdGenerator` for index locality |
| Mapping | Hand-written `ToDto()` extension methods. No AutoMapper — the magic costs more than it saves here |
| Transactions | One per command via a pipeline behaviour or explicit `SaveChangesAsync` |
| Pagination | `PagedResult<T>` with `items`, `page`, `pageSize`, `totalCount` |

---

## 10. Module boundaries

Modules communicate through the Application layer, never by reaching into another module's
domain internals:

- ✅ `Deployments` raises `DeploymentStatusChanged`; `Notifications` handles it.
- ✅ `Dashboard` queries read models across modules (read-only projection).
- ❌ `Deployments` calling `NotificationService.CreateAsync` directly and knowing its rules.

The one deliberate exception is the dashboard: it is a read model that crosses modules by design
(see DEVHUB-082).

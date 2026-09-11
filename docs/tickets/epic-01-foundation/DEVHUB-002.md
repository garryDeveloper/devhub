# DEVHUB-002 — Create the ASP.NET Core solution with layered projects

|  |  |
|---|---|
| **Epic** | EPIC 1 — Foundation & project setup |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | M |
| **Depends on** | DEVHUB-001 |
| **Specs** | [`backend-architecture.md`](../../tech-specs/backend-architecture.md) |

## Context

The four-project layout is what makes the "modular monolith" claim real. Getting the project
references right now means the architecture cannot silently rot later.

## Scope

**In:** solution, four class libraries + web API, project references, DI extension points,
architecture test project.
**Out:** endpoints beyond a placeholder, EF Core (DEVHUB-006), auth (EPIC 2).

## Tasks

- [ ] `dotnet new sln -n DevHub` in `api/`.
- [ ] Create `src/DevHub.Api` (webapi), `src/DevHub.Application`, `src/DevHub.Domain`,
      `src/DevHub.Infrastructure` (classlib), all targeting .NET 10.
- [ ] Wire references: `Api → Application`, `Api → Infrastructure` (composition root only),
      `Application → Domain`, `Infrastructure → Application, Domain`.
- [ ] Add `Domain/Common`: `Entity`, `AggregateRoot`, `IDomainEvent`, `DomainException`.
- [ ] Add `Application/Common`: `Result`, `PagedResult<T>`, `ICurrentUser`, `ITimeProvider`.
- [ ] Add `AddApplication()` and `AddInfrastructure(IConfiguration)` extension methods; keep
      `Program.cs` to a dozen readable lines.
- [ ] Decide and record: MediatR vs plain handler classes. Write the decision in a comment at
      the top of `AddApplication()`.
- [ ] Create `tests/DevHub.ArchitectureTests` with NetArchTest and the two layering rules.
- [ ] Enable `<TreatWarningsAsErrors>`, `<Nullable>enable</Nullable>`,
      `<ImplicitUsings>enable</ImplicitUsings>` in a `Directory.Build.props`.

## Acceptance criteria

- [ ] `dotnet build DevHub.sln` succeeds with zero warnings.
- [ ] `DevHub.Domain.csproj` has **no** `PackageReference`.
- [ ] Architecture tests fail if a reference to EF Core is added to Domain (verify by adding it
      temporarily, watching the test fail, then reverting).
- [ ] `dotnet run --project src/DevHub.Api` starts and serves the placeholder endpoint.

## Technical notes

- The `Api → Infrastructure` reference is a pragmatic exception for DI registration. If it starts
  being used in controllers, introduce a `DevHub.Api.Composition` project instead.
- Nullable reference types on from day one. Turning them on later is a large, boring migration.

## Learning goals

Dependency direction and why it matters, composition root, what a domain project free of
frameworks buys you, enforcing architecture with a test instead of a convention.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §2.

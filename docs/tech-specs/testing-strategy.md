# Testing strategy

What to test, where, and with what. Written to be affordable for one developer: a small number
of high-value tests, run on every push.

---

## 1. Shape

```text
        ▲  E2E (Playwright)            2–4 flows, smoke only
       ─┼─ Integration (API + real DB) the main investment
      ──┼── Unit (domain + handlers)   fast, many
     ───┴─── Architecture tests        1 file, guards the layering
```

Deliberately **integration-heavy**. The interesting bugs in this project live at the boundaries:
authorization, EF Core mapping, migrations, webhook idempotency. Unit-testing a mocked
repository would prove none of them.

---

## 2. Projects

```text
tests/
├── DevHub.Domain.UnitTests/         xUnit + FluentAssertions
├── DevHub.Application.UnitTests/    xUnit + NSubstitute (ports only)
├── DevHub.Api.IntegrationTests/     WebApplicationFactory + Testcontainers PostgreSQL
└── DevHub.ArchitectureTests/        NetArchTest
web/   → Vitest + Testing Library, Playwright in web/e2e
mobile/→ Jest + React Native Testing Library
```

---

## 3. Unit tests — domain

Test business rules, not property assignment.

```csharp
[Fact]
public void ChangeStatus_FromDone_ToInReview_IsRejected()
{
    var issue = IssueFactory.Done();
    var act = () => issue.ChangeStatus(IssueStatus.InReview, actorId);
    act.Should().Throw<DomainException>()
       .Which.Code.Should().Be("invalid-status-transition");
}

[Fact]
public void ChangeStatus_RaisesDomainEvent()
{
    var issue = IssueFactory.Todo();
    issue.ChangeStatus(IssueStatus.InProgress, actorId);
    issue.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<IssueStatusChanged>();
}
```

Cover: every status transition (legal and illegal), issue key formatting, release publish rules,
deployment status transitions including out-of-order callbacks, workspace last-owner protection,
attachment owner XOR constraint.

Do **not** write `Constructor_SetsTitle` tests. They assert the language works.

---

## 4. Unit tests — application handlers

Only where there is orchestration logic worth isolating: mention parsing, notification fan-out
rules, dashboard aggregation shaping, filter parsing. Mock **ports** (`IFileStorage`,
`ITimeProvider`), never `DbContext`.

Banned: tests whose only assertion is `mock.Received().Method()`. If that is all a test can say,
the behaviour belongs in an integration test.

---

## 5. Integration tests — the core investment

`WebApplicationFactory<Program>` + Testcontainers PostgreSQL. A real database, real migrations,
real HTTP pipeline, real serialization.

```csharp
public class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine").Build();

    protected HttpClient Client = default!;

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
        var factory = new DevHubApiFactory(_db.GetConnectionString());
        await factory.MigrateAsync();          // real EF migrations, every run
        Client = factory.CreateClient();
    }
    public Task DisposeAsync() => _db.DisposeAsync().AsTask();
}
```

Rules:

- One container per test **class**; reset data between tests by truncating tables (Respawn) —
  restarting a container per test is far too slow.
- Never mock the database. Mock only external services: S3 (`IFileStorage` fake) and outbound
  HTTP.
- Tests authenticate through the real `/api/auth/login` endpoint, so the auth pipeline is
  covered by every other test for free.

Every endpoint gets at least: the happy path, one validation failure, and one authorization
failure. The authorization test is the one that matters most:

```csharp
[Fact]
public async Task GetIssue_FromAnotherWorkspace_Returns404()
{
    var (_, issueId) = await Seed.IssueInWorkspaceOf(userA);
    await Auth.LoginAs(userB);
    var res = await Client.GetAsync($"/api/issues/{issueId}");
    res.StatusCode.Should().Be(HttpStatusCode.NotFound);   // NOT 403
}
```

Also integration-test: webhook signature validation, duplicate webhook idempotency, refresh
token rotation and reuse detection, issue key sequence under concurrency, dashboard response
shape, search workspace scoping.

---

## 6. Architecture tests

One file, run in CI, guarding the rules in
[`backend-architecture.md`](backend-architecture.md):

```csharp
Types.InAssembly(DomainAssembly)
     .ShouldNot().HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Amazon", "Microsoft.AspNetCore")
     .GetResult().IsSuccessful.Should().BeTrue();

Types.InAssembly(ApplicationAssembly)
     .ShouldNot().HaveDependencyOn("DevHub.Infrastructure")
     .GetResult().IsSuccessful.Should().BeTrue();
```

Cheap, and it catches the layering violation on the day it is introduced instead of six months
later.

---

## 7. Frontend tests

**Web (Vitest + Testing Library):** test behaviour through the DOM, not implementation.

- Forms: validation messages, disabled submit while pending, server field errors mapped.
- Optimistic update rollback on a failed mutation.
- Filter serialization/deserialization round-trip through the URL.
- Loading / empty / error states render for a representative list.

Mock the network with MSW at the HTTP boundary, not by stubbing hooks.

**E2E (Playwright), 2–4 flows only:**

1. Register → create workspace → create project → create issue → move it on the board.
2. Login → open issue → comment → change status → verify the activity feed.
3. (After Phase 2) Post a signed deployment webhook → dashboard shows the deployment.

Run E2E on `main` and nightly, not on every PR — keep the PR loop fast.

**Mobile (Jest + RNTL):** auth bootstrap logic (token present/absent/expired), issue list
rendering states, status change bottom sheet. Navigation snapshot tests are low value; skip them.

---

## 8. Test data

- Builders with sensible defaults, not fixtures shared across files:
  `IssueBuilder.New().InProgress().AssignedTo(user).Build()`.
- Deterministic time via `ITimeProvider` — never `DateTime.UtcNow` in a test.
- Seed helpers per aggregate (`Seed.WorkspaceWithProject(...)`) returning ids, so tests read as
  scenarios, not as setup scripts.
- No shared mutable state between tests; every test creates what it needs.

---

## 9. Coverage

No coverage percentage gate. Instead, these are non-negotiable:

- Every domain rule has a test.
- Every endpoint has an authorization test.
- Every migration runs in CI against an empty database.
- Every bug fixed gets a regression test in the same PR.

A 90% coverage number full of assertion-free tests is worse than 60% of tests that would
actually fail when something breaks.

---

## 10. CI

```text
PR:    dotnet build → domain/app unit tests → architecture tests → integration tests
       web: typecheck + lint + unit tests + build
       mobile: typecheck + lint + unit tests
main:  the above + E2E smoke + deploy staging + health check
```

Integration tests need Docker on the runner (`ubuntu-latest` has it). Target: PR feedback in
under 8 minutes. If it grows past that, parallelize by project before deleting tests.

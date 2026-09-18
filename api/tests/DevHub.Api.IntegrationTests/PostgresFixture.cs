using DevHub.Application.Common;
using DevHub.Domain.Common;
using DevHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace DevHub.Api.IntegrationTests;

/// <summary>
/// A throwaway PostgreSQL 16 container with the DevHub migrations applied.
/// </summary>
/// <remarks>
/// One container per test <b>class</b> (xUnit disposes an IAsyncLifetime fixture once per
/// class), not per test: a container per test turns a two-minute suite into twenty
/// (testing-strategy.md §5).
/// <para>
/// The image matches infrastructure/docker-compose.yml and RDS. Tests never touch the
/// developer's local database, so they cannot pass because of a row someone left behind.
/// </para>
/// <para>
/// DEVHUB-011 replaces this with the full <c>IntegrationTestBase</c>: a
/// <c>WebApplicationFactory</c> over the real HTTP pipeline plus Respawn to truncate between
/// tests. What is here is only what DEVHUB-006 has to prove.
/// </para>
/// </remarks>
public sealed class PostgresFixture : IAsyncLifetime
{
    // The image goes in the constructor: Testcontainers 4.15 obsoleted the parameterless
    // builder, so the .WithImage() form in testing-strategy.md §5 no longer compiles.
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();

        // The real migrations, not EnsureCreated(). EnsureCreated builds the schema from the
        // model and would happily pass while the migration itself is broken — which is the one
        // thing this fixture exists to catch.
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public DevHubDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DevHubDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new DevHubDbContext(options, new FixedClock(), new RecordingDispatcher());
    }

    /// <summary>Frozen time, so audit stamping is asserted against a known value.</summary>
    public sealed class FixedClock : ITimeProvider
    {
        public static readonly DateTimeOffset Instant = new(2026, 9, 12, 10, 30, 0, TimeSpan.Zero);

        public DateTimeOffset UtcNow => Instant;
    }

    /// <summary>Captures what the DbContext dispatched, so the ordering can be asserted.</summary>
    public sealed class RecordingDispatcher : IDomainEventDispatcher
    {
        public List<IDomainEvent> Dispatched { get; } = [];

        public Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken)
        {
            Dispatched.AddRange(domainEvents);
            return Task.CompletedTask;
        }
    }
}

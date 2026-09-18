using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DevHub.Infrastructure.Persistence;

/// <summary>
/// Builds a <see cref="DevHubDbContext"/> for the <c>dotnet ef</c> tooling.
/// </summary>
/// <remarks>
/// Without this, <c>dotnet ef</c> boots DevHub.Api to find a context — which means generating a
/// migration would depend on the whole application starting, including its CORS allow-list and
/// every other bit of startup validation. The factory keeps schema work independent of the app.
/// <para>
/// It is used only by the CLI. It is never part of the running application, and the connection
/// string below is never used at runtime.
/// </para>
/// </remarks>
internal sealed class DevHubDbContextFactory : IDesignTimeDbContextFactory<DevHubDbContext>
{
    private const string ConnectionStringVariable = "ConnectionStrings__Default";

    /// <summary>
    /// Matches infrastructure/docker-compose.yml. Not a secret and not production configuration:
    /// migrations are generated against a local throwaway database.
    /// </summary>
    private const string LocalDefault =
        "Host=localhost;Port=5432;Database=devhub;Username=devhub;Password=devhub";

    public DevHubDbContext CreateDbContext(string[] args)
    {
        // Environment variable first, so a machine whose compose stack moved off 5432
        // (infrastructure/.env) does not have to edit this file.
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = LocalDefault;
        }

        var options = new DbContextOptionsBuilder<DevHubDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        // The design-time context only builds the model and emits SQL; it never saves, so the
        // clock and the dispatcher are never called.
        return new DevHubDbContext(options, new DesignTimeStub(), new DesignTimeStub());
    }

    private sealed class DesignTimeStub : Application.Common.ITimeProvider, Application.Common.IDomainEventDispatcher
    {
        public DateTimeOffset UtcNow => throw new InvalidOperationException(
            "The design-time context does not save changes, so it never needs the clock.");

        public Task DispatchAsync(
            IReadOnlyCollection<Domain.Common.IDomainEvent> domainEvents,
            CancellationToken cancellationToken) => throw new InvalidOperationException(
            "The design-time context does not save changes, so it never dispatches events.");
    }
}

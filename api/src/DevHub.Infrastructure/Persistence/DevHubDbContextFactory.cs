using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DevHub.Infrastructure.Persistence;

/// <summary>
/// Builds a <see cref="DevHubDbContext"/> for the <c>dotnet ef</c> tooling.
/// </summary>
/// <remarks>
/// Without this, <c>dotnet ef</c> boots DevHub.Api to find a context — which means generating a
/// migration would depend on the whole application starting, including its CORS allow-list and
/// every other bit of startup validation. The factory keeps schema work independent of the app.
/// <para>
/// It is used only by the CLI. It is never part of the running application.
/// </para>
/// <para>
/// DEVHUB-113: it reads the connection string from the <b>same user-secrets store as the API</b>
/// (DevHub.Infrastructure declares DevHub.Api's <c>UserSecretsId</c>). Before, it read only an
/// environment variable and silently fell back to port 5432 — so on a machine whose database was
/// on 5433, <c>dotnet ef database update</c> migrated a different server than the API used.
/// </para>
/// </remarks>
internal sealed class DevHubDbContextFactory : IDesignTimeDbContextFactory<DevHubDbContext>
{
    /// <summary>
    /// Matches infrastructure/docker-compose.yml's defaults and api/secrets.example.json. Not a
    /// secret and not production configuration: the last resort when nothing is configured.
    /// </summary>
    private const string LocalDefault =
        "Host=localhost;Port=5432;Database=devhub;Username=devhub;Password=devhub";

    public DevHubDbContext CreateDbContext(string[] args)
    {
        // Same precedence as the API's host: user-secrets, then environment variables on top
        // (ConnectionStrings__Default), so a one-off override still works.
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(DevHubDbContextFactory).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Default");

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

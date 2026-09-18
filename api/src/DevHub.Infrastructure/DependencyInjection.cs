using DevHub.Application.Common;
using DevHub.Infrastructure.Persistence;
using DevHub.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevHub.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the adapters behind the Application layer's ports: persistence (DEVHUB-006),
    /// identity (EPIC 2), storage and AWS clients (EPIC 15). This is the only place where a
    /// concrete implementation is bound to an interface, and the only reason DevHub.Api
    /// references DevHub.Infrastructure at all.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddPersistence(configuration);

        // The production clock. Singleton: it holds no state and reads the OS clock each call.
        services.AddSingleton<ITimeProvider, SystemTimeProvider>();

        return services;
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        // Fail at boot, not on the first request that touches the database. Npgsql happily
        // accepts an empty connection string here and only throws when a connection is opened,
        // which would turn a deployment misconfiguration into a 500 discovered by a user.
        // Same reasoning as the CORS allow-list guard in DEVHUB-003.
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Default is not configured. Set it for this environment, e.g. "
                + "ConnectionStrings__Default=\"Host=localhost;Port=5432;Database=devhub;Username=devhub;Password=devhub\".");
        }

        services.AddDbContext<DevHubDbContext>(options => options
            .UseNpgsql(connectionString)
            // snake_case tables and columns (database-schema.md §1), applied by convention.
            // Renaming by hand in each configuration is how a schema ends up half-converted.
            .UseSnakeCaseNamingConvention());

        // No handlers exist yet; see the type's remarks. Registered now so the DbContext can be
        // resolved and the dispatch ordering is exercised from the start.
        services.AddScoped<IDomainEventDispatcher, NoOpDomainEventDispatcher>();
    }
}

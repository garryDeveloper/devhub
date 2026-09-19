using DevHub.Application.Common;
using DevHub.Infrastructure.Identity;
using DevHub.Infrastructure.Integrations;
using DevHub.Infrastructure.Persistence;
using DevHub.Infrastructure.Storage;
using DevHub.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevHub.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the adapters behind the Application layer's ports: persistence (DEVHUB-006),
    /// identity (EPIC 2), storage and AWS clients (EPIC 15). This is the only place where a
    /// concrete implementation is bound to an interface, and the only reason DevHub.Api
    /// references DevHub.Infrastructure at all.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddPersistence(configuration);
        services.AddConfigurationOptions(configuration, environment);

        // The production clock. Singleton: it holds no state and reads the OS clock each call.
        services.AddSingleton<ITimeProvider, SystemTimeProvider>();

        return services;
    }

    /// <summary>
    /// Binds every strongly-typed options class and validates it at startup (DEVHUB-007), not on
    /// the first request that needs it. Jwt and Storage have no consumer yet (EPIC 2 and EPIC 15
    /// add them) — validating now means the configuration contract is fixed once, and no later
    /// ticket can ship a feature that silently depends on an unvalidated setting.
    /// </summary>
    private static void AddConfigurationOptions(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .ValidateDataAnnotations()
            // AccessKey/SecretKey are not [Required] on the type itself — real AWS must never
            // set them (storage-s3-spec.md §7, instance role only). Development is the one
            // environment that talks to MinIO and needs static credentials for it.
            .Validate(
                options => !environment.IsDevelopment()
                    || (!string.IsNullOrWhiteSpace(options.AccessKey) && !string.IsNullOrWhiteSpace(options.SecretKey)),
                "Storage:AccessKey and Storage:SecretKey are required in Development (MinIO). Set them via "
                    + "user-secrets, e.g. dotnet user-secrets set \"Storage:AccessKey\" \"devhub\".")
            .ValidateOnStart();

        services.AddOptions<WebhookOptions>()
            .Bind(configuration.GetSection(WebhookOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
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

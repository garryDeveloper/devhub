using DevHub.Application.Auth;
using DevHub.Application.Common;
using DevHub.Application.Users;
using DevHub.Infrastructure.Identity;
using DevHub.Infrastructure.Integrations;
using DevHub.Infrastructure.Persistence;
using DevHub.Infrastructure.Persistence.Repositories;
using DevHub.Infrastructure.Storage;
using DevHub.Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

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

        // Stateless wrapper over PasswordHasher<User> (DEVHUB-013): singleton for the same
        // reason as the clock above.
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        services.AddIdentity();

        return services;
    }

    /// <summary>
    /// Token issuing, token validation and login lockout (DEVHUB-014/015), refresh token
    /// cleanup (DEVHUB-016), the current user (DEVHUB-018).
    /// </summary>
    private static void AddIdentity(this IServiceCollection services)
    {
        services.AddSingleton<ITokenService, TokenService>();

        // Singleton on purpose: the lockout state IS the instance. Scoped would forget every
        // failure at the end of the request. See the type's remarks for the multi-instance limit.
        services.AddSingleton<ILoginThrottle, InMemoryLoginThrottle>();

        // Registered as itself too, so tests can resolve it and run one purge on demand; the
        // hosted-service registration forwards to that same singleton instance.
        services.AddSingleton<RefreshTokenCleanupService>();
        services.AddHostedService(provider => provider.GetRequiredService<RefreshTokenCleanupService>());

        // Scoped: it answers for one request. Handlers get the caller from here, never from a
        // workspaceId or userId in the request body (auth-spec.md §5).
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configured through the options system rather than inline in AddJwtBearer(...) so it
        // reads the same validated JwtOptions the issuer uses (DEVHUB-007), resolved when the
        // first token is validated rather than read eagerly from IConfiguration here.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                // Keep claim names as they are on the wire ("sub", not the
                // ".../nameidentifier" URI the legacy mapping rewrites them to), so
                // ICurrentUser reads the same "sub" TokenService wrote.
                bearer.MapInboundClaims = false;

                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = JwtSigningKey.From(jwt),

                    // Pin the algorithm: a token must not be able to choose how it is verified.
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],

                    ValidateLifetime = true,
                    RequireExpirationTime = true,

                    // The default is five minutes, which silently turns a 15-minute token into
                    // a 20-minute one and makes expiry tests flaky (DEVHUB-015 notes).
                    ClockSkew = TimeSpan.Zero,

                    NameClaimType = JwtRegisteredClaimNames.Name,
                };
            });
    }

    /// <summary>
    /// Binds every strongly-typed options class and validates it at startup (DEVHUB-007), not on
    /// the first request that needs it. Storage has no consumer yet (EPIC 15 adds it; Jwt's arrived
    /// in DEVHUB-015) — validating now means the configuration contract is fixed once, and no later
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

        // Scoped like the DbContext they wrap: one unit of work per request, shared by every
        // repository the handler touches, so one SaveChanges commits all of it together.
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    }
}

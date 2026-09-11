using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

namespace DevHub.Api.Extensions;

/// <summary>
/// Everything DevHub.Api itself registers: controllers, CORS, Swagger and health checks.
/// The Application and Infrastructure layers register their own services; this is the API's
/// share, kept out of Program.cs so the composition root stays readable (DEVHUB-002).
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddApiControllers();
        services.AddApiCors(configuration, environment);
        services.AddApiSwagger();

        // No checks registered yet, so this only answers "is the process alive". The database
        // and storage checks, and the /health/live and /health/ready split, are DEVHUB-110.
        services.AddHealthChecks();

        return services;
    }

    private static void AddApiControllers(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Conventions.Add(new RoutePrefixConvention("api"));

            // Every endpoint can fail these three ways and every error is an RFC 7807
            // ProblemDetails (docs/tech-specs/api-conventions.md), so declare it once here
            // instead of on every action. Actions still declare their own success type:
            // [ProducesResponseType<IssueDto>(StatusCodes.Status200OK)].
            // EPIC 2 adds 401 and 403 to this list once authentication exists.
            options.Filters.Add(new ProducesResponseTypeAttribute(typeof(ProblemDetails), StatusCodes.Status400BadRequest));
            options.Filters.Add(new ProducesResponseTypeAttribute(typeof(ProblemDetails), StatusCodes.Status404NotFound));
            options.Filters.Add(new ProducesResponseTypeAttribute(typeof(ProblemDetails), StatusCodes.Status500InternalServerError));
        });
    }

    private static void AddApiCors(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        // An empty allow-list blocks every browser origin without logging anything, so a typo
        // in deployment configuration would look like a frontend bug. Refuse to start instead.
        // Development is exempt so a fresh clone runs before any configuration is set.
        if (allowedOrigins.Length == 0 && !environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Cors:AllowedOrigins is empty. Set it for this environment, e.g. "
                + "Cors__AllowedOrigins__0=https://devhub.example.com.");
        }

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                // No AllowCredentials(): access tokens travel in the Authorization header, not
                // in cookies. If EPIC 2 moves the refresh token into a cookie, add it here —
                // and never alongside AllowAnyOrigin(), which browsers silently ignore.
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
    }

    private static void AddApiSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "DevHub API",
                Version = "v1",
                Description = "The DevHub API is the backend for the DevHub application.",
            });
        });
    }
}

using System.Globalization;
using System.Threading.RateLimiting;
using DevHub.Api.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace DevHub.Api.Extensions;

/// <summary>
/// Everything DevHub.Api itself registers: CORS, Swagger, rate limiting and health checks.
/// Endpoints are mapped rather than registered; see Endpoints/ApiEndpoints.cs.
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
        services.AddApiCors(configuration, environment);
        services.AddApiSwagger();
        services.AddApiRateLimiting(configuration);

        // RFC 7807 bodies for everything the endpoints do not produce themselves: unhandled
        // exceptions (UseExceptionHandler) and bare status codes such as JwtBearer's 401
        // challenge (UseStatusCodePages). Never a stack trace, in any environment.
        services.AddProblemDetails();

        // The authentication scheme itself (JwtBearer) is registered by AddInfrastructure(),
        // next to the signing options it shares with the token issuer.
        services.AddAuthorization();

        // No checks registered yet, so this only answers "is the process alive". The database
        // and storage checks, and the /health/live and /health/ready split, are DEVHUB-110.
        services.AddHealthChecks();

        return services;
    }

    private static void AddApiCors(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // An empty allow-list blocks every browser origin without logging anything, so a typo
        // in deployment configuration would look like a frontend bug. Refuse to start instead.
        // Development is exempt so a fresh clone runs before any configuration is set. This is
        // environment-conditional, so it is a `.Validate()` predicate (DEVHUB-007) rather than a
        // data annotation on CorsOptions, which has no notion of the hosting environment.
        services.AddOptions<CorsOptions>()
            .Bind(configuration.GetSection(CorsOptions.SectionName))
            .Validate(
                options => options.AllowedOrigins.Length > 0 || environment.IsDevelopment(),
                "Cors:AllowedOrigins is empty. Set it for this environment, e.g. "
                    + "Cors__AllowedOrigins__0=https://devhub.example.com.")
            .ValidateOnStart();

        // AddCors builds the policy once, at registration time, so it needs the value now rather
        // than through IOptions<T> (which is only safe to resolve once the container is built).
        var allowedOrigins = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()?.AllowedOrigins ?? [];

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

    /// <summary>
    /// Per-IP limit on the auth endpoints (auth-spec.md §6): 10 requests a minute. The per-account
    /// login lockout is separate, in the Application layer (ILoginThrottle).
    /// </summary>
    /// <remarks>
    /// Partitioned by <c>RemoteIpAddress</c>. Behind the Elastic Beanstalk load balancer that is
    /// the balancer's address, not the client's, until forwarded headers are configured (EPIC 16):
    /// until then every client would share one bucket. Local and Docker runs are unaffected.
    /// </remarks>
    private static void AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RateLimitingOptions>()
            .Bind(configuration.GetSection(RateLimitingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimitingOptions.AuthPolicy, httpContext =>
            {
                // Read per partition, not captured at registration, so tests can override the
                // limit through configuration like any other setting.
                var limits = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

                return RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limits.AuthPermitLimit,
                        Window = TimeSpan.FromMinutes(1),

                        // Reject, do not queue: holding a brute-forcer's requests open until the
                        // window resets only spends our connections on them.
                        QueueLimit = 0,
                    });
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                var problemDetails = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemDetails.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails =
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests.",
                        Detail = "Too many requests from this address. Try again later.",
                        Type = "https://devhub.dev/errors/rate_limited",
                    },
                });
            };
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

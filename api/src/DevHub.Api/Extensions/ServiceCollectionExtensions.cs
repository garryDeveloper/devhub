using System.Globalization;
using System.Threading.RateLimiting;
using DevHub.Api.Configuration;
using DevHub.Api.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

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

        // Absent-vs-null for every PATCH body (DEVHUB-019).
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new OptionalJsonConverterFactory()));

        services.AddApiSwagger();
        services.AddApiRateLimiting(configuration);

        // RFC 7807 bodies for everything the endpoints do not produce themselves: unhandled
        // exceptions (UseExceptionHandler) and bare status codes such as JwtBearer's 401
        // challenge (UseStatusCodePages). Never a stack trace, in any environment.
        services.AddProblemDetails(options => options.CustomizeProblemDetails = UseDevHubAuthProblemTypes);

        // The authentication scheme itself (JwtBearer) is registered by AddInfrastructure(),
        // next to the signing options it shares with the token issuer.
        services.AddAuthorization(options =>
        {
            // Fail closed (DEVHUB-018): any endpoint with no authorization metadata of its own —
            // including one mapped outside the /api group, which never gets its
            // RequireAuthorization() — requires an authenticated user. Forgetting to mark an
            // endpoint breaks a public one; it never opens a private one.
            // It also applies when no endpoint matched at all, so an anonymous request for an
            // unknown route gets 401, not 404: routes are not discoverable without a token.
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        // No checks registered yet, so this only answers "is the process alive". The database
        // and storage checks, and the /health/live and /health/ready split, are DEVHUB-110.
        services.AddHealthChecks();

        return services;
    }

    /// <summary>
    /// Gives the framework's own 401 and 403 — a missing or invalid token, a failed policy — a
    /// DevHub <c>type</c> (api-conventions.md §4), so a client can branch on it like any other error.
    /// </summary>
    /// <remarks>
    /// Runs for every ProblemDetails written, so it only rewrites the ones that still carry the
    /// framework's default RFC 9110 type. A 401 an endpoint returned on purpose, such as
    /// <c>auth.invalid_refresh_token</c>, already has a DevHub type and is left alone.
    /// The <c>WWW-Authenticate</c> header JwtBearer sets (e.g. <c>error="invalid_token"</c>) is
    /// untouched: it is the standard place for the reason, and the body stays generic.
    /// </remarks>
    private static void UseDevHubAuthProblemTypes(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        if (problem.Type?.StartsWith(ErrorResultExtensions.ProblemTypeBase, StringComparison.Ordinal) == true)
        {
            return;
        }

        switch (problem.Status)
        {
            case StatusCodes.Status401Unauthorized:
                problem.Type = ErrorResultExtensions.ProblemTypeBase + "auth.unauthenticated";
                problem.Title = "Authentication required.";
                problem.Detail = "A valid access token is required.";
                break;
            case StatusCodes.Status403Forbidden:
                problem.Type = ErrorResultExtensions.ProblemTypeBase + "auth.forbidden";
                problem.Title = "Forbidden.";
                problem.Detail = "You do not have permission to perform this action.";
                break;
        }
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

        // Registered after AddSwaggerGen, whose own registration is a TryAdd: the last one wins.
        // Built over the same HTTP JSON options the endpoints use, as Swashbuckle's default is.
        services.AddTransient<ISerializerDataContractResolver>(provider =>
            new OptionalAwareDataContractResolver(new JsonSerializerDataContractResolver(
                provider.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>().Value.SerializerOptions)));
    }
}

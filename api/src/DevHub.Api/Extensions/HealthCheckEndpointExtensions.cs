using System.Reflection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace DevHub.Api.Extensions;

public static class HealthCheckEndpointExtensions
{
    // Looks like "1.8.2+9f2c1ab" once the build stamps the commit. The sha is kept rather than
    // trimmed: DevHub exists to tell deployments apart, and two builds of version 1.8.2 are
    // only distinguishable by it.
    private static readonly string Version = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion ?? "unknown";

    /// <summary>
    /// Maps the anonymous <c>GET /health</c> endpoint.
    /// </summary>
    /// <remarks>
    /// It is absent from the OpenAPI document on purpose. Endpoints created by
    /// <c>MapHealthChecks</c> carry no ApiExplorer metadata — <c>.WithTags()</c> does not change
    /// that — and /health is infrastructure for the load balancer and the CI health gate rather
    /// than API surface the frontend consumes. It is documented in
    /// docs/tech-specs/api-endpoints.md §15.
    /// </remarks>
    public static IEndpointConventionBuilder MapApiHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            // The full response shape from observability-spec.md §5 is emitted now, while
            // "checks" is still empty, so DEVHUB-110 only fills it in instead of changing a
            // contract the CI health gate already depends on.
            // Status codes stay on the defaults: Healthy and Degraded 200, Unhealthy 503.
            ResponseWriter = (context, report) => context.Response.WriteAsJsonAsync(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.ToDictionary(entry => entry.Key, entry => entry.Value.Status.ToString()),
                version = Version,
                durationMs = (int)report.TotalDuration.TotalMilliseconds,
            }),
        });
    }
}

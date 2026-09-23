using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DevHub.Api.IntegrationTests.Harness;

/// <summary>
/// Test-only endpoint at <c>GET /api/test/whoami</c> that requires a valid access token and echoes
/// its subject. Lives in the test assembly, so it can never ship.
/// </summary>
/// <remarks>
/// Minimal API endpoints are mapped in Program.cs, which a test cannot edit, so this appends a
/// second routing stage <em>after</em> the real pipeline (<c>next(app)</c> first). A request only
/// reaches it when the real routes did not match, and it has already passed through the real
/// exception handler, status-code pages and authentication — so a 401 here is the same
/// ProblemDetails a real endpoint would return. Only authorization runs again, because it
/// authorizes against the endpoint that routing selected.
/// </remarks>
internal sealed class ProtectedTestEndpoint : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        next(app);

        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints => endpoints
            .MapGet("/api/test/whoami", (HttpContext context) => new { Sub = context.User.FindFirst("sub")?.Value })
            .RequireAuthorization());
    };
}

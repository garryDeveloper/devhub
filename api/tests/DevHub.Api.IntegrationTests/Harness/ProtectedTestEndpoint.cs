using DevHub.Application.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DevHub.Api.IntegrationTests.Harness;

/// <summary>
/// Test-only endpoints under <c>/api/test</c>: <c>whoami</c> echoes the token's subject,
/// <c>current-user</c> echoes <see cref="ICurrentUser"/>, <c>forbidden</c> always answers 403 to an
/// authenticated caller. They live in the test assembly, so they can never ship.
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
        app.UseEndpoints(endpoints =>
        {
            endpoints
                .MapGet("/api/test/whoami", (HttpContext context) => new { Sub = context.User.FindFirst("sub")?.Value })
                .RequireAuthorization();

            // ICurrentUser resolved from the request scope, the same way a handler gets it.
            endpoints
                .MapGet("/api/test/current-user", (ICurrentUser currentUser) =>
                    new { currentUser.UserId, currentUser.IsAuthenticated })
                .RequireAuthorization();

            // A policy nobody satisfies: the only way to get the framework's 403 until the role
            // policies of DEVHUB-026 exist.
            endpoints
                .MapGet("/api/test/forbidden", () => Results.Ok())
                .RequireAuthorization(policy => policy.RequireAssertion(_ => false));
        });
    };
}

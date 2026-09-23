using DevHub.Api.Endpoints.Auth;

namespace DevHub.Api.Endpoints;

/// <summary>
/// The index of the HTTP API: every feature module is mapped here, one line each, under the
/// <c>/api</c> group. Reading this method answers "what does the API expose?".
/// </summary>
/// <remarks>
/// Explicit registration rather than assembly scanning: a module that is not listed here fails
/// visibly (its routes 404 in the first test), and "go to definition" works from this file.
/// Each module is a static class in <c>Endpoints/&lt;Feature&gt;/</c> exposing one
/// <c>Map&lt;Feature&gt;Endpoints(this IEndpointRouteBuilder api)</c> extension.
/// </remarks>
public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api")
            // Secure by default (CLAUDE.md §5): an endpoint is anonymous only if it says so with
            // AllowAnonymous(), never because someone forgot an attribute.
            .RequireAuthorization()

            // Every endpoint can fail these ways and every error is an RFC 7807 ProblemDetails
            // (api-conventions.md), so the OpenAPI document declares it once here. Endpoints
            // declare their own success type through their TypedResults return type.
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        api.MapAuthEndpoints();

        return endpoints;
    }
}

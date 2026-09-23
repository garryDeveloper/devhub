using DevHub.Api.Configuration;
using DevHub.Api.Extensions;
using DevHub.Application.Auth.Contracts;
using DevHub.Application.Auth.Login;
using DevHub.Application.Auth.Register;
using DevHub.Application.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DevHub.Api.Endpoints.Auth;

/// <summary>
/// <c>/api/auth</c> (api-endpoints.md §1). Thin by design: bind, dispatch, map the result.
/// Validation, uniqueness and hashing all happen behind the handler interface.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder api)
    {
        var auth = api.MapGroup("/auth")
            .WithTags("Auth")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingOptions.AuthPolicy)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        auth.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .ProducesProblem(StatusCodes.Status409Conflict);

        auth.MapPost("/login", LoginAsync)
            .WithName("Login");

        return api;
    }

    // Named methods rather than inline lambdas: they read like actions, show up by name in stack
    // traces, and their Results<...> return type is what OpenAPI reads the success shape from.
    private static async Task<Results<Created<AuthResponse>, ProblemHttpResult>> RegisterAsync(
        RegisterCommand command,
        ICommandHandler<RegisterCommand, Result<AuthResponse>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);

        // 201 without a Location header: the new resource is "the current user", which gets its
        // own URL (/api/me) in DEVHUB-019. The body already carries everything the client needs.
        return result.IsSuccess
            ? TypedResults.Created((string?)null, result.Value)
            : result.Error!.ToProblem(httpContext);
    }

    private static async Task<Results<Ok<AuthResponse>, ProblemHttpResult>> LoginAsync(
        LoginCommand command,
        ICommandHandler<LoginCommand, Result<AuthResponse>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error!.ToProblem(httpContext);
    }
}

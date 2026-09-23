using DevHub.Api.Configuration;
using DevHub.Api.Extensions;
using DevHub.Application.Auth.Contracts;
using DevHub.Application.Auth.Login;
using DevHub.Application.Auth.Refresh;
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

        auth.MapPost("/refresh", RefreshAsync)
            .WithName("RefreshToken")
            // Outside the shared register+login budget. That limit exists to slow password
            // guessing; a 256-bit token cannot be guessed, and a refresh every 15 minutes from
            // each user behind one office NAT would otherwise spend other people's login attempts.
            .DisableRateLimiting();

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

    private static async Task<Results<Ok<RefreshResponse>, ProblemHttpResult>> RefreshAsync(
        RefreshCommand command,
        ICommandHandler<RefreshCommand, Result<RefreshResponse>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error!.ToProblem(httpContext);
    }
}

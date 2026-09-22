using DevHub.Api.Configuration;
using DevHub.Api.Extensions;
using DevHub.Application.Auth.Contracts;
using DevHub.Application.Auth.Login;
using DevHub.Application.Auth.Register;
using DevHub.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DevHub.Api.Controllers;

/// <summary>
/// <c>/api/auth</c> (api-endpoints.md §1). Thin by design: bind, dispatch, map the result.
/// Validation, uniqueness and hashing all happen behind the handler interface.
/// </summary>
[ApiController]
[Route("auth")]
[AllowAnonymous]
public sealed class AuthController(
    ICommandHandler<RegisterCommand, Result<AuthResponse>> register,
    ICommandHandler<LoginCommand, Result<AuthResponse>> login)
    : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting(RateLimitingOptions.AuthPolicy)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register(RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await register.HandleAsync(command, cancellationToken);

        // 201 without a Location header: the new resource is "the current user", which gets its
        // own URL (/api/me) in DEVHUB-019. The body already carries everything the client needs.
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : this.ToProblem(result.Error!);
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitingOptions.AuthPolicy)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await login.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : this.ToProblem(result.Error!);
    }
}

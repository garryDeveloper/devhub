using DevHub.Api.Extensions;
using DevHub.Application.Common;
using DevHub.Application.Users.Contracts;
using DevHub.Application.Users.Me;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DevHub.Api.Endpoints.Me;

/// <summary>
/// <c>/api/me</c> (api-endpoints.md §1): the caller's own profile. Protected by the <c>/api</c>
/// group's <c>RequireAuthorization()</c>; the handlers resolve "me" from <c>ICurrentUser</c>.
/// </summary>
public static class MeEndpoints
{
    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder api)
    {
        var me = api.MapGroup("/me").WithTags("Me");

        me.MapGet("", GetMeAsync).WithName("GetMe");

        me.MapPatch("", UpdateMeAsync).WithName("UpdateMe");

        return api;
    }

    private static async Task<Results<Ok<MeDto>, ProblemHttpResult>> GetMeAsync(
        IQueryHandler<GetMeQuery, Result<MeDto>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetMeQuery(), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error!.ToProblem(httpContext);
    }

    // Returns UserDto, not MeDto: a profile edit does not change memberships, so re-reading them
    // here would be a query the client has no use for.
    private static async Task<Results<Ok<UserDto>, ProblemHttpResult>> UpdateMeAsync(
        UpdateMeCommand command,
        ICommandHandler<UpdateMeCommand, Result<UserDto>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error!.ToProblem(httpContext);
    }
}

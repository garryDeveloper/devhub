using DevHub.Api.Extensions;
using DevHub.Application.Common;
using DevHub.Application.Workspaces.Contracts;
using DevHub.Application.Workspaces.Create;
using DevHub.Application.Workspaces.Get;
using DevHub.Application.Workspaces.List;
using DevHub.Application.Workspaces.Members.Add;
using DevHub.Application.Workspaces.Members.ChangeRole;
using DevHub.Application.Workspaces.Members.List;
using DevHub.Application.Workspaces.Members.Remove;
using DevHub.Application.Workspaces.Update;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DevHub.Api.Endpoints.Workspaces;

/// <summary>
/// <c>/api/workspaces</c> (api-endpoints.md §2). Protected by the <c>/api</c> group. Membership
/// and role are checked in the handlers, not here (CLAUDE.md §5): an endpoint cannot see whether
/// the caller is a member without a query, and that query belongs to the Application layer.
/// </summary>
public static class WorkspaceEndpoints
{
    public static IEndpointRouteBuilder MapWorkspaceEndpoints(this IEndpointRouteBuilder api)
    {
        var workspaces = api.MapGroup("/workspaces").WithTags("Workspaces");

        workspaces.MapPost("", CreateAsync)
            .WithName("CreateWorkspace")
            .ProducesProblem(StatusCodes.Status409Conflict);

        workspaces.MapGet("", ListAsync).WithName("ListWorkspaces");

        workspaces.MapGet("/{workspaceId:guid}", GetAsync).WithName("GetWorkspace");

        workspaces.MapPatch("/{workspaceId:guid}", UpdateAsync)
            .WithName("UpdateWorkspace")
            .ProducesProblem(StatusCodes.Status403Forbidden);

        var members = workspaces.MapGroup("/{workspaceId:guid}/members").WithTags("Workspace members");

        members.MapGet("", ListMembersAsync).WithName("ListWorkspaceMembers");

        members.MapPost("", AddMemberAsync)
            .WithName("AddWorkspaceMember")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        members.MapPatch("/{memberId:guid}", ChangeMemberRoleAsync)
            .WithName("ChangeWorkspaceMemberRole")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        members.MapDelete("/{memberId:guid}", RemoveMemberAsync)
            .WithName("RemoveWorkspaceMember")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return api;
    }

    private static async Task<Results<Created<WorkspaceDto>, ProblemHttpResult>> CreateAsync(
        CreateWorkspaceCommand command,
        ICommandHandler<CreateWorkspaceCommand, Result<WorkspaceDto>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Created($"/api/workspaces/{result.Value.Id}", result.Value)
            : result.Error!.ToProblem(httpContext);
    }

    private static async Task<Ok<IReadOnlyList<WorkspaceDto>>> ListAsync(
        IQueryHandler<ListWorkspacesQuery, IReadOnlyList<WorkspaceDto>> handler,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await handler.HandleAsync(new ListWorkspacesQuery(), cancellationToken));
    }

    private static async Task<Results<Ok<WorkspaceDto>, ProblemHttpResult>> GetAsync(
        Guid workspaceId,
        IQueryHandler<GetWorkspaceQuery, Result<WorkspaceDto>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetWorkspaceQuery(workspaceId), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error!.ToProblem(httpContext);
    }

    private static async Task<Results<Ok<WorkspaceDto>, ProblemHttpResult>> UpdateAsync(
        Guid workspaceId,
        UpdateWorkspaceRequest request,
        ICommandHandler<UpdateWorkspaceCommand, Result<WorkspaceDto>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // The id comes from the route only. The body has no workspaceId to disagree with it.
        var result = await handler.HandleAsync(new UpdateWorkspaceCommand(workspaceId, request.Name), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error!.ToProblem(httpContext);
    }

    /// <summary>
    /// The PATCH body. Separate from <see cref="UpdateWorkspaceCommand"/> because the command
    /// also carries the route id, which must never be bindable from JSON. A <c>slug</c> in the
    /// body is ignored: it is immutable, and there is no field to bind it to.
    /// </summary>
    public sealed record UpdateWorkspaceRequest(string? Name);

    private static async Task<Results<Ok<IReadOnlyList<MemberDto>>, ProblemHttpResult>> ListMembersAsync(
        Guid workspaceId,
        IQueryHandler<ListMembersQuery, Result<IReadOnlyList<MemberDto>>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListMembersQuery(workspaceId), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error!.ToProblem(httpContext);
    }

    private static async Task<Results<Created<MemberDto>, ProblemHttpResult>> AddMemberAsync(
        Guid workspaceId,
        AddMemberRequest request,
        ICommandHandler<AddMemberCommand, Result<MemberDto>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new AddMemberCommand(workspaceId, request.Email, request.Role), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Created($"/api/workspaces/{workspaceId}/members/{result.Value.Id}", result.Value)
            : result.Error!.ToProblem(httpContext);
    }

    private static async Task<Results<Ok<MemberDto>, ProblemHttpResult>> ChangeMemberRoleAsync(
        Guid workspaceId,
        Guid memberId,
        ChangeMemberRoleRequest request,
        ICommandHandler<ChangeMemberRoleCommand, Result<MemberDto>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ChangeMemberRoleCommand(workspaceId, memberId, request.Role), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error!.ToProblem(httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RemoveMemberAsync(
        Guid workspaceId,
        Guid memberId,
        ICommandHandler<RemoveMemberCommand, Result> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RemoveMemberCommand(workspaceId, memberId), cancellationToken);

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Error!.ToProblem(httpContext);
    }

    public sealed record AddMemberRequest(string? Email, string? Role);

    /// <summary>Separate from <see cref="ChangeMemberRoleCommand"/> for the same reason as
    /// <see cref="UpdateWorkspaceRequest"/>: the route ids must never be bindable from JSON.</summary>
    public sealed record ChangeMemberRoleRequest(string? Role);
}

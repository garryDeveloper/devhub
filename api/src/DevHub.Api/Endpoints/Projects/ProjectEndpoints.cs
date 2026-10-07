using DevHub.Api.Extensions;
using DevHub.Application.Common;
using DevHub.Application.Projects.Archive;
using DevHub.Application.Projects.Contracts;
using DevHub.Application.Projects.Get;
using DevHub.Application.Projects.Members.Add;
using DevHub.Application.Projects.Members.List;
using DevHub.Application.Projects.Members.Remove;
using DevHub.Application.Projects.Update;
using DevHub.Application.Workspaces.Contracts;
using DevHub.Application.Workspaces.Members.Add;
using DevHub.Application.Workspaces.Members.List;
using DevHub.Domain.Workspaces;
using Microsoft.AspNetCore.Http.HttpResults;
using static DevHub.Api.Endpoints.Workspaces.WorkspaceEndpoints;

namespace DevHub.Api.Endpoints.Projects;

/// <summary>
/// <c>/api/projects</c> (api-endpoints.md §3): the flat, single-project routes. Creation and
/// listing are nested under <c>/api/workspaces/{workspaceId}/projects</c> instead (see
/// <c>WorkspaceEndpoints</c>) — once a project has its own id, every other operation addresses it
/// directly, the same split the ticket's learning goals call out.
/// </summary>
public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder api)
    {
        var projects = api.MapGroup("/projects").WithTags("Projects");

        projects.MapGet("/{projectId:guid}", GetAsync).WithName("GetProject");

        projects.MapPatch("/{projectId:guid}", UpdateAsync)
            .WithName("UpdateProject")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        projects.MapDelete("/{projectId:guid}", ArchiveAsync)
            .WithName("ArchiveProject")
            .ProducesProblem(StatusCodes.Status403Forbidden);

        var members = projects.MapGroup("/{projectId:guid}/members").WithTags("Project members");

        members.MapGet("", ListMembersAsync).WithName("ListProjectMembers");
        members.MapPost("", AddMemberAsync)
            .WithName("AddProjectMember")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status409Conflict);

        members.MapDelete("/{memberId:guid}", RemoveMemberAsync)
            .WithName("RemoveProjectMember")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }

    private static async Task<Results<Ok<IReadOnlyList<ProjectMemberDto>>, ProblemHttpResult>> ListMembersAsync(
        Guid projectId,
        IQueryHandler<ListProjectMembersQuery, Result<IReadOnlyList<ProjectMemberDto>>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListProjectMembersQuery(projectId), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error!.ToProblem(httpContext);
    }

    private static async Task<Results<Created<ProjectMemberDto>, ProblemHttpResult>> AddMemberAsync(
        Guid projectId,
        AddProjectMemberRequest request,
        ICommandHandler<AddProjectMemberCommand, Result<ProjectMemberDto>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new AddProjectMemberCommand(projectId, request.UserId), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Created($"/api/projects/{projectId}/members/{result.Value.Id}", result.Value)
            : result.Error!.ToProblem(httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RemoveMemberAsync(
        Guid projectId,
        Guid memberId,
        ICommandHandler<RemoveProjectMemberCommand, Result> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RemoveProjectMemberCommand(projectId, memberId), cancellationToken);

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Error!.ToProblem(httpContext);
    }

    private static async Task<Results<Ok<ProjectDto>, ProblemHttpResult>> GetAsync(
        Guid projectId,
        IQueryHandler<GetProjectQuery, Result<ProjectDto>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetProjectQuery(projectId), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error!.ToProblem(httpContext);
    }

    /// <summary>
    /// Every field is <see cref="Optional{T}"/>, including <c>key</c>: unlike
    /// <c>UpdateWorkspaceRequest</c>'s slug (silently ignored), a <c>key</c> present in the body
    /// must be rejected with a 422 — so it needs a field to bind to, not an absent one.
    /// </summary>
    public sealed record UpdateProjectRequest(
        Optional<string> Name,
        Optional<string?> Description,
        Optional<string?> Color,
        Optional<string?> Icon,
        Optional<string?> Key);

    private static async Task<Results<Ok<ProjectDto>, ProblemHttpResult>> UpdateAsync(
        Guid projectId,
        UpdateProjectRequest request,
        ICommandHandler<UpdateProjectCommand, Result<ProjectDto>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProjectCommand(
            projectId, request.Name, request.Description, request.Color, request.Icon, request.Key);
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error!.ToProblem(httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ArchiveAsync(
        Guid projectId,
        ICommandHandler<ArchiveProjectCommand, Result> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ArchiveProjectCommand(projectId), cancellationToken);

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Error!.ToProblem(httpContext);
    }

    public sealed record AddProjectMemberRequest(Guid UserId);
}

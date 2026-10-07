using DevHub.Application.Common;
using DevHub.Application.Projects.Contracts;
using DevHub.Application.Users;
using DevHub.Application.Users.Contracts;
using DevHub.Application.Workspaces;
using DevHub.Application.Workspaces.Access;

namespace DevHub.Application.Projects.Members.Add;

/// <summary>
/// Adds a user to a project (DEVHUB-032). "Owner" here is the caller's role in the project's
/// <i>workspace</i> — <c>ProjectRole</c> has no owner value of its own, per-project roles being
/// post-MVP. The target user must already be a workspace member: that rule spans the
/// <c>Project</c> and <c>Workspace</c> aggregates, so it is checked here rather than inside
/// <see cref="DevHub.Domain.Projects.Project.AddMember"/>.
/// </summary>
public sealed class AddProjectMemberHandler(
    IProjectAccessService accessService,
    IWorkspaceRepository workspaces,
    IProjectRepository projects,
    IUserRepository users,
    ITimeProvider timeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AddProjectMemberCommand, Result<ProjectMemberDto>>
{
    public async Task<Result<ProjectMemberDto>> HandleAsync(AddProjectMemberCommand command, CancellationToken cancellationToken)
    {
        var access = (await accessService.ForProjectAsync(command.ProjectId, cancellationToken).ConfigureAwait(false))
            .RequireOwner(ProjectErrors.NotFound);
        if (access.IsFailure)
        {
            return access.Error!;
        }

        var workspace = await workspaces.GetByIdAsync(access.Value.WorkspaceId, cancellationToken).ConfigureAwait(false);
        if (workspace is null)
        {
            return ProjectErrors.NotFound;
        }

        // A user that does not exist trivially is not a workspace member either, so both cases
        // share the same 422 rather than leaking which one it was.
        var user = await users.GetByIdAsync(command.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null || workspace.Members.All(member => member.UserId != command.UserId))
        {
            return ProjectErrors.NotWorkspaceMember;
        }

        var project = await projects.GetByIdAsync(command.ProjectId, cancellationToken).ConfigureAwait(false);
        if (project is null)
        {
            return ProjectErrors.NotFound;
        }

        // The common-case courtesy; Project.AddMember (backed by the unique index on
        // (project_id, user_id)) is the guarantee against two concurrent adds of the same person.
        if (project.Members.Any(member => member.UserId == command.UserId))
        {
            return ProjectErrors.AlreadyMember;
        }

        var member = project.AddMember(command.UserId, timeProvider.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ProjectMemberDto(member.Id, UserDto.From(user), member.Role.ToString(), member.AddedAt);
    }
}

using DevHub.Application.Common;
using DevHub.Application.Workspaces.Access;

namespace DevHub.Application.Projects.Members.Remove;

/// <summary>
/// Removes a project member (DEVHUB-032), owner only — unlike leaving a workspace, there is no
/// self-removal exception here: the ticket and api-endpoints.md §3 mark this endpoint owner-only
/// with no carve-out.
/// </summary>
/// <remarks>
/// Does not yet unassign the member's issues (the ticket's task list says it should): there is no
/// <c>Issue</c> entity in the codebase yet (EPIC 5). Revisit this handler once DEVHUB-036 lands —
/// until then, removing a member can leave their issues assigned to someone no longer in the
/// project.
/// </remarks>
public sealed class RemoveProjectMemberHandler(
    IProjectAccessService accessService,
    IProjectRepository projects,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RemoveProjectMemberCommand, Result>
{
    public async Task<Result> HandleAsync(RemoveProjectMemberCommand command, CancellationToken cancellationToken)
    {
        var access = (await accessService.ForProjectAsync(command.ProjectId, cancellationToken).ConfigureAwait(false))
            .RequireOwner(ProjectErrors.NotFound);
        if (access.IsFailure)
        {
            return Result.Failure(access.Error!);
        }

        var project = await projects.GetByIdAsync(command.ProjectId, cancellationToken).ConfigureAwait(false);
        if (project is null)
        {
            return Result.Failure(ProjectErrors.NotFound);
        }

        var member = project.Members.SingleOrDefault(candidate => candidate.Id == command.MemberId);
        if (member is null)
        {
            return Result.Failure(ProjectErrors.MemberNotFound);
        }

        // TODO(EPIC 5 / Issues, DEVHUB-036+): unassign this user from the project's issues in
        // this same transaction before removing them — the ticket's "easy to forget" warning.
        // There is no Issue entity yet, so there is nothing to unassign.
        project.RemoveMember(member.Id);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}

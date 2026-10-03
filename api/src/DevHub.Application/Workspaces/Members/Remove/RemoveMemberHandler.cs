using DevHub.Application.Common;
using DevHub.Application.Workspaces.Access;
using DevHub.Domain.Workspaces;

namespace DevHub.Application.Workspaces.Members.Remove;

/// <summary>
/// Removes a member, or lets a member remove themselves — "leave" is the same operation as
/// "remove", just without the owner requirement (DEVHUB-025).
/// </summary>
/// <remarks>
/// Does not yet cascade into project memberships (the ticket's task list says it should): there
/// is no <c>Project</c>/<c>ProjectMember</c> entity in the codebase yet (EPIC 4). Revisit this
/// handler when DEVHUB-031/032 land.
/// </remarks>
public sealed class RemoveMemberHandler(
    ICurrentUser currentUser,
    IWorkspaceAccessService accessService,
    IWorkspaceRepository workspaces,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RemoveMemberCommand, Result>
{
    public async Task<Result> HandleAsync(RemoveMemberCommand command, CancellationToken cancellationToken)
    {
        // Membership only, not ownership: a plain member must be able to reach far enough to
        // leave. The owner-only rule for removing someone *else* is enforced below instead.
        var access = (await accessService.ForWorkspaceAsync(command.WorkspaceId, cancellationToken).ConfigureAwait(false))
            .RequireMember(WorkspaceErrors.NotFound);
        if (access.IsFailure)
        {
            return Result.Failure(access.Error!);
        }

        var workspace = await workspaces.GetByIdAsync(command.WorkspaceId, cancellationToken).ConfigureAwait(false);
        if (workspace is null)
        {
            return Result.Failure(WorkspaceErrors.NotFound);
        }

        var member = workspace.Members.SingleOrDefault(candidate => candidate.Id == command.MemberId);
        if (member is null)
        {
            return Result.Failure(WorkspaceErrors.MemberNotFound);
        }

        var isSelf = member.UserId == currentUser.UserIdOrThrow;
        if (!isSelf && access.Value.Role != WorkspaceRole.Owner)
        {
            return Result.Failure(WorkspaceErrors.OwnerRequired);
        }

        var isLastOwner = member.IsOwner && workspace.Members.Count(candidate => candidate.IsOwner) == 1;
        if (isLastOwner)
        {
            return Result.Failure(WorkspaceErrors.LastOwner);
        }

        workspace.RemoveMember(member.Id);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}

using DevHub.Application.Common;
using DevHub.Application.Users;
using DevHub.Application.Users.Contracts;
using DevHub.Application.Workspaces.Access;
using DevHub.Application.Workspaces.Contracts;
using DevHub.Domain.Workspaces;

namespace DevHub.Application.Workspaces.Members.ChangeRole;

/// <summary>
/// Promotes or demotes a member (DEVHUB-025). The last-owner rule is checked here, before
/// calling the aggregate, so the caller gets a clean 422 instead of the domain's generic
/// <c>DomainException</c> — <see cref="Workspace.ChangeMemberRole"/> still re-checks it, so a
/// future caller that skips this handler cannot bypass the rule.
/// </summary>
public sealed class ChangeMemberRoleHandler(
    IProjectAccessService accessService,
    IWorkspaceRepository workspaces,
    IUserRepository users,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ChangeMemberRoleCommand, Result<MemberDto>>
{
    public async Task<Result<MemberDto>> HandleAsync(ChangeMemberRoleCommand command, CancellationToken cancellationToken)
    {
        var access = (await accessService.ForWorkspaceAsync(command.WorkspaceId, cancellationToken).ConfigureAwait(false))
            .RequireOwner(WorkspaceErrors.NotFound);
        if (access.IsFailure)
        {
            return access.Error!;
        }

        var workspace = await workspaces.GetByIdAsync(command.WorkspaceId, cancellationToken).ConfigureAwait(false);
        if (workspace is null)
        {
            return WorkspaceErrors.NotFound;
        }

        var member = workspace.Members.SingleOrDefault(candidate => candidate.Id == command.MemberId);
        if (member is null)
        {
            return WorkspaceErrors.MemberNotFound;
        }

        var role = Enum.Parse<WorkspaceRole>(command.Role!);
        var isLastOwner = member.IsOwner && workspace.Members.Count(candidate => candidate.IsOwner) == 1;
        if (isLastOwner && role != WorkspaceRole.Owner)
        {
            return WorkspaceErrors.LastOwner;
        }

        workspace.ChangeMemberRole(member.Id, role);

        var user = await users.GetByIdAsync(member.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            // The FK cascades membership deletion when a user is deleted, so a member row
            // always points at a live user; this would only happen if that invariant broke.
            throw new InvalidOperationException($"Workspace member {member.Id} has no backing user.");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new MemberDto(member.Id, UserDto.From(user), member.Role.ToString(), member.JoinedAt);
    }
}

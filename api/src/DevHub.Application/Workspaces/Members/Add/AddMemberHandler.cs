using DevHub.Application.Common;
using DevHub.Application.Users;
using DevHub.Application.Users.Contracts;
using DevHub.Application.Workspaces.Access;
using DevHub.Application.Workspaces.Contracts;
using DevHub.Domain.Users;
using DevHub.Domain.Workspaces;

namespace DevHub.Application.Workspaces.Members.Add;

/// <summary>
/// Adds an existing DevHub user to a workspace by email (DEVHUB-025). There is no invitation
/// state in the MVP: the user is a member the moment this succeeds.
/// </summary>
public sealed class AddMemberHandler(
    IProjectAccessService accessService,
    IWorkspaceRepository workspaces,
    IUserRepository users,
    ITimeProvider timeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AddMemberCommand, Result<MemberDto>>
{
    public async Task<Result<MemberDto>> HandleAsync(AddMemberCommand command, CancellationToken cancellationToken)
    {
        var access = (await accessService.ForWorkspaceAsync(command.WorkspaceId, cancellationToken).ConfigureAwait(false))
            .RequireOwner(WorkspaceErrors.NotFound);
        if (access.IsFailure)
        {
            return access.Error!;
        }

        var user = await users.GetByEmailAsync(User.NormalizeEmail(command.Email!), cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return WorkspaceErrors.UserNotFound;
        }

        var workspace = await workspaces.GetByIdAsync(command.WorkspaceId, cancellationToken).ConfigureAwait(false);
        if (workspace is null)
        {
            return WorkspaceErrors.NotFound;
        }

        // The common-case courtesy; Workspace.AddMember (backed by the unique index on
        // (workspace_id, user_id)) is the guarantee against two concurrent invites of the same
        // person, the same split as CreateWorkspaceHandler's slug check.
        if (workspace.Members.Any(member => member.UserId == user.Id))
        {
            return WorkspaceErrors.AlreadyMember;
        }

        var role = Enum.Parse<WorkspaceRole>(command.Role!);
        var member = workspace.AddMember(user.Id, role, timeProvider.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new MemberDto(member.Id, UserDto.From(user), member.Role.ToString(), member.JoinedAt);
    }
}

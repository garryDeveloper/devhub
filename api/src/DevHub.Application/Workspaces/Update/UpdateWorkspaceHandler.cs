using DevHub.Application.Common;
using DevHub.Application.Workspaces.Contracts;

namespace DevHub.Application.Workspaces.Update;

/// <summary>
/// Renames a workspace (DEVHUB-024). The two authorization questions are asked in a fixed order,
/// and the order is the point:
/// <list type="number">
/// <item><b>Scope</b> — is the caller a member? No → 404, exactly as if the workspace did not exist.</item>
/// <item><b>Role</b> — is the caller an owner? No → 403. Safe now: a member already knows it exists.</item>
/// </list>
/// Swapping them would make the 403 an oracle: a stranger could tell real ids from invented ones.
/// </summary>
/// <remarks>
/// Written inline on purpose. DEVHUB-026 moves both checks into <c>IWorkspaceAccessService</c>;
/// this is the pattern it will abstract.
/// </remarks>
public sealed class UpdateWorkspaceHandler(
    ICurrentUser currentUser,
    IWorkspaceRepository workspaces,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateWorkspaceCommand, Result<WorkspaceDto>>
{
    public async Task<Result<WorkspaceDto>> HandleAsync(UpdateWorkspaceCommand command, CancellationToken cancellationToken)
    {
        // The aggregate is loaded anyway to rename it, and it brings its members: both checks
        // are answered from what is already in memory, with no extra query.
        var workspace = await workspaces.GetByIdAsync(command.WorkspaceId, cancellationToken).ConfigureAwait(false);

        var caller = workspace?.Members.SingleOrDefault(member => member.UserId == currentUser.UserIdOrThrow);
        if (workspace is null || caller is null)
        {
            return WorkspaceErrors.NotFound;
        }

        if (!caller.IsOwner)
        {
            return WorkspaceErrors.OwnerRequired;
        }

        workspace.Rename(command.Name!);

        // Renaming to the current name changes nothing, so EF writes nothing and updated_at stays.
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new WorkspaceDto(
            workspace.Id,
            workspace.Name,
            workspace.Slug,
            caller.Role.ToString(),
            workspace.Members.Count,
            workspace.CreatedAt);
    }
}

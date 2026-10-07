using DevHub.Application.Common;
using DevHub.Application.Workspaces.Access;
using DevHub.Application.Workspaces.Contracts;

namespace DevHub.Application.Workspaces.Update;

/// <summary>
/// Renames a workspace (DEVHUB-024). Authorization is <see cref="IProjectAccessService"/>'s
/// (DEVHUB-026): scope first — a non-member gets 404, exactly as if the workspace did not exist —
/// then role — a member who is not an owner gets 403, safe now because they already know it exists.
/// </summary>
public sealed class UpdateWorkspaceHandler(
    IProjectAccessService accessService,
    IWorkspaceRepository workspaces,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateWorkspaceCommand, Result<WorkspaceDto>>
{
    public async Task<Result<WorkspaceDto>> HandleAsync(UpdateWorkspaceCommand command, CancellationToken cancellationToken)
    {
        var access = (await accessService.ForWorkspaceAsync(command.WorkspaceId, cancellationToken).ConfigureAwait(false))
            .RequireOwner(WorkspaceErrors.NotFound);
        if (access.IsFailure)
        {
            return access.Error!;
        }

        // Authorized, so the aggregate is loaded only for someone allowed to change it. Null here
        // means it was deleted between the two queries: the same 404 as never having existed.
        var workspace = await workspaces.GetByIdAsync(command.WorkspaceId, cancellationToken).ConfigureAwait(false);
        if (workspace is null)
        {
            return WorkspaceErrors.NotFound;
        }

        workspace.Rename(command.Name!);

        // Renaming to the current name changes nothing, so EF writes nothing and updated_at stays.
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new WorkspaceDto(
            workspace.Id,
            workspace.Name,
            workspace.Slug,
            access.Value.Role.ToString(),
            workspace.Members.Count,
            workspace.CreatedAt);
    }
}

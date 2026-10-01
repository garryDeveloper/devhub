using DevHub.Application.Common;
using DevHub.Application.Workspaces.Contracts;
using DevHub.Domain.Workspaces;

namespace DevHub.Application.Workspaces.Create;

/// <summary>
/// Creates a workspace with the caller as its only <c>Owner</c> (DEVHUB-024). Input has already
/// passed <see cref="CreateWorkspaceValidator"/>, so the domain will not throw on it.
/// </summary>
public sealed class CreateWorkspaceHandler(
    ICurrentUser currentUser,
    IWorkspaceRepository workspaces,
    ITimeProvider timeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateWorkspaceCommand, Result<WorkspaceDto>>
{
    private const string SlugIndex = "ix_workspaces_slug";

    public async Task<Result<WorkspaceDto>> HandleAsync(CreateWorkspaceCommand command, CancellationToken cancellationToken)
    {
        var name = command.Name!.Trim();
        var slug = command.Slug ?? WorkspaceSlug.FromName(name);

        // Same split as registration (RegisterHandler): this pre-check is the common-case
        // courtesy, the unique index below is the guarantee.
        if (await workspaces.SlugExistsAsync(slug, cancellationToken).ConfigureAwait(false))
        {
            return WorkspaceErrors.SlugTaken;
        }

        var workspace = Workspace.Create(name, currentUser.UserIdOrThrow, timeProvider.UtcNow, slug);
        workspaces.Add(workspace);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException exception) when (exception.ConstraintName == SlugIndex)
        {
            // Two requests read "free" for the same slug; the index let only one INSERT through.
            // Filtered by index name: any other unique violation here would be a bug, and a bug
            // should be a 500, not a misleading "slug taken".
            return WorkspaceErrors.SlugTaken;
        }

        // The creator is the only member, and an owner. CreatedAt was stamped by SaveChanges.
        return new WorkspaceDto(
            workspace.Id,
            workspace.Name,
            workspace.Slug,
            WorkspaceRole.Owner.ToString(),
            workspace.Members.Count,
            workspace.CreatedAt);
    }
}

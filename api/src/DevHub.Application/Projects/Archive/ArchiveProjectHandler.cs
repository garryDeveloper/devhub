using DevHub.Application.Common;
using DevHub.Application.Workspaces.Access;

namespace DevHub.Application.Projects.Archive;

/// <summary>Archives a project (DEVHUB-031). Owner-only, same role as <c>UpdateProjectHandler</c>.
/// Archiving twice is a domain invariant (<c>Project.Archive</c>), not a 422 here: an owner who
/// archives an already-archived project almost certainly raced another request, not a client bug
/// worth a typed error — it is left to surface as the generic domain exception.</summary>
public sealed class ArchiveProjectHandler(
    IWorkspaceAccessService accessService,
    IProjectRepository projects,
    ITimeProvider timeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ArchiveProjectCommand, Result>
{
    public async Task<Result> HandleAsync(ArchiveProjectCommand command, CancellationToken cancellationToken)
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

        if (project.ArchivedAt is null)
        {
            project.Archive(timeProvider.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }
}

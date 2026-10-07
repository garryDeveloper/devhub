using DevHub.Application.Common;
using DevHub.Application.Projects.Contracts;
using DevHub.Application.Workspaces;
using DevHub.Application.Workspaces.Access;
using DevHub.Domain.Projects;

namespace DevHub.Application.Projects.Create;

public sealed class CreateProjectHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ITimeProvider timeProvider,
    IProjectRepository projectRepository,
    IProjectAccessService workspaceAccessService)
    : ICommandHandler<CreateProjectCommand, Result<ProjectDto>>
{
    private const string KeyIndex = "ix_projects_workspace_id_key";

    public async Task<Result<ProjectDto>> HandleAsync(CreateProjectCommand command, CancellationToken cancellationToken)
    {
        // Validate that the user has access to the workspace
        var access = (await workspaceAccessService.ForWorkspaceAsync(command.WorkspaceId, cancellationToken)).RequireMember(WorkspaceErrors.NotFound);
        if (access.IsFailure) return access.Error!;

        if (await projectRepository.KeyExistAsync(command.Key, command.WorkspaceId, cancellationToken))
        {
            return ProjectErrors.KeyAlreadyTaken;
        }

        var project = Project.Create(command.WorkspaceId, command.Name, command.Key, currentUser.UserIdOrThrow, timeProvider.UtcNow);
        project.UpdateDescription(command.Description);
        project.SetAppearance(command.Color, command.Icon);
        projectRepository.Add(project);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException exception) when (exception.ConstraintName == KeyIndex)
        {
            // Two requests read "free" for the same keey; the index let only one INSERT through.
            // Filtered by index name: any other unique violation here would be a bug, and a bug
            // should be a 500, not a misleading "key taken".
            return ProjectErrors.KeyAlreadyTaken;
        }

        return new ProjectDto(project.Id, project.Name, project.Key, project.Description, project.Color, project.Icon);
    }
}

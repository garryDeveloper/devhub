using DevHub.Application.Common;
using DevHub.Application.Projects.Contracts;
using DevHub.Application.Workspaces.Access;

namespace DevHub.Application.Projects.Update;

/// <summary>
/// Renames, re-describes or re-skins a project (DEVHUB-031). Owner-only: same role as every other
/// workspace-level mutation, since there is no per-project role yet (<see cref="ProjectRole"/> has
/// exactly one value). The key is checked before anything else is touched, so a rejected attempt
/// never leaves a partial update staged.
/// </summary>
public sealed class UpdateProjectHandler(
    IWorkspaceAccessService accessService,
    IProjectRepository projects,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateProjectCommand, Result<ProjectDto>>
{
    public async Task<Result<ProjectDto>> HandleAsync(UpdateProjectCommand command, CancellationToken cancellationToken)
    {
        if (command.Key.HasValue)
        {
            return ProjectErrors.KeyIsImmutable;
        }

        var access = (await accessService.ForProjectAsync(command.ProjectId, cancellationToken).ConfigureAwait(false))
            .RequireOwner(ProjectErrors.NotFound);
        if (access.IsFailure)
        {
            return access.Error!;
        }

        // Authorized, so the aggregate is loaded only for someone allowed to change it. Null here
        // means it was deleted/archived between the two queries: the same 404 as never existing.
        var project = await projects.GetByIdAsync(command.ProjectId, cancellationToken).ConfigureAwait(false);
        if (project is null)
        {
            return ProjectErrors.NotFound;
        }

        if (command.Name.HasValue)
        {
            project.Rename(command.Name.Value);
        }

        if (command.Description.HasValue)
        {
            project.UpdateDescription(command.Description.Value);
        }

        // SetAppearance sets both at once, so a PATCH touching only one keeps the other as it was.
        if (command.Color.HasValue || command.Icon.HasValue)
        {
            var color = command.Color.HasValue ? command.Color.Value : project.Color;
            var icon = command.Icon.HasValue ? command.Icon.Value : project.Icon;
            project.SetAppearance(color, icon);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ProjectDto(project.Id, project.Name, project.Key, project.Description, project.Color, project.Icon);
    }
}

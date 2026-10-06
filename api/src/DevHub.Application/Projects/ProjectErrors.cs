using DevHub.Application.Common;

namespace DevHub.Application.Projects;

public static class ProjectErrors
{
    /// <summary>The project does not exist, or the caller is not a member of its workspace. One
    /// error for both, same reasoning as <see cref="Workspaces.WorkspaceErrors.NotFound"/>.</summary>
    public static readonly Error NotFound =
        Error.NotFound("projects.not_found", "The project was not found.");

    public static readonly Error KeyAlreadyTaken =
        Error.Conflict("projects.key_taken", "A project with this key already exists in this workspace.");

    /// <summary>The key is immutable (DEVHUB-031): a PATCH that includes it is rejected outright,
    /// never silently ignored, so the client finds out instead of assuming it worked.</summary>
    public static readonly Error KeyIsImmutable =
        Error.UnprocessableEntity("projects.key_immutable", "A project's key cannot be changed once it is created.");
}

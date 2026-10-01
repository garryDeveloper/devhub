using DevHub.Domain.Workspaces;

namespace DevHub.Application.Workspaces.Access;

/// <summary>
/// What the caller may do with one resource: the workspace it resolves to and the caller's role
/// there. Only ever produced by <see cref="IWorkspaceAccessService"/>, from the resource id —
/// never assembled from a <c>workspaceId</c> the client sent (auth-spec.md §5).
/// </summary>
/// <param name="ProjectId">
/// Null for a workspace-level resource. Set by the project-scoped resolvers (DEVHUB-030 onwards),
/// so a handler that authorized an issue also knows its project without another query.
/// </param>
public sealed record WorkspaceAccess(Guid WorkspaceId, Guid? ProjectId, WorkspaceRole Role);

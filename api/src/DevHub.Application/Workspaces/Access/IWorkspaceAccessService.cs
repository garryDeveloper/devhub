namespace DevHub.Application.Workspaces.Access;

/// <summary>
/// The one place that answers "can the current user touch this resource, and in what role?"
/// (DEVHUB-026, backend-architecture.md §8). Every handler that acts on a workspace-owned
/// resource authorizes through here, then applies <see cref="WorkspaceAccessExtensions"/>.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>
/// <b>The caller is always <c>ICurrentUser</c>.</b> No method takes a user id, so no handler can
/// authorize on behalf of an id that came from a request body.
/// </item>
/// <item>
/// <b>Null means "no access", and it also means "does not exist".</b> The two are deliberately the
/// same answer: both become a 404, so a stranger cannot probe which ids are real.
/// </item>
/// <item>
/// <b>One database round-trip per resolver</b>, a projection with no aggregate loaded — and at most
/// one per resource per request: results are cached for the request's lifetime.
/// </item>
/// </list>
/// </remarks>
public interface IWorkspaceAccessService
{
    Task<WorkspaceAccess?> ForWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken);

    Task<WorkspaceAccess?> ForProjectAsync(Guid projectId, CancellationToken cancellationToken);

    // Each resolver below is added by the ticket that creates its entity, together with its
    // cross-workspace 404 integration test. Uncomment and implement it there.
    //
    // Task<WorkspaceAccess?> ForIssueAsync(Guid issueId, CancellationToken cancellationToken);             // DEVHUB-036
    // Task<WorkspaceAccess?> ForEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken); // DEVHUB-062
    // Task<WorkspaceAccess?> ForDeploymentAsync(Guid deploymentId, CancellationToken cancellationToken);   // DEVHUB-066
    // Task<WorkspaceAccess?> ForReleaseAsync(Guid releaseId, CancellationToken cancellationToken);         // DEVHUB-072
    // Task<WorkspaceAccess?> ForCicdRunAsync(Guid cicdRunId, CancellationToken cancellationToken);         // DEVHUB-077
}

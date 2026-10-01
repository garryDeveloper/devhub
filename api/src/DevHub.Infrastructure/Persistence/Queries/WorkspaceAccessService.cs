using DevHub.Application.Common;
using DevHub.Application.Workspaces.Access;
using DevHub.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;

namespace DevHub.Infrastructure.Persistence.Queries;

/// <summary>
/// <see cref="IWorkspaceAccessService"/> as one projected query per resolver. Scoped: the cache
/// below lives exactly as long as the request, so a handler (or a handler and a filter) asking
/// twice about the same resource costs one query, and nothing leaks between requests or users.
/// </summary>
internal sealed class WorkspaceAccessService(DevHubDbContext dbContext, ICurrentUser currentUser)
    : IWorkspaceAccessService
{
    private readonly Dictionary<(string Resource, Guid Id), WorkspaceAccess?> _cache = [];

    public Task<WorkspaceAccess?> ForWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken) =>
        ResolveAsync("workspace", workspaceId, userId =>
            // Served by the unique index on workspace_members (workspace_id, user_id). The
            // workspaces table is not touched: a membership row cannot outlive its workspace
            // (ON DELETE CASCADE), so "is a member" already implies "exists".
            dbContext.Set<WorkspaceMember>()
                .AsNoTracking()
                .Where(member => member.WorkspaceId == workspaceId && member.UserId == userId)
                .Select(member => new WorkspaceAccess(member.WorkspaceId, null, member.Role))
                .SingleOrDefaultAsync(cancellationToken));

    // Added by the ticket that creates each entity, with its cross-workspace 404 test. Each one
    // is a single query that joins from the resource up to workspace_members for the caller.
    //
    // public Task<WorkspaceAccess?> ForProjectAsync(Guid projectId, CancellationToken cancellationToken) =>
    //     throw new NotImplementedException("DEVHUB-030");
    // public Task<WorkspaceAccess?> ForIssueAsync(Guid issueId, CancellationToken cancellationToken) =>
    //     throw new NotImplementedException("DEVHUB-036");
    // public Task<WorkspaceAccess?> ForEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken) =>
    //     throw new NotImplementedException("DEVHUB-062");
    // public Task<WorkspaceAccess?> ForDeploymentAsync(Guid deploymentId, CancellationToken cancellationToken) =>
    //     throw new NotImplementedException("DEVHUB-066");
    // public Task<WorkspaceAccess?> ForReleaseAsync(Guid releaseId, CancellationToken cancellationToken) =>
    //     throw new NotImplementedException("DEVHUB-072");
    // public Task<WorkspaceAccess?> ForCicdRunAsync(Guid cicdRunId, CancellationToken cancellationToken) =>
    //     throw new NotImplementedException("DEVHUB-077");

    private async Task<WorkspaceAccess?> ResolveAsync(
        string resource, Guid id, Func<Guid, Task<WorkspaceAccess?>> query)
    {
        // A "no access" answer is cached too: null is a result, not a cache miss.
        if (_cache.TryGetValue((resource, id), out var cached))
        {
            return cached;
        }

        var access = await query(currentUser.UserIdOrThrow).ConfigureAwait(false);
        _cache[(resource, id)] = access;

        return access;
    }
}

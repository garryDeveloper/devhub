using DevHub.Application.Common;
using DevHub.Application.Workspaces.Contracts;

namespace DevHub.Application.Workspaces.List;

/// <summary>
/// The caller's workspaces, with their role in each. Returns a plain list rather than a
/// <see cref="Result"/>: there is no expected failure — no memberships is an empty list.
/// </summary>
public sealed class ListWorkspacesHandler(ICurrentUser currentUser, IWorkspaceQueries queries)
    : IQueryHandler<ListWorkspacesQuery, IReadOnlyList<WorkspaceDto>>
{
    public Task<IReadOnlyList<WorkspaceDto>> HandleAsync(ListWorkspacesQuery query, CancellationToken cancellationToken) =>
        queries.ListForUserAsync(currentUser.UserIdOrThrow, cancellationToken);
}

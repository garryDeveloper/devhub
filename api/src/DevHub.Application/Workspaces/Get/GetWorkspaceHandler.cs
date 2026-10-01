using DevHub.Application.Common;
using DevHub.Application.Workspaces.Contracts;

namespace DevHub.Application.Workspaces.Get;

/// <summary>
/// One workspace, if the caller is a member. "Does not exist" and "exists, but not yours" are one
/// query and one answer — 404 — so the response never tells a stranger which ids are real.
/// </summary>
public sealed class GetWorkspaceHandler(ICurrentUser currentUser, IWorkspaceQueries queries)
    : IQueryHandler<GetWorkspaceQuery, Result<WorkspaceDto>>
{
    public async Task<Result<WorkspaceDto>> HandleAsync(GetWorkspaceQuery query, CancellationToken cancellationToken)
    {
        var workspace = await queries
            .GetForUserAsync(query.WorkspaceId, currentUser.UserIdOrThrow, cancellationToken)
            .ConfigureAwait(false);

        return workspace is null ? WorkspaceErrors.NotFound : workspace;
    }
}

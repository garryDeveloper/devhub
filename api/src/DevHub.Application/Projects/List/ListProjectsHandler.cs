using DevHub.Application.Common;
using DevHub.Application.Projects.Contracts;
using DevHub.Application.Workspaces;

namespace DevHub.Application.Projects.List;

/// <summary>
/// Every project in a workspace, if the caller is a member of it (DEVHUB-031). Archived projects
/// are excluded unless <see cref="ListProjectsQuery.IncludeArchived"/> is set.
/// </summary>
public sealed class ListProjectsHandler(ICurrentUser currentUser, IProjectQueries queries)
    : IQueryHandler<ListProjectsQuery, Result<IReadOnlyList<ProjectSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<ProjectSummaryDto>>> HandleAsync(
        ListProjectsQuery query, CancellationToken cancellationToken)
    {
        var projects = await queries
            .ListForWorkspaceAsync(query.WorkspaceId, currentUser.UserIdOrThrow, query.IncludeArchived, cancellationToken)
            .ConfigureAwait(false);

        if (projects is null)
        {
            return WorkspaceErrors.NotFound;
        }

        return Result.Success(projects);
    }
}

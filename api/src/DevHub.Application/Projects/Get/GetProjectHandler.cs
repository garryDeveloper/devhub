using DevHub.Application.Common;
using DevHub.Application.Projects.Contracts;

namespace DevHub.Application.Projects.Get;

/// <summary>
/// One project, if the caller is a member of its workspace. "Does not exist" and "exists, but not
/// yours" are one query and one answer — 404 — same as <c>GetWorkspaceHandler</c>.
/// </summary>
public sealed class GetProjectHandler(ICurrentUser currentUser, IProjectQueries queries)
    : IQueryHandler<GetProjectQuery, Result<ProjectDto>>
{
    public async Task<Result<ProjectDto>> HandleAsync(GetProjectQuery query, CancellationToken cancellationToken)
    {
        var project = await queries
            .GetForUserAsync(query.ProjectId, currentUser.UserIdOrThrow, cancellationToken)
            .ConfigureAwait(false);

        return project is null ? ProjectErrors.NotFound : project;
    }
}

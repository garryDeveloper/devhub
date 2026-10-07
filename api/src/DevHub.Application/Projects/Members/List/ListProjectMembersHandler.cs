using DevHub.Application.Common;
using DevHub.Application.Projects.Contracts;

namespace DevHub.Application.Projects.Members.List;

/// <summary>
/// Every member of a project, visible to any member of its workspace — not only the project's own
/// members. Project membership narrows who can be assigned issues, it does not restrict who may
/// read the project or its member list (domain-model.md, DEVHUB-032).
/// </summary>
public sealed class ListProjectMembersHandler(ICurrentUser currentUser, IProjectQueries queries)
    : IQueryHandler<ListProjectMembersQuery, Result<IReadOnlyList<ProjectMemberDto>>>
{
    public async Task<Result<IReadOnlyList<ProjectMemberDto>>> HandleAsync(ListProjectMembersQuery query, CancellationToken cancellationToken)
    {
        var members = await queries
            .ListMemberAsync(query.ProjectId, currentUser.UserIdOrThrow, cancellationToken)
            .ConfigureAwait(false);

        if (members is null)
        {
            return ProjectErrors.NotFound;
        }

        return Result.Success(members);
    }
}

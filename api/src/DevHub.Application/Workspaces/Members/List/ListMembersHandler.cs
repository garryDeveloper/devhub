using DevHub.Application.Common;
using DevHub.Application.Workspaces.Contracts;

namespace DevHub.Application.Workspaces.Members.List;

/// <summary>
/// Every member of a workspace, if the caller is one of them themselves (DEVHUB-025). Same
/// "does not exist" / "exists, but not yours" collapse as <c>GetWorkspaceHandler</c>.
/// </summary>
public sealed class ListMembersHandler(ICurrentUser currentUser, IWorkspaceQueries queries)
    : IQueryHandler<ListMembersQuery, Result<IReadOnlyList<MemberDto>>>
{
    public async Task<Result<IReadOnlyList<MemberDto>>> HandleAsync(ListMembersQuery query, CancellationToken cancellationToken)
    {
        var members = await queries
            .ListMembersAsync(query.WorkspaceId, currentUser.UserIdOrThrow, cancellationToken)
            .ConfigureAwait(false);

        if (members is null)
        {
            return WorkspaceErrors.NotFound;
        }

        return Result.Success(members);
    }
}

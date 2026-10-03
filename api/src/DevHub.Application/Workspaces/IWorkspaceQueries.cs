using DevHub.Application.Workspaces.Contracts;

namespace DevHub.Application.Workspaces;

/// <summary>
/// The read side of workspaces: projections straight to <see cref="WorkspaceDto"/>, no aggregate
/// loaded, nothing tracked (backend-architecture.md §4). An interface because the projection is
/// EF Core, which DevHub.Application may not reference.
/// </summary>
/// <remarks>
/// Both methods are scoped to <c>userId</c> <b>inside the query</b>: there is no overload that
/// returns a workspace without asking who is looking. A caller cannot forget the membership
/// check, because there is no way to skip it.
/// </remarks>
public interface IWorkspaceQueries
{
    /// <summary>Every workspace <paramref name="userId"/> is a member of, ordered by name.</summary>
    Task<IReadOnlyList<WorkspaceDto>> ListForUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Null when the workspace does not exist <b>or</b> <paramref name="userId"/> is not a member.
    /// The two are deliberately indistinguishable: both become the same 404.
    /// </summary>
    Task<WorkspaceDto?> GetForUserAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Every member of <paramref name="workspaceId"/>, ordered by when they joined — if
    /// <paramref name="userId"/> is one of them. Any member may read the full list
    /// (api-endpoints.md §2), so unlike the role-gated mutations this needs no
    /// <c>IWorkspaceAccessService</c> round trip: membership is the only check, and the query
    /// already makes it. Null for the same two indistinguishable reasons as
    /// <see cref="GetForUserAsync"/>.
    /// </summary>
    Task<IReadOnlyList<MemberDto>?> ListMembersAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);
}

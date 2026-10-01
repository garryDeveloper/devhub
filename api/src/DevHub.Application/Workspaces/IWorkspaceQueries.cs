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
}

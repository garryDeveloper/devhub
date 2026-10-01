using DevHub.Application.Common;
using DevHub.Application.Users.Contracts;
using DevHub.Application.Workspaces;

namespace DevHub.Application.Users.Me;

/// <summary>Turns "I have a token" into "I know who I am and what I can see" (DEVHUB-019).</summary>
public sealed class GetMeHandler(ICurrentUser currentUser, IUserRepository users, IWorkspaceQueries workspaceQueries)
    : IQueryHandler<GetMeQuery, Result<MeDto>>
{
    public async Task<Result<MeDto>> HandleAsync(GetMeQuery query, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(currentUser.UserIdOrThrow, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return MeErrors.AccountNotFound;
        }

        var profile = UserDto.From(user);

        // The same membership-scoped query GET /api/workspaces uses (DEVHUB-024), so /me and the
        // workspace list can never disagree about which workspaces the caller is in.
        var workspaces = await workspaceQueries.ListForUserAsync(user.Id, cancellationToken).ConfigureAwait(false);

        return new MeDto(
            profile.Id,
            profile.Email,
            profile.DisplayName,
            profile.AvatarUrl,
            [.. workspaces.Select(workspace => new WorkspaceSummaryDto(workspace.Id, workspace.Name, workspace.Slug, workspace.Role))]);
    }
}

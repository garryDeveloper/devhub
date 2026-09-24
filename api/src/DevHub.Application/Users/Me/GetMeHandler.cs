using DevHub.Application.Common;
using DevHub.Application.Users.Contracts;

namespace DevHub.Application.Users.Me;

/// <summary>Turns "I have a token" into "I know who I am and what I can see" (DEVHUB-019).</summary>
public sealed class GetMeHandler(ICurrentUser currentUser, IUserRepository users)
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

        // Workspaces do not exist yet (DEVHUB-023). DEVHUB-024 replaces this with the caller's
        // memberships, in the same query shape the workspace list uses.
        return new MeDto(profile.Id, profile.Email, profile.DisplayName, profile.AvatarUrl, Workspaces: []);
    }
}

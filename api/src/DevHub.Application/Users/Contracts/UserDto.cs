using DevHub.Domain.Users;

namespace DevHub.Application.Users.Contracts;

/// <summary>
/// The public shape of a user (api-endpoints.md §1). Deliberately has no <c>PasswordHash</c>;
/// the architecture test <c>PasswordHashExposureRules</c> fails the build if one is ever added.
/// </summary>
/// <param name="AvatarUrl">
/// Always null until EPIC 15: the entity stores an S3 key, and turning it into a URL means
/// presigning it at read time, which needs the storage adapter that ticket builds.
/// </param>
public sealed record UserDto(Guid Id, string Email, string DisplayName, string? AvatarUrl)
{
    public static UserDto From(User user) => new(user.Id, user.Email, user.DisplayName, AvatarUrl: null);
}

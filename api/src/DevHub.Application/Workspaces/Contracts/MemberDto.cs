using DevHub.Application.Users.Contracts;

namespace DevHub.Application.Workspaces.Contracts;

/// <summary>
/// A workspace member as the caller sees it (api-endpoints.md §2): returned by every
/// <c>/api/workspaces/{id}/members</c> endpoint, single or listed.
/// </summary>
/// <param name="Id">The membership's own id — distinct from <see cref="UserDto.Id"/>: it is what
/// <c>PATCH</c>/<c>DELETE .../members/{memberId}</c> address.</param>
public sealed record MemberDto(Guid Id, UserDto User, string Role, DateTimeOffset JoinedAt);

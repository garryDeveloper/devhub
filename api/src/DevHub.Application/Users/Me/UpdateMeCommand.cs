using DevHub.Application.Common;

namespace DevHub.Application.Users.Me;

/// <summary>
/// <c>PATCH /api/me</c>. Each field is <see cref="Optional{T}"/>: absent leaves it alone,
/// present sets it — so <c>{"avatarAttachmentId": null}</c> removes the avatar while <c>{}</c>
/// changes nothing.
/// </summary>
/// <param name="DisplayName">Present-and-null is a validation error: a user always has a name.</param>
/// <param name="AvatarAttachmentId">Null removes the avatar; an id sets it (DEVHUB-090).</param>
public sealed record UpdateMeCommand(Optional<string?> DisplayName, Optional<Guid?> AvatarAttachmentId);

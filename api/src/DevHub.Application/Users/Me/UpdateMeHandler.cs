using DevHub.Application.Common;
using DevHub.Application.Users.Contracts;

namespace DevHub.Application.Users.Me;

/// <summary>
/// Partial update of the caller's own profile (DEVHUB-019). Only the fields present in the
/// request change; the target is always <see cref="ICurrentUser"/>, so no one can edit someone
/// else's profile by sending their id.
/// </summary>
public sealed class UpdateMeHandler(ICurrentUser currentUser, IUserRepository users, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateMeCommand, Result<UserDto>>
{
    public static readonly Error AttachmentNotFound =
        Error.NotFound("attachments.not_found", "The attachment was not found.");

    public async Task<Result<UserDto>> HandleAsync(UpdateMeCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(currentUser.UserIdOrThrow, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return MeErrors.AccountNotFound;
        }

        // Checked before any change is made, so a failure never leaves half an update staged.
        if (command.AvatarAttachmentId.HasValue)
        {
            if (command.AvatarAttachmentId.Value is not null)
            {
                // No attachment exists before DEVHUB-090, so every id is honestly "not found".
                // That ticket replaces this with the lookup: the attachment must exist, be
                // completed, and belong to this user under avatars/{userId}/.
                return AttachmentNotFound;
            }

            user.SetAvatar(null);
        }

        if (command.DisplayName.HasValue)
        {
            // The validator has rejected null and blank values by now.
            user.ChangeDisplayName(command.DisplayName.Value!.Trim());
        }

        // A no-op for {}: EF Core only writes what the change tracker saw change, and updated_at
        // is stamped only on modified rows.
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return UserDto.From(user);
    }
}

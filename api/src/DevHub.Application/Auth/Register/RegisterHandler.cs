using DevHub.Application.Auth.Contracts;
using DevHub.Application.Common;
using DevHub.Application.Users;
using DevHub.Domain.Users;

namespace DevHub.Application.Auth.Register;

/// <summary>
/// Creates an account and signs the new user in. Input has already passed
/// <see cref="RegisterValidator"/> by the time this runs (see <c>ValidationDecorator</c>).
/// </summary>
/// <remarks>
/// Does <b>not</b> create a workspace: the client drives that in onboarding (user-flows.md
/// Flow 1), so "have an account" and "belong to a workspace" stay separate concerns.
/// </remarks>
public sealed class RegisterHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    AuthResponseFactory authResponses,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterCommand, Result<AuthResponse>>
{
    public static readonly Error EmailTaken =
        Error.Conflict("auth.email_taken", "An account with this email already exists.");

    public async Task<Result<AuthResponse>> HandleAsync(RegisterCommand command, CancellationToken cancellationToken)
    {
        // A courtesy, not the guarantee. It saves hashing a password (~100 ms of PBKDF2) for the
        // common case of someone who forgot they already registered. It cannot stop two
        // concurrent requests that both read "free" — see the catch below.
        if (await users.EmailExistsAsync(User.NormalizeEmail(command.Email), cancellationToken).ConfigureAwait(false))
        {
            return EmailTaken;
        }

        var user = User.Create(command.Email, command.DisplayName.Trim(), passwordHasher.Hash(command.Password));
        users.Add(user);

        var response = authResponses.Create(user);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException)
        {
            // The race the pre-check cannot see: another request registered this email between
            // our SELECT and our INSERT. The unique index on users.email rejected the second
            // insert, the transaction rolled back (no user, no refresh token), and the caller
            // gets the same 409 the pre-check would have given. The index is the rule; the
            // pre-check only makes the common case cheaper.
            return EmailTaken;
        }

        return response;
    }
}

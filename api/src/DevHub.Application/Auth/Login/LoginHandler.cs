using DevHub.Application.Auth.Contracts;
using DevHub.Application.Common;
using DevHub.Application.Users;
using DevHub.Domain.Users;

namespace DevHub.Application.Auth.Login;

/// <summary>
/// Exchanges an email and password for tokens (auth-spec.md §2–3).
/// </summary>
/// <remarks>
/// Every credential failure — unknown email, wrong password — returns the same
/// <see cref="InvalidCredentials"/> error, and takes about the same time. Either difference
/// would let anyone test which emails have accounts.
/// </remarks>
public sealed class LoginHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ILoginThrottle loginThrottle,
    AuthResponseFactory authResponses,
    IUnitOfWork unitOfWork)
    : ICommandHandler<LoginCommand, Result<AuthResponse>>
{
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("auth.invalid_credentials", "Invalid email or password.");

    // Hashed once per process, on the first login for an unknown email. Verifying against it
    // costs the same PBKDF2 work as verifying a real user's hash, which is the whole point.
    // A benign race: two first requests may both compute it; either value works.
    private static string? s_dummyHash;

    public async Task<Result<AuthResponse>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var email = User.NormalizeEmail(command.Email);

        // Checked before the password. A locked account must not be a free oracle: verifying
        // first would still tell an attacker whether guess #6 was right.
        if (loginThrottle.GetRemainingLockout(email) is { } retryAfter)
        {
            return Error.TooManyRequests(
                "auth.locked_out",
                "Too many failed login attempts. Try again later.",
                retryAfter);
        }

        var user = await users.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            // Without this, an unknown email answers in ~1 ms and a known one in ~100 ms (the
            // PBKDF2 cost) — a timing oracle for account existence. Burn the same work instead.
            s_dummyHash ??= passwordHasher.Hash(Guid.NewGuid().ToString());
            passwordHasher.Verify(s_dummyHash, command.Password);

            loginThrottle.RecordFailure(email);
            return InvalidCredentials;
        }

        if (!passwordHasher.Verify(user.PasswordHash, command.Password))
        {
            loginThrottle.RecordFailure(email);
            return InvalidCredentials;
        }

        loginThrottle.Reset(email);

        var response = authResponses.Create(user);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return response;
    }
}

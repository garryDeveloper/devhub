using DevHub.Domain.Users;
using FluentValidation;

namespace DevHub.Application.Auth.Register;

/// <summary>
/// Input rules for registration. Password strength is checked here, never in <see cref="User"/>:
/// the entity only ever receives a hash and must never see plaintext (DEVHUB-013 notes).
/// </summary>
/// <remarks>
/// No composition rules — no "must contain a symbol" (auth-spec.md §2). They push people to
/// <c>Password1!</c>; length and a deny-list of the obvious do more for less annoyance.
/// </remarks>
public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public const int MinimumPasswordLength = 10;

    // Bcrypt-style limits do not apply to PBKDF2, but an unbounded input is a free CPU-burning
    // endpoint: hashing a 10 MB "password" costs the server, not the attacker.
    public const int MaximumPasswordLength = 128;

    public RegisterValidator()
    {
        RuleFor(command => command.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required.")
            // 254 is the practical maximum for an address that can actually receive mail.
            .MaximumLength(254).WithMessage("Email must be 254 characters or fewer.")
            .EmailAddress().WithMessage("Email is not a valid email address.");

        RuleFor(command => command.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(MinimumPasswordLength)
                .WithMessage($"Password must be at least {MinimumPasswordLength} characters.")
            .MaximumLength(MaximumPasswordLength)
                .WithMessage($"Password must be {MaximumPasswordLength} characters or fewer.")
            .Must(password => !CommonPasswords.Contains(password))
                .WithMessage("This password is too common. Choose a less predictable one.");

        RuleFor(command => command.DisplayName)
            .Cascade(CascadeMode.Stop)
            // NotEmpty() alone accepts "   ": the handler trims, so that would store "".
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Display name is required.")
            // Matches the varchar(100) column (database-schema.md §2), measured after trimming
            // because the trimmed value is what gets stored.
            .Must(name => name.Trim().Length <= 100).WithMessage("Display name must be 100 characters or fewer.");
    }
}

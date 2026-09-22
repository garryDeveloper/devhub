using FluentValidation;

namespace DevHub.Application.Auth.Login;

/// <summary>
/// Shape only: both fields present. No length or format rules — "wrong password" must be a 401
/// from the handler, not a 400 that tells an attacker which rule their guess broke.
/// </summary>
public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(command => command.Email).NotEmpty().WithMessage("Email is required.");
        RuleFor(command => command.Password).NotEmpty().WithMessage("Password is required.");
    }
}

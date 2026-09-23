using FluentValidation;

namespace DevHub.Application.Auth.Refresh;

/// <summary>
/// Shape only: the field is present. No length or format rule — a malformed token is simply one
/// that is not found, and answers the same 401 as any other bad token.
/// </summary>
public sealed class RefreshValidator : AbstractValidator<RefreshCommand>
{
    public RefreshValidator()
    {
        RuleFor(command => command.RefreshToken).NotEmpty().WithMessage("Refresh token is required.");
    }
}

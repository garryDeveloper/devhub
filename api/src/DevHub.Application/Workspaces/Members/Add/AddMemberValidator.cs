using FluentValidation;

namespace DevHub.Application.Workspaces.Members.Add;

public sealed class AddMemberValidator : AbstractValidator<AddMemberCommand>
{
    public AddMemberValidator()
    {
        RuleFor(command => command.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(254).WithMessage("Email must be 254 characters or fewer.")
            .EmailAddress().WithMessage("Email is not a valid email address.");

        RuleFor(command => command.Role).ValidWorkspaceRole();
    }
}

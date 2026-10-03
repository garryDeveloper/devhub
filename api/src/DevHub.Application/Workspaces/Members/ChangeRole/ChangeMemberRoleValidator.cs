using FluentValidation;

namespace DevHub.Application.Workspaces.Members.ChangeRole;

public sealed class ChangeMemberRoleValidator : AbstractValidator<ChangeMemberRoleCommand>
{
    public ChangeMemberRoleValidator()
    {
        RuleFor(command => command.Role).ValidWorkspaceRole();
    }
}

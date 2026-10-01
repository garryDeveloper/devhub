using FluentValidation;

namespace DevHub.Application.Workspaces.Update;

public sealed class UpdateWorkspaceValidator : AbstractValidator<UpdateWorkspaceCommand>
{
    public UpdateWorkspaceValidator()
    {
        RuleFor(command => command.Name).ValidWorkspaceName();
    }
}

using DevHub.Domain.Projects;
using FluentValidation;

namespace DevHub.Application.Projects.Create;

/// <summary>
/// Everything <see cref="Project.Create"/> would reject, rejected first as a 400 — same reasoning
/// as <see cref="Workspaces.Create.CreateWorkspaceValidator"/>. Uniqueness of the key is not
/// checked here: only the handler, backed by the index, can answer that.
/// </summary>
public sealed class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(Project.NameMaxLength);

        RuleFor(command => command.Key)
            .NotEmpty()
            .Matches("^[A-Z][A-Z0-9]{1,9}$")
            .WithMessage("Key must start with an uppercase letter and contain 2 to 10 uppercase letters or numbers.");

        RuleFor(command => command.Description).MaximumLength(Project.DescriptionMaxLength);

        RuleFor(command => command.Color).MaximumLength(Project.ColorMaxLength);

        RuleFor(command => command.Icon).MaximumLength(Project.IconMaxLength);
    }
}

using DevHub.Domain.Projects;
using FluentValidation;

namespace DevHub.Application.Projects.Update;

/// <summary>Rules apply only to the fields that were sent; an absent field is not validated. The
/// key itself is not validated here — it is rejected outright by the handler.</summary>
public sealed class UpdateProjectValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectValidator()
    {
        When(command => command.Name.HasValue, () =>
            RuleFor(command => command.Name.Value)
                .NotEmpty()
                .MaximumLength(Project.NameMaxLength)
                .OverridePropertyName(nameof(UpdateProjectCommand.Name)));

        When(command => command.Description.HasValue, () =>
            RuleFor(command => command.Description.Value)
                .MaximumLength(Project.DescriptionMaxLength)
                .OverridePropertyName(nameof(UpdateProjectCommand.Description)));

        When(command => command.Color.HasValue, () =>
            RuleFor(command => command.Color.Value)
                .MaximumLength(Project.ColorMaxLength)
                .OverridePropertyName(nameof(UpdateProjectCommand.Color)));

        When(command => command.Icon.HasValue, () =>
            RuleFor(command => command.Icon.Value)
                .MaximumLength(Project.IconMaxLength)
                .OverridePropertyName(nameof(UpdateProjectCommand.Icon)));
    }
}

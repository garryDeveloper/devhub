using FluentValidation;

namespace DevHub.Application.Users.Me;

/// <summary>Rules apply only to the fields that were sent; an absent field is not validated.</summary>
public sealed class UpdateMeValidator : AbstractValidator<UpdateMeCommand>
{
    public UpdateMeValidator()
    {
        When(command => command.DisplayName.HasValue, () =>
            RuleFor(command => command.DisplayName.Value)
                .ValidDisplayName()
                // Otherwise the error key would be "DisplayName.Value", which no client field has.
                .OverridePropertyName(nameof(UpdateMeCommand.DisplayName)));
    }
}

using FluentValidation;

namespace DevHub.Application.Users;

/// <summary>
/// The one definition of a valid display name, shared by registration (DEVHUB-014) and
/// <c>PATCH /api/me</c> (DEVHUB-019) so the two can never disagree.
/// </summary>
public static class DisplayNameRules
{
    /// <summary>Matches the <c>varchar(100)</c> column (database-schema.md §2).</summary>
    public const int MaximumLength = 100;

    /// <summary>1–100 characters after trimming. Handlers store the trimmed value.</summary>
    public static IRuleBuilderOptions<T, string?> ValidDisplayName<T>(this IRuleBuilderInitial<T, string?> rule) =>
        rule
            .Cascade(CascadeMode.Stop)
            // NotEmpty() alone accepts "   ": the handler trims, so that would store "".
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Display name is required.")
            // Measured after trimming, because the trimmed value is what gets stored.
            .Must(name => name!.Trim().Length <= MaximumLength)
                .WithMessage($"Display name must be {MaximumLength} characters or fewer.");
}

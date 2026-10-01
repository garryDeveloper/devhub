using DevHub.Domain.Workspaces;
using FluentValidation;

namespace DevHub.Application.Workspaces;

/// <summary>
/// The one definition of a valid workspace name, shared by create and rename so the two can
/// never disagree. Mirrors <see cref="Workspace"/>'s own check: the domain would throw on the
/// same input, but a <c>DomainException</c> is a 500 today, and a long name is a 400.
/// </summary>
public static class WorkspaceNameRules
{
    /// <summary>1–80 characters after trimming. The aggregate stores the trimmed value.</summary>
    public static IRuleBuilderOptions<T, string?> ValidWorkspaceName<T>(this IRuleBuilderInitial<T, string?> rule) =>
        rule
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Name is required.")
            .Must(name => name!.Trim().Length <= Workspace.NameMaxLength)
                .WithMessage($"Name must be {Workspace.NameMaxLength} characters or fewer.");
}

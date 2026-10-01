using DevHub.Domain.Workspaces;
using FluentValidation;

namespace DevHub.Application.Workspaces.Create;

/// <summary>
/// Everything <see cref="Workspace.Create"/> would reject, rejected first as a 400 — through the
/// domain's own <see cref="WorkspaceSlug"/> rules, so validator and aggregate cannot drift apart.
/// Uniqueness is not checked here: only the handler, backed by the index, can answer that.
/// </summary>
public sealed class CreateWorkspaceValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public CreateWorkspaceValidator()
    {
        RuleFor(command => command.Name).ValidWorkspaceName();

        When(command => command.Slug is not null, () =>
            RuleFor(command => command.Slug)
                .Must(WorkspaceSlug.IsValid)
                .WithMessage(
                    $"Slug must be {WorkspaceSlug.MinLength}–{WorkspaceSlug.MaxLength} characters of lowercase "
                    + "letters, digits and single hyphens between them."));

        // A name such as "日本" or "🚀🚀" is valid, but leaves nothing to build a slug from. The
        // error goes on "slug", because supplying one is how the client fixes it. Only checked
        // when the name itself passed, so the client does not get two errors for one problem.
        When(command => command.Slug is null && !string.IsNullOrWhiteSpace(command.Name), () =>
            RuleFor(command => command.Slug)
                .Must((command, _) => WorkspaceSlug.TryFromName(command.Name!.Trim(), out var _))
                .WithMessage("A slug cannot be derived from this name. Supply one."));
    }
}

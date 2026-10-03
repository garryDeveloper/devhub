using DevHub.Domain.Workspaces;
using FluentValidation;

namespace DevHub.Application.Workspaces;

/// <summary>
/// The one definition of a valid role on the wire, shared by every command that accepts one
/// (DEVHUB-025), so "add" and "change role" can never disagree on what a client may send.
/// </summary>
public static class WorkspaceRoleRules
{
    /// <summary>Must be exactly <c>"Owner"</c> or <c>"Member"</c> — the enum's own casing, not a
    /// case-insensitive match, so the client sends back exactly what it was shown.</summary>
    public static IRuleBuilderOptions<T, string?> ValidWorkspaceRole<T>(this IRuleBuilderInitial<T, string?> rule) =>
        rule
            .Cascade(CascadeMode.Stop)
            .Must(role => !string.IsNullOrWhiteSpace(role)).WithMessage("Role is required.")
            // Enum.TryParse alone would also accept "0"/"1" (the underlying int) and would match
            // "owner" case-insensitively by default on some runtimes — an exact name lookup is
            // the only check that is both strict and unambiguous.
            .Must(role => Enum.GetNames<WorkspaceRole>().Contains(role, StringComparer.Ordinal))
                .WithMessage("Role must be 'Owner' or 'Member'.");
}

using DevHub.Application.Common;
using DevHub.Domain.Workspaces;

namespace DevHub.Application.Workspaces.Access;

/// <summary>
/// Turns a resolved <see cref="WorkspaceAccess"/> into a decision. Scope is always checked before
/// role, so a non-member gets 404 and only a member can ever see a 403:
/// <code>
/// var access = (await accessService.ForWorkspaceAsync(id, ct)).RequireOwner(WorkspaceErrors.NotFound);
/// if (access.IsFailure) return access.Error!;
/// </code>
/// </summary>
/// <remarks>
/// Returns a <see cref="Result{TValue}"/> rather than throwing: "you may not" is an expected
/// outcome, and expected outcomes are returned in this codebase (see <see cref="Result"/>).
/// </remarks>
public static class WorkspaceAccessExtensions
{
    /// <param name="notFound">
    /// The resource's own 404 (<c>workspaces.not_found</c>, later <c>issues.not_found</c>…): the
    /// caller is told the thing they asked for does not exist, never that access was refused.
    /// </param>
    public static Result<WorkspaceAccess> RequireMember(this WorkspaceAccess? access, Error notFound)
    {
        ArgumentNullException.ThrowIfNull(notFound);

        return access is null ? notFound : access;
    }

    /// <inheritdoc cref="RequireMember" path="/param"/>
    public static Result<WorkspaceAccess> RequireOwner(this WorkspaceAccess? access, Error notFound)
    {
        ArgumentNullException.ThrowIfNull(notFound);

        if (access is null)
        {
            return notFound;
        }

        return access.Role == WorkspaceRole.Owner ? access : WorkspaceErrors.OwnerRequired;
    }
}

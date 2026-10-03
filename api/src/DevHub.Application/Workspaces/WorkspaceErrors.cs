using DevHub.Application.Common;

namespace DevHub.Application.Workspaces;

public static class WorkspaceErrors
{
    /// <summary>
    /// The workspace does not exist, or the caller is not a member. One error for both: answering
    /// 403 to a non-member would confirm to a stranger that the id is real (api-conventions.md §3).
    /// </summary>
    public static readonly Error NotFound =
        Error.NotFound("workspaces.not_found", "The workspace was not found.");

    /// <summary>
    /// The caller is a member — so the workspace's existence is no secret to them — but the
    /// operation needs the <c>Owner</c> role.
    /// </summary>
    public static readonly Error OwnerRequired =
        Error.Forbidden("workspaces.owner_required", "Only a workspace owner can do this.");

    public static readonly Error SlugTaken =
        Error.Conflict("workspaces.slug_taken", "A workspace with this slug already exists.");

    /// <summary>No DevHub account has this email. "Add by email" leaks whether an email is
    /// registered, which is acceptable only because this endpoint is authenticated and
    /// owner-only (DEVHUB-025 technical notes).</summary>
    public static readonly Error UserNotFound =
        Error.NotFound("workspaces.user_not_found", "No DevHub account with that email.");

    public static readonly Error AlreadyMember =
        Error.Conflict("workspaces.already_member", "This user is already a member of the workspace.");

    /// <summary>The member id does not belong to this workspace. Safe to say so (unlike
    /// <see cref="NotFound"/>): the caller already knows the workspace exists.</summary>
    public static readonly Error MemberNotFound =
        Error.NotFound("workspaces.member_not_found", "The member was not found.");

    public static readonly Error LastOwner =
        Error.UnprocessableEntity("workspaces.last_owner", "A workspace must keep at least one owner.");
}

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
}

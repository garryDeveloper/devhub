namespace DevHub.Domain.Workspaces;

/// <summary>
/// What a member may do inside a workspace. MVP roles only (domain-model.md): <c>Admin</c> and
/// <c>Viewer</c> are post-MVP, and adding them now would mean enforcing rules nobody has
/// specified yet.
/// </summary>
/// <remarks>
/// Stored as text (<c>'Owner'</c>, <c>'Member'</c>) with a CHECK constraint, not as an integer:
/// reordering or inserting a value here must never silently re-map rows already in the table.
/// </remarks>
public enum WorkspaceRole
{
    Owner,
    Member,
}

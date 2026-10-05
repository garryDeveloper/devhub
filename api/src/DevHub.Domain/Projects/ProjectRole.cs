namespace DevHub.Domain.Projects;

/// <summary>
/// What a member may do inside a project. MVP has exactly one value — per-project roles are
/// post-MVP (domain-model.md, DEVHUB-032) — kept as an enum anyway so a future role slots in
/// without reshaping <c>project_members</c>, the same way <see cref="Workspaces.WorkspaceRole"/>
/// does for workspaces.
/// </summary>
/// <remarks>
/// Stored as text (<c>'Member'</c>) with a CHECK constraint, not as an integer — see
/// <see cref="Workspaces.WorkspaceRole"/> for why.
/// </remarks>
public enum ProjectRole
{
    Member,
}

namespace DevHub.Application.Workspaces.Contracts;

/// <summary>
/// A workspace as the caller sees it (api-endpoints.md §2): returned by every
/// <c>/api/workspaces</c> endpoint, single or listed.
/// </summary>
/// <remarks>
/// Not the same type as <c>Users.Contracts.WorkspaceSummaryDto</c> on purpose: <c>GET /api/me</c>
/// carries only what a workspace switcher needs, and keeping the two contracts separate means a
/// field added here for a workspace screen does not silently grow every client's <c>/me</c>.
/// <c>/me</c> is built from the same query, so the two never disagree on membership.
/// </remarks>
/// <param name="Role">The <b>caller's</b> role in this workspace: <c>Owner</c> or <c>Member</c>.</param>
public sealed record WorkspaceDto(
    Guid Id,
    string Name,
    string Slug,
    string Role,
    int MemberCount,
    DateTimeOffset CreatedAt);

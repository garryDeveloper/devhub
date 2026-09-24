namespace DevHub.Application.Users.Contracts;

/// <summary>
/// What <c>GET /api/me</c> returns (api-endpoints.md §1): the caller's profile plus the workspaces
/// they belong to — everything a client needs to render its shell after a cold start.
/// </summary>
/// <param name="Workspaces">
/// Always empty until workspaces exist: DEVHUB-024 fills it. The shape is fixed now so the web
/// and mobile clients (DEVHUB-020/022) consume the final contract from day one.
/// </param>
public sealed record MeDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    IReadOnlyList<WorkspaceSummaryDto> Workspaces);

/// <param name="Role"><c>Owner</c> or <c>Member</c> (auth-spec.md §5).</param>
public sealed record WorkspaceSummaryDto(Guid Id, string Name, string Slug, string Role);

namespace DevHub.Application.Projects.Contracts;

/// <summary>
/// A project as it appears in <c>GET /api/workspaces/{workspaceId}/projects</c> (api-endpoints.md
/// §3): one row per project, no members loaded.
/// </summary>
/// <param name="OpenIssueCount">
/// Hardcoded to 0 until EPIC 5 adds <c>Issue</c> (DEVHUB-031 technical notes: this must become a
/// subquery, never a loaded collection, to avoid an N+1 across the list).
/// </param>
public sealed record ProjectSummaryDto(
    Guid Id,
    string Name,
    string Key,
    string? Description,
    string? Color,
    string? Icon,
    int OpenIssueCount,
    DateTimeOffset? ArchivedAt,
    DateTimeOffset CreatedAt);

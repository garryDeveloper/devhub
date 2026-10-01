namespace DevHub.Application.Workspaces.Update;

/// <summary>
/// <c>PATCH /api/workspaces/{workspaceId}</c>. The name is the only editable field — the slug is
/// immutable — so it is required rather than <c>Optional&lt;T&gt;</c>: a PATCH without it would
/// have nothing to do.
/// </summary>
/// <param name="WorkspaceId">From the route, never from the body.</param>
public sealed record UpdateWorkspaceCommand(Guid WorkspaceId, string? Name);

using DevHub.Application.Common;

namespace DevHub.Application.Projects.Update;

/// <summary>
/// <c>PATCH /api/projects/{projectId}</c>. Each field is <see cref="Optional{T}"/>: absent leaves
/// it alone, present sets it — same mechanism as <c>UpdateMeCommand</c>.
/// </summary>
/// <param name="ProjectId">From the route, never from the body.</param>
/// <param name="Key">
/// Present-and-anything is a 422 (DEVHUB-031 acceptance criteria): the key is immutable, and a
/// client that tries deserves <see cref="ProjectErrors.KeyIsImmutable"/>, not a silent no-op.
/// </param>
public sealed record UpdateProjectCommand(
    Guid ProjectId,
    Optional<string> Name,
    Optional<string?> Description,
    Optional<string?> Color,
    Optional<string?> Icon,
    Optional<string?> Key);

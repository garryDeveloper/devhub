namespace DevHub.Application.Common;

/// <summary>
/// The database refused a write because a unique index already holds the value.
/// </summary>
/// <remarks>
/// Defined here so a handler can react to it without referencing EF Core or Npgsql, which the
/// architecture tests forbid in DevHub.Application. Infrastructure translates the provider's
/// exception (PostgreSQL SQLSTATE 23505) into this one.
/// <para>
/// This is an exception rather than a <see cref="Result"/> because the handler cannot know in
/// advance that it will happen: a pre-check that says "free" can be wrong a millisecond later
/// under a concurrent request. The index is the only thing that is always right.
/// </para>
/// </remarks>
public sealed class UniqueConstraintViolationException(string? constraintName, Exception innerException)
    : Exception($"Unique constraint '{constraintName}' was violated.", innerException)
{
    /// <summary>The index name, e.g. <c>ix_users_email</c>, when the provider reports it.</summary>
    public string? ConstraintName { get; } = constraintName;
}
